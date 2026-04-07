# Commands as Command Arguments — Tasks

## Status

- [ ] Planned
- [ ] In Progress
- [ ] Completed

## Inputs

- Requirements: `.github/tasks/commands-as-arguments/requirements.md`
- Design: `.github/tasks/commands-as-arguments/design.md`

## Branch

- Name: `feat_commands-as-arguments`
- Rationale: `feat_` — new user-facing capability enabling nested command invocation as argument values

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status -> Completed` may be checked only after all tasks and `## Coverage Check` items are checked.
- Tasks 1–3 are independent; implement in order for clean commits. Task 4 depends on 1–3 and 7 (enum values added in Task 2). Task 5 is independent. Task 6 is independent of 4 but logically paired with it.

## Task List

---

### Task 1: Add `ReturnType` to `CommandDefinition`

- [ ] Not started

**Objective:**

Add an internal `Type ReturnType` property to `CommandDefinition`. Populate it from reflection at scan time in `AttributeScanner`, `InstanceScanner`, and `InstanceCallbackBuilder`. Default to `typeof(object)` in the constructor and for all manual `Register()` calls. No behavioral change; all existing tests must pass unchanged.

**Inputs:**

- Requirements refs: "The library MUST validate type compatibility … inner command declared return type vs. outer parameter declared type"
- Design refs: `CommandDefinition` change section; `AttributeScanner`/`InstanceScanner`/`InstanceCallbackBuilder` update notes

**Implementation Steps:**

1. In `src/Core/CommandDefinition.cs`: add `internal Type ReturnType { get; }` property; add `Type returnType = null` parameter to constructor; assign `ReturnType = returnType ?? typeof(object);`
2. In `src/Core/AttributeScanner.cs`: at the `new CommandDefinition(…)` call (line 149), pass `method.ReturnType` as the `returnType` argument. For void methods, `method.ReturnType` is already `typeof(void)` — pass it directly.
3. In `src/Core/InstanceCallbackBuilder.cs`: in `BuildMethodCallback`, surface the method's `ReturnType`. Pass it alongside `BuildMethodCallback` return or via a new dedicated return if needed — see step 4.
4. In `src/Core/InstanceScanner.cs`: at all 7 `new CommandDefinition(…)` call sites, pass the correct return type (method return type for method commands; getter property type for getter commands; `typeof(void)` for setter-only commands; property type for setter commands used as setters is effectively `void`).
   - Attribute-decorated methods: `me.Method.ReturnType`
   - Auto-scan methods: `me.Method.ReturnType`
   - Getter properties: `propertyInfo.PropertyType`
   - Setter properties: `typeof(void)`
5. In `src/CommandSystem.cs` `Register()` overloads: pass `typeof(object)` explicitly (or rely on constructor default — no public API surface change).
6. Run all existing tests — no failures expected.

**Validation:**

- Unit tests: No new test file required for this task. Verify via regression: all 391+ existing tests pass.
- Additional checks: Spot-check with a debugger or test assertion that `CommandDefinition.ReturnType` equals the expected type for a scanned void method, a scanned int-returning method, and a manually registered command.
- QA quick pass (`taskReviewer`): Review diff for completeness — all `new CommandDefinition(…)` call sites updated.
- taskReviewer review request:
  - Review scope: `CommandDefinition.cs`, `AttributeScanner.cs`, `InstanceScanner.cs`, `InstanceCallbackBuilder.cs`, `CommandSystem.cs` (Register paths)
  - Primary checks: Every `new CommandDefinition(…)` call site passes `returnType`; void methods pass `typeof(void)`; property getter/setter distinction is correct; no public API surface change.
  - Required evidence: All existing tests pass. No new public API members.
  - Blocking conditions: Any `new CommandDefinition(…)` call site missing `returnType`; regression failure; `ReturnType` exposed publicly.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete

**Documentation Sync:**

- Docs to update in `docs/`: None required — `ReturnType` is internal only; no consumer-facing behavior changes.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update: `src/Core/CommandDefinition.cs` bullet — add `ReturnType` (internal `Type`, defaults to `typeof(object)`) to the property list.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `src/Core/CommandDefinition.cs`, `src/Core/AttributeScanner.cs`, `src/Core/InstanceScanner.cs`, `src/Core/InstanceCallbackBuilder.cs`
- Suggested commit message: `feat(commands-as-arguments): add ReturnType to CommandDefinition`

---

### Task 2: Add `ExecutionError` Enum Values, `ResolvedArg` Struct, and `ExecutionHandler.ExecuteResolved`

- [ ] Not started

**Objective:**

Add the 5 new `ExecutionError` enum values. Create the `ResolvedArg` internal struct. Add `ExecutionHandler.ExecuteResolved(string, ResolvedArg[])` — the parallel execution path for pre-resolved arguments. Add `NestedResolveResult` internal struct. The existing `Execute(string, string[])` method on `ExecutionHandler` is NOT modified. Unit-testable in isolation.

**Inputs:**

- Requirements refs: All type-validation requirements; structured error propagation
- Design refs: `ResolvedArg` contract; `ExecutionHandler.ExecuteResolved` contract; `ExecutionError` new values; `NestedResolveResult` contract

**Implementation Steps:**

1. In `src/Results/ExecutionResult.cs`: add 5 new `ExecutionError` enum values in this order after `InstanceNull`:
   ```
   NestedCommandDepthExceeded,
   NestedCommandFailed,
   NestedCommandVoidReturn,
   NestedCommandParseFailed,
   NestedCommandTypeMismatch
   ```
2. Create `src/Core/ResolvedArg.cs`:
   - `internal readonly struct ResolvedArg`
   - Private fields: `_isPreResolved (bool)`, `_stringValue (string)`, `_objectValue (object)`
   - Properties: `IsPreResolved`, `StringValue`, `ObjectValue`
   - `internal static ResolvedArg FromString(string value)` — `IsPreResolved = false`
   - `internal static ResolvedArg FromObject(object value)` — `IsPreResolved = true`
   - Add required source header
3. Create `src/Core/NestedResolveResult.cs`:
   - `internal readonly struct NestedResolveResult`
   - Properties: `Success (bool)`, `ResolvedArgs (ResolvedArg[])`, `Error (ExecutionResult)`
   - `internal static NestedResolveResult Ok(ResolvedArg[] args)`
   - `internal static NestedResolveResult Fail(ExecutionResult error)`
   - Add required source header
4. In `src/Core/ExecutionHandler.cs`: add `internal ExecutionResult ExecuteResolved(string commandName, ResolvedArg[] args)` method implementing the logic from the design's `ExecuteResolved` sketch — same lookup/count/default logic as `Execute(string, string[])`, but with the pre-resolved vs. string branch for each argument slot. The callback invocation and try/catch pattern are identical to `Execute`.
5. Add `ResolvedArg` parameter type to `ExecutionHandler` dependencies (it references `ResolvedArg` so needs `using kmCommands.Core;` or is in same namespace — confirm).
6. Run all existing tests — no failures expected.

**Validation:**

- Unit tests: Create `tests/kmCommands.Tests/ResolvedArgTests.cs`:
  - `FromString_SetsIsPreResolvedFalse`
  - `FromString_StringValueIsPreserved`
  - `FromObject_SetsIsPreResolvedTrue`
  - `FromObject_ObjectValueIsPreserved`
  - `FromObject_NullObjectValue_IsValid`
- Add `ExecuteResolved` isolation tests within a new `tests/kmCommands.Tests/NestedCommandTests.cs` (created empty now, main tests added in Task 4):
  - `ExecuteResolved_StringArg_ConvertedNormally`
  - `ExecuteResolved_PreResolvedArg_AssignableType_Passes`
  - `ExecuteResolved_PreResolvedArg_NullForValueType_ReturnsTypeMismatch`
  - `ExecuteResolved_PreResolvedArg_IncompatibleType_NoStringConverter_ReturnsTypeMismatch`
  - `ExecuteResolved_PreResolvedArg_IncompatibleTypeWithStringFallback_Passes`
  - `ExecuteResolved_CommandNotFound_ReturnsCommandNotFound`
  - `ExecuteResolved_ArgumentCountMismatch_ReturnsCountMismatch`
- QA quick pass (`taskReviewer`): Verify `Execute(string, string[])` is untouched; verify `ExecuteResolved` is additive only.
- taskReviewer review request:
  - Review scope: `ExecutionResult.cs` (enum), `ResolvedArg.cs` (new), `NestedResolveResult.cs` (new), `ExecutionHandler.cs` (new method only)
  - Primary checks: Existing `Execute(string, string[])` method is byte-for-byte unchanged. `ExecuteResolved` handles all pre-resolved arg branches (null value type, null ref type, assignable, fallback string conversion, incompatible). All 5 new `ExecutionError` values present.
  - Required evidence: All existing tests pass; new `ResolvedArgTests.cs` tests pass; `ExecuteResolved` isolation tests in `NestedCommandTests.cs` pass.
  - Blocking conditions: `Execute(string, string[])` modified; any new `ExecutionError` value missing; regression failure.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete

**Documentation Sync:**

- Docs to update in `docs/`: None for internal types. `ExecutionError` additions are public — will be covered in Task 4's doc sync when the complete feature is visible to consumers.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update:
  - `src/Results/ExecutionResult.cs` bullet — list the 5 new `ExecutionError` values.
  - `src/Core/` section — add `ResolvedArg.cs` and `NestedResolveResult.cs` entries.
  - `src/Core/ExecutionHandler.cs` bullet — note `ExecuteResolved` addition.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `src/Results/ExecutionResult.cs`, `src/Core/ResolvedArg.cs`, `src/Core/NestedResolveResult.cs`, `src/Core/ExecutionHandler.cs`, `tests/kmCommands.Tests/ResolvedArgTests.cs`, `tests/kmCommands.Tests/NestedCommandTests.cs`
- Suggested commit message: `feat(commands-as-arguments): add ResolvedArg, NestedResolveResult, ExecuteResolved, new error codes`

---

### Task 3: Add `NestedCommandTokenizer`

- [ ] Not started

**Objective:**

Create the pure-static `NestedCommandTokenizer` class with a single `Tokenize(string content)` method. This is a balanced-delimiter-aware whitespace tokenizer. No dependencies; fully unit-testable in isolation.

**Inputs:**

- Requirements refs: "the library MUST extract the inner command name and its own arguments"
- Design refs: `NestedCommandTokenizer` component description; Tokenizer pseudocode section

**Implementation Steps:**

1. Create `src/Core/NestedCommandTokenizer.cs`:
   - `internal static class NestedCommandTokenizer`
   - `internal static string[] Tokenize(string content)` — implements the balanced-delimiter state machine from the design's pseudocode:
     - Skip leading/inter-token whitespace (spaces only; tabs treated as spaces is acceptable)
     - For tokens starting with `$(`: track `depth`, advance until matching `)` closes depth back to 0
     - For normal tokens: advance until next space
     - Collect each token as `content.Substring(start, i - start)`
   - Null/empty input returns `Array.Empty<string>()`
   - Add required source header
2. Delimiter constants used internally: `"$("` open, `')'` close — these are local to the tokenizer method or sourced from a shared constant (define in `NestedCommandTokenizer` as `private const string OpenDelimiter = "$(";` and `private const char CloseDelimiter = ')';`).
3. Run all existing tests — no failures expected.

**Validation:**

- Unit tests: Create `tests/kmCommands.Tests/NestedCommandTokenizerTests.cs`:
  - `Tokenize_BasicArgs_ReturnsSplitTokens` — `"cmd arg1 arg2"` → `["cmd","arg1","arg2"]`
  - `Tokenize_SingleToken_ReturnsOneElement` — `"cmd"` → `["cmd"]`
  - `Tokenize_NullInput_ReturnsEmpty`
  - `Tokenize_EmptyString_ReturnsEmpty`
  - `Tokenize_LeadingAndTrailingSpaces_AreTrimmed`
  - `Tokenize_MultipleSpacesBetweenTokens_Collapsed`
  - `Tokenize_NestedDelimiterToken_KeptAtomic` — `"cmd $(inner 1) arg2"` → `["cmd","$(inner 1)","arg2"]`
  - `Tokenize_DeepNestedDelimiterToken_KeptAtomic` — `"cmd $(a $(b 1))"` → `["cmd","$(a $(b 1))"]`
  - `Tokenize_OnlyNestedToken` — `"$(inner 1)"` → `["$(inner 1)"]`
  - `Tokenize_UnbalancedParenTreatedAsLiteral_NoException` — `"$(unclosed"` does not throw
  - `Tokenize_EmptyNestedExpression` — `"$()"` → `["$()"]` (parse-failed detection happens in resolver, not tokenizer)
- QA quick pass (`taskReviewer`): Verify stateless, no allocations beyond `List<string>` + final `ToArray`, no LINQ.
- taskReviewer review request:
  - Review scope: `NestedCommandTokenizer.cs` (new), `NestedCommandTokenizerTests.cs` (new)
  - Primary checks: State machine is correct for all depth transitions; no exceptions on malformed input; no LINQ; `Array.Empty` for null/empty input.
  - Required evidence: All tokenizer tests pass; all existing tests pass.
  - Blocking conditions: LINQ usage; exception on malformed input; incorrect depth counting.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete

**Documentation Sync:**

- Docs to update in `docs/`: None — internal implementation detail.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update: `src/Core/` section — add `NestedCommandTokenizer.cs` entry.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `src/Core/NestedCommandTokenizer.cs`, `tests/kmCommands.Tests/NestedCommandTokenizerTests.cs`
- Suggested commit message: `feat(commands-as-arguments): add NestedCommandTokenizer`

---

### Task 4: Add `NestedCommandResolver` and Wire into `CommandSystem.Execute`

- [ ] Not started

**Objective:**

Create `NestedCommandResolver` with the recursive `ResolveArgs` logic. Wire it into `CommandSystem.Execute` via the fast-path `HasNestedTokens` check. `CommandSystem.InitializeCore` instantiates the resolver. `CommandSystem.Shutdown` nulls it. This is the core integration task.

**Depends on:** Tasks 1, 2, 3 complete.

**Inputs:**

- Requirements refs: All execution, depth-limit, history-recording, and error-propagation requirements
- Design refs: `NestedCommandResolver` component; resolver pseudocode; `CommandSystem.Execute` fast-path sketch; `InitializeCore` change; `Shutdown` change; `DefaultNestedCommandDepth` constant

**Implementation Steps:**

1. Create `src/Core/NestedCommandResolver.cs`:
   - `internal sealed class NestedCommandResolver`
   - Fields: `_registry (CommandRegistry)`, `_executionHandler (ExecutionHandler)`, `_historyBuffer (CommandHistoryBuffer)`, `_maxDepth (int readonly)`
   - Constructor: `internal NestedCommandResolver(CommandRegistry, ExecutionHandler, CommandHistoryBuffer, int maxDepth)`
   - `internal NestedResolveResult ResolveArgs(string[] args, int currentDepth)` — implements full resolver logic from design pseudocode:
     - For each arg: `IsNestedToken` check
     - Depth guard: `currentDepth >= _maxDepth` → `NestedCommandDepthExceeded`
     - Parse content via `NestedCommandTokenizer.Tokenize`; empty → `NestedCommandParseFailed`
     - Registry lookup; not found → `NestedCommandFailed`; void return → `NestedCommandVoidReturn`
     - Recursive `ResolveArgs(innerArgs, currentDepth + 1)`
     - Execute inner via `_executionHandler.ExecuteResolved`
     - Record inner to `_historyBuffer` (always, success and failure)
     - Inner failure → `NestedCommandFailed` (wrapping inner message)
     - `!HasReturnValue` → `NestedCommandVoidReturn`
     - Accumulate `ResolvedArg.FromObject(innerResult.ReturnValue)`
   - Private helpers: `static bool IsNestedToken(string arg)`, `static string[] BuildRawInput(string name, string[] args)` (mirrors the one in `CommandSystem`)
   - Add required source header
2. In `src/CommandSystem.cs`:
   - Add `public const int DefaultNestedCommandDepth = 4;` alongside `DefaultHistoryCapacity`
   - Add `private int _nestedCommandDepth = DefaultNestedCommandDepth;` field
   - Add `private NestedCommandResolver _nestedResolver;` field
   - In `InitializeCore`: after `_historyBuffer = new CommandHistoryBuffer(…)`, add `_nestedResolver = new NestedCommandResolver(_registry, _executionHandler, _historyBuffer, _nestedCommandDepth < 1 ? 1 : _nestedCommandDepth);`
   - In `Shutdown`: add `_nestedResolver = null;` and `_nestedCommandDepth = DefaultNestedCommandDepth;`
   - Modify `Execute(string commandName, string[] args)`: add `HasNestedTokens(args)` branch (see design fast-path sketch); when nesting present, call `_nestedResolver.ResolveArgs` then `_executionHandler.ExecuteResolved`; history recording happens after both branches, identical to today
   - Add private static `bool HasNestedTokens(string[] args)` method
3. Confirm `Initialize(CommandConfig config)` path already sets `_nestedCommandDepth = config.NestedCommandDepth` before `InitializeCore` (will be wired in Task 5 — ensure the field exists and is read by `InitializeCore` before Task 5 lands; leave default for now).
4. Run full test suite — all existing tests pass.

**Validation:**

- Unit tests: Complete `tests/kmCommands.Tests/NestedCommandTests.cs` with all integration tests (the file was created in Task 2 with `ExecuteResolved` isolation tests):

  _Happy path:_
  - `Execute_SingleNestedArg_InnerExecutes_OuterReceivesReturnValue`
  - `Execute_TwoLevelNesting_ResolvesFromInnermostFirst`
  - `Execute_ThreeLevelNesting_ResolvesCorrectly`
  - `Execute_MixedLiteralAndNestedArgs_BothResolveCorrectly`
  - `Execute_NoNestedArgs_BehaviorIdenticalToExistingPath`

  _Error paths:_
  - `Execute_NestedCommandEmpty_ReturnsNestedCommandParseFailed`
  - `Execute_NestedCommandNotFound_ReturnsNestedCommandFailed`
  - `Execute_NestedCommandExecutionFails_ReturnsNestedCommandFailed`
  - `Execute_NestedCommandVoidReturn_ReturnsNestedCommandVoidReturn`
  - `Execute_DepthExceeded_ReturnsNestedCommandDepthExceeded`
  - `Execute_DepthAtMax_Succeeds` (depth exactly at limit is ok)

  _History recording:_
  - `Execute_SuccessfulNesting_InnerAndOuterBothRecordedInHistory`
  - `Execute_InnerFailure_InnerEntryRecordedWithFailureStatus`
  - `Execute_InnerFailure_OuterEntryRecordedWithNestedCommandFailedStatus`
  - `Execute_InnerBeforeOuter_HistoryOrderIsCorrect`

  _Regression:_
  - Run full existing suite; all 391+ tests pass.

- QA quick pass (`taskReviewer`): Full integration review.
- taskReviewer review request:
  - Review scope: `NestedCommandResolver.cs` (new), `CommandSystem.cs` (Execute, InitializeCore, Shutdown additions)
  - Primary checks: Fast path is truly zero-overhead when no `$(` tokens present; inner history entries appear before outer in buffer chronologically; outer callback is never invoked when inner resolution fails; depth limit enforced correctly (off-by-one); `Shutdown` clears resolver and resets depth field.
  - Required evidence: All new integration tests in `NestedCommandTests.cs` pass; all existing tests pass.
  - Blocking conditions: Existing `Execute(string, string[])` flow broken; inner failure causes outer callback invocation; history recording inconsistency; off-by-one in depth limit; exception instead of structured error on any malformed input.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete

**Documentation Sync:**

- Docs to update in `docs/`:
  - `docs/commands.md` — add a section "Commands as Command Arguments": describe the `$(…)` delimiter syntax, depth limit, what errors to expect, consumer usage example.
  - `docs/architecture.md` — add `NestedCommandResolver` to the component overview; update data flow diagram entry for `Execute()` to note the nested path.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update:
  - Add `src/Core/NestedCommandResolver.cs` to `src/Core/` section.
  - Update `CommandSystem.cs` bullet to note `DefaultNestedCommandDepth`, `_nestedResolver`, `HasNestedTokens`, `_nestedCommandDepth` field.
  - Update Execution API summary: note nested command resolution via `$(…)` syntax.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `src/Core/NestedCommandResolver.cs`, `src/CommandSystem.cs`, `tests/kmCommands.Tests/NestedCommandTests.cs`, `docs/commands.md`, `docs/architecture.md`
- Suggested commit message: `feat(commands-as-arguments): add NestedCommandResolver, wire into CommandSystem.Execute`

---

### Task 5: Add `CommandConfig.NestedCommandDepth` and Wire `InitializeCore`

- [ ] Not started

**Objective:**

Add `NestedCommandDepth` to `CommandConfig`. Extend `FromJson` to parse `"nestedCommandDepth"` key with the same pattern as `historyCapacity`. Wire `Initialize(CommandConfig config)` to set `_nestedCommandDepth` before `InitializeCore` runs, so the resolver picks up the configured value.

**Inputs:**

- Requirements refs: "The depth limit MUST be configurable at `Initialize()` time via `CommandConfig` (new JSON key) and clamped to ≥ 1. The default value MUST be 4 when not specified."
- Design refs: `CommandConfig` changes section; `InitializeCore` change; `Initialize(CommandConfig)` snippet

**Implementation Steps:**

1. In `src/CommandConfig.cs`:
   - Add `public int NestedCommandDepth { get; set; } = CommandSystem.DefaultNestedCommandDepth;`
   - In `FromJson` key-matching loop, add `else if (StringEquals(entry.Key, "nestedCommandDepth"))` branch:
     - Type check: must be `typeof(int)` — else `ConfigResult.Fail(ConfigError.TypeMismatch, …)`
     - Assign `config.NestedCommandDepth = (int)entry.Value;`
2. In `src/CommandSystem.cs`, `Initialize(CommandConfig config)` method: add `_nestedCommandDepth = config.NestedCommandDepth;` before the `InitializeCore(config.HistoryCapacity)` call (field assignment gates into `InitializeCore`).
3. Verify `InitializeCore` already uses `_nestedCommandDepth` to construct the resolver (added in Task 4). If Task 4 is already complete, this is automatic.
4. Run full test suite — no failures.

**Validation:**

- Unit tests: Extend `tests/kmCommands.Tests/ConfigTests.cs`:
  - `FromJson_NestedCommandDepth_ParsedCorrectly` — `{ "nestedCommandDepth": 2 }` → `config.NestedCommandDepth == 2`
  - `FromJson_NestedCommandDepthAbsent_DefaultApplied` — `{}` → `config.NestedCommandDepth == CommandSystem.DefaultNestedCommandDepth` (i.e., 4)
  - `FromJson_NestedCommandDepth_TypeMismatch_ReturnsFail` — `{ "nestedCommandDepth": "bad" }` → `ConfigResult.Fail`
  - `Initialize_WithConfig_DepthApplied_DepthExceededAtConfiguredLimit` — integration test: init with depth=2, nest 3 levels, verify `NestedCommandDepthExceeded`; nest exactly 2 levels → success.
  - `Initialize_WithConfig_DepthZero_ClampedToOne` — depth=0 in config → resolver allows only 1 level.
- QA quick pass (`taskReviewer`): Verify key matching is case-insensitive (matching `historyCapacity` pattern); default applied when key absent; clamping in `InitializeCore` (≥1).
- taskReviewer review request:
  - Review scope: `CommandConfig.cs` (new property + JSON branch), `CommandSystem.cs` (`Initialize(CommandConfig)` addition)
  - Primary checks: Case-insensitive key match; type mismatch returns `Fail`; absent key uses default; `_nestedCommandDepth` is set before `InitializeCore` is called; clamped to ≥1 in `InitializeCore`.
  - Required evidence: All config tests pass; integration depth tests pass; all existing tests pass.
  - Blocking conditions: Key match is case-sensitive; missing clamping; depth not propagated to resolver.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete

**Documentation Sync:**

- Docs to update in `docs/`:
  - `docs/commands.md` — note `nestedCommandDepth` JSON config key and `DefaultNestedCommandDepth` constant in the "Commands as Command Arguments" section (added in Task 4).
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update:
  - `src/CommandConfig.cs` bullet — add `NestedCommandDepth` (int, default `DefaultNestedCommandDepth`).
  - Config API summary — note `nestedCommandDepth` JSON key.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `src/CommandConfig.cs`, `src/CommandSystem.cs`, `tests/kmCommands.Tests/ConfigTests.cs`
- Suggested commit message: `feat(commands-as-arguments): add CommandConfig.NestedCommandDepth`

---

### Task 6: Add Suggestion Delimiter Detection

- [ ] Not started

**Objective:**

Extend `CommandSystem.GetSuggestions(string prefix, ISuggestionMatcher)` to detect an unclosed `$(` in the prefix and strip to the innermost active command-name token for matching. When typing `$(get`, suggestions return commands starting with `"get"`. Non-nested prefixes are completely unaffected.

**Inputs:**

- Requirements refs: "`GetSuggestions(prefix)` MUST detect when the active token starts with the opening delimiter and return suggestions scoped to inner command names based on the content following the delimiter."
- Design refs: Suggestion delimiter detection section; `ExtractInnermostPrefix` method sketch

**Implementation Steps:**

1. In `src/CommandSystem.cs`: add `private static string ExtractInnermostPrefix(string prefix)` implementing the design's balanced-depth walk:
   - Walk `prefix` tracking depth via `$(` open / `)` close
   - Track `lastUnclosedStart` = index after the last unclosed `$(`
   - If `depth > 0` at end: extract `inner = prefix.Substring(lastUnclosedStart)`; find `lastSpace` in `inner`; return content after last space (the partial command name token)
   - Otherwise return `prefix` unchanged
2. In `GetSuggestions(string prefix, ISuggestionMatcher matcher)`: call `string effectivePrefix = ExtractInnermostPrefix(prefix);` immediately after the `IsInitialized` check; pass `effectivePrefix` to `effective.Match(…)` instead of `prefix`; keep the rest of the method unchanged.
3. Run full test suite.

**Validation:**

- Unit tests: Add suggestion delimiter tests to `tests/kmCommands.Tests/NestedCommandTests.cs` (or `SuggestionTests.cs` if preferred for grouping):
  - `GetSuggestions_OpenDelimiterAlone_ReturnsAllCommands` — `"$("` → all registered names
  - `GetSuggestions_OpenDelimiterWithPartialName_FiltersCorrectly` — `"$(get"` → names starting with "get"
  - `GetSuggestions_DoubleNested_InnermostPrefixUsed` — `"$(outer $(get"` → names starting with "get"
  - `GetSuggestions_ClosedNestedExpression_RevertsToOuterPrefix` — `"$(inner 1) suf"` → names starting with "suf" (outer prefix after closed expr)
  - `GetSuggestions_NormalPrefix_Unaffected` — `"healt"` → names starting with "healt" (no change)
  - `GetSuggestions_NullPrefix_ReturnsAll` — unchanged behavior
  - `GetSuggestions_EmptyPrefix_ReturnsAll` — unchanged behavior
- QA quick pass (`taskReviewer`): Verify `GetSuggestions(string)` single-arg overload still delegates to the two-arg overload unchanged.
- taskReviewer review request:
  - Review scope: `CommandSystem.cs` (`GetSuggestions` modification, `ExtractInnermostPrefix` new method)
  - Primary checks: `ExtractInnermostPrefix` is pure static with no side effects; depth tracking is correct (off-by-one risk); non-nested prefix returns unchanged; existing suggestion tests all still pass.
  - Required evidence: All new suggestion delimiter tests pass; all existing `SuggestionTests.cs` tests pass.
  - Blocking conditions: Existing suggestion behavior changed; off-by-one in depth; `GetSuggestions(string)` overload broken.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete

**Documentation Sync:**

- Docs to update in `docs/`:
  - `docs/commands.md` — in the "Commands as Command Arguments" section, describe the autocomplete behavior when `$(` is detected in the prefix.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update:
  - Suggestion API summary — note that `GetSuggestions` detects `$(` prefix and returns inner command suggestions.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `src/CommandSystem.cs`, `tests/kmCommands.Tests/NestedCommandTests.cs` (or `SuggestionTests.cs`)
- Suggested commit message: `feat(commands-as-arguments): add nested command suggestion delimiter detection`

---

### Task 7: Final Regression, Documentation Polish, and `projectOverview` Sync

- [ ] Not started

**Objective:**

Run the full test suite from clean to confirm all tasks integrate cleanly. Polish the `docs/commands.md` and `docs/architecture.md` sections added across tasks 4–6. Ensure `.github/instructions/projectOverview.instructions.md` is fully and accurately updated. Update `vision.md` to mark the feature as implemented.

**Inputs:**

- Requirements refs: All acceptance criteria
- Design refs: Final Review Contract; Testing Strategy

**Implementation Steps:**

1. Run the full test suite: `dotnet test tests/kmCommands.Tests/kmCommands.Tests.csproj`. All tests pass.
2. Review `docs/commands.md` "Commands as Command Arguments" section added in Tasks 4–6: ensure completeness — delimiter syntax, consumer example, depth config, suggestion behavior, all error codes documented.
3. Review `docs/architecture.md` updated in Task 4: verify component overview and `Execute()` data flow are accurate.
4. Review `.github/instructions/projectOverview.instructions.md`: verify every task's sync items were applied. If any were deferred, apply now.
5. In `docs/vision.md`: change `### 🔲 Commands as Command Arguments` to `### ✅ Commands as Command Arguments` and mark all sub-items `[x]`.

**Validation:**

- Unit tests: Full suite run. Target: all existing (391+) + all new tests pass. The specific new test counts expected:
  - `ResolvedArgTests.cs`: ≥ 5 tests
  - `NestedCommandTokenizerTests.cs`: ≥ 11 tests
  - `NestedCommandTests.cs`: ≥ 22 tests (including `ExecuteResolved` isolation + integration + history + suggestion tests)
  - `ConfigTests.cs`: ≥ 3 new tests
- Additional checks: Confirm no compiler warnings introduced. Confirm no new public API beyond what is explicitly specified in design.
- QA quick pass (`taskReviewer`): Final design-vs-implementation fidelity check.
- taskReviewer review request:
  - Review scope: End-to-end — all files changed across Tasks 1–6; full test suite output; `docs/`; `projectOverview.instructions.md`
  - Primary checks: All acceptance criteria from requirements met; all design invariants hold; docs are accurate and complete; `projectOverview` is accurate; `vision.md` updated.
  - Required evidence: Full test suite run output showing 100% pass; docs review notes; `projectOverview` diff.
  - Blocking conditions: Any test failure; any acceptance criterion unmet; any design invariant violated; `projectOverview` inaccurate.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete

**Documentation Sync:**

- Docs to update in `docs/`: Final polish of `commands.md`, `architecture.md`.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update: All items deferred from Tasks 1–6 + final check of all changed files.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `docs/commands.md`, `docs/architecture.md`, `docs/vision.md`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `docs(commands-as-arguments): final docs and projectOverview sync`

---

## Coverage Check

- Requirements coverage:
  - [ ] Every requirement is mapped to at least one task
  - [ ] No requirement is left unplanned

- Design coverage:
  - [ ] Key design components are mapped to tasks
  - [ ] Critical design constraints are represented in validation gates

### Requirements-to-Task Mapping

| Requirement                                                             | Task(s)                                                                                                              |
| ----------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| Fixed delimiter pair defined as library constant                        | T3 (constants in tokenizer), T4 (resolver uses them)                                                                 |
| Arg starting with `$(` and ending with `)` triggers inner resolution    | T4 (`IsNestedToken`, resolver)                                                                                       |
| Recursive resolution up to depth limit                                  | T4 (resolver recursion)                                                                                              |
| Depth limit clamped to ≥ 1, default 4, configurable via `CommandConfig` | T4 (`DefaultNestedCommandDepth`, `InitializeCore`), T5 (`CommandConfig`)                                             |
| Type compatibility validation (inner return vs. outer param)            | T2 (`ExecuteResolved` type-check branch)                                                                             |
| Structured error on type mismatch — no callback invoked                 | T2 (`NestedCommandTypeMismatch`), T4 (propagation)                                                                   |
| Depth exceeded returns structured error, no partial execution           | T4 (`NestedCommandDepthExceeded`)                                                                                    |
| Inner command failure propagates as structured error in outer           | T4 (`NestedCommandFailed` wrapping)                                                                                  |
| Inner commands recorded in history independently                        | T4 (resolver records after inner execution)                                                                          |
| Outer command recorded in history (all outcomes)                        | T4 (existing path unchanged + nested path records)                                                                   |
| `GetSuggestions` detects `$(` and returns inner command suggestions     | T6                                                                                                                   |
| AOT/IL2CPP safe — no codegen, no Emit, no Expressions                   | T2, T3, T4 (no prohibited APIs used; enforced in QA reviews)                                                         |
| Deterministic unit test coverage                                        | T2 (`ResolvedArgTests`), T3 (`TokenizerTests`), T4 (`NestedCommandTests`), T5 (`ConfigTests`), T6 (suggestion tests) |

### Design-to-Task Mapping

| Design Component                                    | Task(s)                                                        |
| --------------------------------------------------- | -------------------------------------------------------------- |
| `ResolvedArg` struct                                | T2                                                             |
| `NestedResolveResult` struct                        | T2                                                             |
| `ExecutionHandler.ExecuteResolved`                  | T2                                                             |
| 5 new `ExecutionError` values                       | T2                                                             |
| `NestedCommandTokenizer` class                      | T3                                                             |
| `CommandDefinition.ReturnType`                      | T1                                                             |
| `NestedCommandResolver` class                       | T4                                                             |
| `CommandSystem.Execute` fast-path + nested path     | T4                                                             |
| `CommandSystem.InitializeCore` wiring               | T4                                                             |
| `CommandSystem.Shutdown` cleanup                    | T4                                                             |
| `CommandSystem.DefaultNestedCommandDepth` constant  | T4                                                             |
| `CommandConfig.NestedCommandDepth` property         | T5                                                             |
| `CommandConfig.FromJson` `"nestedCommandDepth"` key | T5                                                             |
| `CommandSystem.GetSuggestions` delimiter detection  | T6                                                             |
| `ExtractInnermostPrefix` helper                     | T6                                                             |
| `docs/commands.md` update                           | T4 (section), T5 (depth config), T6 (suggestions), T7 (polish) |
| `docs/architecture.md` update                       | T4, T7                                                         |
| `projectOverview.instructions.md` sync              | T1–T6 (per-task), T7 (final)                                   |

### Gaps or Follow-ups

- `ReturnType` on manually registered commands defaults to `typeof(object)`. This means the pre-execution void check is bypassed for manual registrations (runtime `HasReturnValue` check still catches it). Acceptable deviation — documented in design; no follow-up required unless consumer feedback warrants it.
- `CommandMetadataSnapshot` is not changed — it does not need to expose nesting metadata. Verified in Task 7 regression.
- The `$(…)$` suffix notation in the requirements file was a placeholder — design resolved this to `$(…)` (close paren only, balanced). Requirements open question is closed.
