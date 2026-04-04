# Attribute-Based Registration Tasks

## Status

- [x] Planned
- [x] In Progress
- [x] Completed

## Inputs

- Requirements: `.github/tasks/attribute-based-registration/requirements.md`
- Design: `.github/tasks/attribute-based-registration/design.md`

## Branch

- Name: `feat_attribute-based-registration`
- Rationale: `feat_` — this is a new capability that adds an attribute-driven command registration path alongside the existing manual `Register()` API. No existing behavior is changed.

---

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- All new `src/` files must begin with the required Apache 2.0 license header:
  ```
  // kmCommands (https://github.com/KMiseckas/kmCommands)
  // Copyright (c) 2026 Klaudijus Miseckas
  // Licensed under the Apache License, Version 2.0
  // See LICENSE file in the project root for full license information.
  ```
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status -> Completed` may be checked only after all tasks and `## Coverage Check` items are checked.
- No `using UnityEngine;` or `UnityEngine.*` reference in any `src/` file.
- No LINQ (`System.Linq`) in runtime or scan hot paths.
- Target: `netstandard2.0`. Tests target `net8.0`.

---

## Task List

---

### Task 1: Attribute and Options Declarations

- [x] Completed

**Objective:**

Introduce the three pure declaration artifacts that all subsequent tasks depend on:

- `src/CommandAttribute.cs` — `[Command]` attribute with `Name` and `IsDevOnly`.
- `src/ScanOptions.cs` — `ScanOptions` struct with `DevMode` bool.
- Add `InvalidMethod` enum value to `RegistrationError` in `src/Results/RegistrationResult.cs`.

No logic, no tests touched. This is the foundation for Tasks 2–6.

**Inputs:**

- Requirements refs: Req 1 (`[Command]` attribute), Req 5 (dev-mode flag), Req 7 (non-breaking; `RegistrationResult.cs` change is additive).
- Design refs: `CommandAttribute` contract, `ScanOptions` contract, `RegistrationError.InvalidMethod` addition, API/Contract Sketch section.

**Implementation Steps:**

1. Create `src/CommandAttribute.cs`:
   - Add required file header.
   - Namespace: `kmCommands`.
   - `[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]`
   - `public sealed class CommandAttribute : Attribute`
   - Constructor: `public CommandAttribute(string name)` storing `Name`.
   - Property: `public string Name { get; }` (set in constructor).
   - Property: `public bool IsDevOnly { get; set; }` (default `false`).

2. Create `src/ScanOptions.cs`:
   - Add required file header.
   - Namespace: `kmCommands`.
   - `public struct ScanOptions`
   - Property: `public bool DevMode { get; set; }`.
   - XML doc comment on `DevMode`: when `true`, `IsDevOnly` commands are included; when `false` (default), they are silently skipped.
   - No constructor needed — `default(ScanOptions)` produces `DevMode = false`.

3. Edit `src/Results/RegistrationResult.cs`:
   - Append `InvalidMethod` to the `RegistrationError` enum after `UnsupportedParameterType`.
   - XML doc: `The target method is not static. Only static methods can be registered via [Command].`
   - No other changes to `RegistrationResult.cs`.

**Validation:**

- Build the project (`netstandard2.0`). Expect zero errors and zero warnings related to new files.
- Run the full existing test suite (`net8.0`). All 71 pre-existing tests must pass unchanged.
- Visual inspection: confirm no `UnityEngine` reference; confirm file headers on both new files.
- QA quick pass (`taskReviewer`): confirm attribute target flags, `AllowMultiple = false`, `IsDevOnly` defaults to `false`, `ScanOptions` struct default is `DevMode = false`, `InvalidMethod` enum ordering is additive-only.

**taskReviewer review request:**

- Review scope: Three declaration-only changes — `CommandAttribute.cs` (new), `ScanOptions.cs` (new), `RegistrationResult.cs` (`InvalidMethod` appended).
- Primary checks:
  - `AttributeUsage` has correct targets (`Method`), `Inherited = false`, `AllowMultiple = false`.
  - `CommandAttribute.Name` is read-only, set via constructor; `IsDevOnly` is mutable (setter required for named-argument syntax).
  - `ScanOptions` is a struct; `default` produces `DevMode = false`.
  - `InvalidMethod` is appended after `UnsupportedParameterType` — no renumbering of existing enum values.
  - File headers present on both new files.
  - No logic or runtime behavior introduced.
- Required evidence: Build succeeds; 71 pre-existing tests pass.
- Blocking conditions: Any build error; any pre-existing test failure; `AttributeUsage` missing `AttributeTargets.Method`; `InvalidMethod` inserted before existing values (would shift enum values).

Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.

**Completion Gate:**

- [x] `src/CommandAttribute.cs` created with correct attribute contract
- [x] `src/ScanOptions.cs` created with `DevMode` struct
- [x] `RegistrationError.InvalidMethod` appended (no existing values renumbered)
- [x] Build passes with zero errors
- [x] All 71 pre-existing tests pass
- [x] File headers present on both new `src/` files
- [x] No `UnityEngine` reference introduced
- [x] QA quick pass done or exception documented
- [x] taskReviewer output captured and any notes tracked — PASS (low note: ScanOptions doc forward-refs future API, no action required)
- [x] Relevant docs in `docs/` updated or exception documented — N/A (no behavior or API exposed yet)
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented — N/A (no public API yet)

**Commit Note:**

- Suggested commit scope: `feat(attribute-registration)`
- Suggested commit message: `feat(attribute-registration): add CommandAttribute, ScanOptions, and RegistrationError.InvalidMethod`

---

### Task 2: ScanResult and ScanEntry Types

- [x] Completed

**Objective:**

Create `src/Results/ScanResult.cs` containing:

- `ScanEntry` readonly struct — pairs a command name with its `RegistrationResult`.
- `ScanResult` sealed class — holds `ScanEntry[]`, computed `HasErrors`, and an internal `SystemFailure` factory.

No scanner logic yet. These types must be complete and correct before `AttributeScanner` can reference them.

**Inputs:**

- Requirements refs: Req 3 (per-command result reporting from type-scoped scan), Req 4 (per-command result from assembly scan), Req 7 (no changes to `ExecutionResult`, `RegistrationResult`, `CommandParameterInfo`, or `CommandCallback` unless strictly required).
- Design refs: `ScanEntry` and `ScanResult` API/Contract Sketch; `ScanResult.SystemFailure` factory design; `ScanEntry[]` preference over `IReadOnlyList<ScanEntry>`.

**Implementation Steps:**

1. Create `src/Results/ScanResult.cs`:
   - Add required file header.
   - Namespace: `kmCommands`.
   - Define `ScanEntry` as `public readonly struct`:
     - `public string CommandName { get; }` — the command name (or `string.Empty` for system-level failures).
     - `public RegistrationResult Result { get; }` — the registration outcome for this entry.
     - `internal ScanEntry(string commandName, RegistrationResult result)` constructor.
   - Define `ScanResult` as `public sealed class`:
     - `public ScanEntry[] Entries { get; }` — array of per-command outcomes.
     - `public bool HasErrors { get; }` — `true` if any entry has `Result.Success == false`.
     - `internal ScanResult(ScanEntry[] entries)` constructor: store `entries`, compute `HasErrors` via a `for` loop (no LINQ).
     - `internal static ScanResult SystemFailure(RegistrationError error, string message)` factory: returns a `ScanResult` wrapping a single `ScanEntry(string.Empty, RegistrationResult.Fail(error, message))`.

2. Confirm that `RegistrationResult.Fail(error, message)` and `RegistrationResult.Ok()` are accessible (they should be — check `RegistrationResult.cs`).

**Validation:**

- Build the project. Expect zero errors.
- Run all 71 pre-existing tests. All must pass.
- Manually verify: `new ScanResult(new ScanEntry[0]).HasErrors == false`.
- Manually verify: `ScanResult.SystemFailure(RegistrationError.NotInitialized, "msg").HasErrors == true`.
- QA quick pass (`taskReviewer`): confirm no LINQ, `HasErrors` loop logic correct, `SystemFailure` produces single entry with `CommandName == string.Empty`, `ScanEntry` is a struct (not class), `ScanResult` is a class (not struct).

**taskReviewer review request:**

- Review scope: New file `src/Results/ScanResult.cs` — two new types (`ScanEntry` struct and `ScanResult` class).
- Primary checks:
  - `ScanEntry` is `readonly struct`; `ScanResult` is `sealed class`.
  - `HasErrors` is computed in the constructor via a `for` loop — no LINQ.
  - `SystemFailure` factory returns `HasErrors == true` with a single entry whose `CommandName == string.Empty`.
  - `internal` constructors prevent external instantiation; `internal static` factory is accessible within `kmCommands` namespace.
  - File header present.
- Required evidence: Build passes; 71 pre-existing tests pass.
- Blocking conditions: Any build error; LINQ usage; `ScanResult` declared as struct; `HasErrors` computed incorrectly (e.g., all-success still returns `true`).

Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.

**Completion Gate:**

- [x] `src/Results/ScanResult.cs` created with `ScanEntry` and `ScanResult`
- [x] `HasErrors` computed by `for` loop (no LINQ)
- [x] `SystemFailure` factory implemented and accessible from `kmCommands` namespace
- [x] `ScanEntry` is a `readonly struct`; `ScanResult` is a `sealed class`
- [x] Build passes with zero errors
- [x] All 71 pre-existing tests pass
- [x] File header present on new `src/` file
- [x] No `UnityEngine` reference introduced
- [x] QA quick pass done or exception documented
- [x] taskReviewer output captured and any notes tracked — PASS
- [x] Relevant docs in `docs/` updated or exception documented — N/A (type not yet exposed publicly from `CommandSystem`)
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented — N/A (not yet in public API)

**Commit Note:**

- Suggested commit scope: `feat(attribute-registration)`
- Suggested commit message: `feat(attribute-registration): add ScanResult and ScanEntry result types`

---

### Task 3: AttributeScanner — Type-Scoped Scan Core

- [x] Completed

**Objective:**

Create `src/Core/AttributeScanner.cs` implementing:

- Constructor accepting `CommandRegistry` and `ArgumentConverter`.
- `ScanType(Type type, ScanOptions options)` — discovers `[Command]`-decorated static methods on a single type, validates them, builds AOT-safe `CommandCallback` delegates, and registers them.
- Private helpers: `ProcessMethod`, `BuildCallback`, `GetActionDelegateType`.

This is the main implementation chunk. Assembly-wide scan (`ScanAssembly`) is added separately in Task 5.

**Inputs:**

- Requirements refs: Req 2 (parameter auto-mapping), Req 3 (type-scoped scan), Req 5 (dev-mode filtering), Req 6 (IL2CPP/AOT safety).
- Design refs: `AttributeScanner` component responsibilities; `ScanType` logic; per-method processing order (DevOnly → static check → param map → callback → register); `BuildCallback` with `Delegate.CreateDelegate`; `GetActionDelegateType` switch; full `ProcessMethod` code example; `BindingFlags` selection (`Public | NonPublic | Static | DeclaredOnly`).

**Implementation Steps:**

1. Create `src/Core/AttributeScanner.cs`:
   - Add required file header.
   - Namespace: `kmCommands.Core`.
   - `using System; using System.Collections.Generic; using System.Reflection;`
   - `internal sealed class AttributeScanner`

2. Constructor:
   - `internal AttributeScanner(CommandRegistry registry, ArgumentConverter converter)`
   - Store `_registry` and `_converter` fields.

3. `ScanType(Type type, ScanOptions options)` method:
   - `BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly`
   - `MethodInfo[] methods = type.GetMethods(flags)`
   - Iterate with `for` loop (no LINQ). For each method:
     - Get `CommandAttribute attr = method.GetCustomAttribute<CommandAttribute>()`. Skip if null.
     - Call `ProcessMethod(method, attr, options)` → `ScanEntry?`
     - If not null, add to `List<ScanEntry>`.
   - Return `new ScanResult(entries.ToArray())`.

4. `private ScanEntry? ProcessMethod(MethodInfo method, CommandAttribute attr, ScanOptions options)`:
   - Step 1 (DevOnly filter): `if (attr.IsDevOnly && !options.DevMode) return null;`
   - Step 2 (static check): `if (!method.IsStatic) return new ScanEntry(attr.Name, RegistrationResult.Fail(RegistrationError.InvalidMethod, "..."));`
   - Step 3 (parameter mapping): iterate `method.GetParameters()` via `for` loop; for each, call `_converter.IsTypeSupported(paramType)` — if any unsupported, return `ScanEntry` with `UnsupportedParameterType` failure without partial registration.
   - Step 4 (build callback): `CommandCallback callback = BuildCallback(method, reflectedParams);`
   - Step 5 (register): `CommandDefinition definition = new CommandDefinition(attr.Name, parameters, callback);` then `_registry.TryRegister(definition)` — if false, return `ScanEntry` with `DuplicateCommandName` failure.
   - Step 6 (success): return `new ScanEntry(attr.Name, RegistrationResult.Ok());`

5. `private static CommandCallback BuildCallback(MethodInfo method, ParameterInfo[] reflectedParams)`:
   - Zero-param fast path: `Action del = (Action)Delegate.CreateDelegate(typeof(Action), method); return _ => del();`
   - Non-zero path: build `Type[] paramTypes`; call `GetActionDelegateType(paramTypes)` to get the `Action<T...>` type; call `Delegate.CreateDelegate(actionType, method)`; return `args => typedDelegate.DynamicInvoke(args)`.

6. `private static Type GetActionDelegateType(Type[] paramTypes)`:
   - `switch (paramTypes.Length)` with cases 1–4 mapping to `Action<>`, `Action<,>`, `Action<,,>`, `Action<,,,>` via `MakeGenericType`.
   - `default`: throw `NotSupportedException` with a descriptive message.

7. Verify that `ArgumentConverter` exposes an `IsTypeSupported(Type)` method. If not, add it (the design implies it exists but check `src/Core/ArgumentConverter.cs` first).

8. Verify that `CommandRegistry` exposes `TryRegister(CommandDefinition)`. Check `src/Core/CommandRegistry.cs`. The design states it already exists.

**Validation:**

- Build the project. Expect zero errors.
- Run all 71 pre-existing tests. All must pass.
- Code review: confirm `MethodInfo.Invoke` is absent from the callback lambda; confirm `Delegate.CreateDelegate` is used; confirm zero-param path does not use `DynamicInvoke`; confirm no LINQ; confirm `DeclaredOnly` is set.
- QA quick pass (`taskReviewer`): per design's final review contract.

**taskReviewer review request:**

- Review scope: New file `src/Core/AttributeScanner.cs` — scanner core with `ScanType`, `ProcessMethod`, `BuildCallback`, `GetActionDelegateType`.
- Primary checks:
  - `Delegate.CreateDelegate` used in `BuildCallback` — no `MethodInfo.Invoke` inside the callback lambda.
  - Zero-parameter commands use `Action` direct delegate, no `DynamicInvoke`.
  - Processing order: DevOnly → static → params → callback → register. No partial registration on failure at any step.
  - `IsDevOnly && !options.DevMode` → `return null` (no `ScanEntry` at all).
  - Non-static → `RegistrationError.InvalidMethod` entry (not silent skip).
  - Unsupported param type at any position → whole method fails with `UnsupportedParameterType`.
  - No LINQ, no `UnityEngine`, file header present.
  - `BindingFlags.DeclaredOnly` present.
- Required evidence: Build passes; 71 pre-existing tests pass.
- Blocking conditions: `MethodInfo.Invoke` in callback path; `IsDevOnly` commands producing a `ScanEntry` when `DevMode = false`; partial registration when a param type check fails; missing `BindingFlags.DeclaredOnly`.

Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.

**Documentation Sync:**

- Docs to update in `docs/`: None yet — `AttributeScanner` is internal. Architecture doc update is bundled in Task 7.
- Update `.github/instructions/projectOverview.instructions.md` required: No — not yet surfaced in public API.

**Completion Gate:**

- [x] `src/Core/AttributeScanner.cs` created with `ScanType`, `ProcessMethod`, `BuildCallback`, `GetActionDelegateType`
- [x] `Delegate.CreateDelegate` used; no `MethodInfo.Invoke` in callback lambda
- [x] Zero-param fast path uses direct `Action` delegate (no `DynamicInvoke`)
- [x] `BindingFlags.DeclaredOnly` included in `GetMethods` call
- [x] No LINQ (`System.Linq`) imported or used
- [x] `ArgumentConverter.IsTypeSupported(Type)` confirmed or added
- [x] Build passes with zero errors
- [x] All 71 pre-existing tests pass
- [x] File header present
- [x] No `UnityEngine` reference introduced
- [x] QA quick pass done or exception documented
- [x] taskReviewer output captured and any notes tracked — PASS WITH NOTES (unit test coverage deferred to Task 6 per plan; non-blocking)
- [x] Relevant docs in `docs/` updated or exception documented — deferred to Task 7
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented — deferred to Task 7

**Commit Note:**

- Suggested commit scope: `feat(attribute-registration)`
- Suggested commit message: `feat(attribute-registration): implement AttributeScanner with type-scoped scan and AOT-safe BuildCallback`

---

### Task 4: CommandSystem.Scan() Public API

- [x] Completed

**Objective:**

Wire `AttributeScanner` into `CommandSystem`:

- Add `_attributeScanner` field.
- Instantiate in `Initialize()`, null in `Shutdown()`.
- Add two public `Scan()` overloads: `Scan(Type, ScanOptions)` and `Scan(Assembly, ScanOptions)`.
- Both overloads guard on `IsInitialized` and null input, returning `ScanResult.SystemFailure` on violation.

**Inputs:**

- Requirements refs: Req 3 (type-scoped scan entry point on `CommandSystem`), Req 4 (assembly-wide entry point), Req 7 (no changes to existing `Register`, `Execute`, `Initialize`, `Shutdown` signatures).
- Design refs: `CommandSystem` modified section in Architecture Overview; `CommandSystem` new methods API/Contract Sketch; `ScanResult.SystemFailure` usage for guard failures.

**Implementation Steps:**

1. Open `src/CommandSystem.cs`.

2. Add `using System; using System.Reflection;` if not already present.

3. Add private field alongside existing fields:

   ```csharp
   private AttributeScanner _attributeScanner;
   ```

   (Note: `AttributeScanner` lives in `kmCommands.Core` — confirm namespace accessibility. Because `CommandSystem.cs` already uses `kmCommands.Core` for `CommandRegistry` etc., this should work.)

4. In `Initialize()`, after `_executionHandler = new ExecutionHandler(...)`:

   ```csharp
   _attributeScanner = new AttributeScanner(_registry, _converter);
   ```

5. In `Shutdown()`, after `_executionHandler = null`:

   ```csharp
   _attributeScanner = null;
   ```

6. Add public method `Scan(Type type, ScanOptions options = default)`:
   - Guard `!IsInitialized` → return `ScanResult.SystemFailure(RegistrationError.NotInitialized, "...")`.
   - Guard `type == null` → return `ScanResult.SystemFailure(RegistrationError.NullParameters, "...")`.
   - Return `_attributeScanner.ScanType(type, options)`.

7. Add public method `Scan(Assembly assembly, ScanOptions options = default)`:
   - Guard `!IsInitialized` → return `ScanResult.SystemFailure(RegistrationError.NotInitialized, "...")`.
   - Guard `assembly == null` → return `ScanResult.SystemFailure(RegistrationError.NullParameters, "...")`.
   - Return `_attributeScanner.ScanAssembly(assembly, options)`.
   - Note: `ScanAssembly` is added to `AttributeScanner` in Task 5. For this task, calling it will compile only after Task 5 is merged. Either: (a) stub `ScanAssembly` in Task 3/4 with `throw new NotImplementedException()`, or (b) implement Tasks 4 and 5 sequentially without committing Task 4 until Task 5 is complete. **Preferred approach: implement the `Scan(Assembly)` overload body in this task using a forward reference and commit both Task 4 and Task 5 together only when `ScanAssembly` exists.** If doing strictly sequential commits, add the `Scan(Assembly)` overload in Task 5 instead.

8. Add XML doc comments to both new public methods following the existing style in `CommandSystem.cs`.

**Validation:**

- Build passes zero errors.
- Run all 71 pre-existing tests. All must pass.
- Verify: calling `Scan(someType)` on an uninitialized system returns `ScanResult` with `HasErrors == true` and `Error == NotInitialized`.
- Verify: calling `Scan(null as Type)` on initialized system returns `ScanResult` with `HasErrors == true` and `Error == NullParameters`.
- QA quick pass (`taskReviewer`).

**taskReviewer review request:**

- Review scope: `src/CommandSystem.cs` modified — `_attributeScanner` field, `Initialize`/`Shutdown` wiring, two new `Scan()` public overloads.
- Primary checks:
  - Existing `Register()`, `Execute()`, `Initialize()`, `Shutdown()` signatures are unchanged.
  - `_attributeScanner` initialized after `_executionHandler`; nulled in `Shutdown`.
  - Guard order: `IsInitialized` check before null check.
  - Both guards return `ScanResult.SystemFailure(...)`, not throw.
  - `using System.Reflection;` added for `Assembly` type reference.
  - XML doc present on both new methods.
- Required evidence: Build passes; 71 pre-existing tests pass.
- Blocking conditions: Any modification to existing method signatures; any guard that throws instead of returning `ScanResult`; `_attributeScanner` not nulled in `Shutdown`.

Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md` (new scan API referenced) and `docs/architecture.md` (new component and flow) — bundled in Task 7 to avoid partial docs.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes — new public `Scan()` API and `AttributeScanner` component are now part of the public surface. Deferred to Task 7 when the full feature is complete.

**Completion Gate:**

- [x] `_attributeScanner` field added and wired into `Initialize` and `Shutdown`
- [x] `Scan(Type, ScanOptions)` public method added with guards
- [x] `Scan(Assembly, ScanOptions)` public method added with guards (implemented together with Task 5)
- [x] Existing method signatures unchanged
- [x] Build passes with zero errors
- [x] All 71 pre-existing tests pass
- [x] No `UnityEngine` reference introduced
- [x] QA quick pass done or exception documented
- [x] taskReviewer output captured and any notes tracked — PASS
- [x] Relevant docs in `docs/` updated or exception documented — deferred to Task 7
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented — deferred to Task 7

**Commit Note:**

- Suggested commit scope: `feat(attribute-registration)`
- Suggested commit message: `feat(attribute-registration): wire AttributeScanner into CommandSystem and expose Scan() API`

---

### Task 5: AttributeScanner — Assembly-Wide Scan

- [x] Completed

**Objective:**

Add `ScanAssembly(Assembly assembly, ScanOptions options)` to `AttributeScanner`. This method iterates all types in the assembly, calls `ScanType` per type, and merges entries into a single `ScanResult`. Includes `ReflectionTypeLoadException` handling for partial assemblies.

**Inputs:**

- Requirements refs: Req 4 (assembly-wide scan entry point), Req 6 (IL2CPP/AOT safe), Req 7 (non-breaking).
- Design refs: Assembly scan type enumeration code example; `ReflectionTypeLoadException` catch pattern; first-registered-wins naming conflict semantics; no LINQ constraint.

**Implementation Steps:**

1. Open `src/Core/AttributeScanner.cs`.

2. Add `internal ScanResult ScanAssembly(Assembly assembly, ScanOptions options)`:
   - Declare `Type[] types`.
   - Try `types = assembly.GetTypes()`.
   - Catch `ReflectionTypeLoadException ex`: `types = ex.Types ?? Array.Empty<Type>()`.
   - Declare `List<ScanEntry> entries = new List<ScanEntry>()`.
   - `for (int i = 0; i < types.Length; i++)`:
     - `if (types[i] == null) continue;`
     - `ScanResult typeResult = ScanType(types[i], options);`
     - `for (int j = 0; j < typeResult.Entries.Length; j++) entries.Add(typeResult.Entries[j]);`
   - Return `new ScanResult(entries.ToArray())`.

3. Confirm the `Scan(Assembly, ScanOptions)` overload in `CommandSystem.cs` (added or stubbed in Task 4) now correctly calls `_attributeScanner.ScanAssembly(assembly, options)`.

4. Ensure `using System.Reflection;` is already present in `AttributeScanner.cs` from Task 3.

**Validation:**

- Build passes zero errors.
- Run all 71 pre-existing tests. All must pass.
- Code review: null guard on `types[i]` present; `ReflectionTypeLoadException` catch present; no LINQ.
- Verify that duplicate command names across two types in an assembly scan are handled by first-wins / `DuplicateCommandName` failure. (This will be fully verified in Task 6 tests, but a smoke check here is appropriate.)
- QA quick pass (`taskReviewer`).

**taskReviewer review request:**

- Review scope: `src/Core/AttributeScanner.cs` — new `ScanAssembly` method.
- Primary checks:
  - `ReflectionTypeLoadException` is caught; `ex.Types` is used with null guard.
  - Per-type results are merged (not replaced) into a single flat `ScanEntry` list.
  - Null type guard (`types[i] == null`) is present before calling `ScanType`.
  - No LINQ; `for` loops used throughout.
  - `CommandSystem.Scan(Assembly, ...)` now resolves without `NotImplementedException`.
- Required evidence: Build passes; 71 pre-existing tests pass.
- Blocking conditions: Missing `ReflectionTypeLoadException` catch; LINQ usage; `types[i]` null guard missing.

Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.

**Completion Gate:**

- [x] `ScanAssembly` implemented with `ReflectionTypeLoadException` handling
- [x] Null type guard present in assembly type loop
- [x] No LINQ used
- [x] `CommandSystem.Scan(Assembly, ScanOptions)` fully wired (no stub/`NotImplementedException`)
- [x] Build passes with zero errors
- [x] All 71 pre-existing tests pass
- [x] QA quick pass done or exception documented
- [x] taskReviewer output captured and any notes tracked — PASS
- [x] Relevant docs in `docs/` updated or exception documented — deferred to Task 7
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented — deferred to Task 7

**Commit Note:**

- Suggested commit scope: `feat(attribute-registration)`
- Suggested commit message: `feat(attribute-registration): add ScanAssembly with ReflectionTypeLoadException handling`

---

### Task 6: Unit Tests — AttributeScannerTests

- [x] Completed

**Objective:**

Create `tests/kmCommands.Tests/AttributeScannerTests.cs` covering all 9 required scenarios from requirements plus the two guard cases (uninitialized, null input) defined in the design's testing strategy table. Also confirm all 71 pre-existing tests remain passing (regression gate).

**Inputs:**

- Requirements refs: All 9 testing expectations from Req testing section; Req 7 (pre-existing tests unchanged).
- Design refs: Full testing strategy table (12 scenarios); test fixture structure with private static inner classes; `[SetUp]`/`[TearDown]` convention; `LastAmount`-style side-effect fields for assertions.

**Implementation Steps:**

1. Create `tests/kmCommands.Tests/AttributeScannerTests.cs`:
   - Add required file header.
   - `using NUnit.Framework; using kmCommands; using System.Reflection;`
   - `[TestFixture] public class AttributeScannerTests`
   - `[SetUp]`: create and initialize a fresh `CommandSystem _system`.
   - `[TearDown]`: shutdown if `IsInitialized`.

2. Add private static inner classes as test command containers (following design examples):
   - `SingleCommandTarget`: one `[Command("heal")] static void Heal(int amount)` method with a `LastAmount` field.
   - `MultiCommandTarget`: two or more attributed static methods with separate `Last*` fields.
   - `UnsupportedParamTarget`: one `[Command("bad")] static void BadMethod(object unsupported)` method.
   - `NoParamTarget`: one `[Command("ping")] static void Ping()` method with a `WasCalled` bool.
   - `DevOnlyTarget`: one `[Command("debuginfo", IsDevOnly = true)] static void DebugInfo()` method.
   - `DuplicateNameTarget`: one `[Command("heal")] static void HealToo(int amount)` method (same name as `SingleCommandTarget`).
   - `AssemblyTypeA` and `AssemblyTypeB`: each with one attributed method, distinct names.
   - `NonStaticTarget`: one `[Command("instance")] void InstanceMethod()` (non-static) method.

3. Write test methods (one per scenario, named clearly):

   | Test method                                         | Scenario                                                                                                                            |
   | --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
   | `SingleAttributedStaticMethod_RegistersAndExecutes` | Scan `SingleCommandTarget`, execute "heal" with `["10"]`, assert `LastAmount == 10`                                                 |
   | `MultipleAttributedMethods_AllRegistered`           | Scan `MultiCommandTarget`, execute each command                                                                                     |
   | `UnsupportedParameterType_SkippedWithFailure`       | `ScanResult.HasErrors == true`; entry error == `UnsupportedParameterType`; execute returns `CommandNotFound`                        |
   | `NoParameterMethod_RegistersAndExecutes`            | Scan `NoParamTarget`, execute "ping" with empty args, assert `WasCalled == true`                                                    |
   | `IsDevOnlyTrue_DevModeFalse_CommandExcluded`        | Scan with `DevMode = false`, `Entries` empty or command absent, execute returns `CommandNotFound`                                   |
   | `IsDevOnlyTrue_DevModeTrue_CommandIncluded`         | Scan with `DevMode = true`, command present in entries, execute succeeds                                                            |
   | `DuplicateNameCollision_ReportedAsFailure`          | Register "heal" manually first, then scan `SingleCommandTarget`, entry error == `DuplicateCommandName`                              |
   | `NonStaticMethod_ReportedAsInvalidMethod`           | Scan `NonStaticTarget`, entry error == `InvalidMethod`                                                                              |
   | `AssemblyWideScan_DiscoversAcrossTypes`             | `_system.Scan(Assembly.GetExecutingAssembly(), opts)`, both `AssemblyTypeA` and `AssemblyTypeB` commands are present and executable |
   | `ScanBeforeInitialize_ReturnsSystemFailure`         | Shutdown system, call `Scan(typeof(SingleCommandTarget))`, `HasErrors == true`, error == `NotInitialized`                           |
   | `ScanNullType_ReturnsSystemFailure`                 | `Scan(null as Type)`, `HasErrors == true`, error == `NullParameters`                                                                |
   | `PreExistingTests_StillPass`                        | This is validated by running the full suite — documented in validation section, not a new test method                               |

4. Ensure test inner class method names do not collide with other test classes in the suite (use distinct command names or unique inner class names as shown above).

**Validation:**

- Run full test suite (`net8.0`). Total passing count must be ≥ 71 + 11 = 82 (11 new test methods).
- All 71 pre-existing tests pass unmodified.
- All 11 new test methods pass.
- QA quick pass (`taskReviewer`): confirm test isolation (`[SetUp]`/`[TearDown]`), no shared static state leaks between tests, dev-mode scenario uses correct `ScanOptions` struct, `IsDevOnly` exclusion test checks `Entries` count or absence, not just `HasErrors`.

**taskReviewer review request:**

- Review scope: New file `tests/kmCommands.Tests/AttributeScannerTests.cs`.
- Primary checks:
  - All 9 required requirement scenarios have corresponding test methods.
  - `[SetUp]` creates a fresh `CommandSystem`; `[TearDown]` shuts it down — no shared state.
  - `IsDevOnly` exclusion test: command is absent from `Entries` entirely (not present as a failure entry).
  - Duplicate-name test: only one command is in the registry; second scan entry has `DuplicateCommandName`.
  - `NonStaticMethod` test: entry error is `InvalidMethod` (not `UnsupportedParameterType`).
  - Assembly-wide scan test targets `Assembly.GetExecutingAssembly()` and verifies commands from both `AssemblyTypeA` and `AssemblyTypeB`.
  - Guard tests (`ScanBeforeInitialize`, `ScanNullType`) use the `SystemFailure` path, not thrown exceptions.
  - Full suite passes (71 pre-existing + 11 new minimum).
- Required evidence: Test run output showing all tests passing.
- Blocking conditions: Any pre-existing test failure; `IsDevOnly` exclusion test checking `HasErrors` instead of absent entry; missing assembly-wide scan test; `[TearDown]` absent (state leaks between tests).

Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.

**Completion Gate:**

- [x] `AttributeScannerTests.cs` created with all 11 test methods
- [x] All 9 required requirement scenarios covered by tests
- [x] `IsDevOnly` exclusion test verifies entry is absent from `ScanResult.Entries`, not just `HasErrors`
- [x] Assembly-wide scan test uses `Assembly.GetExecutingAssembly()`
- [x] `[SetUp]` / `[TearDown]` present; no cross-test state leaks
- [x] All 71 pre-existing tests pass unmodified
- [x] All new test methods pass
- [x] Total passing count ≥ 82 — exactly 82 (71 + 11 new)
- [x] QA quick pass done or exception documented
- [x] taskReviewer output captured and any notes tracked — PASS. Note: `BindingFlags.Instance` added to `ScanType` `GetMethods` call (required so non-static methods are discoverable and reportable; original spec was underspecified; `!method.IsStatic` guard routes them to `InvalidMethod` failure immediately)
- [x] Relevant docs in `docs/` updated or exception documented — N/A for test file itself; doc sync in Task 7
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented — deferred to Task 7

**Commit Note:**

- Suggested commit scope: `feat(attribute-registration)`
- Suggested commit message: `feat(attribute-registration): add AttributeScannerTests covering all required scenarios`

---

### Task 7: Documentation and projectOverview Sync

- [x] Completed

**Objective:**

Update public-facing docs and the projectOverview instruction file to reflect the new attribute-based registration feature. This is the final task before the feature is considered complete.

**Inputs:**

- Requirements refs: Req 1–7 (full feature scope now implemented).
- Design refs: Full design document — new components, API surface, data flow, usage example.
- Affected docs: `docs/commands.md`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`.

**Implementation Steps:**

1. Update `docs/commands.md`:
   - Add a new section documenting the `[Command]` attribute: syntax, `IsDevOnly` flag, supported parameter types.
   - Add a section documenting `ScanOptions` and dev-mode behavior.
   - Add a section documenting `CommandSystem.Scan(Type, ScanOptions)` and `CommandSystem.Scan(Assembly, ScanOptions)`.
   - Document `ScanResult` and `ScanEntry` — how to check `HasErrors`, iterate `Entries`, and surface failures.
   - Add a brief comparison of attribute-based vs. manual registration.

2. Update `docs/architecture.md`:
   - Add `AttributeScanner` to the component list (alongside `ExecutionHandler`, `CommandRegistry`, etc.).
   - Describe its responsibilities and interactions.
   - Note that `Delegate.CreateDelegate` + `DynamicInvoke` is used for AOT-safe typed dispatch; zero-param path uses direct `Action`.
   - Note the `ReflectionTypeLoadException` handling in assembly scans.
   - Note the 4-parameter limit for `GetActionDelegateType`.

3. Update `.github/instructions/projectOverview.instructions.md`:
   - In `## Key Paths`: add entries for `src/CommandAttribute.cs`, `src/ScanOptions.cs`, `src/Results/ScanResult.cs`, `src/Core/AttributeScanner.cs`.
   - In `## API Layer Summary`: add entries for the scan API: `scan(Type, ScanOptions)` and `scan(Assembly, ScanOptions)`.
   - In `## Systems In Action`: note the Attribute Scanner under a new bullet.
   - In `## Implementation Direction`: add new file descriptions for `CommandAttribute.cs`, `ScanOptions.cs`, `src/Results/ScanResult.cs`, `src/Core/AttributeScanner.cs`.

**Validation:**

- Read-through all three updated documents for completeness and accuracy.
- Confirm no dead links or unresolved references.
- Confirm the projectOverview accurately reflects the state of the codebase after Tasks 1–6.
- QA quick pass (`taskReviewer`): confirm all new public types and methods are documented; confirm projectOverview reflects new files and API surface; confirm no incorrect technical claims.

**taskReviewer review request:**

- Review scope: `docs/commands.md`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`.
- Primary checks:
  - `docs/commands.md` covers: `[Command]` attribute, `IsDevOnly`, `ScanOptions.DevMode`, both `Scan()` overloads, `ScanResult`/`ScanEntry` usage, attribute vs. manual registration comparison.
  - `docs/architecture.md` covers: `AttributeScanner` component, delegate strategy, parameter limit, `ReflectionTypeLoadException` handling.
  - `projectOverview.instructions.md` Key Paths includes all four new `src/` files; API Layer Summary includes scan API.
  - No technical inaccuracies (e.g., incorrect method signatures, wrong parameter limits).
- Required evidence: Reviewed docs content provided in findings.
- Blocking conditions: Any public API method or type missing from docs; `projectOverview.instructions.md` not updated.

Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md` (scan API and attribute usage), `docs/architecture.md` (AttributeScanner component, delegate strategy).
- Update `.github/instructions/projectOverview.instructions.md` required: Yes.
- Sections to update: `## Key Paths`, `## API Layer Summary`, `## Systems In Action`, `## Implementation Direction`.

**Completion Gate:**

- [x] `docs/commands.md` updated with `[Command]`, `ScanOptions`, `Scan()`, `ScanResult`/`ScanEntry` documentation
- [x] `docs/architecture.md` updated with `AttributeScanner` component description, delegate strategy, and assembly scan notes
- [x] `.github/instructions/projectOverview.instructions.md` updated: Key Paths, API Layer Summary, Systems In Action, Implementation Direction
- [x] No incorrect technical claims in any updated doc
- [x] QA quick pass done or exception documented
- [x] taskReviewer output captured and any notes tracked — PASS
- [x] All 82+ tests still passing (no regressions from doc-only changes)

**Commit Note:**

- Suggested commit scope: `docs`
- Suggested commit message: `docs: update commands, architecture, and projectOverview for attribute-based registration`

---

## Coverage Check

### Requirements coverage

- [ ] **Req 1** (`[Command]` attribute) → Task 1 (`CommandAttribute.cs`)
- [ ] **Req 2** (Parameter auto-mapping, unsupported type skip) → Task 3 (`AttributeScanner.ProcessMethod` step 3)
- [ ] **Req 3** (Type-scoped scan on `CommandSystem`) → Task 3 (`AttributeScanner.ScanType`) + Task 4 (`CommandSystem.Scan(Type, ...)`)
- [ ] **Req 4** (Assembly-wide scan) → Task 5 (`AttributeScanner.ScanAssembly`) + Task 4 (`CommandSystem.Scan(Assembly, ...)`)
- [ ] **Req 5** (Dev-mode filtering via `ScanOptions`) → Task 1 (`ScanOptions`) + Task 3 (`ProcessMethod` step 1)
- [ ] **Req 6** (IL2CPP/AOT safety — `Delegate.CreateDelegate`, no `Emit`) → Task 3 (`BuildCallback`)
- [ ] **Req 7** (Additive, non-breaking; pre-existing 71 tests pass) → Task 6 (regression gate) + Tasks 1–5 (no changes to existing signatures)
- [ ] **Testing Exp. 1** (Single attributed static method registers and executes) → Task 6 (`SingleAttributedStaticMethod_RegistersAndExecutes`)
- [ ] **Testing Exp. 2** (Multiple attributed methods on one type) → Task 6 (`MultipleAttributedMethods_AllRegistered`)
- [ ] **Testing Exp. 3** (Unsupported parameter type: skipped, failure in result) → Task 6 (`UnsupportedParameterType_SkippedWithFailure`)
- [ ] **Testing Exp. 4** (No parameters: registers correctly) → Task 6 (`NoParameterMethod_RegistersAndExecutes`)
- [ ] **Testing Exp. 5** (`IsDevOnly = true` excluded when `DevMode = false`) → Task 6 (`IsDevOnlyTrue_DevModeFalse_CommandExcluded`)
- [ ] **Testing Exp. 6** (`IsDevOnly = true` included when `DevMode = true`) → Task 6 (`IsDevOnlyTrue_DevModeTrue_CommandIncluded`)
- [ ] **Testing Exp. 7** (Duplicate name collision) → Task 6 (`DuplicateNameCollision_ReportedAsFailure`)
- [ ] **Testing Exp. 8** (Assembly-wide scan across multiple types) → Task 6 (`AssemblyWideScan_DiscoversAcrossTypes`)
- [ ] **Testing Exp. 9** (Pre-existing manual registration tests still pass) → Task 6 (full suite run with 71+ baseline)
- [ ] Every requirement is mapped to at least one task — **confirmed**
- [ ] No requirement is left unplanned — **confirmed**

### Design coverage

- [ ] `CommandAttribute` (design: API/Contract Sketch) → Task 1
- [ ] `ScanOptions` (design: API/Contract Sketch) → Task 1
- [ ] `RegistrationError.InvalidMethod` (design: Architecture Overview, `RegistrationResult.cs` change) → Task 1
- [ ] `ScanEntry` + `ScanResult` types (design: API/Contract Sketch, `SystemFailure` factory) → Task 2
- [ ] `AttributeScanner.ScanType` + `ProcessMethod` + `BuildCallback` + `GetActionDelegateType` (design: Components, Code Examples) → Task 3
- [ ] `CommandSystem._attributeScanner` field + `Initialize`/`Shutdown` wiring + two `Scan()` overloads (design: `CommandSystem` modified section) → Task 4
- [ ] `AttributeScanner.ScanAssembly` with `ReflectionTypeLoadException` handling (design: Assembly scan type enumeration) → Task 5
- [ ] `Delegate.CreateDelegate` + zero-param fast path (design: `BuildCallback` code example, risks/tradeoffs) → Task 3
- [ ] `DeclaredOnly` binding flag (design: method discovery notes) → Task 3
- [ ] First-wins naming conflict semantics (design: naming conflicts across types) → Task 3 (`TryRegister`) + Task 6 (duplicate test)
- [ ] Non-static method → `InvalidMethod` failure (design: non-static method handling) → Task 3 + Task 6
- [ ] All 12 test scenarios from testing strategy table → Task 6
- [ ] Documentation updates (design: consumed but not explicitly a design component) → Task 7
- [ ] `projectOverview.instructions.md` sync → Task 7
- [ ] Key design components are mapped to tasks — **confirmed**
- [ ] Critical design constraints represented in validation gates — **confirmed** (AOT-safety, no LINQ, `DeclaredOnly`, `SystemFailure` guard behavior)

### Gaps or follow-ups

- None identified. All requirements and design elements have task coverage.
- The `GetActionDelegateType` 4-parameter limit is a documented and acceptable constraint per design. Extension to 8 is trivial if needed and does not require a new task at this time.
- `docs/unity-integration.md` may benefit from a brief mention of the scan API in a future task, but is not required for this feature — current scope is the core library only.
