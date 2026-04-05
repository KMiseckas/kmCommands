# Command History Tasks

## Status

- [ ] Planned
- [ ] In Progress
- [ ] Completed

## Inputs

- Requirements: `.github/tasks/command-history/requirements.md`
- Design: `.github/tasks/command-history/design.md`

## Branch

- Planning branch: `plan/command-history`
- Implementation branch: `feat/command-history` (branched from `main`; created by developer agent)
- Rationale: `feat_` prefix — new user-facing capability added to public API

> **Note:** `requirements.md` records the implementation branch as `feat_command-history`. The authoritative name for implementation is `feat/command-history` as directed by the task request. The developer agent must branch from `main`.

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit on `feat/command-history`.
- Keep commits scoped to the task objective.
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status -> Completed` may be checked only after all tasks and `## Coverage Check` items are checked.
- All new source files in `src/` must carry the required Apache 2.0 file header.
- All public API members must have XML doc comments following the style in `CommandSystem.cs`.

---

## Task List

### T-01: Add `CommandHistoryEntry` public readonly struct

- [ ] Not started

**Objective:**

Create `src/CommandHistoryEntry.cs` — the public value-type that represents a single recorded history entry. This type is the data contract between the internal buffer and callers of `GetHistory()`.

**Files to create / modify:**

- Create: `src/CommandHistoryEntry.cs`

**Implementation Notes:**

- Declare `public readonly struct CommandHistoryEntry` in the `kmCommands` namespace.
- Two auto-properties: `public string CommandName { get; }` and `public string[] Args { get; }`.
- Provide one `internal` constructor: `internal CommandHistoryEntry(string commandName, string[] args)`.
- `CommandName` and `Args` are assigned in the constructor with no defensive copying — the caller (`CommandHistoryBuffer`) is responsible for passing an already-copied args array.
- `Args` must never be null from the constructor's perspective (the buffer always passes a non-null copy).
- Add required Apache 2.0 file header.
- Add XML doc comments on the type, both properties, and the constructor following the API contract sketch in `design.md` §API/Contract Sketch.

**Inputs:**

- Requirements refs: REQ-1 (entry shape), REQ-4 (documented default), REQ-12 (AOT safe)
- Design refs: §Resolved Open Questions #4 (readonly struct decision), §Components/CommandHistoryEntry, §API/Contract Sketch

**Validation:**

- Unit tests: No dedicated unit tests at this task stage — struct is tested transitively through buffer and system tests in T-05.
- Build check: Project (`kmCommands.csproj`) builds without errors after file is added.
- Sanity: `CommandHistoryEntry` is accessible from the `kmCommands` namespace; constructor is `internal`.

- QA quick pass (`taskReviewer`):
  - Review scope: New `src/CommandHistoryEntry.cs` only.
  - Primary checks: `readonly struct` declaration; no public constructor; `Args` and `CommandName` are get-only; file header present; XML docs present and accurate.
  - Required evidence: Successful build output.
  - Blocking conditions: Public constructor exposed; `Args` can be set externally; missing file header; LINQ or reflection in file.

- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Must be resolved before completion gate.

**Documentation Sync:**

- Docs to update in `docs/`: None at this task — type is documented in T-06 after the full API is in place.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to T-06 when all new types and API are stable.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (project builds without errors)
- [ ] Unit tests passed or exception documented (N/A — transitively covered in T-05)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-06)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to T-06)

**Commit Note:**

- Suggested commit scope: `src/CommandHistoryEntry.cs`
- Suggested commit message: `feat(command-history): add CommandHistoryEntry readonly struct`

---

### T-02: Add `CommandHistoryBuffer` internal class

- [ ] Not started

**Objective:**

Create `src/Core/CommandHistoryBuffer.cs` — the fixed-capacity ring-buffer that stores and manages `CommandHistoryEntry` values. This is the core data structure for the feature.

**Files to create / modify:**

- Create: `src/Core/CommandHistoryBuffer.cs`
- Depends on: T-01 (`CommandHistoryEntry` must exist)

**Implementation Notes:**

- Declare `internal sealed class CommandHistoryBuffer` in the `kmCommands.Core` namespace.
- Fields: `private readonly CommandHistoryEntry[] _buffer; private readonly int _capacity; private int _head; private int _count;`
- Constructor: `internal CommandHistoryBuffer(int capacity)` — allocates `_buffer` once, sets `_capacity`, initializes `_head = 0`, `_count = 0`.
- `internal void Record(string commandName, string[] args)`:
  - Call `CopyArgs(args)` to get an independent copy.
  - Construct `new CommandHistoryEntry(commandName, argsCopy)`.
  - If `_count < _capacity`: write to `_buffer[(_head + _count) % _capacity]`, then `_count++`.
  - Else (buffer full): write to `_buffer[_head]`, then `_head = (_head + 1) % _capacity`.
- `internal CommandHistoryEntry[] GetSnapshot()`:
  - If `_count == 0`: return `Array.Empty<CommandHistoryEntry>()`.
  - Allocate `new CommandHistoryEntry[_count]`.
  - Fill in oldest-to-newest order using `_buffer[(_head + i) % _capacity]` for `i` in `[0, _count)`.
  - Return the array.
- `internal int Count { get { return _count; } }`
- `internal void Clear()`: `_head = 0; _count = 0;` — no need to zero-fill; `_count` controls validity.
- `private static string[] CopyArgs(string[] args)`: return `Array.Empty<string>()` if `args == null || args.Length == 0`; otherwise allocate and `Array.Copy`.
- No LINQ; no reflection; `Array.Copy` is AOT-safe.
- Add required Apache 2.0 file header.
- `CopyArgs` is a private static helper — no allocation on re-call of the method; allocation is data-driven per `args.Length`.

**Inputs:**

- Requirements refs: REQ-2 (ordered oldest→newest), REQ-3 (oldest eviction), REQ-12 (AOT safe), REQ-13 (allocation discipline)
- Design refs: §Data Flow / Ring buffer index arithmetic, §Implementation Notes, §Components/CommandHistoryBuffer

**Validation:**

- Unit tests: No dedicated unit tests at this task stage — tested transitively in T-05. However, a smoke test calling `Record()` and `GetSnapshot()` directly may be written if the reviewer requires it.
- Build check: Project builds without errors after file is added.
- Sanity: Class is `internal sealed`; `Count` property accessible from `CommandSystem`; `Record`, `GetSnapshot`, `Clear` callable from `CommandSystem`.

- QA quick pass (`taskReviewer`):
  - Review scope: New `src/Core/CommandHistoryBuffer.cs` only.
  - Primary checks: `internal sealed class`; ring buffer write-index arithmetic correct for both full and non-full paths; `GetSnapshot` traversal order produces oldest-first; `CopyArgs` returns `Array.Empty` for null/empty input; `Clear` resets `_head` and `_count`; no LINQ; no allocation beyond data-driven `new string[]` and `new CommandHistoryEntry[]`; file header present.
  - Required evidence: Successful build output.
  - Blocking conditions: Ring buffer index arithmetic incorrect; full-buffer eviction path missing or wrong; `GetSnapshot` ordering incorrect; LINQ present; public access modifier on class.

- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Must be resolved before completion gate.

**Documentation Sync:**

- Docs to update in `docs/`: None — internal class, no public-facing doc needed.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to T-06.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (project builds without errors)
- [ ] Unit tests passed or exception documented (N/A — transitively covered in T-05)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A — internal class)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to T-06)

**Commit Note:**

- Suggested commit scope: `src/Core/CommandHistoryBuffer.cs`
- Suggested commit message: `feat(command-history): add CommandHistoryBuffer ring-buffer implementation`

---

### T-03: Integrate history buffer into `CommandSystem` core (field, constant, Initialize, Shutdown, Execute)

- [ ] Not started

**Objective:**

Wire `CommandHistoryBuffer` into `CommandSystem`: add the backing field and default-capacity constant, add the `Initialize(int historyCapacity)` overload, update the no-arg `Initialize()` to use the constant, update `Shutdown()` to release the buffer, and update `Execute()` to record successful executions.

**Files to create / modify:**

- Modify: `src/CommandSystem.cs`
- Depends on: T-02 (`CommandHistoryBuffer` must exist)

**Implementation Notes:**

- Add field: `private CommandHistoryBuffer _historyBuffer;` (alongside existing private fields).
- Add constant: `public const int DefaultHistoryCapacity = 64;` with XML doc comment matching the design's contract sketch.
- Add `Initialize(int historyCapacity)` overload:
  - Guard: `if (IsInitialized) return;`
  - Clamp: `int effectiveCapacity = historyCapacity < 1 ? 1 : historyCapacity;`
  - Construct existing internal components (`_registry`, `_converter`, `_executionHandler`, `_attributeScanner`).
  - Flush `_pendingConverters` exactly as the existing `Initialize()` does.
  - `_historyBuffer = new CommandHistoryBuffer(effectiveCapacity);`
  - `IsInitialized = true;`
- Update existing `Initialize()` (no-arg):
  - Keep it as a symmetric overload. **Do not delegate to `Initialize(int)`** — keep both overloads explicit with the idempotent guard at their own top, per the design note: "Prefer the two symmetric overloads without cross-delegation".
  - Add `_historyBuffer = new CommandHistoryBuffer(DefaultHistoryCapacity);` before `IsInitialized = true;`.
- Update `Shutdown()`:
  - Add `_historyBuffer = null;` alongside the existing nulling of other fields.
- Update `Execute(string commandName, string[] args)`:
  - After `ExecutionResult result = _executionHandler.Execute(commandName, args);`, add:
    ```
    if (result.Success)
    {
        _historyBuffer.Record(commandName, args);
    }
    ```
  - Return `result` unchanged.
- XML doc for `Initialize(int historyCapacity)` must document the `historyCapacity` parameter and the clamping behavior, matching the design contract sketch.

**Inputs:**

- Requirements refs: REQ-3 (configurable capacity), REQ-4 (default capacity = 64), REQ-5 (min capacity ≥ 1 clamped), REQ-9 (Execute integration), REQ-10 (Shutdown resets)
- Design refs: §Resolved Open Questions #2 (Initialize overload), §Resolved Open Questions #1 (success-only), §Architecture Overview, §CommandSystem changes, §Idempotent Initialize interaction, §CommandSystem.Execute integration

**Validation:**

- Build check: Project builds without errors after modifications.
- Unit tests: Not added at this task — covered in T-05. If individual integration smoke tests exist in existing files that touch `Execute`, they must still pass (run `dotnet test` against existing test suite).
- Existing tests: All 139 existing tests must continue to pass after modifications.

- QA quick pass (`taskReviewer`):
  - Review scope: `src/CommandSystem.cs` diff for T-03 changes only.
  - Primary checks: `DefaultHistoryCapacity = 64` is `public const int`; `Initialize(int)` clamps `< 1` capacity to 1; no-arg `Initialize()` passes `DefaultHistoryCapacity`; neither overload delegates to the other (symmetric); `Shutdown()` nulls `_historyBuffer`; `Execute()` records only on `result.Success`; `ExecutionHandler` remains history-unaware; all existing `CommandSystem` behavior is unchanged.
  - Required evidence: `dotnet test` output showing all existing 139 tests pass; successful build.
  - Blocking conditions: `Initialize(int)` delegates to no-arg overload or vice versa creating cross-delegation; capacity clamping missing; `Execute()` records on failures; `ExecutionResult` shape changed; existing tests broken.

- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Must be resolved before completion gate.

**Documentation Sync:**

- Docs to update in `docs/`: None at this task — full docs sync in T-06.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to T-06.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (project builds without errors; existing 139 tests pass)
- [ ] Unit tests passed or exception documented (existing tests pass; new tests in T-05)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-06)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to T-06)

**Commit Note:**

- Suggested commit scope: `src/CommandSystem.cs`
- Suggested commit message: `feat(command-history): wire CommandHistoryBuffer into CommandSystem core`

---

### T-04: Add public history API to `CommandSystem` (`GetHistory`, `HistoryCount`, `ClearHistory`)

- [ ] Not started

**Objective:**

Expose the three public history API members on `CommandSystem`: `GetHistory()` method, `HistoryCount` property, and `ClearHistory()` method. Add pre-initialization guards consistent with existing API patterns.

**Files to create / modify:**

- Modify: `src/CommandSystem.cs`
- Depends on: T-03

**Implementation Notes:**

- Add `public int HistoryCount`:
  - Get-only property.
  - Returns `_historyBuffer != null ? _historyBuffer.Count : 0`.
  - Consistent with pre-init guard for other read operations (e.g., `GetCommandNames()`).
  - XML doc must document the "Returns 0 when not initialized" contract.
- Add `public CommandHistoryEntry[] GetHistory()`:
  - If `_historyBuffer == null`, return `Array.Empty<CommandHistoryEntry>()`.
  - Otherwise return `_historyBuffer.GetSnapshot()`.
  - XML doc must document snapshot independence and empty-result guarantee.
- Add `public void ClearHistory()`:
  - If `_historyBuffer == null`, return (no-op).
  - Otherwise call `_historyBuffer.Clear()`.
  - XML doc must document the no-op pre-init behavior.
- Place the three members logically near the history-related API region in `CommandSystem.cs` (after `Execute`, before `Scan`, or as a grouped region — consistent with file layout).
- All three members must have XML doc comments matching the contract sketches in `design.md` §API/Contract Sketch.

**Inputs:**

- Requirements refs: REQ-6 (GetHistory snapshot), REQ-7 (HistoryCount non-allocating), REQ-8 (ClearHistory), REQ-11 (pre-init does not throw)
- Design refs: §API/Contract Sketch, §Components/CommandSystem changes

**Validation:**

- Build check: Project builds without errors.
- Unit tests: Covered in T-05.
- Smoke: `HistoryCount` returns `0` before `Initialize()`; `GetHistory()` returns empty array before `Initialize()`; `ClearHistory()` does not throw before `Initialize()`.

- QA quick pass (`taskReviewer`):
  - Review scope: Three new members in `src/CommandSystem.cs`.
  - Primary checks: `HistoryCount` is a property (not a method); returns 0 pre-init; `GetHistory()` returns `Array.Empty` pre-init (not `null`); `ClearHistory()` is a void no-op pre-init; all three have XML docs; return types match design contract (`CommandHistoryEntry[]`, `int`, `void`).
  - Required evidence: Successful build; all existing 139 tests still pass.
  - Blocking conditions: `GetHistory()` returns `null` instead of `Array.Empty`; `HistoryCount` throws pre-init; missing XML docs; wrong return type.

- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Must be resolved before completion gate.

**Documentation Sync:**

- Docs to update in `docs/`: None at this task — full docs sync in T-06.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to T-06.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (project builds without errors; all 139 existing tests pass)
- [ ] Unit tests passed or exception documented (new tests in T-05)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-06)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to T-06)

**Commit Note:**

- Suggested commit scope: `src/CommandSystem.cs`
- Suggested commit message: `feat(command-history): add GetHistory, HistoryCount, ClearHistory public API`

---

### T-05: Write unit tests covering all acceptance criteria and requirements testing expectations

- [ ] Not started

**Objective:**

Create `tests/kmCommands.Tests/CommandHistoryTests.cs` with NUnit tests that fully cover every acceptance criterion from `requirements.md` and every testing expectation listed in the requirements. Run the full test suite to confirm the 139 prior tests and all new tests pass.

**Files to create / modify:**

- Create: `tests/kmCommands.Tests/CommandHistoryTests.cs`
- Depends on: T-04 (all public API in place)

**Implementation Notes:**

- Test fixture class: `CommandHistoryTests` in `kmCommands.Tests` namespace, `[TestFixture]` decorated.
- `[SetUp]` / `[TearDown]` pattern: `_system = new CommandSystem(); _system.Initialize();` / `_system.Shutdown();` — consistent with `CommandExecutionTests`.
- Helper: a private registration helper (similar pattern to `CommandExecutionTests.Register()`).
- Required test methods — names must follow `<Subject>_<Condition>_<ExpectedOutcome>` pattern used in existing test files:

  **Pre-init / lifecycle:**
  - `HistoryCount_BeforeInitialize_ReturnsZero` — create a fresh `CommandSystem`, do not call `Initialize()`, assert `HistoryCount == 0`.
  - `GetHistory_BeforeInitialize_ReturnsEmptyArray` — same setup, assert result is empty array (not null, length 0).
  - `ClearHistory_BeforeInitialize_DoesNotThrow` — same setup, assert no exception.
  - `HistoryCount_AfterInitialize_IsZero` — normal setup, assert count is 0 immediately after `Initialize()`.
  - `HistoryCount_AfterShutdownAndReinitialize_IsZero` — call `Shutdown()` then `Initialize()`, assert count is 0.

  **Recording behavior:**
  - `Execute_SuccessfulCommand_IncrementsHistoryCount` — register a no-arg command, execute it, assert `HistoryCount == 1`.
  - `Execute_SuccessfulCommand_RecordsCorrectName` — assert `GetHistory()[0].CommandName` equals the executed command name.
  - `Execute_SuccessfulCommand_RecordsCorrectArgs` — register a command with one string param, execute with `"hello"`, assert `GetHistory()[0].Args[0] == "hello"`.
  - `Execute_FailedCommand_DoesNotIncrementHistoryCount` — execute a command that does not exist, assert `HistoryCount == 0`.
  - `Execute_ArgumentConversionFailed_DoesNotIncrementHistoryCount` — register a command expecting `int`, execute with non-numeric arg, assert `HistoryCount == 0`.

  **Argument snapshot independence:**
  - `Execute_MutatingArgsAfterExecute_DoesNotAffectStoredEntry` — capture `string[]` array, execute, mutate the array, assert stored entry args are unchanged.

  **Entry ordering:**
  - `GetHistory_MultipleEntries_ReturnsOldestToNewest` — execute commands "alpha", "beta", "gamma" in order; assert `GetHistory()[0].CommandName == "alpha"` and `[2].CommandName == "gamma"`.

  **Capacity and eviction:**
  - `Initialize_CustomCapacity_LimitsBufferSize` — call `Initialize(3)`, execute 3 commands, assert count is 3.
  - `Execute_BeyondCapacity_EvictsOldestEntry` — `Initialize(3)`, execute "cmd1" through "cmd4", assert `GetHistory()[0].CommandName == "cmd2"` (oldest evicted).
  - `Execute_BeyondCapacity_CountStaysAtCapacity` — same setup, assert `HistoryCount == 3`.
  - `Initialize_CapacityLessThanOne_ClampsToOne` — `Initialize(0)`, register and execute one command, assert `HistoryCount == 1`; execute another, assert `HistoryCount == 1` and second command is the stored entry.
  - `DefaultHistoryCapacity_IsPositiveInteger` — assert `CommandSystem.DefaultHistoryCapacity > 0`.
  - `Initialize_DefaultCapacity_IsUsedWhenNoArgOverload` — execute `DefaultHistoryCapacity` commands, assert `HistoryCount == CommandSystem.DefaultHistoryCapacity`; execute one more, assert `HistoryCount == CommandSystem.DefaultHistoryCapacity`.

  **Clear:**
  - `ClearHistory_ResetsCountToZero` — execute a command, call `ClearHistory()`, assert `HistoryCount == 0`.
  - `ClearHistory_GetHistoryReturnsEmpty` — execute a command, call `ClearHistory()`, assert `GetHistory().Length == 0`.
  - `ClearHistory_AfterClear_NewEntryIsRecorded` — execute, clear, execute again, assert `HistoryCount == 1`.

  **Snapshot independence from live buffer:**
  - `GetHistory_Snapshot_IsNotAffectedBySubsequentExecute` — capture snapshot, execute another command, assert snapshot length unchanged.

- All tests must be self-contained and use the `[SetUp]`/`[TearDown]` pattern.
- No LINQ in tests. Use indexed access and `Assert.That` with NUnit constraint API.

**Inputs:**

- Requirements refs: REQ-1 through REQ-13 (all requirements), §Acceptance Overview, §Testing Expectations
- Design refs: All resolved open questions

**Validation:**

- Run: `dotnet test tests/kmCommands.Tests/kmCommands.Tests.csproj`
- All new tests in `CommandHistoryTests` must pass.
- All 139 prior tests must continue to pass.
- Total test count must be 139 + (count of new tests).

- QA quick pass (`taskReviewer`):
  - Review scope: `tests/kmCommands.Tests/CommandHistoryTests.cs`
  - Primary checks: Every requirement from §Testing Expectations is referenced by at least one test; eviction boundary is tested (capacity exactly full then +1); snapshot independence verified; no LINQ in tests; pre-init guard tests are independent of `[SetUp]` (must not call `Initialize()`); test method names follow existing convention.
  - Required evidence: Full `dotnet test` output showing all tests pass with count.
  - Blocking conditions: Any new test failing; any of the 139 prior tests broken; missing coverage for eviction, snapshot independence, or pre-init guards; `Initialize()` called inside a "pre-init" test.

- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Must be resolved before completion gate.

**Documentation Sync:**

- Docs to update in `docs/`: None at this task — full docs sync in T-06.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to T-06.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (`dotnet test` — all new tests pass, all 139 prior tests pass)
- [ ] Unit tests passed (explicitly verified via `dotnet test` output)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-06)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to T-06)

**Commit Note:**

- Suggested commit scope: `tests/kmCommands.Tests/CommandHistoryTests.cs`
- Suggested commit message: `test(command-history): add CommandHistoryTests covering all acceptance criteria`

---

### T-06: Docs sync — update `docs/` and `.github/instructions/projectOverview.instructions.md`

- [ ] Not started

**Objective:**

Update `docs/commands.md` and `docs/architecture.md` to document the new command history API and architecture changes. Update `projectOverview.instructions.md` to reflect new public types, new public API members, new internal components, and any changed constraints.

**Files to create / modify:**

- Modify: `docs/commands.md`
- Modify: `docs/architecture.md`
- Modify: `.github/instructions/projectOverview.instructions.md`
- Depends on: T-05 (full working feature before finalizing docs)

**Implementation Notes:**

**`docs/commands.md`** — add a new top-level section `## Command History`:

- Explain what command history records (successful executions only), and when it is cleared.
- Document `DefaultHistoryCapacity` value and its meaning.
- Show how to configure a custom capacity via `Initialize(int historyCapacity)`.
- Show usage of `GetHistory()`, `HistoryCount`, and `ClearHistory()` with brevity consistent with the existing command doc style.
- Note that history is reset on `Shutdown()` and empty after each `Initialize()`.
- Note pre-init behavior (no-throw, returns empty/zero).

**`docs/architecture.md`** — update the following:

- The ASCII architecture diagram: add `CommandHistoryBuffer` as a new box under `CommandSystem`, alongside `CommandRegistry`, `ArgumentConverter`, `ExecutionHandler`, `AttributeScanner`.
- The public API listing inside `CommandSystem`'s component description: add `DefaultHistoryCapacity`, `Initialize(int)`, `GetHistory()`, `HistoryCount`, `ClearHistory()`.
- The Namespaces table: add `CommandHistoryEntry` to the `kmCommands` public namespace row; add `CommandHistoryBuffer` to the `kmCommands.Core` internal namespace row.
- The `CommandSystem` component description: update "Owns:" to include `CommandHistoryBuffer`.

**`.github/instructions/projectOverview.instructions.md`** — update the following sections:

- **Key Paths**: add `src/CommandHistoryEntry.cs` (public `CommandHistoryEntry` readonly struct) and `src/Core/CommandHistoryBuffer.cs` (internal ring-buffer).
- **API Layer Summary**:
  - Execution API: note that `Execute()` records successful executions to the history buffer.
  - Add a new "History API" entry: `DefaultHistoryCapacity` constant; `Initialize(int historyCapacity)` overload; `GetHistory()`, `HistoryCount`, `ClearHistory()`.
- **Implementation Direction** (if the section exists): add `CommandHistoryEntry` and `CommandHistoryBuffer` with their file paths and brief descriptions, consistent with existing entries.
- **Current Repository State**: update the test count if it has changed from 139.

**Inputs:**

- Requirements refs: REQ-1 through REQ-13 (documentation of delivered behaviors)
- Design refs: §API/Contract Sketch, §Components, §Architecture Overview

**Validation:**

- Docs review: All new API members (`GetHistory`, `HistoryCount`, `ClearHistory`, `Initialize(int)`, `DefaultHistoryCapacity`) are mentioned in `docs/commands.md`.
- Arch review: Architecture diagram and namespace table in `docs/architecture.md` reflect the new components.
- Overview review: `projectOverview.instructions.md` accurately reflects new file paths, API entries.
- No broken links or stale references to old behavior.

- QA quick pass (`taskReviewer`):
  - Review scope: `docs/commands.md`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`
  - Primary checks: New history section in `commands.md` documents all three API members + `Initialize(int)` + `DefaultHistoryCapacity`; architecture diagram updated; namespace table includes `CommandHistoryEntry` and `CommandHistoryBuffer`; `projectOverview.instructions.md` lists the new file paths and API entries; no old behavior misrepresented; no new implementation introduced in docs task.
  - Required evidence: Diff of the three modified files; final `dotnet test` run showing all tests still pass.
  - Blocking conditions: Any public API member undocumented; `projectOverview.instructions.md` not updated; architecture diagram still shows the old structure without `CommandHistoryBuffer`.

- Expected `taskReviewer` output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Must be resolved before completion gate.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md`, `docs/architecture.md` (this is the docs task itself).
- Update `.github/instructions/projectOverview.instructions.md` required: Yes.
- Sections to update: Key Paths, API Layer Summary (History API), Implementation Direction, Current Repository State (test count).

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (docs review complete; no broken references; final `dotnet test` passes)
- [ ] Unit tests passed (final `dotnet test` run — all new + prior tests pass)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] `docs/commands.md` updated with history API section
- [ ] `docs/architecture.md` updated with `CommandHistoryBuffer` in diagram and tables
- [ ] `.github/instructions/projectOverview.instructions.md` synced — Key Paths, API Layer Summary, Implementation Direction updated

**Commit Note:**

- Suggested commit scope: `docs/commands.md`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `docs(command-history): update commands, architecture, and project overview`

---

## Coverage Check

### Requirements coverage

- [ ] Every requirement is mapped to at least one task
- [ ] No requirement is left unplanned

| Requirement                                                              | Task(s)                                                                      |
| ------------------------------------------------------------------------ | ---------------------------------------------------------------------------- |
| REQ-1: Entry captures name + immutable args snapshot                     | T-01 (struct shape), T-02 (CopyArgs in Record)                               |
| REQ-2: Buffer ordered oldest → newest                                    | T-02 (GetSnapshot traversal order)                                           |
| REQ-3: Configurable capacity, oldest eviction at capacity                | T-02 (ring buffer eviction logic), T-03 (Initialize overload wires capacity) |
| REQ-4: Default capacity defined and documented                           | T-03 (`DefaultHistoryCapacity = 64`), T-06 (docs)                            |
| REQ-5: Min capacity ≥ 1, clamped                                         | T-03 (clamping in `Initialize(int)`)                                         |
| REQ-6: `GetHistory()` returns snapshot array                             | T-04 (GetHistory), T-02 (GetSnapshot)                                        |
| REQ-7: `HistoryCount` non-allocating                                     | T-04 (property delegates to `_buffer.Count`)                                 |
| REQ-8: `ClearHistory()` resets buffer                                    | T-04 (ClearHistory), T-02 (Clear)                                            |
| REQ-9: Recording inside `Execute()` flow; not when uninitialized         | T-03 (Execute integration)                                                   |
| REQ-10: `Shutdown()` discards history; next `Initialize()` starts empty  | T-03 (Shutdown nulls buffer, Initialize creates fresh buffer)                |
| REQ-11: Pre-init calls do not throw; return empty/zero/no-op             | T-04 (guards on all three members)                                           |
| REQ-12: IL2CPP/AOT safe                                                  | T-01, T-02, T-03, T-04 (no LINQ, no reflection, no emit)                     |
| REQ-13: Allocation discipline — one copy per Execute; no hot-path extras | T-02 (CopyArgs strategy)                                                     |

### Design coverage

- [ ] Key design components are mapped to tasks
- [ ] Critical design constraints are represented in validation gates

| Design component / decision                                    | Task(s) |
| -------------------------------------------------------------- | ------- |
| `CommandHistoryEntry` as `readonly struct`                     | T-01    |
| `CommandHistoryBuffer` internal sealed class with ring buffer  | T-02    |
| Ring buffer index arithmetic (both full and non-full paths)    | T-02    |
| `CopyArgs` static helper with `Array.Copy` (AOT-safe, no LINQ) | T-02    |
| `GetSnapshot` oldest-to-newest traversal                       | T-02    |
| `Clear` resets `_head` and `_count` (no zero-fill)             | T-02    |
| `Initialize(int)` overload; symmetric (no cross-delegation)    | T-03    |
| No-arg `Initialize()` uses `DefaultHistoryCapacity`            | T-03    |
| `Shutdown()` nulls `_historyBuffer`                            | T-03    |
| `Execute()` records only on `result.Success`                   | T-03    |
| `ExecutionHandler` remains history-unaware                     | T-03    |
| `HistoryCount`, `GetHistory()`, `ClearHistory()` public API    | T-04    |
| Pre-init guards (null buffer check)                            | T-04    |
| Pre-init `ClearHistory()` is a no-op                           | T-04    |
| Unit tests covering all acceptance criteria                    | T-05    |
| `docs/commands.md`, `docs/architecture.md` updated             | T-06    |
| `projectOverview.instructions.md` updated                      | T-06    |

### Gaps or follow-ups

- None identified. All requirements and all design decisions are represented in tasks.
- The `feat/command-history` branch name diverges from the `feat_command-history` recorded in `requirements.md §Branch`. The authoritative name is `feat/command-history` per the task request; the developer agent should reconcile this when branching from `main`.
