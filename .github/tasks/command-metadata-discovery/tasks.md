# Command Metadata / Discovery API Tasks

## Status

- [ ] Planned
- [ ] In Progress
- [ ] Completed

## Inputs

- Requirements: `.github/tasks/command-metadata-discovery/requirements.md`
- Design: `.github/tasks/command-metadata-discovery/design.md`

## Branch

- Name: `feature/command-metadata-discovery`
- Rationale: New public capability — exposes read-only command discovery to consumers without changing registration or execution behavior. Branch name is taken verbatim from `requirements.md`.
- Note: `requirements.md` uses the `feature/` prefix. The standard convention in this project is `feat_`. This deviation is preserved from `requirements.md` as source of truth; align with project convention in a future cleanup if desired.

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- Do not proceed to the next task until the current task's Completion Gate is fully satisfied.
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status → Completed` may be checked only after **all** tasks and `## Coverage Check` items are checked.

---

## Task List

### Task 1: Create `CommandMetadataSnapshot` sealed class

- [ ] Not started

**Objective:**

Create the new public `CommandMetadataSnapshot` sealed class in `src/CommandMetadataSnapshot.cs`. This is the foundational type for the discovery API — it must exist before `CommandRegistry` can build one, and before `CommandSystem` can return one.

**Inputs:**

- Requirements refs: Req 4, Req 5, Req 6, Req 9, Req 10
- Design refs: "Components — `CommandMetadataSnapshot`", "API / Contract Sketch — `CommandMetadataSnapshot`", "Implementation Notes — Snapshot isolation strategy", "Implementation Notes — `CommandMetadataSnapshot.Empty` singleton", "IL2CPP / AOT Safety Notes"

**Files to create/modify:**

- **Create:** `src/CommandMetadataSnapshot.cs`

**Implementation Steps:**

1. Create `src/CommandMetadataSnapshot.cs` with the required copyright header.
2. Declare `namespace kmCommands`.
3. Add `using System; using System.Collections.Generic;`.
4. Declare `public sealed class CommandMetadataSnapshot`.
5. Add a private `readonly Dictionary<string, CommandParameterInfo[]> _entries` field (key comparer: `StringComparer.OrdinalIgnoreCase`).
6. Add `public string[] CommandNames { get; }` — backed by the `string[]` passed to the constructor.
7. Add the internal constructor `internal CommandMetadataSnapshot(string[] names, Dictionary<string, CommandParameterInfo[]> entries)` — assigns both fields.
8. Add `internal static CommandMetadataSnapshot Empty { get; }` — initialized as a static readonly singleton with `Array.Empty<string>()` and an empty dictionary.
9. Implement `public bool TryGetParameters(string name, out CommandParameterInfo[] parameters)`:
   - If `string.IsNullOrEmpty(name)` → `parameters = null; return false`.
   - Otherwise delegate to `_entries.TryGetValue(name, out parameters)`.
10. Add XML doc comments on all public members consistent with the contract sketches in `design.md`.

**Validation:**

- Unit tests: Not applicable for this task in isolation — `CommandMetadataSnapshot` has no dependencies to mock and will be covered wholesale by Task 4's test suite. Defer to Task 4.
- Additional checks:
  - Build the solution — confirm zero compile errors and zero warnings introduced.
  - Confirm the `Empty` singleton's `CommandNames` is `Array.Empty<string>()`.
  - Confirm `TryGetParameters` returns false and null-out on the `Empty` singleton.
- QA quick pass (`taskReviewer`): Yes — after implementation, invoke `taskReviewer` with the review request below.
- taskReviewer review request:
  - Review scope: New file `src/CommandMetadataSnapshot.cs` — sealed class, internal constructor, `Empty` singleton, `TryGetParameters` method.
  - Primary checks: Copyright header present; `Empty` is a true singleton (not re-allocated on each call); `CommandNames` array is not exposed as mutable (`get;` only, assigned once in constructor); `TryGetParameters` guard for null/empty name; `Dictionary` comparer is `OrdinalIgnoreCase`; no `UnityEngine` references; no LINQ; no reflection; no unconstrained generics.
  - Required evidence: Zero build errors/warnings; reviewer confirms the type matches the API contract sketch in `design.md`.
  - Blocking conditions: Build error; missing copyright header; `Empty` re-allocated per call; `CommandNames` setter exposed; `UnityEngine` reference introduced.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve all before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None at this task — `CommandMetadataSnapshot` is an internal detail of a not-yet-wired API. Documentation updates are deferred to Task 5 after the full API surface is wired.
- Update `.github/instructions/projectOverview.instructions.md` required: No — the type is added but not yet part of the wired public API. Update deferred to Task 5.

**Completion Gate:**

- [ ] `src/CommandMetadataSnapshot.cs` created with the copyright header
- [ ] `CommandNames` property is `public string[] CommandNames { get; }` (no setter)
- [ ] `TryGetParameters` correctly returns false/null for null or empty name
- [ ] `Empty` singleton constructed once at type initialization; `CommandNames` is `Array.Empty<string>()`
- [ ] Internal constructor accepts `string[]` and `Dictionary<string, CommandParameterInfo[]>`
- [ ] `Dictionary` uses `StringComparer.OrdinalIgnoreCase`
- [ ] No `UnityEngine` references, no LINQ, no reflection
- [ ] Solution builds with zero errors and zero new warnings
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented (deferred to Task 5)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to Task 5)

**Commit Note:**

- Suggested commit scope: `src/CommandMetadataSnapshot.cs`
- Suggested commit message: `feat: add CommandMetadataSnapshot sealed class`

---

### Task 2: Add discovery methods to `CommandRegistry`

- [ ] Not started

**Objective:**

Add `GetAllNames()` and `BuildSnapshot()` internal methods to `CommandRegistry`. These are the data-layer building blocks for the public discovery API and must exist before `CommandSystem` can delegate to them.

**Inputs:**

- Requirements refs: Req 1, Req 4, Req 5, Req 8, Req 10
- Design refs: "Components — `CommandRegistry`", "API / Contract Sketch — New internal methods on `CommandRegistry`", "Data Flow — `GetCommandNames()` flow", "Data Flow — `GetSnapshot()` flow", "Code Examples — `GetAllNames()`, `BuildSnapshot()`", "IL2CPP / AOT Safety Notes"

**Files to create/modify:**

- **Modify:** `src/Core/CommandRegistry.cs`

**Implementation Steps:**

1. Add `GetAllNames()` internal method to `CommandRegistry`:
   - If `_commands.Count == 0`, return `Array.Empty<string>()`.
   - Allocate `string[Count]`.
   - Iterate `_commands` with `foreach (KeyValuePair<string, CommandDefinition> pair in _commands)` — capture `pair.Value.Name` (original casing from `CommandDefinition`).
   - Call `Array.Sort(names, StringComparer.OrdinalIgnoreCase)`.
   - Return the sorted array.
2. Add `BuildSnapshot()` internal method to `CommandRegistry`:
   - If `_commands.Count == 0`, return `CommandMetadataSnapshot.Empty`.
   - Allocate `string[Count]` for names.
   - Allocate `new Dictionary<string, CommandParameterInfo[]>(count, StringComparer.OrdinalIgnoreCase)` for entries.
   - Iterate `_commands`: capture `def.Name` into names; perform `Array.Copy(def.Parameters, paramsCopy, def.Parameters.Length)` for a structural copy; add to dictionary.
   - Call `Array.Sort(names, StringComparer.OrdinalIgnoreCase)`.
   - Return `new CommandMetadataSnapshot(names, entries)`.
3. Add `using kmCommands;` to `CommandRegistry.cs` if not already present (needed to reference `CommandMetadataSnapshot`).
4. Add XML doc comments on both new methods consistent with contract sketch in `design.md`.

**Validation:**

- Unit tests: Not applicable for this task in isolation — `CommandRegistry` internal methods will be exercised through `CommandSystem` in Task 4's tests. Verify basic logic correctness by inspection and build confirmation here.
- Additional checks:
  - Build the solution — zero compile errors and zero warnings.
  - Manually review that `BuildSnapshot()` copies the `CommandParameterInfo[]` array (not just copies the reference to `def.Parameters`).
  - Confirm `BuildSnapshot()` returns `CommandMetadataSnapshot.Empty` unconditionally when `Count == 0` (reuses singleton, no extra allocation).
  - Confirm `GetAllNames()` returns `Array.Empty<string>()` when `Count == 0`.
- QA quick pass (`taskReviewer`): Yes — after implementation, invoke `taskReviewer` with the review request below.
- taskReviewer review request:
  - Review scope: Two new internal methods in `src/Core/CommandRegistry.cs` — `GetAllNames()` and `BuildSnapshot()`.
  - Primary checks: `BuildSnapshot()` performs structural copy (`Array.Copy`) not reference aliasing of `def.Parameters`; sort is `OrdinalIgnoreCase` on both methods; empty fast-paths return singletons; `foreach`/`KeyValuePair` is AOT-safe; no LINQ; no reflection; `CommandParameterInfo` instances themselves are shared (not deep-copied); `CommandMetadataSnapshot.Empty` is used (not `new CommandMetadataSnapshot(empty, empty)`).
  - Required evidence: Zero build errors/warnings; reviewer confirms snapshot structural copy behavior from code inspection.
  - Blocking conditions: Build error; `def.Parameters` reference copied directly (not structurally copied); LINQ used; sort order incorrect or missing; `CommandMetadataSnapshot` constructed with `new` for empty case instead of `.Empty` singleton.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve all before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — these are internal methods. Documentation deferred to Task 5.
- Update `.github/instructions/projectOverview.instructions.md` required: No — internal additions. Deferred to Task 5.

**Completion Gate:**

- [ ] `GetAllNames()` added to `CommandRegistry`, returns `Array.Empty<string>()` when empty, sorted `OrdinalIgnoreCase` otherwise
- [ ] `BuildSnapshot()` added to `CommandRegistry`, returns `CommandMetadataSnapshot.Empty` when registry is empty
- [ ] `BuildSnapshot()` performs `Array.Copy` for parameter arrays (structural copy, not reference aliasing)
- [ ] Both methods use no LINQ, no reflection, no `UnityEngine` references
- [ ] Solution builds with zero errors and zero new warnings
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented (deferred to Task 5)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to Task 5)

**Commit Note:**

- Suggested commit scope: `src/Core/CommandRegistry.cs`
- Suggested commit message: `feat: add GetAllNames and BuildSnapshot to CommandRegistry`

---

### Task 3: Add public discovery methods to `CommandSystem`

- [ ] Not started

**Objective:**

Wire the three new public discovery methods — `GetCommandNames()`, `TryGetCommandParameters()`, `GetSnapshot()` — onto `CommandSystem`. This completes the public API surface for the feature.

**Inputs:**

- Requirements refs: Req 1, Req 2, Req 3, Req 4, Req 6, Req 7, Req 10
- Design refs: "Components — `CommandSystem`", "API / Contract Sketch — New public methods on `CommandSystem`", "Data Flow — all three flows", "Implementation Notes — Guard pattern consistency", "Allocation Analysis", "IL2CPP / AOT Safety Notes"

**Files to create/modify:**

- **Modify:** `src/CommandSystem.cs`

**Implementation Steps:**

1. Add `GetCommandNames()` public method:
   - Guard: `if (!IsInitialized) return Array.Empty<string>();`
   - Delegate: `return _registry.GetAllNames();`
   - Include XML doc comment from design.md API contract sketch.
2. Add `TryGetCommandParameters(string name, out CommandParameterInfo[] parameters)` public method:
   - Guard: `if (!IsInitialized || string.IsNullOrEmpty(name)) { parameters = null; return false; }`
   - Delegate to `_registry.TryGetCommand(name, out CommandDefinition definition)`.
   - If not found: `parameters = null; return false`
   - If found: `parameters = definition.Parameters; return true`
   - Include XML doc comment noting the returned array is the same instance stored in the registry — do not mutate.
3. Add `GetSnapshot()` public method:
   - Guard: `if (!IsInitialized) return CommandMetadataSnapshot.Empty;`
   - Delegate: `return _registry.BuildSnapshot();`
   - Include XML doc comment from design.md API contract sketch.
4. Verify that all three methods follow the existing guard pattern in `CommandSystem` (no exceptions thrown, consistent with `Execute()` / `Register()` guard style).
5. Do not modify any existing method signatures, behaviors, or XML docs.

**Validation:**

- Unit tests: Not yet added — deferred to Task 4. Validate via build-pass and spot-check here.
- Additional checks:
  - Build the solution — zero compile errors and zero warnings.
  - Confirm all three methods appear in the `CommandSystem` public API (not internal, not protected).
  - Confirm that `TryGetCommandParameters` uses `_registry.TryGetCommand` (the already-existing method) rather than reimplementing lookup.
  - Confirm that none of the three methods throw when `IsInitialized` is false.
  - Run existing test suite (`kmCommands.Tests`) — all 82 passing tests must still pass.
- QA quick pass (`taskReviewer`): Yes — after implementation, invoke `taskReviewer` with the review request below.
- taskReviewer review request:
  - Review scope: Three new public methods added to `src/CommandSystem.cs`.
  - Primary checks: Guard pattern matches existing `CommandSystem` convention (no throw); `TryGetCommandParameters` delegates lookup to `TryGetCommand` (not re-implementing); `GetCommandNames` returns `Array.Empty<string>()` not null pre-init; `GetSnapshot` returns `CommandMetadataSnapshot.Empty` not null pre-init; `TryGetCommandParameters` null/empty name guard present; all XML docs match design contract sketch; no `UnityEngine` references introduced; existing tests remain green.
  - Required evidence: Zero build errors/warnings; all pre-existing 82 tests pass; reviewer confirmation of guard consistency.
  - Blocking conditions: Build error; any existing test broken; `null` returned instead of `Array.Empty<string>()` or `CommandMetadataSnapshot.Empty` in guard paths; exception thrown in any guard path.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve all before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None yet — documentation updates are co-located in Task 5 after the full API surface and tests are complete.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to Task 5.

**Completion Gate:**

- [ ] `GetCommandNames()` added to `CommandSystem`: guards on `!IsInitialized`, delegates to `_registry.GetAllNames()`
- [ ] `TryGetCommandParameters()` added to `CommandSystem`: guards on `!IsInitialized` and null/empty name, delegates to `_registry.TryGetCommand()`, returns `definition.Parameters` ref on success
- [ ] `GetSnapshot()` added to `CommandSystem`: guards on `!IsInitialized`, delegates to `_registry.BuildSnapshot()`
- [ ] No exceptions thrown from any guard path
- [ ] No existing `CommandSystem` method signatures or behaviors changed
- [ ] All 82 pre-existing tests remain passing
- [ ] Solution builds with zero errors and zero new warnings
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented (deferred to Task 5)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to Task 5)

**Commit Note:**

- Suggested commit scope: `src/CommandSystem.cs`
- Suggested commit message: `feat: add GetCommandNames, TryGetCommandParameters, GetSnapshot to CommandSystem`

---

### Task 4: Write unit tests for the discovery API

- [ ] Not started

**Objective:**

Create `tests/kmCommands.Tests/CommandMetadataDiscoveryTests.cs` covering all test cases specified in `design.md`'s Testing Strategy. This validates the full feature end-to-end and confirms snapshot isolation, case-insensitivity, guard behavior, and zero-argument-command handling.

**Inputs:**

- Requirements refs: Req 1, Req 2, Req 3, Req 4, Req 5, Req 6 (Testing Expectations section), Acceptance Overview
- Design refs: "Testing Strategy" — all three test tables (`GetCommandNames`, `TryGetCommandParameters`, `GetSnapshot`)

**Files to create/modify:**

- **Create:** `tests/kmCommands.Tests/CommandMetadataDiscoveryTests.cs`

**Implementation Steps:**

1. Create `tests/kmCommands.Tests/CommandMetadataDiscoveryTests.cs`.
   - Match the existing test fixture pattern: `[SetUp]` creates and initializes `CommandSystem`; `[TearDown]` calls `Shutdown()`.
   - Use NUnit attributes (`[TestFixture]`, `[Test]`, `[SetUp]`, `[TearDown]`).
2. Implement all `GetCommandNames` tests (5 tests):
   - `GetCommandNames_BeforeInit_ReturnsEmptyArray`
   - `GetCommandNames_InitNoCommands_ReturnsEmptyArray`
   - `GetCommandNames_WithRegisteredCommands_ReturnsAllNames`
   - `GetCommandNames_NamesAreSortedOrdinalIgnoreCase`
   - `GetCommandNames_AfterShutdown_ReturnsEmptyArray`
3. Implement all `TryGetCommandParameters` tests (7 tests):
   - `TryGetCommandParameters_BeforeInit_ReturnsFalse`
   - `TryGetCommandParameters_NullName_ReturnsFalse`
   - `TryGetCommandParameters_UnknownCommand_ReturnsFalse`
   - `TryGetCommandParameters_KnownCommand_ReturnsTrueAndParams`
   - `TryGetCommandParameters_IsCaseInsensitive`
   - `TryGetCommandParameters_EmptyParams_ReturnsEmptyArray`
   - `TryGetCommandParameters_AfterShutdown_ReturnsFalse`
4. Implement all `GetSnapshot` tests (9 tests):
   - `GetSnapshot_BeforeInit_ReturnsEmptySnapshot`
   - `GetSnapshot_NoCommands_ReturnsEmptyCommandNames`
   - `GetSnapshot_CommandNames_ContainsAllRegisteredNames`
   - `GetSnapshot_TryGetParameters_ReturnsCorrectParameters`
   - `GetSnapshot_TryGetParameters_IsCaseInsensitive`
   - `GetSnapshot_TryGetParameters_UnknownCommand_ReturnsFalse`
   - `GetSnapshot_IsIsolatedFromSubsequentRegistrations`
   - `GetSnapshot_ParameterArray_IsStructurallyCopied`
   - `GetSnapshot_AfterShutdown_ReturnsEmptySnapshot`
5. For `GetSnapshot_IsIsolatedFromSubsequentRegistrations`: register command A, take snapshot, register command B, assert snapshot `CommandNames` does not contain B.
6. For `GetSnapshot_ParameterArray_IsStructurallyCopied`: register a command with parameters, take snapshot, retrieve the snapshot's parameter array via `TryGetParameters`, assert it is not the same object reference as retrieved via `TryGetCommandParameters` from `CommandSystem` (verifying structural copy, not aliasing).
7. Ensure tests use only public API (`CommandSystem` methods, `CommandParameterInfo`, `CommandMetadataSnapshot`) — no internal type access.
8. Follow no-LINQ pattern in test code consistent with project conventions.

**Validation:**

- Unit tests: This task IS the unit test creation. Run the full `kmCommands.Tests` suite after adding the file.
- Additional checks:
  - All 21 new tests pass.
  - All 82 pre-existing tests continue to pass (total: 103 passing).
  - Zero test warnings or skips.
- QA quick pass (`taskReviewer`): Yes — after test suite passes, invoke `taskReviewer` with the review request below.
- taskReviewer review request:
  - Review scope: New file `tests/kmCommands.Tests/CommandMetadataDiscoveryTests.cs` — 21 new NUnit tests.
  - Primary checks: All 21 test names from `design.md` Testing Strategy are present; `[SetUp]`/`[TearDown]` lifecycle matches existing fixture pattern; snapshot isolation test (`GetSnapshot_IsIsolatedFromSubsequentRegistrations`) registers command B after snapshot and correctly asserts absence in snapshot; structural copy test (`GetSnapshot_ParameterArray_IsStructurallyCopied`) asserts reference inequality between snapshot array and live registry array; before-init and after-shutdown tests do not call `Initialize()` or call `Shutdown()` before exercising the guard; case-insensitive tests use distinct casing (not just same string); zero-param test registers a zero-parameter command; no internal type access.
  - Required evidence: Test runner output showing 103 passing, 0 failing, 0 skipped.
  - Blocking conditions: Any test failure; any pre-existing test broken; missing test case from design.md table; `Initialize()` called in a before-init guard test; internal type access in tests.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve all before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None at this task — documentation update is in Task 5.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to Task 5.

**Completion Gate:**

- [ ] `CommandMetadataDiscoveryTests.cs` created with all 21 tests from `design.md` Testing Strategy
- [ ] All 21 new tests pass
- [ ] All 82 pre-existing tests continue to pass (103 total)
- [ ] Zero test warnings or skips
- [ ] Snapshot isolation test correctly registers a second command after snapshot and asserts absence
- [ ] Structural copy test asserts reference inequality between snapshot array and live registry array
- [ ] No internal types accessed from test code
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented (deferred to Task 5)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to Task 5)

**Commit Note:**

- Suggested commit scope: `tests/kmCommands.Tests/CommandMetadataDiscoveryTests.cs`
- Suggested commit message: `test: add CommandMetadataDiscoveryTests covering GetCommandNames, TryGetCommandParameters, GetSnapshot`

---

### Task 5: Update documentation and project overview

- [ ] Not started

**Objective:**

Update `docs/commands.md`, `docs/architecture.md`, and `.github/instructions/projectOverview.instructions.md` to reflect the new discovery API and the `CommandMetadataSnapshot` type. Ensure consumers and future agents have accurate reference material.

**Inputs:**

- Requirements refs: Req 1–6 (acceptance overview), Req 9
- Design refs: "API / Contract Sketch", "Allocation Analysis", "Risks and Tradeoffs" (consumer guidance), "Code Examples — Consumer usage"

**Files to create/modify:**

- **Modify:** `docs/commands.md`
- **Modify:** `docs/architecture.md`
- **Modify:** `.github/instructions/projectOverview.instructions.md`

**Implementation Steps:**

1. **`docs/commands.md`** — Add a new section covering the discovery API:
   - Document `GetCommandNames()`: purpose, return type, sort order, guidance that it allocates per call and callers should use `GetSnapshot()` if called frequently.
   - Document `TryGetCommandParameters(name, out parameters)`: purpose, case-insensitivity, caveat that the returned array is the same reference held by the registry (do not mutate).
   - Document `GetSnapshot()`: purpose, snapshot isolation contract (subsequent registrations do not affect an already-taken snapshot), `CommandMetadataSnapshot.TryGetParameters()` usage.
   - Document before-init / after-shutdown safe return behavior for all three methods.
   - Include a short consumer usage code example consistent with the `design.md` "Consumer usage" example.
2. **`docs/architecture.md`** — Add or update relevant sections:
   - Add `CommandMetadataSnapshot` to the component list with a one-line description.
   - Note the discovery data flow: `CommandSystem` → `CommandRegistry.GetAllNames()` / `BuildSnapshot()` → `CommandMetadataSnapshot`.
   - Note allocation profile (bounded by registry size, outside execution hot path).
3. **`.github/instructions/projectOverview.instructions.md`** — Update:
   - Under "Implementation Direction": add `src/CommandMetadataSnapshot.cs` — public `CommandMetadataSnapshot` sealed class capturing immutable registry snapshot.
   - Under "API Layer Summary": add Discovery API — `GetCommandNames()`, `TryGetCommandParameters()`, `GetSnapshot()`.
   - Under "Key Paths": add `src/CommandMetadataSnapshot.cs`.
   - Under "Systems In Action" or equivalent: note that `CommandRegistry` now also provides `GetAllNames()` and `BuildSnapshot()` for discovery.

**Validation:**

- Unit tests: Not applicable — documentation changes.
- Additional checks:
  - Re-read all three updated files after edits and verify accuracy against `design.md`.
  - Confirm the consumer usage example in `docs/commands.md` is valid C# (matches the actual public API signatures).
  - Confirm no documentation references internal types (`CommandDefinition`, `CommandRegistry`) from a consumer-facing perspective.
  - Run the full test suite one final time — 103 passing, 0 failing.
- QA quick pass (`taskReviewer`): Yes — invoke `taskReviewer` with the review request below.
- taskReviewer review request:
  - Review scope: Documentation updates to `docs/commands.md`, `docs/architecture.md`, and `.github/instructions/projectOverview.instructions.md`.
  - Primary checks: All three new public methods documented in `docs/commands.md`; mutation caveat for `TryGetCommandParameters` array present; snapshot isolation contract documented; before-init/after-shutdown safe behavior documented; `CommandMetadataSnapshot` listed in `architecture.md`; `projectOverview.instructions.md` reflects new file, new API methods, and registry additions; no internal types exposed in consumer docs; consumer code example compiles against actual API signatures.
  - Required evidence: Text review confirmation; final test suite run showing 103 passing.
  - Blocking conditions: Any of the three new public methods missing from `docs/commands.md`; mutation caveat absent; `projectOverview.instructions.md` not updated; consumer example uses wrong method signatures.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve all before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md`, `docs/architecture.md` — this task IS the documentation update.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes.
- Sections to update: "Implementation Direction", "API Layer Summary", "Key Paths", "Systems In Action".

**Completion Gate:**

- [ ] `docs/commands.md` updated with discovery API section covering all three methods, mutation caveat, snapshot isolation contract, before-init/after-shutdown behavior, and consumer usage example
- [ ] `docs/architecture.md` updated with `CommandMetadataSnapshot` component entry and discovery data flow note
- [ ] `.github/instructions/projectOverview.instructions.md` updated: `src/CommandMetadataSnapshot.cs` in Implementation Direction and Key Paths; discovery API methods in API Layer Summary; registry additions noted
- [ ] Consumer code example in docs matches actual public API signatures
- [ ] No internal type names exposed in consumer-facing documentation
- [ ] Final test suite run: 103 passing, 0 failing
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed

**Commit Note:**

- Suggested commit scope: `docs/`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `docs: document discovery API, CommandMetadataSnapshot, and update projectOverview`

---

## Coverage Check

### Requirements coverage

- [ ] **Req 1** — Expose all registered command names as read-only collection of strings → Task 1 (type), Task 2 (`GetAllNames`), Task 3 (`GetCommandNames`), Task 4 (tests), Task 5 (docs)
- [ ] **Req 2** — Expose parameter descriptors for a specific command by name (case-insensitive) → Task 3 (`TryGetCommandParameters`), Task 4 (tests), Task 5 (docs)
- [ ] **Req 3** — Structured result when command not found (no throw, defined false return) → Task 3 (guard + `TryGetCommand` delegation), Task 4 (`UnknownCommand` and null-name tests)
- [ ] **Req 4** — Read-only snapshot of full registry state → Task 1 (`CommandMetadataSnapshot`), Task 2 (`BuildSnapshot`), Task 3 (`GetSnapshot`), Task 4 (snapshot tests)
- [ ] **Req 5** — Snapshot safe to store across registrations (isolated, not live) → Task 2 (`Array.Copy` structural copy), Task 4 (`GetSnapshot_IsIsolatedFromSubsequentRegistrations`, `GetSnapshot_ParameterArray_IsStructurallyCopied`)
- [ ] **Req 6** — All methods safe before `Initialize()` and after `Shutdown()` — no throw → Task 3 (guard pattern), Task 4 (before-init and after-shutdown tests for all three methods)
- [ ] **Req 7** — No new `UnityEngine` dependency → Task 1 (gate check), Task 2 (gate check), Task 3 (gate check)
- [ ] **Req 8** — Allocations bounded by registry size; nothing in execution hot path → Task 2 (foreach/array, no per-frame use), Task 3 (`TryGetCommandParameters` zero allocation path), Task 5 (allocation guidance in docs)
- [ ] **Req 9** — Copyright header on all new `src/` files → Task 1 (gate check on `CommandMetadataSnapshot.cs`)
- [ ] **Req 10** — IL2CPP/AOT-safe patterns throughout → Task 1, Task 2, Task 3 (gate checks: no LINQ, no reflection, no unconstrained generics, concrete `Dictionary` + `foreach`)
- [ ] Every requirement is mapped to at least one task
- [ ] No requirement is left unplanned

### Design coverage

- [ ] `CommandMetadataSnapshot` sealed class with internal constructor and `Empty` singleton → Task 1
- [ ] `CommandRegistry.GetAllNames()` with `OrdinalIgnoreCase` sort and `Array.Empty` fast-path → Task 2
- [ ] `CommandRegistry.BuildSnapshot()` with structural copy (`Array.Copy`) and `Empty` fast-path → Task 2
- [ ] `CommandSystem.GetCommandNames()` with initialization guard and delegation → Task 3
- [ ] `CommandSystem.TryGetCommandParameters()` with null/empty-name guard, not-found path, and live-array-ref return → Task 3
- [ ] `CommandSystem.GetSnapshot()` with initialization guard and delegation → Task 3
- [ ] All 21 test cases from design.md Testing Strategy tables → Task 4
- [ ] Consumer documentation with mutation caveat and snapshot isolation contract → Task 5
- [ ] `projectOverview.instructions.md` updated with new file, new API surface, registry additions → Task 5
- [ ] Key design components are mapped to tasks
- [ ] Critical design constraints (AOT safety, no LINQ, bounded allocation, no `UnityEngine`) are represented in validation gates

### Gaps or follow-ups

- **`TryGetCommandParameters` mutation risk:** The design explicitly accepts this tradeoff (zero-allocation priority). The documentation caveat in Task 5 is the mitigation. No code-level guard is added (consistent with design decision).
- **`GetCommandNames()` per-keystroke allocation warning:** Documented in Task 5 consumer guidance. No API change needed.
- **Help text / aliases / live observation:** Explicitly out of scope per `requirements.md`. Not planned.
- **Branch name deviation (`feature/` vs `feat_`):** Noted in `## Branch` section. Preserved from `requirements.md` as source of truth. No task needed; flagged for convention alignment if desired.
