# Architecture

## Overview

kmCommands is a pure C# command-system library targeting `netstandard2.0`. It is designed to be consumed by a Unity integration layer without depending on `UnityEngine` itself.

The library exposes a single public entry point (`CommandSystem`) that the consumer instantiates and controls. All internal components are hidden behind the `kmCommands.Core` namespace.

```
┌─────────────────────────────────────────────────────┐
│                   Consumer Code                     │
│  (Unity layer or any .NET host)                     │
└─────────────┬───────────────────────────────────────┘
              │ public API  (kmCommands namespace)
              ▼
┌─────────────────────────────────────────────────────┐
│                  CommandSystem                      │
│  Initialize() / Initialize(historyCapacity)         │
│  Initialize(types, options, capacity)               │
│  Initialize(assemblies, options, capacity)          │
│  Initialize(types, assemblies, options, capacity)   │
│  Shutdown()                                         │
│  Register(name, parameters, callback)               │
│  Register(name, parameters, callback, description)  │
│  Execute(commandName, args)                         │
│  Scan(type, options) / Scan(assembly, options)      │
│  GetHistory() / HistoryCount / ClearHistory()       │
└──────┬──────────────┬───────────────┬───────────────┬───────────────┬───────────────┘
       │              │               │               │               │  (kmCommands.Core namespace)
       ▼              ▼               ▼               ▼               ▼
┌────────────┐ ┌──────────────┐ ┌────────────────┐ ┌──────────────────┐ ┌──────────────────────┐
│  Command   │ │  Argument    │ │  Execution     │ │  Attribute       │ │  Command History     │
│  Registry  │ │  Converter   │ │  Handler       │ │  Scanner         │ │  Buffer              │
└────────────┘ └──────────────┘ └────────────────┘ └──────────────────┘ └──────────────────────┘
```

## Namespaces

| Namespace         | Contents                                                                                                                                                                                                                                                  | Visibility |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- |
| `kmCommands`      | `CommandSystem`, `CommandAttribute`, `ScanOptions`, `CommandCallback`, `CommandParameterInfo`, `CommandMetadataSnapshot`, `CommandHistoryEntry`, `TypeConverterDelegate`, `RegistrationResult`, `ExecutionResult`, `ScanResult`, `ScanEntry`, error enums | Public     |
| `kmCommands.Core` | `CommandRegistry`, `ArgumentConverter`, `ExecutionHandler`, `AttributeScanner`, `CommandDefinition`, `CommandHistoryBuffer`                                                                                                                               | Internal   |

## Components

### CommandSystem

The public entry point. The consumer creates an instance, calls `Initialize()`, then uses `Register`, `Execute`, and `RegisterConverter`. `Shutdown()` clears all state and allows re-initialization.

- **Lifecycle:** Idempotent `Initialize()` / `Initialize(int historyCapacity)` and `Shutdown()`. Calling either method multiple times or out of order is safe.
- **Scan-at-init overloads:** Three additional `Initialize` overloads accept scan targets (`Type[]`, `Assembly[]`, or both) and a `ScanOptions` value, run attribute-based scanning during initialization, and return an aggregated `ScanResult`. If already initialized, these overloads return `ScanResult.AlreadyInitialized()` immediately without re-scanning. `ScanResult.IsAlreadyInitialized` distinguishes this no-op path from a zero-entry scan result.
- **Owns:** `CommandRegistry`, `ArgumentConverter`, `ExecutionHandler`, `CommandHistoryBuffer`. All are nulled on `Shutdown()`.
- **Custom converters:** `RegisterConverter(Type, TypeConverterDelegate)` buffers converters (pre-init) or applies them directly (post-init). `_pendingConverters` is a `readonly` field that survives `Initialize()` and `Shutdown()` cycles — only `.Clear()` is called on it.
- **History:** `Initialize()` creates a `CommandHistoryBuffer` using `DefaultHistoryCapacity` (64). `Initialize(int)` and the scan-at-init overloads accept an explicit capacity (clamped to ≥ 1). `Execute()` records successful executions to the buffer. `GetHistory()`, `HistoryCount`, and `ClearHistory()` expose buffer state. All three return safe empty results (or zero) before initialization.
- **Thread safety:** Not thread-safe. All calls must originate from the same thread (main thread in Unity).

### CommandRegistry

An `internal sealed class` backed by a `Dictionary<string, CommandDefinition>` with `StringComparer.OrdinalIgnoreCase`. Command names are stored with their original casing but matched case-insensitively.

In addition to registration and lookup, `CommandRegistry` provides two internal discovery methods:

- `GetAllNames()` — returns a new `string[]` of all registered names, sorted `OrdinalIgnoreCase`.
- `BuildSnapshot()` — returns a `CommandMetadataSnapshot` with a structural copy of all names and parameter arrays.

### CommandMetadataSnapshot

A `public sealed class` with an internal constructor. Carries an immutable, point-in-time copy of the command registry's metadata. Obtained via `CommandSystem.GetSnapshot()`.

- `CommandNames` — sorted `string[]` of all command names at snapshot time.
- `TryGetParameters(name, out parameters)` — O(1) case-insensitive parameter lookup from the captured copy.
- `TryGetDescription(name, out description)` — O(1) case-insensitive description lookup. Returns `false`/`null` when the command was registered without a description or is not in the snapshot.
- `Empty` — internal singleton returned by guard paths (pre-init, post-shutdown).

The snapshot is isolated from subsequent registrations: `BuildSnapshot()` performs a structural copy of each `CommandParameterInfo[]` (via `Array.Copy`), ensuring that new registrations do not affect any already-taken snapshot.

### ArgumentConverter

An `internal sealed class` that converts string tokens to .NET types using a `Dictionary<Type, TryConvertFunc>`. Ships with built-in converters for `int`, `float`, `bool`, and `string`. All numeric parsing uses `CultureInfo.InvariantCulture`.

The converter registry is extensible: `AddConverter(Type, TryConvertFunc)` inserts or replaces an entry in the dictionary (last-write wins). `CommandSystem.RegisterConverter` drives this extension — pre-`Initialize()` registrations are buffered in `_pendingConverters` on `CommandSystem` and flushed into `ArgumentConverter` during `Initialize()`; post-`Initialize()` registrations call `AddConverter` directly. `Shutdown()` clears `_pendingConverters` (and nulls the `ArgumentConverter` instance), so the converter set reverts to built-ins only on the next `Initialize()` cycle.

Supported built-in types:

| Type     | Notes                                                  |
| -------- | ------------------------------------------------------ |
| `int`    | Parsed with `NumberStyles.Integer`, `InvariantCulture` |
| `float`  | Parsed with `NumberStyles.Float`, `InvariantCulture`   |
| `bool`   | Strict: `"True"` / `"False"` only (case-insensitive)   |
| `string` | Always succeeds; returns the token as-is               |

### ExecutionHandler

An `internal sealed class` that orchestrates the full execution path:

1. Validate command name is not null/empty.
2. Look up the command in the registry.
3. Validate argument count matches the command's parameter count.
4. Convert each string argument to the declared parameter type.
5. Invoke the callback with the converted `object[]`.
6. Catch any exception thrown by the callback and wrap it in the result.

### CommandDefinition

An `internal sealed class` that stores a command's name, parameter signature (`CommandParameterInfo[]`), callback delegate, and optional description string. Created at registration time and stored in the registry.

### AttributeScanner

An `internal sealed class` that implements attribute-based discovery and registration. Constructed in `CommandSystem.Initialize()` alongside `ExecutionHandler`.

- **Responsibilities:** Discovers `[Command]`-decorated methods via reflection, validates them, builds AOT-safe `CommandCallback` delegates, and registers commands into `CommandRegistry`. Returns a `ScanResult` per scan call.
- **`ScanType(Type, ScanOptions)`:** Inspects all public/non-public static and instance methods declared directly on the type (using `BindingFlags.DeclaredOnly`). For each method with a `[Command]` attribute, runs the processing pipeline.
- **`ScanAssembly(Assembly, ScanOptions)`:** Enumerates all types in the assembly via `assembly.GetTypes()`, calls `ScanType` per type, and merges all entries into a single `ScanResult`. Handles `ReflectionTypeLoadException` for partially-loaded assemblies.
- **Processing pipeline per method:**
  1. `IsDevOnly && !DevMode` → silently skip (no entry produced).
  2. Not static → `ScanEntry` with `RegistrationError.InvalidMethod`.
  3. Any parameter type unsupported → `ScanEntry` with `RegistrationError.UnsupportedParameterType` (no partial registration).
  4. Build AOT-safe callback via `Delegate.CreateDelegate`.
  5. `TryRegister` fails (duplicate) → `ScanEntry` with `RegistrationError.DuplicateCommandName`.
  6. Success → `ScanEntry` with `RegistrationResult.Ok()`.
- **Delegate strategy:** Uses `Delegate.CreateDelegate` to create a strongly-typed `Action` or `Action<T1,...>` intermediate delegate at scan time. The zero-parameter path calls the typed `Action` directly (no `DynamicInvoke`). All other paths wrap with `DynamicInvoke` on the pre-bound typed delegate — AOT-safe on Unity 2021+ IL2CPP.
- **Parameter limit:** `GetActionDelegateType` supports 1–4 parameters via a `switch`. Commands with 5+ parameters throw `NotSupportedException` at scan time.
- **Naming conflicts across types:** First-registered-wins. Duplicate names produce a `DuplicateCommandName` failure entry for the later registration.

## Data Types

### CommandCallback

```csharp
public delegate void CommandCallback(object[] args);
```

Arguments are delivered as a pre-converted `object[]`. Each element is typed per the command's parameter signature — cast to the expected type before use.

### CommandParameterInfo

```csharp
public sealed class CommandParameterInfo
{
    public string Name { get; }
    public Type Type { get; }
}
```

Describes a single command parameter. `Name` is used in error messages. `Type` must be a type supported by the `ArgumentConverter`.

### Result Types

Both result types are `readonly struct` — no heap allocation.

```
RegistrationResult { bool Success, RegistrationError Error, string ErrorMessage }
ExecutionResult    { bool Success, ExecutionError Error, string ErrorMessage, Exception Exception }
```

`Exception` on `ExecutionResult` is only non-null when `Error == CallbackThrewException`.

## Execution Flow

```
CommandSystem.Execute("set_health", ["player1", "100"])
  │
  ├─ [gate] IsInitialized? No → ExecutionResult.Fail(NotInitialized)
  │
  └─ ExecutionHandler.Execute(...)
       ├─ name null/empty? → Fail(NullOrEmptyCommandName)
       ├─ registry lookup failed? → Fail(CommandNotFound)
       ├─ arg count mismatch? → Fail(ArgumentCountMismatch)
       ├─ per argument: TryConvert failed? → Fail(ArgumentConversionFailed)
       ├─ callback throws? → Fail(CallbackThrewException, ex)
       └─ → ExecutionResult.Ok()
```

## Registration Flow

```
CommandSystem.Register("set_health", parameters, callback)
  │
  ├─ [gate] IsInitialized? No → RegistrationResult.Fail(NotInitialized)
  ├─ name null/empty? → Fail(NullOrEmptyName)
  ├─ parameters null? → Fail(NullParameters)
  ├─ callback null? → Fail(NullCallback)
  ├─ any parameter type unsupported? → Fail(UnsupportedParameterType)
  ├─ registry duplicate? → Fail(DuplicateCommandName)
  └─ → RegistrationResult.Ok()
```

## Scan Flow

```
CommandSystem.Scan(typeof(PlayerCommands), options)
  │
  ├─ [gate] IsInitialized? No → ScanResult.SystemFailure(NotInitialized)
  ├─ type null? → ScanResult.SystemFailure(NullParameters)
  │
  └─ AttributeScanner.ScanType(type, options)
       For each method with [Command]:
         ├─ IsDevOnly && !DevMode? → skip silently (no entry)
         ├─ not static? → ScanEntry(InvalidMethod)
         ├─ unsupported param type? → ScanEntry(UnsupportedParameterType)
         ├─ build callback via Delegate.CreateDelegate
         ├─ TryRegister fails (duplicate)? → ScanEntry(DuplicateCommandName)
         └─ → ScanEntry(Ok)
       → new ScanResult(entries[])
```

## Key Design Decisions

**Instance-based, no static state.** `CommandSystem` is a plain class. The consumer owns the lifecycle. This is domain-reload safe (Unity editor re-enters play mode without stale state).

**`object[]` callback arguments.** The only AOT-safe way to deliver heterogeneous typed arguments through a single delegate without runtime code generation. Boxing is acceptable because commands fire at human-input frequency, not per-frame.

**`readonly struct` results.** `RegistrationResult` and `ExecutionResult` avoid heap allocation. Internal factory methods (`Ok()`, `Fail(...)`) keep construction logic hidden from consumers.

**Case-insensitive command names.** `OrdinalIgnoreCase` matching. Command-console UX expects case-insensitivity. Original casing is preserved for future metadata/display use.

**`InvariantCulture` numeric parsing.** Prevents locale-dependent decimal separator issues across platforms and regions.

**No exceptions on expected error paths.** All foreseeable failures return structured results. Exceptions are only thrown from `CommandParameterInfo` constructor on null arguments (programming-error guards, not runtime conditions).

## Discovery Flow

```
CommandSystem.GetCommandNames()
  → if (!IsInitialized) return Array.Empty<string>()
  → _registry.GetAllNames()
      → foreach entry: copy Name (original casing)
      → Array.Sort(OrdinalIgnoreCase)
      → return string[]

CommandSystem.TryGetCommandParameters(name, out parameters)
  → if (!IsInitialized || null/empty name) { parameters = null; return false }
  → _registry.TryGetCommand(name, out definition)
  → if not found: { parameters = null; return false }
  → parameters = definition.Parameters   // same reference, zero allocation
  → return true

CommandSystem.GetSnapshot()
  → if (!IsInitialized) return CommandMetadataSnapshot.Empty
  → _registry.BuildSnapshot()
      → if Count == 0, return CommandMetadataSnapshot.Empty
      → allocate string[Count] and Dictionary(Count, OrdinalIgnoreCase)
      → foreach entry: copy Name; Array.Copy(def.Parameters, paramsCopy, …)
      → Array.Sort(names, OrdinalIgnoreCase)
      → return new CommandMetadataSnapshot(names, entries)
```

**Allocation profile:** `GetCommandNames()` allocates one `string[]` per call. `GetSnapshot()` allocates one `string[]`, one `Dictionary`, and one `CommandParameterInfo[]` per registered command. `TryGetCommandParameters()` allocates nothing. All discovery allocations are bounded by registry size and occur outside the execution hot path.

## IL2CPP / AOT Compatibility

- No `System.Reflection.Emit`, no `dynamic`, no runtime code generation.
- No generic virtual dispatch in hot paths.
- Static converter methods in `ArgumentConverter` — no closures.
- All generic types used are explicitly instantiated.
- No LINQ in `src/`.
- `AttributeScanner` uses `Delegate.CreateDelegate` (not `MethodInfo.Invoke`) to bind method references at scan time. The resulting typed `Delegate` is captured in the callback lambda — IL2CPP preserves the method reference through this typed delegate, preventing linker stripping of the target methods.
- `MakeGenericType` for `Action<T1,...>` with the four supported types (`int`, `float`, `bool`, `string`) is reliably AOT-compiled on Unity 2021+ IL2CPP. No `link.xml` entry is required for standard Unity setups.
- Reflection in `AttributeScanner` is scanning/initialization-time only — never called on the per-execute hot path.
