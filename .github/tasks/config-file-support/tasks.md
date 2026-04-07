# Configuration File Support Tasks

## Status

- [x] Planned
- [x] In Progress
- [x] Completed

## Inputs

- Requirements: `.github/tasks/config-file-support/requirements.md`
- Design: `.github/tasks/config-file-support/design.md`

## Branch

- Name: `feat_config-file-support`
- Rationale: `feat_` — new capability; consumers can now drive `CommandSystem` initialisation from a declarative JSON file rather than code-only `Initialize()` calls.

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status -> Completed` may be checked only after all tasks and `## Coverage Check` items are checked.

## Task List

---

### Task 1: Implement `JsonConfigParser` (internal)

- [x] Completed (commit 0310db9)

**Objective:**

Implement the internal `JsonConfigParser` static class in `src/Core/JsonConfigParser.cs`. This is the hand-rolled, AOT-safe, dependency-free JSON object parser that backs `CommandConfig.FromJson`. No public API changes in this task.

**Inputs:**

- Requirements refs: REQ-8 (no third-party deps), REQ-9 (AOT/IL2CPP safety).
- Design refs: `JsonConfigParser` component spec; `ParseOutput`/`ParsedValue` structs; implementation notes on parser scope; duplicate-key last-write-wins; negative integer support.

**Implementation Steps:**

1. Create `src/Core/JsonConfigParser.cs` with the required source header.
2. Declare `namespace kmCommands.Core`.
3. Implement internal `readonly struct ParsedValue` with fields: `string Key`, `object Value`, `Type ValueType` (`null` for JSON null).
4. Implement internal `readonly struct ParseOutput` with fields: `ParsedValue[] Values`, `string Error`, `bool HasError`.
5. Implement `internal static class JsonConfigParser` with `internal static ParseOutput Parse(string json)`.
6. Parser logic:
   - Skip leading whitespace; expect `{`; return error if not found.
   - Loop: skip whitespace; parse quoted string key; skip whitespace; expect `:`; skip whitespace; parse value (string/integer/boolean/null); add `ParsedValue` to list; skip whitespace; expect `,` or `}`; on `}` return success.
   - Support negative integers (leading `-`).
   - Last-write-wins for duplicate keys (no warning or error).
   - Return `ParseOutput` with error string if any structurally malformed input is encountered.
   - Return error for trailing non-whitespace content after closing `}`.
7. Values supported: quoted `string`, signed `integer`, `true`/`false`, `null`.
8. Do NOT support: nested objects, arrays, floating-point, unicode escape sequences beyond basic ASCII.

**Validation:**

- Unit tests: Add `ConfigParserTests` class in `tests/kmCommands.Tests/ConfigTests.cs` (internal parser tests via `InternalsVisibleTo` if needed, or cover via `CommandConfig` integration — see note below).
  - Note: If `InternalsVisibleTo` is already configured for the test project, add direct `JsonConfigParser` tests. Otherwise, parser coverage will be captured indirectly in Task 5 via `FromJson` tests. Either is acceptable.
- Test cases to cover directly or via `FromJson`:
  - Valid flat object with string, int, bool, null values → `HasError == false`, correct `Values`.
  - Empty object `{}` → `HasError == false`, `Values.Length == 0`.
  - Whitespace-heavy input → parses correctly.
  - Negative integer → parsed as `int`.
  - Duplicate keys → last value wins.
  - Missing `{` → `HasError == true`.
  - Missing `:` → `HasError == true`.
  - Unclosed object → `HasError == true`.
  - Trailing content after `}` → `HasError == true`.
- QA quick pass (`taskReviewer`): Run after implementation.
- taskReviewer review request:
  - Review scope: `src/Core/JsonConfigParser.cs` — new internal static class.
  - Primary checks: AOT safety (no reflection, no generics, no `dynamic`); correct `netstandard2.0` compatibility; all value type branches covered (string, int, bool, null); error cases return `HasError = true` with a non-null `Error`; whitespace handling; negative integers accepted; duplicate key last-write-wins.
  - Required evidence: Relevant parser tests (direct or via `FromJson`) pass.
  - Blocking conditions: Any reflection usage; `dynamic` keyword; third-party dependency; build failure on `netstandard2.0`; value-type branches missing.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — internal implementation only; no public API surface yet.
- Update `.github/instructions/projectOverview.instructions.md` required: No (internal type; no public API change).

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

- Suggested commit scope: `feat(config-file-support)`
- Suggested commit message: `feat(config-file-support): add internal JsonConfigParser`

---

### Task 2: Add `ConfigError` enum and `ConfigResult` struct

- [x] Completed (commit 12ab2ff)

**Objective:**

Create the public result type pair `ConfigError` (enum) and `ConfigResult` (readonly struct) in `src/Results/ConfigResult.cs`. These are the output types for `CommandConfig.FromJson` and `CommandConfig.FromFile`. No parser or `CommandConfig` code in this task.

**Inputs:**

- Requirements refs: REQ-2 (ConfigResult carrying success/warnings or failure), REQ-6 (malformed/type-mismatch errors).
- Design refs: `ConfigError` enum spec; `ConfigResult` readonly struct spec; `internal static Ok/Fail` factory pattern; `Warnings` never null on success; `Config` null on failure.

**Implementation Steps:**

1. Create `src/Results/ConfigResult.cs` with the required source header.
2. Declare `namespace kmCommands`.
3. Implement `public enum ConfigError { None = 0, InvalidJson, TypeMismatch, FileReadError }`.
4. Implement `public readonly struct ConfigResult`:
   - `public bool Success { get; }`
   - `public CommandConfig Config { get; }` — `null` when `Success == false`.
   - `public ConfigError Error { get; }`
   - `public string ErrorMessage { get; }`
   - `public string[] Warnings { get; }` — never `null` when `Success == true`; empty array if no warnings.
   - `internal static ConfigResult Ok(CommandConfig config, string[] warnings)` factory.
   - `internal static ConfigResult Fail(ConfigError error, string message)` factory: sets `Success = false`, `Config = null`, `Warnings = Array.Empty<string>()`.
5. Follow existing result type conventions (see `UnregisterResult.cs`, `ExecutionResult.cs` for patterns).

**Validation:**

- Unit tests: Verify factory behaviour in isolation — `Ok(config, warnings)` sets correct fields; `Fail(error, message)` sets `Success = false`, `Config == null`, `Warnings` not null.
- All tests pass.
- QA quick pass (`taskReviewer`): Run after implementation.
- taskReviewer review request:
  - Review scope: `src/Results/ConfigResult.cs` — new public `ConfigError` enum and `ConfigResult` readonly struct.
  - Primary checks: `Warnings` never null; `Config` null on failure; `Success` correct in both factory paths; enum values match spec; follows existing result type patterns in `src/Results/`.
  - Required evidence: Factory behaviour tests pass.
  - Blocking conditions: `Warnings` null on success; `Config` non-null on failure; deviations from enum spec.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None yet — public types declared but no consumer-facing API available until Task 3.
- Update `.github/instructions/projectOverview.instructions.md` required: No — will be updated in Task 4 once the full public API is in place.

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

- Suggested commit scope: `feat(config-file-support)`
- Suggested commit message: `feat(config-file-support): add ConfigError enum and ConfigResult struct`

---

### Task 3: Implement `CommandConfig` class

- [x] Completed (commit 31b8c2d)

**Objective:**

Create the public `CommandConfig` sealed class in `src/CommandConfig.cs` with typed defaults, `FromJson`, and `FromFile` static factories. This task wires `JsonConfigParser` (Task 1) and `ConfigResult` (Task 2) into a single consumer-facing API.

**Inputs:**

- Requirements refs: REQ-1 (`CommandConfig` class with defaults), REQ-2 (`FromJson`), REQ-3 (`FromFile`), REQ-5 (unknown-key warnings), REQ-6 (malformed/type-mismatch errors), REQ-7 (coded defaults), REQ-9 (AOT safety).
- Design refs: `CommandConfig` class spec; `FromJson` implementation sketch; `FromFile` implementation sketch; case-insensitive key matching; type validation rules table; null/empty JSON guard.

**Implementation Steps:**

1. Create `src/CommandConfig.cs` with the required source header.
2. Declare `namespace kmCommands`.
3. Implement `public sealed class CommandConfig`:
   - `public int HistoryCapacity { get; set; } = CommandSystem.DefaultHistoryCapacity;`
   - `public bool DevMode { get; set; }` (default `false`).
4. Implement `public static ConfigResult FromJson(string json)`:
   - Guard: null/empty → `ConfigResult.Fail(ConfigError.InvalidJson, "JSON string must not be null or empty.")`.
   - Call `JsonConfigParser.Parse(json)`; on error → `ConfigResult.Fail(ConfigError.InvalidJson, output.Error)`.
   - Iterate `output.Values`; use case-insensitive `StringComparer.OrdinalIgnoreCase` (or manual `string.Equals(..., OrdinalIgnoreCase)`) to match `"historyCapacity"` and `"devMode"`.
   - Known key with correct type → assign to `CommandConfig`.
   - Known key with wrong type (including JSON null) → `ConfigResult.Fail(ConfigError.TypeMismatch, descriptive message)`.
   - Unknown key → add warning `"Unknown config key: '<key>'."`.
   - Return `ConfigResult.Ok(config, warnings.ToArray())` or `Array.Empty<string>()` when no warnings.
5. Implement `public static ConfigResult FromFile(string filePath)`:
   - Guard: null/empty path → `ConfigResult.Fail(ConfigError.FileReadError, "File path must not be null or empty.")`.
   - `System.IO.File.ReadAllText(filePath)` in a `try/catch (Exception ex)` → `ConfigResult.Fail(ConfigError.FileReadError, ex.Message)`.
   - Delegate to `FromJson(text)`.
6. Add private static `StringEquals` helper using `StringComparison.OrdinalIgnoreCase`.

**Validation:**

- Unit tests (subset targeted at this task, full suite in Task 5):
  - `new CommandConfig()` defaults: `HistoryCapacity == CommandSystem.DefaultHistoryCapacity`, `DevMode == false`.
  - `FromJson("{}")` → `Success`, defaults preserved.
  - `FromJson` with full valid JSON → `Success`, correct values set.
  - `FromJson` with unknown key → `Success`, one warning.
  - `FromJson` with null → `Fail`, `ConfigError.InvalidJson`.
  - `FromJson` with type mismatch → `Fail`, `ConfigError.TypeMismatch`.
  - `FromFile` with non-existent path → `Fail`, `ConfigError.FileReadError`.
  - `FromFile` with null/empty path → `Fail`, `ConfigError.FileReadError`.
- All existing tests (306) still pass.
- QA quick pass (`taskReviewer`): Run after implementation.
- taskReviewer review request:
  - Review scope: `src/CommandConfig.cs` — new public `CommandConfig` sealed class.
  - Primary checks: Coded defaults match `DefaultHistoryCapacity` and `false`; case-insensitive key matching; type-mismatch path returns `Fail` not warning; unknown key returns warning not `Fail`; `Warnings` never null; `FromFile` delegates to `FromJson` after read; broad `catch (Exception)` at `FromFile` file-read boundary.
  - Required evidence: Targeted unit tests pass; full existing test suite (306) passes.
  - Blocking conditions: Type mismatch treated as warning; null JSON not caught; defaults diverge from `DefaultHistoryCapacity`; reflection usage.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None yet — deferred to Task 4 where the full API including `Initialize(CommandConfig)` is added.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to Task 4.

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

- Suggested commit scope: `feat(config-file-support)`
- Suggested commit message: `feat(config-file-support): add CommandConfig class with FromJson and FromFile`

---

### Task 4: Add `CommandSystem.Initialize(CommandConfig)` overload and update docs

- [x] Completed (commit 943e46d)

**Objective:**

Add the `Initialize(CommandConfig config)` overload to `CommandSystem`. Update `docs/commands.md` (or `docs/unity-integration.md` as applicable) with config file usage guidance. Update `.github/instructions/projectOverview.instructions.md` with the new public types and API surface.

**Inputs:**

- Requirements refs: REQ-4 (`Initialize(CommandConfig)` overload), REQ-10 (`Shutdown()` re-init), REQ-4 (null config no-op), REQ-4 (already-initialised no-op).
- Design refs: `CommandSystem.Initialize(CommandConfig)` overload spec; null config guard; `_devMode` + `InitializeCore` call pattern; `Shutdown()` behaviour note.

**Implementation Steps:**

1. Open `src/CommandSystem.cs`.
2. Add the new overload adjacent to the existing `Initialize` overloads:
   ```csharp
   public void Initialize(CommandConfig config)
   {
       if (IsInitialized) { return; }
       if (config == null) { return; }
       _devMode = config.DevMode;
       InitializeCore(config.HistoryCapacity);
   }
   ```
3. Confirm this new overload does not require any changes to `Shutdown()` (config consumed at init time; existing cleanup covers `_devMode` reset).
4. Update `docs/commands.md` (config file usage section): document `CommandConfig.FromFile`, `CommandConfig.FromJson`, `ConfigResult`, `ConfigError`, and `Initialize(CommandConfig)` with a minimal usage example.
5. Update `docs/unity-integration.md` to reference config file initialisation as an alternative to code-only init.
6. Update `.github/instructions/projectOverview.instructions.md`:
   - Add `src/CommandConfig.cs` to Key Paths.
   - Add `src/Results/ConfigResult.cs` to Key Paths.
   - Add `src/Core/JsonConfigParser.cs` to Key Paths.
   - Add `ConfigError` and `ConfigResult` entries to the Results section in Implementation Direction.
   - Add `CommandConfig` entry to Implementation Direction.
   - Update API Layer Summary with the new `Initialize(CommandConfig)` overload and config-related factory methods.

**Validation:**

- Unit tests: `Initialize(CommandConfig)` integration — see design; verify idempotency, null no-op, devMode and capacity applied.
- All existing tests (306) still pass.
- QA quick pass (`taskReviewer`): Run after implementation.
- taskReviewer review request:
  - Review scope: `src/CommandSystem.cs` new overload; `docs/` updates; `projectOverview.instructions.md` updates.
  - Primary checks: Overload behaviour identical to `Initialize(historyCapacity, devMode)` for equivalent inputs; null no-op; already-initialised no-op; `Shutdown()` → re-init works; doc examples are accurate; projectOverview reflects new types.
  - Required evidence: Integration unit tests pass; full existing test suite (306) passes.
  - Blocking conditions: `Initialize(CommandConfig)` behaves differently from equivalent `Initialize(historyCapacity, devMode)`; null config throws instead of no-op; docs not updated.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md` (config file usage section); `docs/unity-integration.md` (alternative init path).
- Update `.github/instructions/projectOverview.instructions.md` required: **Yes**.
- Sections to update: Key Paths; API Layer Summary; Implementation Direction (results, `CommandConfig`, `CommandSystem`).

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

- Suggested commit scope: `feat(config-file-support)`
- Suggested commit message: `feat(config-file-support): add Initialize(CommandConfig) overload and update docs`

---

### Task 5: Full integration and unit test suite (`ConfigTests.cs`)

- [x] Completed (commit d745083)

**Objective:**

Write the complete `ConfigTests.cs` test class covering all acceptance scenarios from the design's testing strategy. This is the final validation pass confirming the entire feature works end-to-end before PR.

**Inputs:**

- Requirements refs: All requirements — this is the acceptance verification pass.
- Design refs: Testing strategy section (all bullets); final review contract (critical behaviours 1–10; design invariants; required test evidence).

**Implementation Steps:**

1. Create `tests/kmCommands.Tests/ConfigTests.cs` (or consolidate into an existing file if one was started in earlier tasks).
2. Follow existing test conventions: `[TestFixture]`, `CommandSystem` instance, `SetUp`/`TearDown`.
3. Implement all test cases from the design's testing strategy:

   **`CommandConfig` defaults:**
   - `new CommandConfig()` → `HistoryCapacity == DefaultHistoryCapacity`, `DevMode == false`.

   **`FromJson` — valid inputs:**
   - Full config `{ "historyCapacity": 128, "devMode": true }` → `Success`, values correct.
   - Partial `{ "historyCapacity": 256 }` → `Success`, `DevMode == false`.
   - Partial `{ "devMode": true }` → `Success`, `HistoryCapacity == DefaultHistoryCapacity`.
   - Empty `{}` → `Success`, all defaults.
   - Whitespace-heavy `{  "historyCapacity" :  128  }` → `Success`.
   - Negative integer `{ "historyCapacity": -5 }` → `Success` (clamping is InitializeCore's job).
   - Zero `{ "historyCapacity": 0 }` → `Success`.
   - Case-insensitive: `{ "HISTORYCAPACITY": 100 }` → `Success`, `HistoryCapacity == 100`.

   **`FromJson` — unknown keys (warnings):**
   - `{ "historyCapacity": 64, "unknownKey": "foo" }` → `Success`, one warning containing `"unknownKey"`.
   - `{ "a": 1, "b": true }` → `Success`, two warnings.
   - Unknown key with string, int, bool, null value — all produce warnings, not errors.

   **`FromJson` — errors:**
   - Null → `Fail`, `ConfigError.InvalidJson`.
   - Empty string → `Fail`, `ConfigError.InvalidJson`.
   - Malformed `{ broken` → `Fail`, `ConfigError.InvalidJson`.
   - `{ "devMode": 42 }` → `Fail`, `ConfigError.TypeMismatch`.
   - `{ "historyCapacity": true }` → `Fail`, `ConfigError.TypeMismatch`.
   - `{ "historyCapacity": "128" }` → `Fail`, `ConfigError.TypeMismatch`.
   - `{ "devMode": "true" }` → `Fail`, `ConfigError.TypeMismatch`.
   - `{ "historyCapacity": null }` → `Fail`, `ConfigError.TypeMismatch`.

   **`FromFile` — errors:**
   - Non-existent path → `Fail`, `ConfigError.FileReadError`.
   - Null path → `Fail`, `ConfigError.FileReadError`.
   - Empty path → `Fail`, `ConfigError.FileReadError`.

   **`FromFile` — valid temp file:**
   - Write temp JSON, call `FromFile`, verify `Success` and correct values. Clean up temp file in `TearDown`.

   **`Initialize(CommandConfig)` — integration:**
   - `Initialize(config)` with capacity 128, `DevMode = true` → system initialised, devMode applied (register dev-only command; verify it is registered).
   - `Initialize(config)` when already initialised → no-op, `IsInitialized` remains `true`.
   - `Initialize(null)` → no-op, `IsInitialized` remains `false`.
   - `Shutdown()` → `Initialize(config)` → works correctly.
   - `Initialize(config)` with `HistoryCapacity = 0` → initialised (clamped), history works.

4. Confirm all 306 pre-existing tests still pass (no regressions).
5. Confirm total passing test count increases by the number of new tests added.

**Validation:**

- All new tests in `ConfigTests.cs` pass.
- All 306 pre-existing tests pass (no regressions).
- QA quick pass (`taskReviewer`): Run after tests are written and passing.
- taskReviewer review request:
  - Review scope: `tests/kmCommands.Tests/ConfigTests.cs` — full config feature test suite.
  - Primary checks: All design testing strategy bullets covered; design invariants verified (`Warnings` never null on success; `Config` null on failure; no third-party deps; AOT-safe); final review contract critical behaviours 1–10 each have a corresponding passing test; no regressions.
  - Required evidence: Full test run output showing all new tests passing and no regressions in the 306 pre-existing tests.
  - Blocking conditions: Any design critical behaviour without a test; regressions in pre-existing tests; `ConfigResult` invariant violated.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — documentation was completed in Task 4.
- Update `.github/instructions/projectOverview.instructions.md` required: No — completed in Task 4; update test count to reflect actual final count after this task.
  - If Yes: update `tests/kmCommands.Tests/` test count note in Key Paths section.

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

- Suggested commit scope: `feat(config-file-support)`
- Suggested commit message: `feat(config-file-support): add ConfigTests covering full acceptance suite`

---

## Coverage Check

- Requirements coverage:
  - [ ] Every requirement is mapped to at least one task
  - [ ] No requirement is left unplanned

- Design coverage:
  - [ ] Key design components are mapped to tasks
  - [ ] Critical design constraints are represented in validation gates

- Gaps or follow-ups:
  - REQ-1 (`CommandConfig` class + defaults) → Task 3
  - REQ-2 (`FromJson` + `ConfigResult`) → Tasks 1, 2, 3
  - REQ-3 (`FromFile`) → Task 3
  - REQ-4 (`Initialize(CommandConfig)`) → Task 4
  - REQ-5 (unknown-key warnings) → Task 3, validated in Task 5
  - REQ-6 (malformed/type-mismatch errors) → Tasks 1, 3, validated in Task 5
  - REQ-7 (coded defaults matching existing `Initialize()`) → Task 3, validated in Task 5
  - REQ-8 (no third-party dependencies) → Task 1 (parser), enforced in all tasks
  - REQ-9 (AOT/IL2CPP safety) → Task 1 (parser), Task 3 (`FromJson` key matching), enforced throughout
  - REQ-10 (`Shutdown()` → re-init) → Task 4 (doc/overload), Task 5 (integration test)
  - Design `JsonConfigParser` component → Task 1
  - Design `ConfigError` + `ConfigResult` → Task 2
  - Design `CommandConfig` → Task 3
  - Design `Initialize(CommandConfig)` → Task 4
  - Design testing strategy (all bullets) → Task 5
  - Design final review contract (critical behaviours 1–10) → Task 5
  - Documentation update (`docs/commands.md`, `docs/unity-integration.md`) → Task 4
  - `projectOverview.instructions.md` sync → Task 4
  - No known gaps.
