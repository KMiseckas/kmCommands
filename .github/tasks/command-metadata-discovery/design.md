# Command Metadata / Discovery API

## Status

Draft

## Summary

Add a read-only discovery API to `CommandSystem` that exposes the names and parameter signatures of registered commands. This enables Unity UI layers and tooling to populate autocomplete lists, display parameter hints, and take stable snapshots for multi-frame UI use — without introducing any new registration or execution behavior.

The design adds three public methods to `CommandSystem`, one new public type (`CommandMetadataSnapshot`), and two new internal methods on `CommandRegistry`. No existing public API is modified.

## Requirements Input

- Source: `.github/tasks/command-metadata-discovery/requirements.md`
- Key requirements carried into design:
  - Req 1: Expose all registered command names as a read-only collection.
  - Req 2: Expose parameter descriptors for a specific command by name (case-insensitive).
  - Req 3: Structured, no-throw return when a command name is not found.
  - Req 4/5: Read-only snapshot of full registry state at moment of call; safe to store across registrations.
  - Req 6: All methods return empty/failure state before `Initialize()` or after `Shutdown()` — no throw.
  - Req 7: No `UnityEngine` dependency.
  - Req 8: Allocations bounded by registry size; nothing in execution hot paths.
  - Req 9: Source header on all new `src/` files.
  - Req 10: IL2CPP/AOT-safe patterns throughout.

## Scope Notes

- **In scope:** `GetCommandNames()`, `TryGetCommandParameters()`, `GetSnapshot()` on `CommandSystem`; `CommandMetadataSnapshot` type; `CommandRegistry` iteration support.
- **Out of scope:** Help/description text (separate feature), aliases, live/reactive observation, filtering by argument count or type, any Unity UI or input handling.

---

## Architecture Overview

The discovery API sits entirely in the public `kmCommands` layer. It reads data that already exists inside `CommandRegistry`; it does not add any new runtime state or execution paths.

```
Consumer (Unity UI layer)
        │
        │  GetCommandNames()
        │  TryGetCommandParameters(name, out params)
        │  GetSnapshot()
        ▼
  CommandSystem   ──── delegates to ────►  CommandRegistry
                                              │  GetAllNames()
                                              │  BuildSnapshot()
                                              │  TryGetCommand()   (already exists)
                                              ▼
                                        CommandMetadataSnapshot   (public, immutable)
```

`CommandSystem` performs initialization-guard checks and delegates all data retrieval to `CommandRegistry`. `CommandRegistry` owns the iteration logic and the snapshot builder. `CommandMetadataSnapshot` is a public sealed class with an internal constructor; consumers can freely store and read it but cannot construct or mutate it.

---

## Data Flow / Control Flow

### `GetCommandNames()` flow

```
CommandSystem.GetCommandNames()
  → if (!IsInitialized) return Array.Empty<string>()
  → _registry.GetAllNames()
      → iterate _commands dictionary
      → copy Name (original casing) from each CommandDefinition into new string[]
      → sort array OrdinalIgnoreCase
      → return string[]
  ← returns string[]
```

### `TryGetCommandParameters(name, out parameters)` flow

```
CommandSystem.TryGetCommandParameters(name, out parameters)
  → if (!IsInitialized || string.IsNullOrEmpty(name)) { parameters = null; return false; }
  → _registry.TryGetCommand(name, out definition)
      (existing method — case-insensitive dict lookup)
  → if not found: { parameters = null; return false; }
  → parameters = definition.Parameters   // same array reference as stored in registry
  → return true
```

### `GetSnapshot()` flow

```
CommandSystem.GetSnapshot()
  → if (!IsInitialized) return CommandMetadataSnapshot.Empty
  → _registry.BuildSnapshot()
      → if Count == 0, return CommandMetadataSnapshot.Empty
      → allocate string[Count]
      → allocate Dictionary<string, CommandParameterInfo[]>(Count, OrdinalIgnoreCase)
      → foreach pair in _commands:
            names[i] = def.Name                                   // original casing
            paramsCopy = new CommandParameterInfo[def.Parameters.Length]   // structural copy
            Array.Copy(def.Parameters, paramsCopy, def.Parameters.Length)
            entries[def.Name] = paramsCopy
      → Array.Sort(names, StringComparer.OrdinalIgnoreCase)
      → return new CommandMetadataSnapshot(names, entries)
  ← returns CommandMetadataSnapshot
```

---

## Components and Responsibilities

### `CommandSystem` (modified)

- **Responsibility:** Guard initialization state; expose the three new public discovery methods; delegate to `_registry`.
- **Interactions:** Reads `IsInitialized`; calls `_registry.GetAllNames()`, `_registry.TryGetCommand()`, `_registry.BuildSnapshot()`.

### `CommandRegistry` (modified, internal)

- **Responsibility:** Provide iteration primitives for discovery. Add `GetAllNames()` and `BuildSnapshot()` internal methods alongside existing `TryGetCommand()` and `TryRegister()`.
- **Interactions:** Accessed by `CommandSystem` only. No new dependencies introduced.

### `CommandMetadataSnapshot` (new, public)

- **Responsibility:** Carry an immutable point-in-time copy of registry names and parameter signatures. Provide O(1) case-insensitive parameter lookup via `TryGetParameters()`.
- **Interactions:** Constructed only by `CommandRegistry.BuildSnapshot()` (internal constructor). Read by consumer (Unity UI layer).

---

## Dependency Evaluation

- **New dependencies:** None.
- **Rationale:** All required data already exists in `CommandRegistry`. Standard BCL types (`Dictionary`, `Array`, `string[]`) are sufficient and fully AOT-safe. No third-party package solves a problem here that warrants a dependency.

---

## API / Contract Sketch

### New public methods on `CommandSystem`

```csharp
/// <summary>
/// Returns the names of all currently registered commands.
/// Names are returned sorted by ordinal case-insensitive comparison for deterministic output.
/// </summary>
/// <returns>
/// A snapshot array of command names, or <see cref="Array.Empty{T}()"/> if the system is not
/// initialized or no commands are registered.
/// </returns>
public string[] GetCommandNames();

/// <summary>
/// Attempts to retrieve the parameter descriptors for the named command.
/// Lookup is case-insensitive.
/// </summary>
/// <param name="name">The command name to look up.</param>
/// <param name="parameters">
/// When this method returns <c>true</c>, the parameter descriptors for the command.
/// The returned array is the same instance stored in the registry — do not mutate it.
/// <c>null</c> when this method returns <c>false</c>.
/// </param>
/// <returns>
/// <c>true</c> if the command was found; <c>false</c> if the system is not initialized,
/// <paramref name="name"/> is null or empty, or no command with that name is registered.
/// </returns>
public bool TryGetCommandParameters(string name, out CommandParameterInfo[] parameters);

/// <summary>
/// Returns a read-only snapshot of the full registry state at this moment.
/// The snapshot is isolated: subsequent <see cref="Register"/> or <see cref="Scan"/> calls
/// do not affect an already-taken snapshot.
/// </summary>
/// <returns>
/// A <see cref="CommandMetadataSnapshot"/> capturing the current registry contents,
/// or <see cref="CommandMetadataSnapshot.Empty"/> if the system is not initialized.
/// </returns>
public CommandMetadataSnapshot GetSnapshot();
```

### New public type `CommandMetadataSnapshot`

```csharp
namespace kmCommands
{
    /// <summary>
    /// An immutable, point-in-time snapshot of the command registry's metadata.
    /// Obtained via <see cref="CommandSystem.GetSnapshot()"/>.
    /// </summary>
    public sealed class CommandMetadataSnapshot
    {
        /// <summary>
        /// All command names captured at snapshot time, sorted by ordinal case-insensitive order.
        /// </summary>
        public string[] CommandNames { get; }

        /// <summary>
        /// Attempts to retrieve the parameter descriptors for the named command.
        /// Lookup is case-insensitive.
        /// </summary>
        public bool TryGetParameters(string name, out CommandParameterInfo[] parameters);

        // Internal constructor — consumers cannot instantiate directly.
        internal CommandMetadataSnapshot(string[] names,
            Dictionary<string, CommandParameterInfo[]> entries);

        /// <summary>
        /// A reusable empty snapshot. Returned when the system is not initialized.
        /// </summary>
        internal static CommandMetadataSnapshot Empty { get; }
    }
}
```

`Empty` is `internal` — it is an implementation detail of `CommandSystem`'s guard logic, not part of the public API. Consumers who check `IsInitialized` before calling `GetSnapshot()` will never observe it.

### New internal methods on `CommandRegistry`

```csharp
/// Returns a new string[] containing all registered command names (original casing),
/// sorted OrdinalIgnoreCase. Returns Array.Empty<string>() if the registry is empty.
internal string[] GetAllNames();

/// Builds and returns a CommandMetadataSnapshot capturing the current registry state.
/// Returns CommandMetadataSnapshot.Empty if the registry is empty.
internal CommandMetadataSnapshot BuildSnapshot();
```

---

## Implementation Notes

### Snapshot isolation strategy

`BuildSnapshot()` performs a **structural copy**:

- Allocates a new `string[]` for names — ensures subsequent registrations do not affect `CommandNames`.
- Allocates a new `CommandParameterInfo[]` per command (shallow copy via `Array.Copy`) — ensures the snapshot's per-command array is not the same reference held by `CommandDefinition`. New registrations replace entire `CommandDefinition` entries; they do not modify existing ones. The copy is therefore sufficient for full snapshot isolation.
- Does **not** deep-copy `CommandParameterInfo` instances — they are `sealed class` with readonly properties and are effectively immutable. Sharing references between the snapshot and the live registry is safe.

### `TryGetCommandParameters` array aliasing

The array returned via `TryGetCommandParameters` is the **same instance** stored inside `CommandDefinition`. This is intentional (zero allocation per call). Callers must not mutate the returned array. This caveat must be documented in the XML doc comment.

### `CommandNames` sort order

All `GetAllNames()` and `BuildSnapshot()` results are sorted by `StringComparer.OrdinalIgnoreCase`. This gives consumers a deterministic, predictable ordering suitable for populating autocomplete lists without additional sorting on their side.

### `CommandMetadataSnapshot.Empty` singleton

Constructed once at type initialization time. All pre-init and post-shutdown paths return this same reference. `CommandNames` is `Array.Empty<string>()`. `TryGetParameters` always returns `false` on the empty instance.

### Guard pattern consistency

All three new `CommandSystem` methods follow the existing guard pattern:

```csharp
if (!IsInitialized) { /* return empty/false */ }
```

No exceptions are thrown. This mirrors `Execute()` returning `ExecutionResult.Fail(ExecutionError.NotInitialized, ...)` and `Register()` returning `RegistrationResult.Fail(RegistrationError.NotInitialized, ...)`. Unlike those methods, the discovery methods return simple values (arrays, bools, snapshot objects) rather than result structs — a result struct for discovery would add ceremony without benefit since the failure mode (not initialized, name not found) is fully captured by the return value shape.

---

## Code Examples

### Registry iteration in `GetAllNames()`

```csharp
internal string[] GetAllNames()
{
    int count = _commands.Count;
    if (count == 0)
        return Array.Empty<string>();

    string[] names = new string[count];
    int i = 0;
    foreach (KeyValuePair<string, CommandDefinition> pair in _commands)
    {
        names[i++] = pair.Value.Name; // original casing from CommandDefinition
    }
    Array.Sort(names, StringComparer.OrdinalIgnoreCase);
    return names;
}
```

### Registry snapshot build in `BuildSnapshot()`

```csharp
internal CommandMetadataSnapshot BuildSnapshot()
{
    int count = _commands.Count;
    if (count == 0)
        return CommandMetadataSnapshot.Empty;

    string[] names = new string[count];
    Dictionary<string, CommandParameterInfo[]> entries =
        new Dictionary<string, CommandParameterInfo[]>(count, StringComparer.OrdinalIgnoreCase);

    int i = 0;
    foreach (KeyValuePair<string, CommandDefinition> pair in _commands)
    {
        CommandDefinition def = pair.Value;
        names[i++] = def.Name;

        // Structural copy: new array, same immutable CommandParameterInfo refs
        CommandParameterInfo[] paramsCopy = new CommandParameterInfo[def.Parameters.Length];
        Array.Copy(def.Parameters, paramsCopy, def.Parameters.Length);
        entries[def.Name] = paramsCopy;
    }

    Array.Sort(names, StringComparer.OrdinalIgnoreCase);
    return new CommandMetadataSnapshot(names, entries);
}
```

### `CommandMetadataSnapshot.TryGetParameters()`

```csharp
public bool TryGetParameters(string name, out CommandParameterInfo[] parameters)
{
    if (string.IsNullOrEmpty(name))
    {
        parameters = null;
        return false;
    }
    return _entries.TryGetValue(name, out parameters);
}
```

### Consumer usage (Unity UI layer)

```csharp
// Populate autocomplete — called once after all Register()/Scan() complete
string[] names = commandSystem.GetCommandNames();

// Show parameter hints — called when user confirms a command name
if (commandSystem.TryGetCommandParameters(inputName, out CommandParameterInfo[] parms))
{
    for (int i = 0; i < parms.Length; i++)
        ShowHint(parms[i].Name, parms[i].Type);
}

// Stable multi-frame snapshot — taken once, referenced repeatedly
CommandMetadataSnapshot snapshot = commandSystem.GetSnapshot();
// ... later during UI render ...
if (snapshot.TryGetParameters(selectedCommand, out CommandParameterInfo[] p))
    RenderParameterPanel(p);
```

---

## Diagram

```mermaid
flowchart TD
    Consumer["Unity UI Layer"]

    subgraph CommandSystem
        GCN["GetCommandNames()"]
        TGP["TryGetCommandParameters(name, out params)"]
        GS["GetSnapshot()"]
        Guard["IsInitialized guard\n(returns empty/false if not init)"]
    end

    subgraph CommandRegistry["CommandRegistry (internal)"]
        GAN["GetAllNames()"]
        TC["TryGetCommand()"]
        BS["BuildSnapshot()"]
        Dict["_commands Dictionary\n(OrdinalIgnoreCase)"]
    end

    CMS["CommandMetadataSnapshot\n(public sealed class)"]
    Empty["CommandMetadataSnapshot.Empty\n(internal singleton)"]

    Consumer -->|GetCommandNames| GCN
    Consumer -->|TryGetCommandParameters| TGP
    Consumer -->|GetSnapshot| GS

    GCN --> Guard
    TGP --> Guard
    GS --> Guard

    Guard -->|not initialized| Empty
    Guard -->|initialized| GAN
    Guard -->|initialized| TC
    Guard -->|initialized| BS

    GAN --> Dict
    TC --> Dict
    BS --> Dict
    BS -->|new| CMS

    GCN -->|returns string[]| Consumer
    TGP -->|returns bool + CommandParameterInfo[]| Consumer
    GS -->|returns CommandMetadataSnapshot| Consumer
```

---

## Allocation Analysis

| Call                                         | Allocations                                                                   | Frequency                |
| -------------------------------------------- | ----------------------------------------------------------------------------- | ------------------------ |
| `GetCommandNames()`                          | One `string[n]`                                                               | UI prep only             |
| `TryGetCommandParameters()`                  | **Zero** (returns existing array ref)                                         | UI prep or per-keystroke |
| `GetSnapshot()`                              | One `string[n]` + one `Dictionary` + one `CommandParameterInfo[]` per command | UI prep only             |
| `CommandMetadataSnapshot.Empty`              | Zero (singleton)                                                              | Pre-init guard           |
| `CommandMetadataSnapshot.TryGetParameters()` | Zero (dictionary lookup)                                                      | Zero alloc               |

All allocations are **bounded by registry size** and **outside the execution hot path**. `Execute()` is unaffected. The most allocation-sensitive consumer pattern (`TryGetCommandParameters` per-keystroke) allocates nothing.

---

## IL2CPP / AOT Safety Notes

- `Dictionary<string, CommandParameterInfo[]>` — concrete generic type, fully AOT-safe.
- `Dictionary<string, CommandDefinition>` — already in use in `CommandRegistry`, confirmed AOT-safe.
- `Array.Sort(string[], StringComparer)` — no code generation; AOT-safe.
- `foreach (KeyValuePair<string, CommandDefinition>)` — concrete enumerator, AOT-safe.
- `Array.Copy` — AOT-safe BCL method.
- No `System.Linq`, no `Expression<T>`, no `Emit`, no `dynamic`, no unconstrained `MakeGenericType`.
- `CommandMetadataSnapshot` uses no generics that would require AOT code generation.

---

## Testing Strategy

**Test file:** `tests/kmCommands.Tests/CommandMetadataDiscoveryTests.cs` (new file, NUnit, `net8.0`)
**Fixture pattern:** Match existing test fixtures — `[SetUp]` creates `CommandSystem`, `[TearDown]` shuts down.

### `GetCommandNames` tests

| Test name                                                | Scenario                                                         |
| -------------------------------------------------------- | ---------------------------------------------------------------- |
| `GetCommandNames_BeforeInit_ReturnsEmptyArray`           | Called before `Initialize()` — returns empty, no throw.          |
| `GetCommandNames_InitNoCommands_ReturnsEmptyArray`       | Initialized, no registrations — returns empty array.             |
| `GetCommandNames_WithRegisteredCommands_ReturnsAllNames` | Two commands registered — both names in result.                  |
| `GetCommandNames_NamesAreSortedOrdinalIgnoreCase`        | Names returned in sorted order regardless of registration order. |
| `GetCommandNames_AfterShutdown_ReturnsEmptyArray`        | `Shutdown()` called — returns empty, no throw.                   |

### `TryGetCommandParameters` tests

| Test name                                                   | Scenario                                                  |
| ----------------------------------------------------------- | --------------------------------------------------------- |
| `TryGetCommandParameters_BeforeInit_ReturnsFalse`           | Not initialized — returns false, out is null.             |
| `TryGetCommandParameters_NullName_ReturnsFalse`             | Null name — returns false.                                |
| `TryGetCommandParameters_UnknownCommand_ReturnsFalse`       | Name not in registry — returns false, out is null.        |
| `TryGetCommandParameters_KnownCommand_ReturnsTrueAndParams` | Known command — returns true, out has correct parameters. |
| `TryGetCommandParameters_IsCaseInsensitive`                 | Query with different casing — still finds command.        |
| `TryGetCommandParameters_EmptyParams_ReturnsEmptyArray`     | Zero-param command — returns true, out is empty array.    |
| `TryGetCommandParameters_AfterShutdown_ReturnsFalse`        | After `Shutdown()` — returns false, no throw.             |

### `GetSnapshot` tests

| Test name                                                  | Scenario                                                                                                                    |
| ---------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `GetSnapshot_BeforeInit_ReturnsEmptySnapshot`              | Not initialized — returns snapshot with empty `CommandNames`.                                                               |
| `GetSnapshot_NoCommands_ReturnsEmptyCommandNames`          | Initialized, no registrations — `CommandNames` is empty.                                                                    |
| `GetSnapshot_CommandNames_ContainsAllRegisteredNames`      | Two commands registered — both names in `CommandNames`.                                                                     |
| `GetSnapshot_TryGetParameters_ReturnsCorrectParameters`    | Known command — `TryGetParameters` returns true + correct data.                                                             |
| `GetSnapshot_TryGetParameters_IsCaseInsensitive`           | Mixed-case name query — resolves correctly.                                                                                 |
| `GetSnapshot_TryGetParameters_UnknownCommand_ReturnsFalse` | Unknown name — returns false.                                                                                               |
| `GetSnapshot_IsIsolatedFromSubsequentRegistrations`        | Register command A, take snapshot, register command B — snapshot does not contain B.                                        |
| `GetSnapshot_ParameterArray_IsStructurallyCopied`          | Verifies snapshot's parameter array is not the same reference as used by a subsequently registered entry (integrity check). |
| `GetSnapshot_AfterShutdown_ReturnsEmptySnapshot`           | After `Shutdown()` — returns empty snapshot, no throw.                                                                      |

---

## Risks and Tradeoffs

| Risk / Tradeoff                                           | Assessment                                                                                                                                                          |
| --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `TryGetCommandParameters` returns live array reference    | Consumer can mutate the registry's internal parameter array. Documented caveat. Acceptable given Unity client-controlled environment and zero-allocation priority.  |
| Dictionary in snapshot adds allocation per snapshot build | One `Dictionary` allocation + N `CommandParameterInfo[]` copies per snapshot. Acceptable: snapshot is a one-time-per-UI-session operation.                          |
| `GetCommandNames()` allocates per call                    | One array per call. If called per-keystroke, consumer should use `GetSnapshot()` instead. Document this guidance.                                                   |
| `CommandMetadataSnapshot` is a class, not a struct        | Suitable — snapshot is a non-trivial multi-field object intended for storage, not repeated value passing. Using a struct would require copying on every assignment. |

---

## Open Questions

All design-time open questions from `requirements.md` are resolved:

1. **Snapshot type** → `CommandMetadataSnapshot` (dedicated named sealed class with internal constructor). More extensible than `IReadOnlyDictionary` for future features (help text, aliases).
2. **Parameter collection type** → `CommandParameterInfo[]` (plain array). Consistent with existing API; AOT-safe; zero allocation on `TryGetCommandParameters`.
3. **Pre-initialize return shape** → `Array.Empty<string>()`, `false`, `CommandMetadataSnapshot.Empty`. No throw. Consistent with project convention.
4. **Snapshot freshness contract** → Documentation only. No version field or timestamp. Snapshot captures state at moment of call; subsequent registrations have no effect on an already-taken snapshot.

No unresolved questions remain.

---

## Task Planning Handoff

### Suggested implementation slices

1. **`CommandMetadataSnapshot` type** — New file `src/CommandMetadataSnapshot.cs`. Includes `CommandNames`, `TryGetParameters()`, internal constructor, `Empty` singleton.
2. **`CommandRegistry` additions** — Add `GetAllNames()` and `BuildSnapshot()` internal methods to `src/Core/CommandRegistry.cs`.
3. **`CommandSystem` discovery methods** — Add `GetCommandNames()`, `TryGetCommandParameters()`, `GetSnapshot()` to `src/CommandSystem.cs`.
4. **Tests** — New test file `tests/kmCommands.Tests/CommandMetadataDiscoveryTests.cs` covering all cases above.

### Coupling notes for task splitting

- Slice 1 can be implemented independently (no dependency on existing types beyond `CommandParameterInfo`).
- Slice 2 requires Slice 1 (`BuildSnapshot()` returns `CommandMetadataSnapshot`).
- Slice 3 requires Slices 1 and 2.
- Slice 4 (tests) can be written alongside Slice 3 or after.
- No changes to `ExecutionHandler`, `AttributeScanner`, `ArgumentConverter`, or any `Results/` file.

### Areas to validate after full integration

- Snapshot isolation under rapid consecutive `Register()` → `GetSnapshot()` sequences.
- Case-insensitive lookup consistency between `TryGetCommandParameters()` and `GetSnapshot().TryGetParameters()`.
- Empty-command-count edge paths (both methods return consistently empty results).

---

## Review Contract

### Critical behaviors to verify

- [ ] `GetCommandNames()` returns empty (not null) array before `Initialize()` and after `Shutdown()`.
- [ ] `TryGetCommandParameters()` returns `false` and `null` out-param before `Initialize()` and after `Shutdown()`.
- [ ] `GetSnapshot()` returns `CommandMetadataSnapshot.Empty` before `Initialize()` and after `Shutdown()`.
- [ ] `TryGetCommandParameters()` and `GetSnapshot().TryGetParameters()` are both case-insensitive.
- [ ] Snapshot taken before a `Register()` call does not reflect commands registered after it.
- [ ] `CommandMetadataSnapshot.CommandNames` contains exactly the commands registered at snapshot time, no more, no fewer.
- [ ] `GetCommandNames()` and `CommandMetadataSnapshot.CommandNames` are sorted OrdinalIgnoreCase.
- [ ] `TryGetCommandParameters()` returns the correct `CommandParameterInfo[]` for a registered command.
- [ ] No method in the discovery API throws under any documented input condition.

### Design invariants that must hold

- `CommandMetadataSnapshot` has no public constructor. Only `CommandRegistry.BuildSnapshot()` may produce non-empty instances.
- `CommandMetadataSnapshot.Empty` is a singleton; its `CommandNames` is `Array.Empty<string>()`.
- `CommandRegistry.GetAllNames()` and `CommandRegistry.BuildSnapshot()` produce sorted output.
- No new `UnityEngine` using directive appears in any `src/` file.
- All new `src/` files carry the required copyright header.

### Required test evidence for acceptance

- Test fixture `CommandMetadataDiscoveryTests` (or equivalent) must pass in full.
- Existing 82 tests must continue to pass without modification.
- At minimum, the following test cases must be present and green: pre-init no-throw for all 3 methods; post-shutdown no-throw for all 3 methods; snapshot isolation test; case-insensitive lookup test; correct parameters returned for a known command.

### Known acceptable deviations

- `TryGetCommandParameters()` returns a reference to the live registry's parameter array (not a copy). This is documented and intentional.
- `CommandMetadataSnapshot.Empty` is internal — not part of the public contract. Callers should check `IsInitialized` before calling `GetSnapshot()` if they need to distinguish "not initialized" from "no commands registered".

### Blocking conditions for final approval

- Any discovery method throws an exception under any documented input path.
- A second `Register()` call mutates a previously taken snapshot.
- IL2CPP-incompatible patterns introduced in new source files.
- Missing copyright header on any new `src/` file.
- New `UnityEngine` reference in `src/`.
