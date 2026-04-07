# Rich History Entries Tasks

## Status

- [ ] Planned
- [ ] In Progress
- [x] Completed

## Feature Slug

`rich-history-entries`

## Inputs

- Requirements: `.github/tasks/rich-history-entries/requirements.md`
- Design: `.github/tasks/rich-history-entries/design.md`

## Branch

- Name: `feat_rich-history-entries`
- Rationale: `feat_` — new capability extending an existing type and recording policy with additional data fields

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- Slices T-01 → T-02 → T-03 must land in order; each depends on the previous.
- T-04 and T-05 must follow T-03 (recording policy change must be in place before test assertions are updated or new tests are written).
- T-06 (docs) and T-07 (final validation) are the last tasks in order.
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status -> Completed` may be checked only after all tasks and `## Coverage Check` items are checked.
- **Behavioural breaking change**: failed executions (past the `IsInitialized` guard) are now recorded. This must be communicated in release notes.

---

## Task List

### Task T-01: Extend `CommandHistoryEntry` struct

- [x] Not started

**Objective:**

Add four new backing fields and four new get-only properties to `CommandHistoryEntry`. Extend the `internal` constructor with four additional parameters. Preserve the struct as a `readonly struct` with an `internal`-only constructor.

**Inputs:**

- Requirements refs: FR-1, FR-2, FR-3, FR-4, FR-5, FR-6; API Changes — `CommandHistoryEntry`
- Design refs: Step 1 (`CommandHistoryEntry` extension); API / Contract Sketch; `CommandHistoryEntry` component responsibility

**Scope:**

- `src/CommandHistoryEntry.cs`

**Implementation Steps:**

1. Replace the three auto-property fields (`CommandName`, `Args`, `ReturnValue`) with explicit `private readonly` backing fields: `_commandName`, `_args`, `_returnValue`.
2. Add four new `private readonly` fields: `_timestamp` (`System.DateTime`), `_rawInput` (`string[]`), `_status` (`ExecutionError`), `_errorDetail` (`string`).
3. Update the three existing get-only properties to read from their backing fields.
4. Add four new get-only properties:
   - `public DateTime Timestamp => _timestamp;`
   - `public string[] RawInput => _rawInput;`
   - `public ExecutionError Status => _status;`
   - `public string ErrorDetail => _errorDetail;`
5. Extend the `internal` constructor signature to accept four new parameters after the existing three (order: `commandName`, `args`, `returnValue`, `timestamp`, `rawInput`, `status`, `errorDetail`).
6. Assign all seven parameters to their corresponding fields in the constructor body.
7. Update the XML doc summary to reflect that the struct records all executions (not just successful ones).
8. Add XML `<summary>` comments for each new property per the design API sketch.

**Validation:**

- Unit tests: N/A for this task in isolation — the constructor change breaks the existing callers (`CommandHistoryBuffer.Record()` and any test helpers) until T-02 and T-04 are complete.
- Additional checks: Project builds without errors after this task. (Compilation errors in `CommandHistoryBuffer.cs` and test files are expected and will be resolved in subsequent tasks.)
- QA quick pass (`taskReviewer`): Deferred to T-04 after all callers are updated.
- taskReviewer review request:
  - Review scope: `src/CommandHistoryEntry.cs` — all field and property additions, constructor signature change.
  - Primary checks: (1) Struct is still `readonly struct`. (2) No public constructors added. (3) All seven fields are `private readonly`. (4) All seven properties are get-only (no `set` accessors). (5) Constructor parameter order matches design spec exactly.
  - Required evidence: Project build succeeds (expected callers still broken at this stage is acceptable).
  - Blocking conditions: Any `set` accessor; any public constructor; missing backing field; wrong parameter order in constructor.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Resolve before marking complete.

**Completion Gate:**

- [x] Implementation done
- [x] Project builds with no errors in `src/` (downstream callers may have broken call sites — acceptable)
- [x] `CommandHistoryEntry` is still a `readonly struct`
- [x] All four new properties are get-only
- [x] `internal` constructor has exactly seven parameters in the correct order
- [x] Unit tests passed or exception documented (exception: downstream compilation breaks expected until T-02)
- [x] QA quick pass done or exception documented (deferred to T-04)
- [x] taskReviewer output captured and any notes tracked
- [x] Comments/check comments addressed
- [x] Relevant docs in `docs/` updated or exception documented (N/A for this task — doc update in T-06)
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — projectOverview sync in T-07)

**Commit Note:**

- Suggested commit scope: `src/CommandHistoryEntry.cs`
- Suggested commit message: `feat(rich-history-entries): add Timestamp, RawInput, Status, ErrorDetail to CommandHistoryEntry`

---

### Task T-02: Extend `CommandHistoryBuffer.Record()`

- [x] Not started

**Objective:**

Add four new parameters to `CommandHistoryBuffer.Record()` matching the new `CommandHistoryEntry` constructor parameters. Pass the new values through to the constructor. The `rawInput` array is stored directly (already isolated by the caller); `args` continues to be copied inside `Record()` as before.

**Inputs:**

- Requirements refs: FR-8, FR-10
- Design refs: Step 2 (`CommandHistoryBuffer.Record()` extension); Internal Method Signatures table; `CommandHistoryBuffer` component responsibility

**Scope:**

- `src/Core/CommandHistoryBuffer.cs`

**Implementation Steps:**

1. Update the `Record()` method signature to add four new parameters after `returnValue`: `DateTime timestamp`, `string[] rawInput`, `ExecutionError status`, `string errorDetail`.
2. Pass `rawInput` directly to the `CommandHistoryEntry` constructor (no copy — it is already an isolated snapshot built by the caller).
3. Continue calling `CopyArgs(args)` for the `args`/`Args` field as before.
4. Pass all seven arguments to the `CommandHistoryEntry` constructor in the correct order: `commandName`, `argsCopy`, `returnValue`, `timestamp`, `rawInput`, `status`, `errorDetail`.
5. Update the XML doc comment on `Record()` to document the four new parameters.

**Validation:**

- Unit tests: N/A in isolation — `CommandSystem.Execute()` call site is still broken until T-03.
- Additional checks: `src/` directory builds without errors after this task. (The one call site in `CommandSystem.cs` will remain broken until T-03.)
- QA quick pass (`taskReviewer`): Deferred to T-04.
- taskReviewer review request:
  - Review scope: `src/Core/CommandHistoryBuffer.cs` — `Record()` signature and body.
  - Primary checks: (1) `CopyArgs(args)` is still called for `_args`; `rawInput` is NOT re-copied. (2) Constructor call uses all seven arguments in correct order. (3) No unintended logic changes to ring buffer eviction.
  - Required evidence: `src/` builds without errors.
  - Blocking conditions: `rawInput` being copied when it should be stored directly; `args` not being copied when it must be; incorrect argument order to constructor.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Resolve before marking complete.

**Completion Gate:**

- [x] Implementation done
- [x] `src/` builds without errors (broken call site in `CommandSystem.cs` is expected)
- [x] `Record()` signature has seven parameters in the correct order
- [x] `args` is still copied via `CopyArgs(args)` for the `Args` field
- [x] `rawInput` is stored directly (not re-copied)
- [x] Ring buffer eviction logic is unchanged
- [x] Unit tests passed or exception documented (exception: broken call site in `CommandSystem.cs` prevents full build until T-03)
- [x] QA quick pass done or exception documented (deferred to T-04)
- [x] taskReviewer output captured and any notes tracked
- [x] Comments/check comments addressed
- [x] Relevant docs in `docs/` updated or exception documented (N/A — doc update in T-06)
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — sync in T-07)

**Commit Note:**

- Suggested commit scope: `src/Core/CommandHistoryBuffer.cs`
- Suggested commit message: `feat(rich-history-entries): extend CommandHistoryBuffer.Record() with new entry fields`

---

### Task T-03: Update `CommandSystem.Execute()` recording

- [x] Not started

**Objective:**

Capture `DateTime.UtcNow` once at the top of `Execute()` (after the `IsInitialized` guard). Add the private static `BuildRawInput()` helper to `CommandSystem`. Update the `_historyBuffer.Record()` call to pass all seven parameters. Remove the `if (result.Success)` guard so recording is unconditional for all outcomes past the `IsInitialized` guard.

**Inputs:**

- Requirements refs: FR-1, FR-2, FR-7, FR-9; Execute() call sites section
- Design refs: Step 3 and Step 4; Data Flow / Control Flow diagram; Execute() Call Sites — Before/After; `BuildRawInput` helper spec

**Scope:**

- `src/CommandSystem.cs`

**Implementation Steps:**

1. Locate `CommandSystem.Execute(string commandName, string[] args)`.
2. After the `!IsInitialized` guard (the early return for `NotInitialized`), add: `DateTime timestamp = DateTime.UtcNow;`
3. On the next line, add: `string[] rawInput = BuildRawInput(commandName, args);`
4. Remove the `if (result.Success)` wrapper around `_historyBuffer.Record(...)`.
5. Update `_historyBuffer.Record(...)` to pass all seven arguments: `commandName`, `args`, `result.ReturnValue`, `timestamp`, `rawInput`, `result.Error`, `result.ErrorMessage`.
6. Add the private static helper method `BuildRawInput(string commandName, string[] args)`:
   - If `args` is `null` or `args.Length == 0`, return `new string[] { commandName }`.
   - Otherwise, allocate `new string[1 + args.Length]`, set index 0 to `commandName`, copy `args` elements to indices 1..n using a `for` loop (no LINQ).
   - Return the new array.
7. Verify that `BuildRawInput` is called **before** `_executionHandler.Execute()` is invoked.
8. Confirm the `NotInitialized` early-return exits before `DateTime.UtcNow` and `BuildRawInput` are reached.

**Validation:**

- Unit tests: All pre-existing tests must compile and run. Two tests (`Execute_FailedCommand_DoesNotIncrementHistoryCount`, `Execute_ArgumentConversionFailed_DoesNotIncrementHistoryCount`) will **fail** after this step because those test assertions contradict the new policy. This is expected — they will be fixed in T-04. All other existing tests must pass.
- Additional checks: Full project + test project builds with no errors or new warnings. `dotnet build` should succeed.
- QA quick pass (`taskReviewer`): Deferred to T-04 after tests are updated.
- taskReviewer review request:
  - Review scope: `src/CommandSystem.cs` — `Execute()` method body + new `BuildRawInput()` helper.
  - Primary checks: (1) `timestamp` is captured after `!IsInitialized` guard, before `_executionHandler.Execute()`. (2) `rawInput` is built from `BuildRawInput()` before `_executionHandler.Execute()`. (3) `if (result.Success)` wrapper is fully removed. (4) `BuildRawInput(name, null)` returns length-1 array containing only the command name. (5) `BuildRawInput` uses a `for` loop (no LINQ). (6) `NotInitialized` path still exits before recording.
  - Required evidence: Project builds; `dotnet build` output shows no errors.
  - Blocking conditions: `timestamp` captured after `_executionHandler.Execute()`; `if (result.Success)` guard still present; LINQ used in `BuildRawInput`; `NotInitialized` path reaching the recording code.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Resolve before marking complete.

**Completion Gate:**

- [x] Implementation done
- [x] Project and test project build with no errors
- [x] `!IsInitialized` guard still exits before `timestamp` and `rawInput` are captured
- [x] `timestamp = DateTime.UtcNow` is captured before `_executionHandler.Execute()` is called
- [x] `rawInput = BuildRawInput(commandName, args)` is called before `_executionHandler.Execute()` is called
- [x] `if (result.Success)` guard is removed; `Record()` call is unconditional
- [x] `BuildRawInput` uses a `for` loop with no LINQ or closures
- [x] All pre-existing tests (except the two recording-policy tests) pass
- [x] Unit tests passed or exception documented (two recording-policy tests expected to fail; will be fixed in T-04)
- [x] QA quick pass done or exception documented (deferred to T-04)
- [x] taskReviewer output captured and any notes tracked
- [x] Comments/check comments addressed
- [x] Relevant docs in `docs/` updated or exception documented (N/A — doc update in T-06)
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — sync in T-07)

**Commit Note:**

- Suggested commit scope: `src/CommandSystem.cs`
- Suggested commit message: `feat(rich-history-entries): update Execute() to record all outcomes unconditionally`

---

### Task T-04: Update failing tests in `CommandHistoryTests.cs`

- [x] Not started

**Objective:**

Update the two existing tests that directly contradict the new failure-recording policy. Rename them to reflect the new behaviour and change their assertions. Update any test helper that constructs `CommandHistoryEntry` directly with the old 3-arg constructor to use the new 7-arg constructor. All pre-existing tests must pass after this task.

**Inputs:**

- Requirements refs: FR-7; Acceptance Criteria AC-8; Backward Compatibility notes; Testing Expectations
- Design refs: Testing Strategy — Tests to update; OQ-2 resolution; Risks and Tradeoffs

**Scope:**

- `tests/kmCommands.Tests/CommandHistoryTests.cs`

**Implementation Steps:**

1. **Rename** `Execute_FailedCommand_DoesNotIncrementHistoryCount` → `Execute_FailedCommand_RecordsFailureEntryInHistory`.
   - Change the assertion from `HistoryCount == 0` to `HistoryCount == 1`.
   - Add assertion: `Status == ExecutionError.CommandNotFound` on the recorded entry.
2. **Rename** `Execute_ArgumentConversionFailed_DoesNotIncrementHistoryCount` → `Execute_ArgumentConversionFailed_RecordsFailureEntryInHistory`.
   - Change the assertion from `HistoryCount == 0` to `HistoryCount == 1`.
   - Add assertion: `Status == ExecutionError.ArgumentConversionFailed` on the recorded entry.
3. Search the entire `CommandHistoryTests.cs` for any other assertion of `HistoryCount == 0` following a failed `Execute()` call that contradicts the new policy. Update any found.
4. Search for any direct `new CommandHistoryEntry(...)` constructor call in the test file. Update any found to pass all seven arguments (use `DateTime.UtcNow`, empty `string[] { commandName }`, `ExecutionError.None`, `null` as sensible defaults for the new parameters where context does not dictate otherwise).
5. Run the full test suite and confirm zero failures.

**Validation:**

- Unit tests: Run full `dotnet test` — zero failures required. All 376+ pre-existing tests must pass.
- Additional checks: No test assertions reference the old 3-arg constructor or old policy.
- QA quick pass (`taskReviewer`): Yes, perform a full pass after this task.
- taskReviewer review request:
  - Review scope: `tests/kmCommands.Tests/CommandHistoryTests.cs` — the two renamed/updated tests and any test-helper updates.
  - Primary checks: (1) Both renamed tests now assert `HistoryCount == 1`. (2) Both renamed tests assert the correct `Status` value. (3) No other test in the file asserts `HistoryCount == 0` for a case that contradicts the new policy. (4) No direct `CommandHistoryEntry(...)` construction uses the old 3-arg signature. (5) All pre-existing test names that did NOT contradict the policy are unchanged.
  - Required evidence: `dotnet test` output with zero failures; test count ≥ 376.
  - Blocking conditions: Any renamed test still asserting `HistoryCount == 0`; any failing pre-existing test; any 3-arg `CommandHistoryEntry` constructor call remaining in test helpers.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Resolve before marking complete.

**Completion Gate:**

- [x] Implementation done
- [x] Both recording-policy tests renamed and assertions updated
- [x] No remaining test asserts `HistoryCount == 0` after a failed execute in contradiction with the new policy
- [x] No remaining 3-arg `CommandHistoryEntry` construction in test codebase
- [x] Full `dotnet test` passes with zero failures
- [x] Test count is ≥ 376 (pre-existing tests unchanged)
- [x] Unit tests passed (zero failures)
- [x] QA quick pass done
- [x] taskReviewer output captured and any notes tracked
- [x] Comments/check comments addressed
- [x] Relevant docs in `docs/` updated or exception documented (N/A — doc update in T-06)
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — sync in T-07)

**Commit Note:**

- Suggested commit scope: `tests/kmCommands.Tests/CommandHistoryTests.cs`
- Suggested commit message: `test(rich-history-entries): update recording-policy tests to reflect unconditional recording`

---

### Task T-05: Write new tests for new `CommandHistoryEntry` fields

- [x] Not started

**Objective:**

Add comprehensive new unit tests covering all four new `CommandHistoryEntry` properties (`Timestamp`, `RawInput`, `Status`, `ErrorDetail`) for both success and failure execution paths. Cover the `NotInitialized` guard. Target a minimum of 15 new test methods.

**Inputs:**

- Requirements refs: FR-1 through FR-7; Acceptance Criteria AC-1 through AC-10; Testing Expectations
- Design refs: Testing Strategy — New tests to add; Risks and Tradeoffs; Review Contract

**Scope:**

- `tests/kmCommands.Tests/CommandHistoryTests.cs`

**Implementation Steps:**

Add the following test groups to `CommandHistoryTests.cs`. Each test follows the existing NUnit `[Test]` attribute + arrange/act/assert pattern used in the file.

**Timestamp tests:**

1. `Execute_SuccessfulCommand_Timestamp_KindIsUtc` — assert `entry.Timestamp.Kind == DateTimeKind.Utc`.
2. `Execute_SuccessfulCommand_Timestamp_IsWithinOneSecondOfUtcNow` — record before/after `DateTime.UtcNow`; assert entry timestamp falls within [before, after + 1s].
3. `Execute_FailedCommand_Timestamp_KindIsUtc` — execute non-existent command; assert `entry.Timestamp.Kind == DateTimeKind.Utc`.

**Status tests:** 4. `Execute_SuccessfulCommand_Status_IsNone` — assert `entry.Status == ExecutionError.None`. 5. `Execute_CommandNotFound_Status_IsCommandNotFound` — assert `entry.Status == ExecutionError.CommandNotFound`. 6. `Execute_ArgumentConversionFailed_Status_IsArgumentConversionFailed` — assert `entry.Status == ExecutionError.ArgumentConversionFailed`. 7. `Execute_ArgumentCountMismatch_Status_IsArgumentCountMismatch` — register a command expecting 1 arg; call with 0 args; assert `entry.Status == ExecutionError.ArgumentCountMismatch`.

**ErrorDetail tests:** 8. `Execute_SuccessfulCommand_ErrorDetail_IsNull` — assert `entry.ErrorDetail == null`. 9. `Execute_FailedCommand_ErrorDetail_MatchesExecutionResultErrorMessage` — execute non-existent command; capture `ExecutionResult.ErrorMessage`; assert `entry.ErrorDetail == errorMessage`.

**RawInput tests:** 10. `Execute_ZeroArgs_RawInput_LengthIsOneAndContainsCommandName` — call with `Array.Empty<string>()`; assert `RawInput.Length == 1`; assert `RawInput[0] == commandName`. 11. `Execute_MultipleArgs_RawInput_ContainsCommandNameAtIndexZeroAndAllArgs` — call with `new[] { "a", "b" }`; assert `RawInput.Length == 3`; assert `RawInput[0] == commandName`; assert `RawInput[1] == "a"`; assert `RawInput[2] == "b"`. 12. `Execute_NullArgs_RawInput_LengthIsOne` — call with `null` args; assert `RawInput.Length == 1`; assert `RawInput[0] == commandName`. 13. `Execute_MutatingArgsAfterExecute_DoesNotAffectRawInput` — mutate caller's `args` array after `Execute()` returns; assert `entry.RawInput` is unaffected (same content as before mutation).

**NotInitialized guard test:** 14. `Execute_BeforeInitialize_DoesNotRecord` — create a new `CommandSystem`; call `Execute()` without `Initialize()`; assert `HistoryCount == 0`.

**ReturnValue on failure test:** 15. `Execute_FailedCommand_ReturnValue_IsNull` — execute non-existent command; assert `entry.ReturnValue == null`.

**Validation:**

- Unit tests: All 15+ new tests must pass. Previous test count ≥ 376; new test count ≥ 391.
- Additional checks: `dotnet test` produces zero failures. No new compiler warnings.
- QA quick pass (`taskReviewer`): Yes, perform a full pass after this task.
- taskReviewer review request:
  - Review scope: `tests/kmCommands.Tests/CommandHistoryTests.cs` — all new test methods.
  - Primary checks: (1) All listed test IDs are present. (2) `Timestamp` tests use tolerance-based comparison (not equality). (3) `RawInput` null-args test calls `Execute()` with `null` (not `Array.Empty`). (4) Mutation isolation test mutates the array **after** `Execute()` returns. (5) `NotInitialized` test uses a freshly constructed `CommandSystem` with no `Initialize()` call. (6) All tests follow existing naming convention (`Method_Scenario_ExpectedOutcome`).
  - Required evidence: `dotnet test` output with zero failures; total count ≥ 391.
  - Blocking conditions: Any new test asserting exact `DateTime` equality (flaky); missing isolation or mutation test; `HistoryCount == 0` asserted for the `NotInitialized` case without using a fresh `CommandSystem`.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Resolve before marking complete.

**Completion Gate:**

- [x] Implementation done
- [x] Minimum 15 new test methods added
- [x] All four new properties covered (Timestamp, RawInput, Status, ErrorDetail)
- [x] Both success and failure paths covered for each property
- [x] NotInitialized guard test present
- [x] `Timestamp` tests use tolerance-based comparison (not exact equality)
- [x] `RawInput` isolation test mutates array after `Execute()` and verifies no effect
- [x] Full `dotnet test` passes with zero failures
- [x] Total test count ≥ 391
- [x] QA quick pass done
- [x] taskReviewer output captured and any notes tracked
- [x] Comments/check comments addressed
- [x] Relevant docs in `docs/` updated or exception documented (N/A — doc update in T-06)
- [x] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — sync in T-07)

**Commit Note:**

- Suggested commit scope: `tests/kmCommands.Tests/CommandHistoryTests.cs`
- Suggested commit message: `test(rich-history-entries): add tests for Timestamp, RawInput, Status, ErrorDetail`

---

### Task T-06: Update `docs/architecture.md`

- [x] Not started

**Objective:**

Update the Command History section in `docs/architecture.md` to accurately reflect the new recording policy, the four new `CommandHistoryEntry` fields, the `BuildRawInput` helper, and the behavioural breaking change.

**Inputs:**

- Requirements refs: Testing Expectations; backward compatibility notes; PR Scope
- Design refs: Overview; Data Flow / Control Flow; Risks and Tradeoffs; Review Contract

**Scope:**

- `docs/architecture.md`

**Implementation Steps:**

1. Locate the Command History section in `docs/architecture.md`.
2. Update the recording-policy description from "records successful executions" to "records all executions that pass the `IsInitialized` guard (including failures)".
3. Update the `CommandHistoryEntry` property list to include the four new properties: `Timestamp`, `RawInput`, `Status`, `ErrorDetail` — with brief descriptions.
4. Add a note that `NotInitialized` calls are never recorded (the buffer does not exist before initialization).
5. Add a note about the `BuildRawInput` private static helper in `CommandSystem` and its role in building the isolated `RawInput` snapshot.
6. Add a clearly marked **breaking change note** explaining that failure entries now appear in history (consumers iterating `GetHistory()` and assuming all entries are successful must add a `Status` filter).

**Validation:**

- Unit tests: N/A (documentation task).
- Additional checks: Read the updated section aloud / review for accuracy and clarity.
- QA quick pass (`taskReviewer`): Yes — reviewer should confirm the documented behaviour matches the implemented behaviour.
- taskReviewer review request:
  - Review scope: `docs/architecture.md` — Command History section.
  - Primary checks: (1) Recording policy accurately described as unconditional past `IsInitialized`. (2) All four new fields documented. (3) `NotInitialized` exemption is explicit. (4) Breaking change note is present and clear. (5) No stale references to success-only recording.
  - Required evidence: Updated `docs/architecture.md`; no contradictions with implemented behaviour.
  - Blocking conditions: Docs still describe success-only recording; breaking change not mentioned.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/architecture.md` — Command History section.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- Sections to update:
  - `src/CommandHistoryEntry.cs` description line — add the four new properties.
  - `src/Core/CommandHistoryBuffer.cs` description line — update `Record()` signature mention.
  - `src/CommandSystem.cs` description line — mention `BuildRawInput` helper and unconditional recording.
  - History API summary section — update `CommandHistoryEntry` description to reflect new fields and recording policy.

**Completion Gate:**

- [x] Implementation done
- [x] `docs/architecture.md` Command History section updated
- [x] All four new properties documented
- [x] Recording policy (unconditional past `IsInitialized`) documented
- [x] `NotInitialized` exemption documented
- [x] Breaking change note present
- [x] `BuildRawInput` helper mentioned
- [x] `.github/instructions/projectOverview.instructions.md` updated for `CommandHistoryEntry`, `CommandHistoryBuffer`, `CommandSystem`, and History API
- [x] QA quick pass done
- [x] taskReviewer output captured and any notes tracked
- [x] Comments/check comments addressed

**Commit Note:**

- Suggested commit scope: `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `docs(rich-history-entries): update architecture docs and project overview for new history fields`

---

### Task T-07: Final build and test validation

- [x] Not started

**Objective:**

Run the complete build and test suite clean to confirm there are zero failures, zero new warnings, and the full feature coheres end-to-end.

**Inputs:**

- Requirements refs: All acceptance criteria (AC-1 through AC-10)
- Design refs: Review Contract; AOT / IL2CPP Safety Notes; Allocation Analysis

**Scope:**

- All changed files

**Implementation Steps:**

1. Run `dotnet build` on the solution and confirm no errors or new compilation warnings.
2. Run `dotnet test` on `tests/kmCommands.Tests` and confirm zero failures.
3. Confirm total test count ≥ 391 (376 pre-existing + minimum 15 new).
4. Manually review the public API surface for `CommandHistoryEntry` — confirm:
   - It is still a `readonly struct`.
   - No `set` accessors on any property.
   - No public constructor.
   - `GetHistory()`, `HistoryCount`, and `ClearHistory()` signatures are unchanged.
5. Confirm `BuildRawInput` is `private static` and not exposed in the public API.
6. Confirm release notes mention the behavioural breaking change (failure entries now appear in history).

**Validation:**

- Unit tests: `dotnet test` — zero failures, test count ≥ 391.
- Additional checks: `dotnet build` — no new warnings. Public API surface audit passes. Breaking change communication in place.
- QA quick pass (`taskReviewer`): Yes — final reviewer sign-off.
- taskReviewer review request:
  - Review scope: Full feature diff across `src/CommandHistoryEntry.cs`, `src/Core/CommandHistoryBuffer.cs`, `src/CommandSystem.cs`, `tests/kmCommands.Tests/CommandHistoryTests.cs`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`.
  - Primary checks from Review Contract:
    1. `CommandHistoryEntry` compiles as `readonly struct` with exactly seven `private readonly` fields and seven get-only properties.
    2. `internal` constructor has exactly seven parameters in order: `commandName`, `args`, `returnValue`, `timestamp`, `rawInput`, `status`, `errorDetail`.
    3. `CommandHistoryBuffer.Record()` calls `CopyArgs(args)` for `Args`; `rawInput` is stored directly.
    4. `DateTime.UtcNow` is captured before `_executionHandler.Execute()` is called.
    5. `BuildRawInput(commandName, null)` produces a `string[]` of length 1.
    6. `if (result.Success)` guard is fully removed.
    7. `NotInitialized` early return exits before `BuildRawInput` and `DateTime.UtcNow` are evaluated.
    8. `ReturnValue` in a failure entry is `null`.
    9. All acceptance criteria from `requirements.md` are checkable against tests.
    10. Breaking change is documented.
  - Required evidence: `dotnet build` output (zero errors/warnings); `dotnet test` output (zero failures; count ≥ 391).
  - Blocking conditions: Any failing test; any public constructor on `CommandHistoryEntry`; any `set` accessor; `if (result.Success)` still present; `timestamp` captured after `_executionHandler.Execute()`; breaking change not documented.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: Resolve before marking complete.

**Completion Gate:**

- [x] `dotnet build` passes with zero errors and zero new warnings
- [x] `dotnet test` passes with zero failures
- [x] Total test count ≥ 391
- [x] `CommandHistoryEntry` is a `readonly struct` with no public constructors
- [x] All new properties are get-only
- [x] `GetHistory()`, `HistoryCount`, `ClearHistory()` signatures are unchanged
- [x] `BuildRawInput` is `private static` (not in public API)
- [x] Breaking change documented (release notes or `docs/`)
- [x] QA quick pass done
- [x] taskReviewer output captured and any notes tracked
- [x] Comments/check comments addressed
- [x] All tasks T-01 through T-06 completion gates satisfied

**Commit Note:**

- Suggested commit scope: No code changes expected; any final cleanup only
- Suggested commit message: `chore(rich-history-entries): final validation pass`

---

## Coverage Check

### Requirements Coverage

- [x] FR-1 (`Timestamp` UTC DateTime) — T-01 (struct), T-03 (capture), T-05 (tests)
- [x] FR-2 (`RawInput` string[] snapshot) — T-01 (struct), T-03 (`BuildRawInput`), T-05 (tests)
- [x] FR-3 (`Status` ExecutionError) — T-01 (struct), T-03 (pass Error), T-05 (tests)
- [x] FR-4 (`ErrorDetail` string) — T-01 (struct), T-03 (pass ErrorMessage), T-05 (tests)
- [x] FR-5 (`readonly struct`, `internal` constructor) — T-01 (struct shape), T-07 (audit)
- [x] FR-6 (all new properties get-only) — T-01 (properties), T-07 (audit)
- [x] FR-7 (record all outcomes past `IsInitialized` guard, not `NotInitialized`) — T-03 (unconditional recording), T-04 (update failing tests), T-05 (NotInitialized test)
- [x] FR-8 (`CommandHistoryBuffer.Record()` extended) — T-02
- [x] FR-9 (`Execute()` call site updated) — T-03
- [x] FR-10 (`Args` isolation unchanged) — T-02 (CopyArgs preserved), T-05 (mutation test for Args)
- [x] AC-1 through AC-10 — covered by T-01 through T-05; T-07 performs final audit
- [x] Behavioural breaking change communication — T-06 (docs), T-07 (final audit)
- [x] Every requirement is mapped to at least one task ✓
- [x] No requirement is left unplanned ✓

### Design Coverage

- [x] Step 1 (CommandHistoryEntry extension) — T-01
- [x] Step 2 (CommandHistoryBuffer.Record() extension) — T-02
- [x] Step 3 (CommandSystem.Execute() unconditional recording) — T-03
- [x] Step 4 (BuildRawInput helper) — T-03
- [x] Testing Strategy (update 2 existing tests) — T-04
- [x] Testing Strategy (15+ new tests for all new fields) — T-05
- [x] AOT / IL2CPP safety constraints — represented in T-01, T-03 implementation steps; validated in T-07
- [x] Allocation analysis (one new array per Execute() call) — T-03 (BuildRawInput allocates one array)
- [x] OQ-1 resolution (string[] RawInput) — T-01 (property type), T-03 (BuildRawInput)
- [x] OQ-2 resolution (record all outcomes past IsInitialized) — T-03 (guard removed), T-04 (tests updated)
- [x] OQ-3 resolution (NotInitialized never recorded) — T-03 (early return intact), T-05 (guard test)
- [x] Review Contract — represented in T-07 reviewer handoff block
- [x] Key design components are mapped to tasks ✓
- [x] Critical design constraints are represented in validation gates ✓

### Gaps and Follow-ups

- `IHistoryWriter` adapter interface — explicitly out of scope; tracked in vision document.
- History filtering/querying — explicitly out of scope.
- Command chaining history integration — explicitly out of scope.
- Serialisation of `CommandHistoryEntry` — explicitly out of scope.

