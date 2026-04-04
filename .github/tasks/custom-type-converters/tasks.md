# Custom Type Converters Tasks

## Status

- [ ] Planned
- [ ] In Progress
- [ ] Completed

## Inputs

- Requirements: `.github/tasks/custom-type-converters/requirements.md`
- Design: `.github/tasks/custom-type-converters/design.md`

## Branch

- Name: `feature/custom-type-converters`
- Rationale: New consumer-facing capability extending the argument-conversion system; `feat_`-class work.

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status -> Completed` may be checked only after all tasks and `## Coverage Check` items are checked.

---

## Task List

### Task 1: Add `RegistrationError.NullConverter` and `TypeConverterDelegate` public delegate

- [ ] Not started

Objective:

- Introduce the two new public API surface elements that are prerequisites for everything else: the `NullConverter` enum value that signals a null delegate at registration time, and the named `TypeConverterDelegate` public delegate type that consumers use to supply converters.
- These are purely additive declarations with no behavior change. No existing code paths are modified.

Inputs:

- Requirements refs: REQ-1 (public `RegisterConverter` whose return type needs `NullConverter`), REQ-2 (delegate signature `bool(string, out object)`), REQ-3 (named public delegate type), REQ-5 (null-delegate → specific error result).
- Design refs: `RegistrationError — new value` section; `TypeConverterDelegate.cs` component section; API Contract Sketch (`TypeConverterDelegate.cs` block and `RegistrationError.NullConverter` block).

Implementation Steps:

1. Open `src/Results/RegistrationResult.cs`. After the existing `OptionalParameterBeforeRequired` member of `RegistrationError`, add:
   ```csharp
   /// <summary>The provided converter delegate was null.</summary>
   NullConverter,
   ```
   Preserve existing member order and XML doc style.

2. Create `src/TypeConverterDelegate.cs` with the required source header (see `projectOverview.instructions.md`) and the following content, matching the design API sketch exactly:
   ```csharp
   // kmCommands (https://github.com/KMiseckas/kmCommands)
   // Copyright (c) 2026 Klaudijus Miseckas
   // Licensed under the Apache License, Version 2.0
   // See LICENSE file in the project root for full license information.

   namespace kmCommands
   {
       /// <summary>
       /// Represents a method that attempts to convert a string token to an object of a specific type.
       /// </summary>
       /// <param name="input">The raw string token to convert.</param>
       /// <param name="result">
       /// When this method returns <c>true</c>, contains the converted value; otherwise <c>null</c>.
       /// </param>
       /// <returns><c>true</c> if conversion succeeded; <c>false</c> otherwise.</returns>
       public delegate bool TypeConverterDelegate(string input, out object result);
   }
   ```

Validation:

- Unit tests: None required — purely declarative additions with no runtime behavior to verify.
- Additional checks:
  - `dotnet build` on `netstandard2.0` — 0 errors, 0 warnings.
  - `dotnet test` targeting `net8.0` — all 103 existing tests pass, none regressed.
- QA quick pass (`taskReviewer`): Yes.
- taskReviewer review request:
  - Review scope: `src/Results/RegistrationResult.cs` (one new enum value) and `src/TypeConverterDelegate.cs` (new file).
  - Primary checks: `NullConverter` is placed after `OptionalParameterBeforeRequired` with XML doc present; `TypeConverterDelegate` delegate signature is `bool(string input, out object result)`; source header is present in the new file; namespace is `kmCommands`; no existing members in `RegistrationError` are modified or reordered.
  - Required evidence: `dotnet build` output showing 0 errors; `dotnet test` output showing all 103 existing tests passing.
  - Blocking conditions: Any compile error; any regression in existing tests; delegate signature diverges from design; source header missing from new file; any modification to existing `RegistrationError` members.
- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Required before marking complete.

Documentation Sync:

- Docs to update in `docs/`: None — no public behavior change; consumer-facing docs are deferred to Task 4 when the full feature is wired.
- Update `.github/instructions/projectOverview.instructions.md` required: No — projectOverview sync is deferred to Task 4.

Completion Gate:

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented (N/A — no behavior to test in this task)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A — deferred to Task 4)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to Task 4)

Commit Note:

- Suggested commit scope: `src/Results/RegistrationResult.cs`, `src/TypeConverterDelegate.cs`
- Suggested commit message: `feat(custom-type-converters): add RegistrationError.NullConverter and TypeConverterDelegate delegate`

---

### Task 2: Extend `ArgumentConverter` with `AddConverter` internal method

- [ ] Not started

Objective:

- Add the single `AddConverter(Type, TryConvertFunc)` internal method to `ArgumentConverter`. This method inserts or replaces an entry in the existing `_converters` dictionary, enabling last-write-wins override of built-in or previously registered converters. The `TryConvert` and `IsTypeSupported` hot paths remain completely unchanged.

Inputs:

- Requirements refs: REQ-4 (registering a converter for an already-supported type replaces the prior converter; last-write wins), REQ-11 (no allocations on the execute hot path — `AddConverter` is called at registration time, not at execute time).
- Design refs: `ArgumentConverter (modified)` component section; `ArgumentConverter.AddConverter` API sketch; `Implementation Notes — Register() interaction` (confirms `IsTypeSupported` reads from the same dictionary `AddConverter` writes to).

Implementation Steps:

1. Open `src/Core/ArgumentConverter.cs`. After the `IsTypeSupported` method, add the following internal method:
   ```csharp
   /// <summary>
   /// Adds or replaces the converter for the given type.
   /// </summary>
   internal void AddConverter(Type type, TryConvertFunc converter)
   {
       _converters[type] = converter;
   }
   ```
   Use the indexer assignment (`_converters[type] = converter`) — not `_converters.Add(type, converter)` — to ensure last-write-wins semantics without throwing on duplicate keys.

2. Confirm that `TryConvert` and `IsTypeSupported` are not modified. Their read paths use `_converters.TryGetValue` and `_converters.ContainsKey` respectively, which already reflect any entry written by `AddConverter`.

Validation:

- Unit tests: No dedicated unit tests required for `AddConverter` in isolation — its correctness is fully exercised by the integration tests added in Task 3. Confirm the existing `ArgumentConverterTests.cs` tests still pass to verify the hot-path methods are unaffected.
- Additional checks:
  - `dotnet build` on `netstandard2.0` — 0 errors, 0 warnings.
  - `dotnet test` targeting `net8.0` — all 103 existing tests pass.
- QA quick pass (`taskReviewer`): Yes.
- taskReviewer review request:
  - Review scope: `src/Core/ArgumentConverter.cs` — one new internal method; no other changes.
  - Primary checks: `AddConverter` uses indexer `_converters[type] = converter` (not `Dictionary.Add`); method is `internal void`, not `public`; `TryConvert` and `IsTypeSupported` are byte-for-byte unchanged; no new fields, properties, or constructor changes introduced.
  - Required evidence: `dotnet build` 0 errors; `dotnet test` 103 passing.
  - Blocking conditions: `AddConverter` made `public`; `Add` used instead of indexer assignment (would throw on duplicate); `TryConvert` or `IsTypeSupported` modified; compile errors.
- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Required before marking complete.

Documentation Sync:

- Docs to update in `docs/`: None — internal method; no public API surface change.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to Task 4.

Completion Gate:

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented (existing 103 tests pass; no new isolated tests needed for this internal method)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A — internal change)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to Task 4)

Commit Note:

- Suggested commit scope: `src/Core/ArgumentConverter.cs`
- Suggested commit message: `feat(custom-type-converters): add ArgumentConverter.AddConverter internal method`

---

### Task 3: Implement `CommandSystem.RegisterConverter` + pending-converter lifecycle + write `CustomTypeConverterTests.cs`

- [ ] Not started

Objective:

- This is the core integration task. Wire the full `RegisterConverter` feature in `CommandSystem`: add the `_pendingConverters` pre-init buffer, implement `RegisterConverter` with input validation and both pre-/post-init branches, flush pending converters into `ArgumentConverter` during `Initialize()`, and clear the buffer during `Shutdown()`. Write all 10 unit tests in `CustomTypeConverterTests.cs` alongside the implementation (TDD-alongside style: write each test, then confirm the corresponding code path satisfies it before moving on).

Inputs:

- Requirements refs: REQ-1 through REQ-11 (all requirements are either implemented or verified in this task).
- Design refs: `CommandSystem (modified)` component section; `Pre-Initialize() registration`, `Post-Initialize() registration`, `Initialize()`, `Shutdown()` data-flow diagrams; `Implementation Notes — Delegate adaptation`; `Implementation Notes — Thread-safety scope`; `Implementation Notes — Shutdown() change`; `Implementation Notes — Register() interaction`; `Testing Strategy` table (all 10 test cases).

Implementation Steps:

1. Open `src/CommandSystem.cs`. Add `using System.Collections.Generic;` to the using directives if not already present (`using System;` is already present).

2. Add a private `_pendingConverters` field initialized at declaration time. It must be `readonly` so it is never nulled by `Initialize()` or `Shutdown()`:
   ```csharp
   private readonly Dictionary<Type, TypeConverterDelegate> _pendingConverters
       = new Dictionary<Type, TypeConverterDelegate>();
   ```

3. Add a private static `AdaptConverter` helper method that wraps a `TypeConverterDelegate` into an `ArgumentConverter.TryConvertFunc` using a thin lambda. The wrapper is allocated once at registration time — not on the execute hot path:
   ```csharp
   private static ArgumentConverter.TryConvertFunc AdaptConverter(TypeConverterDelegate d)
   {
       return (string input, out object result) => d(input, out result);
   }
   ```

4. Add the public `RegisterConverter` method immediately after the existing `Shutdown()` method:
   ```csharp
   public RegistrationResult RegisterConverter(Type type, TypeConverterDelegate converter)
   ```
   The method body must:
   - Guard `type == null` → `RegistrationResult.Fail(RegistrationError.NullParameters, "Type argument must not be null.")`.
   - Guard `converter == null` → `RegistrationResult.Fail(RegistrationError.NullConverter, "Converter delegate must not be null.")`.
   - Pre-init path (`!IsInitialized`): `_pendingConverters[type] = converter; return RegistrationResult.Ok();`.
   - Post-init path: `_converter.AddConverter(type, AdaptConverter(converter)); return RegistrationResult.Ok();`.

5. In `Initialize()`, after `_converter = new ArgumentConverter();` and before `IsInitialized = true`, add a flush loop and a clear call:
   ```csharp
   foreach (KeyValuePair<Type, TypeConverterDelegate> entry in _pendingConverters)
   {
       _converter.AddConverter(entry.Key, AdaptConverter(entry.Value));
   }
   _pendingConverters.Clear();
   ```

6. In `Shutdown()`, after the existing null assignments (`_attributeScanner = null`), add:
   ```csharp
   _pendingConverters.Clear();
   ```
   Do **not** null `_pendingConverters` — the field must survive `Shutdown()` to accept new registrations before the next `Initialize()`.

7. Create `tests/kmCommands.Tests/CustomTypeConverterTests.cs`. Write all 10 tests from the design Testing Strategy table. Each test follows the existing NUnit setup pattern in the project (new `CommandSystem` instance per test, `TearDown` calls `Shutdown()` if initialized). Test names and coverage:

   | Test method | What it verifies |
   |---|---|
   | `RegisterConverter_CustomType_AllowsCommandWithThatType` | Custom converter registered → command with that parameter type registers and executes successfully end-to-end |
   | `RegisterConverter_NullType_ReturnsFailure` | `null` `type` arg → `RegistrationResult.Success == false`, `RegistrationError.NullParameters`; no state mutation |
   | `RegisterConverter_NullDelegate_ReturnsFailure` | `null` `converter` arg → `RegistrationResult.Success == false`, `RegistrationError.NullConverter`; no state mutation |
   | `RegisterConverter_OverridesBuiltIn_UsesNewConverter` | Registering a replacement converter for `typeof(int)` produces consumer-defined conversion behavior on `Execute()` |
   | `RegisterConverter_BeforeInitialize_SurvivesInitialize` | Converter registered before `Initialize()` is active and usable after `Initialize()` |
   | `Shutdown_ClearsCustomConverters` | Custom converter registered in one lifecycle session is absent after `Shutdown()` + re-`Initialize()` without re-registration; command with that type is rejected with `UnsupportedParameterType` |
   | `Register_WithNoConverter_RejectsCommand` | `Register()` for a command whose parameter type has no registered converter returns `RegistrationError.UnsupportedParameterType` |
   | `Execute_FailingCustomConverter_ReturnsConversionFailed` | Custom converter returning `false` during `Execute()` → `ExecutionResult.Error == ExecutionError.ArgumentConversionFailed` |
   | `RegisterConverter_PreInit_MultipleConverters_AllFlushed` | Multiple converters for distinct types registered before `Initialize()` are all available after `Initialize()` |
   | `RegisterConverter_PreInit_Override_LastWriteWins` | Registering the same type twice in the pre-init buffer → only the last converter is active after `Initialize()` |

Validation:

- Unit tests: All 10 new tests in `CustomTypeConverterTests.cs` must pass. All 103 pre-existing tests must still pass. Total: 113 passing.
- Additional checks:
  - `dotnet build` on `netstandard2.0` — 0 errors, 0 warnings.
  - `dotnet test` on `net8.0` — 113 tests pass.
  - Manually trace: `_pendingConverters` is never `null` after `Shutdown()` — can accept registrations immediately after `Shutdown()`.
  - Verify `Shutdown()` clears `_pendingConverters` but does not null it.
  - Verify `Initialize()` calls `_pendingConverters.Clear()` after the flush loop completes.
- QA quick pass (`taskReviewer`): Yes.
- taskReviewer review request:
  - Review scope: `src/CommandSystem.cs` (new field, new method, `Initialize()` and `Shutdown()` changes); `tests/kmCommands.Tests/CustomTypeConverterTests.cs` (new file, 10 tests).
  - Primary checks:
    - `_pendingConverters` is declared `readonly` and initialized at declaration — never set to `null`.
    - `RegisterConverter` null-`type` guard returns `NullParameters`; null-`converter` guard returns `NullConverter` (not the other way around).
    - Pre-init path stores to `_pendingConverters` and returns `Ok()` without touching `_converter` (which is `null` pre-init).
    - Post-init path calls `_converter.AddConverter(type, AdaptConverter(converter))` and returns `Ok()`.
    - `Initialize()` flush loop runs before `IsInitialized = true` and calls `_pendingConverters.Clear()` after the loop.
    - `Shutdown()` calls `_pendingConverters.Clear()` after the null assignments; does not null the field.
    - `AdaptConverter` uses a thin lambda, not a direct cast — allocation occurs once at registration time.
    - Thread-safety note in code comment or XML doc: no lock is added; single-threaded caller contract is preserved as documented on the class.
    - All 10 required test method names are present and match the design table exactly.
    - `Shutdown_ClearsCustomConverters` registers a custom converter, calls `Shutdown()`, re-calls `Initialize()`, and then verifies that `Register()` rejects a command with that parameter type with `UnsupportedParameterType`.
    - `RegisterConverter_BeforeInitialize_SurvivesInitialize` registers before `Initialize()`, calls `Initialize()`, then successfully registers and executes a command using that type.
    - `RegisterConverter_PreInit_Override_LastWriteWins` registers type A with converter X, then registers type A again with converter Y, calls `Initialize()`, and verifies converter Y is used.
  - Required evidence: `dotnet build` 0 errors; `dotnet test` showing exactly 113 passing (103 pre-existing + 10 new).
  - Blocking conditions: Any of the 10 new tests absent or failing; any of the 103 existing tests regressed; `_pendingConverters` nulled in `Shutdown()`; `_pendingConverters.Clear()` missing from `Shutdown()`; flush loop missing from `Initialize()`; wrong error code for null-`type` vs. null-`converter`; `AdaptConverter` not used (raw cast used instead of lambda wrapper); `Execute()` hot path modified.
- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Required before marking complete.

Documentation Sync:

- Docs to update in `docs/`: Consumer-facing documentation updates are planned and must be completed in Task 4 (the adjacent documentation task). This task's implementation is the authoritative reference for those docs.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes — deferred to Task 4. Sections to update: `## Key Paths`, `## API Layer Summary`, `## Implementation Direction`.

Completion Gate:

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented (113 tests passing: 103 existing + 10 new)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to adjacent Task 4)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to Task 4)

Commit Note:

- Suggested commit scope: `src/CommandSystem.cs`, `tests/kmCommands.Tests/CustomTypeConverterTests.cs`
- Suggested commit message: `feat(custom-type-converters): implement RegisterConverter, pending-converter lifecycle, and CustomTypeConverterTests`

---

### Task 4: Update documentation and sync `projectOverview.instructions.md`

- [ ] Not started

Objective:

- Finalize all consumer-facing documentation in `docs/` and bring `.github/instructions/projectOverview.instructions.md` in sync with the completed feature. This locks down the docs deferred from Task 3.

Inputs:

- Requirements refs: REQ-1 (public API), REQ-2 (delegate contract), REQ-3 (named delegate), REQ-4 (override semantics), REQ-5 (null-input errors), REQ-6 (pre-init lifecycle), REQ-7 (Shutdown clearing), REQ-9 (failure error path).
- Design refs: `Code Examples` section (for usage examples); `Architecture Overview`; `API / Contract Sketch`; component descriptions for `TypeConverterDelegate`, `CommandSystem`, `ArgumentConverter`, and `RegistrationError`.

Implementation Steps:

1. In `docs/commands.md`, add a **Custom Type Converters** section that covers:
   - The `TypeConverterDelegate` delegate signature and semantics (`bool(string input, out object result)`).
   - The `RegisterConverter(Type, TypeConverterDelegate)` method: parameters, return type, both error codes (`NullParameters` for null type, `NullConverter` for null delegate), and override/last-write-wins behavior for built-ins.
   - Lifecycle rules: converters registered before `Initialize()` are buffered and flushed on `Initialize()`; `Shutdown()` clears all custom converters; re-registering after a new `Initialize()` cycle is supported.
   - A short code example adapted from the design's `Code Examples` section (`Vector2` custom type or equivalent).

2. In `docs/architecture.md`, update the argument-conversion component description to note:
   - The converter registry in `ArgumentConverter` is extensible via `AddConverter`.
   - `CommandSystem.RegisterConverter` drives this extension: pre-`Initialize()` registrations are buffered in `_pendingConverters` and flushed at `Initialize()`; post-`Initialize()` registrations call `AddConverter` directly.

3. In `.github/instructions/projectOverview.instructions.md`, update:
   - `## Key Paths` — add entry: `` `src/TypeConverterDelegate.cs`: public `TypeConverterDelegate` delegate for custom converter registration ``.
   - `## API Layer Summary — Registration API` — append: `` `RegisterConverter(Type, TypeConverterDelegate)` returning `RegistrationResult`; registers or overrides a converter for a given `System.Type`. ``.
   - `## Implementation Direction` — add the following entries:
     - `` `src/TypeConverterDelegate.cs`: public `TypeConverterDelegate` delegate ``
     - Note that `CommandSystem` gains `RegisterConverter` + private `_pendingConverters` pre-init buffer.
     - Note that `ArgumentConverter` gains `AddConverter(Type, TryConvertFunc)` internal method.
     - Note that `RegistrationError` gains `NullConverter` value.

Validation:

- Unit tests: N/A — documentation-only task.
- Additional checks:
  - `dotnet build` on `netstandard2.0` — 0 errors (no source files changed in this task).
  - `dotnet test` on `net8.0` — 113 tests still passing (no regression introduced).
  - Read-through of `docs/commands.md` Custom Type Converters section against Task 3's implementation to confirm accuracy.
  - Read-through of `docs/architecture.md` changes against actual `ArgumentConverter` and `CommandSystem` state.
  - Confirm `.github/instructions/projectOverview.instructions.md` lists `src/TypeConverterDelegate.cs`, `RegisterConverter`, `AddConverter`, and `NullConverter`.
- QA quick pass (`taskReviewer`): Yes.
- taskReviewer review request:
  - Review scope: `docs/commands.md`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`.
  - Primary checks: `RegisterConverter` API is documented accurately (both error codes, override semantics, lifecycle); `TypeConverterDelegate` signature is documented; pre-`Initialize()` buffering and `Shutdown()` clearing lifecycle are clearly explained; `projectOverview.instructions.md` reflects all new files and API surface introduced across Tasks 1–3.
  - Required evidence: `dotnet build` 0 errors; `dotnet test` 113 passing; reviewer confirms docs accurately describe the implemented behavior with no contradictions.
  - Blocking conditions: Any doc statement that contradicts implementation (wrong error codes, wrong lifecycle rules); `projectOverview.instructions.md` missing any of the four new items (`TypeConverterDelegate.cs`, `RegisterConverter`, `AddConverter`, `NullConverter`); any test regression.
- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Required before marking complete.

Documentation Sync:

- Docs to update in `docs/`: `docs/commands.md`, `docs/architecture.md` — this task IS the documentation implementation step.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes — this task IS the sync step.
- Sections to update: `## Key Paths`, `## API Layer Summary`, `## Implementation Direction`.

Completion Gate:

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented (N/A — documentation-only task; regression check: 113 passing)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

Commit Note:

- Suggested commit scope: `docs/commands.md`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `docs(custom-type-converters): update commands, architecture, and projectOverview`

---

## Coverage Check

- Requirements coverage:
  - [ ] Every requirement is mapped to at least one task
  - [ ] No requirement is left unplanned
- Design coverage:
  - [ ] Key design components are mapped to tasks
  - [ ] Critical design constraints are represented in validation gates
- Gaps or follow-ups:
  - None identified. All 11 requirements and all design components are covered.

### Requirements-to-Task Mapping

| Req | Description | Task(s) |
|---|---|---|
| REQ-1 | `RegisterConverter(Type, TypeConverterDelegate)` public method on `CommandSystem` | Task 3 |
| REQ-2 | Delegate signature `bool(string input, out object result)` | Task 1 (`TypeConverterDelegate`), Task 3 (used in `RegisterConverter`) |
| REQ-3 | Named public delegate type `TypeConverterDelegate` | Task 1 |
| REQ-4 | Override built-ins / last-write-wins | Task 2 (`AddConverter` indexer), Task 3 (wiring in `RegisterConverter`) |
| REQ-5 | `null` type → `NullParameters`; `null` delegate → `NullConverter` | Task 1 (`NullConverter` enum value), Task 3 (guard logic in `RegisterConverter`) |
| REQ-6 | Converters registered before `Initialize()` survive `Initialize()` | Task 3 (`_pendingConverters` buffer + flush loop in `Initialize()`) |
| REQ-7 | `Shutdown()` clears all custom converters | Task 3 (`_pendingConverters.Clear()` in `Shutdown()`) |
| REQ-8 | Commands with unsupported parameter types rejected at registration | Task 3 (test `Register_WithNoConverter_RejectsCommand`; existing `Register()` logic unchanged) |
| REQ-9 | Failed custom converter → `ArgumentConversionFailed` | Task 3 (test `Execute_FailingCustomConverter_ReturnsConversionFailed`; existing execution path unchanged) |
| REQ-10 | Thread safety by caller contract — no lock introduced | Task 3 (implementation note; design confirms single-threaded contract satisfies requirement) |
| REQ-11 | No allocations on execute hot path | Task 2 (`AddConverter` called at registration time only), Task 3 (`AdaptConverter` lambda allocated once at registration; `Execute()` path not modified) |

### Design-to-Task Mapping

| Design component | Task(s) |
|---|---|
| `src/TypeConverterDelegate.cs` — new public delegate file | Task 1 |
| `RegistrationError.NullConverter` — new enum value | Task 1 |
| `ArgumentConverter.AddConverter` — internal insert-or-replace method | Task 2 |
| `CommandSystem._pendingConverters` — pre-init buffer field | Task 3 |
| `CommandSystem.RegisterConverter` — public method with null guards and pre/post-init branches | Task 3 |
| `CommandSystem.Initialize()` — flush `_pendingConverters` into `ArgumentConverter` | Task 3 |
| `CommandSystem.Shutdown()` — `_pendingConverters.Clear()` | Task 3 |
| `AdaptConverter` private static helper — thin lambda, allocated once at registration | Task 3 |
| `tests/kmCommands.Tests/CustomTypeConverterTests.cs` — all 10 unit tests | Task 3 |
| `docs/commands.md` update — Custom Type Converters section | Task 4 |
| `docs/architecture.md` update — extensible converter registry description | Task 4 |
| `.github/instructions/projectOverview.instructions.md` sync | Task 4 |
