# Command Description / Help Text — Tasks

## Status

- [ ] Planned
- [ ] In Progress
- [ ] Completed

## Inputs

- Requirements: `.github/tasks/command-help-text/requirements.md`
- Design: `.github/tasks/command-help-text/design.md`

## Branch

- Name: `feat_command-help-text`
- Rationale: `feat_` — new public API capability; adds optional description to commands and exposes it through the snapshot discovery API.

---

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task results in one commit.
- Keep commits scoped to the task objective.
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status → Completed` may be checked only after all tasks and `## Coverage Check` items are checked.

---

## Task List

### Task 1: Data Model — `CommandDefinition` and `CommandAttribute`

- [ ] Not started

**Objective:**

Add the `Description` string field to the internal `CommandDefinition` storage model and the `Description` named property to the public `[Command]` attribute. These are pure data-model additions with no behavior change; all existing callers continue to compile and behave identically.

**Inputs:**

- Requirements refs: Goals #1, #2, #3, FR #1–#4, Assumptions #1, #4, #6
- Design refs: Components → `CommandDefinition`, `CommandAttribute`; Code Examples → `CommandDefinition` updated constructor, `CommandAttribute` new property

**Implementation Steps:**

1. Open `src/Core/CommandDefinition.cs`.
   - Add `internal string Description { get; }` get-only auto-property.
   - Add a `description` parameter (type `string`) as the 4th positional argument of the existing constructor, after `callback`.
   - In the constructor body, assign `Description = description;` directly after the `Callback` assignment.
   - No other change to the constructor body.
2. Open `src/CommandAttribute.cs`.
   - Add `public string Description { get; set; }` — settable named property, defaulting to `null` (no initializer needed; C# default for `string` is `null`).
   - Follow the existing `IsDevOnly` property pattern (same access level, no backing field needed for an auto-property).
   - Add an XML doc comment: "An optional human-readable description of what this command does. Defaults to `null` when not set."

**Validation:**

- Build check: `dotnet build` — zero errors, zero new warnings.
- Regression guard: `dotnet test tests/kmCommands.Tests/kmCommands.Tests.csproj` — all 103 existing tests pass. (Note: this step will temporarily cause a compile error in `AttributeScanner.cs` and `CommandRegistry.cs` because they still construct `CommandDefinition` with the old 3-arg signature. Suppress by adding `description: null` to those construction sites as interim fixes, or accept the compile error will be resolved in Tasks 2-4.)

  > **Practical note:** Because `CommandDefinition`'s constructor is internal, only the files in `src/` that directly call `new CommandDefinition(...)` need updating. Identify them before marking this task done. The call sites are: `CommandSystem.cs` (Task 2) and `AttributeScanner.cs` (Task 3). Both will be updated in their respective tasks. For Task 1 itself, the build **will fail** until those callers are updated. You may combine Task 1 into a single commit with Task 2 if the compile error is blockers to independent validation; however, the preferred approach is:
  >  - Update `CommandSystem.cs` temporarily to pass `null` as the 4th arg (an interim change that will be overwritten in Task 2).
  >  - Update `AttributeScanner.cs` temporarily to pass `null` as the 4th arg (an interim change that will be overwritten in Task 3).
  >  - This allows Task 1 to compile and pass all existing tests independently.

- QA quick pass (`taskReviewer`): verify `CommandDefinition` constructor accepts `null` for `description`; verify `CommandAttribute.Description` compiles with named-arg syntax.

- taskReviewer review request:
  - Review scope: `src/Core/CommandDefinition.cs` (constructor + new property), `src/CommandAttribute.cs` (new property).
  - Primary checks: `Description` property is get-only on `CommandDefinition`; no mutation possible after construction. `CommandAttribute.Description` is settable (named-arg pattern). No existing constructor call sites broken (interim null values or callers updated).
  - Required evidence: `dotnet build` output showing zero errors; `dotnet test` showing 103/103 passing.
  - Blocking conditions: `Description` is mutable on `CommandDefinition`; existing constructor callers fail to compile; test regression.

- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — this is an internal data model change with no public-facing behavior yet.
- Update `.github/instructions/projectOverview.instructions.md` required: No (no public API or architecture change yet).

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (build + 103 tests passing)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `feat(command-help-text)`
- Suggested commit message: `feat(command-help-text): add Description field to CommandDefinition and CommandAttribute`

---

### Task 2: Registration API — `CommandSystem.Register()` 4-Arg Overload

- [ ] Not started

**Objective:**

Refactor `CommandSystem.Register()` so the existing 3-arg overload becomes a one-line delegation wrapper that calls the new 4-arg overload with `description: null`. All existing validation logic moves into the 4-arg overload unchanged. This ensures backward compatibility and opens the public API for description-bearing registrations.

**Inputs:**

- Requirements refs: FR #1, FR #2, AC #1–#3, AC #10, AC #11
- Design refs: Components → `CommandSystem.cs`; Code Examples → `Register()` delegation + new overload; Data Flow → Registration (manual)

**Implementation Steps:**

1. Open `src/CommandSystem.cs`.
2. Locate the existing `public RegistrationResult Register(string name, CommandParameterInfo[] parameters, CommandCallback callback)` method.
3. Cut the entire body of that method (all validation and `CommandDefinition` construction).
4. Paste the body into a new `public RegistrationResult Register(string name, CommandParameterInfo[] parameters, CommandCallback callback, string description)` method (4-arg overload).
5. In the 4-arg overload body, update the `new CommandDefinition(...)` call to pass `description` as the 4th argument (replacing the interim `null` introduced in Task 1, if applicable).
6. Replace the 3-arg overload body with a single `return Register(name, parameters, callback, null);` delegation statement.
7. No other changes — all validation error paths, error messages, and behavior remain exactly as-is in the 4-arg overload.

**Validation:**

- Build check: `dotnet build` — zero errors.
- Regression tests: `dotnet test tests/kmCommands.Tests/kmCommands.Tests.csproj` — all 103 tests pass. This is the critical gate: the entire existing test suite covers all error paths of the old 3-arg overload and they must all still pass through the delegation chain.
- Manual spot-check: confirm `Register("cmd", params, cb)` (3-arg) still returns a successful result in a quick test.

- taskReviewer review request:
  - Review scope: `src/CommandSystem.cs` — 3-arg becomes delegation wrapper; 4-arg carries all logic.
  - Primary checks: All validation error paths (NotInitialized, NullOrEmptyName, NullParameters, NullCallback, unsupported type, duplicate, optional-before-required) are present in the 4-arg overload unchanged. The 3-arg calls `Register(name, parameters, callback, null)` — exactly one line, no other logic.
  - Required evidence: `dotnet test` showing 103/103 passing.
  - Blocking conditions: any validation behavior removed or moved to wrong method; 3-arg overload contains any logic beyond the delegation call; test regression.

- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — the description-bearing overload is not yet documented until Task 6 (docs sync task). The 3-arg API is unchanged from the consumer's perspective.
- Update `.github/instructions/projectOverview.instructions.md` required: No — public API summary does not change yet (the 4-arg overload is additive; the summary will be updated in Task 6).

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (build + 103 tests passing)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `feat(command-help-text)`
- Suggested commit message: `feat(command-help-text): add 4-arg Register overload; 3-arg delegates with null description`

---

### Task 3: Attribute Scanner — Forward `attr.Description`

- [ ] Not started

**Objective:**

Update `AttributeScanner.ProcessMethod()` to read `attr.Description` from the `[Command]` attribute and pass it as the 4th argument when constructing `CommandDefinition`. This wires the attribute-based registration path through the same description storage introduced in Tasks 1–2.

**Inputs:**

- Requirements refs: FR #3, FR #4, AC #4, AC #5
- Design refs: Components → `AttributeScanner.cs`; Code Examples → `AttributeScanner.ProcessMethod()` snippet; Data Flow → Registration (attribute)

**Implementation Steps:**

1. Open `src/Core/AttributeScanner.cs`.
2. Locate `ProcessMethod()` (or the equivalent method that constructs `CommandDefinition`).
3. Find the line `new CommandDefinition(name, parameters, callback)` (or the interim `new CommandDefinition(name, parameters, callback, null)` from Task 1).
4. Change it to `new CommandDefinition(name, parameters, callback, attr.Description)`.
5. No other changes to `AttributeScanner.cs`.

**Validation:**

- Build check: `dotnet build` — zero errors.
- Regression tests: `dotnet test tests/kmCommands.Tests/kmCommands.Tests.csproj` — all 103 tests pass (scanner tests in `AttributeScannerTests.cs` must all still pass).
- Targeted check: verify that `attr.Description` is the correct property name matching the change made in Task 1.

- taskReviewer review request:
  - Review scope: `src/Core/AttributeScanner.cs` — one-line change to `CommandDefinition` construction.
  - Primary checks: `attr.Description` is passed (not a hardcoded `null`); no other logic in `ProcessMethod()` changed; `AttributeScannerTests.cs` all pass.
  - Required evidence: `dotnet test` showing 103/103 passing; diff showing exactly one changed line in `AttributeScanner.cs`.
  - Blocking conditions: `attr.Description` not forwarded (description always null from scanner path); any other logic in scanner modified; test regression.

- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — attribute usage docs will be covered in Task 6.
- Update `.github/instructions/projectOverview.instructions.md` required: No.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (build + 103 tests passing)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `feat(command-help-text)`
- Suggested commit message: `feat(command-help-text): forward attr.Description through AttributeScanner to CommandDefinition`

---

### Task 4: Snapshot — `CommandRegistry.BuildSnapshot()` and `CommandMetadataSnapshot`

- [ ] Not started

**Objective:**

Complete the outbound data path: update `CommandRegistry.BuildSnapshot()` to collect per-command descriptions into a separate `Dictionary<string, string>` (OrdinalIgnoreCase) and pass it to an updated `CommandMetadataSnapshot` constructor. Update `CommandMetadataSnapshot` to store `_descriptions`, update its `Empty` singleton to pass an empty descriptions dictionary, and add the `TryGetDescription(string name, out string description)` public method.

**Inputs:**

- Requirements refs: FR #5, FR #6, FR #7, FR #8, FR #9, FR #10, FR #11, AC #1–#9
- Design refs: Components → `CommandRegistry.cs`, `CommandMetadataSnapshot.cs`; Code Examples → `BuildSnapshot()`, `CommandMetadataSnapshot` updated constructor + new method + Empty; Implementation Notes (OrdinalIgnoreCase, non-null-only storage, empty-string semantics)

**Implementation Steps:**

1. Open `src/Core/CommandRegistry.cs`.
   - In `BuildSnapshot()`, declare a new local dictionary: `Dictionary<string, string> descriptions = new Dictionary<string, string>(count, StringComparer.OrdinalIgnoreCase);`
   - Inside the `foreach` loop over `_commands`, after populating `entries`, add:
     ```csharp
     if (def.Description != null)
         descriptions[def.Name] = def.Description;
     ```
   - Update the `return new CommandMetadataSnapshot(names, entries, ...)` call to pass `descriptions` as the third argument.
   - The early-exit `return CommandMetadataSnapshot.Empty;` path is unaffected (it predates the loop).

2. Open `src/CommandMetadataSnapshot.cs`.
   - Add `private readonly Dictionary<string, string> _descriptions;` field after `_entries`.
   - Update the internal constructor signature to accept `Dictionary<string, string> descriptions` as the third parameter.
   - Assign `_descriptions = descriptions;` in the constructor body.
   - Update the `_empty` static field initializer to pass `new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)` as the third argument to the constructor.
   - Add the `TryGetDescription` public method:
     ```csharp
     public bool TryGetDescription(string name, out string description)
     {
         if (string.IsNullOrEmpty(name))
         {
             description = null;
             return false;
         }
         return _descriptions.TryGetValue(name, out description);
     }
     ```
   - Add appropriate XML doc comment to `TryGetDescription` as described in design.md.

**Validation:**

- Build check: `dotnet build` — zero errors, zero new warnings.
- Regression tests: `dotnet test tests/kmCommands.Tests/kmCommands.Tests.csproj` — all 103 existing tests pass (snapshot tests in `CommandMetadataDiscoveryTests.cs` must all still pass).
- Manual sanity: confirm `CommandMetadataSnapshot.Empty.TryGetDescription("anything", out var d)` returns `false` and sets `d` to `null`.

- taskReviewer review request:
  - Review scope: `src/Core/CommandRegistry.cs` (BuildSnapshot update) and `src/CommandMetadataSnapshot.cs` (constructor, Empty, TryGetDescription).
  - Primary checks: `_descriptions` initialized with `OrdinalIgnoreCase`; `null` descriptions not stored (only non-null); `Empty` singleton updated (compile error if not); `TryGetDescription` returns `false`+`null` for empty/null name; `TryGetDescription` on `Empty` returns `false`; `_descriptions` never `null` after construction.
  - Required evidence: `dotnet test` showing 103/103 passing; `CommandMetadataSnapshot.Empty` compiles without error.
  - Blocking conditions: `_descriptions` is `null` at any point; wrong comparer (case-sensitive); `null` descriptions stored in dict; `TryGetDescription` throws on null/empty input; test regression.

- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None yet — full docs sync happens in Task 6.
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to Task 6 when all API changes are confirmed complete.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (build + 103 tests passing)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `feat(command-help-text)`
- Suggested commit message: `feat(command-help-text): collect descriptions in BuildSnapshot; add TryGetDescription to CommandMetadataSnapshot`

---

### Task 5: Tests — `CommandDescriptionTests.cs`

- [ ] Not started

**Objective:**

Add `tests/kmCommands.Tests/CommandDescriptionTests.cs` with 9 test methods covering all 9 `command-help-text` acceptance criteria. Run the full test suite to confirm 112 tests pass (103 pre-existing + 9 new).

**Inputs:**

- Requirements refs: AC #1–#9, AC #10 (regression), AC #11 (backward compat)
- Design refs: Testing Strategy → `CommandDescriptionTests.cs`, method-to-AC mapping table, `ScanTargets` inner class example

**Implementation Steps:**

1. Create `tests/kmCommands.Tests/CommandDescriptionTests.cs`.
2. Add `using` directives: `NUnit.Framework`, `kmCommands`, `kmCommands.Results`, `System` (and `System.Collections.Generic` if needed).
3. Declare `[TestFixture] public class CommandDescriptionTests` with a `CommandSystem _system;` field.
4. Add `[SetUp]` calling `_system = new CommandSystem(); _system.Initialize();` and `[TearDown]` calling `_system.Shutdown();`.
5. Add the private inner class `ScanTargets` with two static methods decorated with `[Command]`:
   - `[Command("described", Description = "A described command")] public static void DescribedCommand() { }`
   - `[Command("nodesc")] public static void NoDescCommand() { }`
6. Implement the following 9 test methods (each as a distinct `[Test]`):

   | Method name | AC | What to assert |
   |---|---|---|
   | `Register_WithNonNullDescription_SnapshotContainsDescription` | AC #1 | Register with `"Help text"` description; `GetSnapshot().TryGetDescription("cmd", out var d)` returns `true` and `d == "Help text"`. |
   | `Register_WithoutDescription_SnapshotDescriptionIsNull` | AC #2 | Register via 3-arg overload; `TryGetDescription` returns `false` and `out` value is `null`. |
   | `Register_WithEmptyStringDescription_SnapshotDescriptionIsEmptyString` | AC #3 | Register with `""` description; `TryGetDescription` returns `true` and `d == ""`. |
   | `Scan_AttributeWithDescription_SnapshotContainsDescription` | AC #4 | Scan `ScanTargets`; `TryGetDescription("described", out var d)` returns `true` and `d == "A described command"`. |
   | `Scan_AttributeWithoutDescription_SnapshotDescriptionIsNull` | AC #5 | Scan `ScanTargets`; `TryGetDescription("nodesc", out var d)` returns `false` and `d == null`. |
   | `TryGetDescription_ExistingCommandWithDescription_CaseInsensitiveLookup` | AC #6 | Register with name `"myCmd"` and description; look up via `"MYCMD"` and `"mycmd"` — both return `true` and correct description. |
   | `TryGetDescription_CommandWithNullDescription_ReturnsFalse` | AC #7 | Register via 3-arg overload; snapshot `TryGetDescription` returns `false`. |
   | `Empty_TryGetDescription_ReturnsFalseWithNullDescription` | AC #8 | `CommandMetadataSnapshot.Empty.TryGetDescription("any", out var d)` returns `false`; `d == null`. |
   | `SnapshotIsolation_DescriptionNotIncludedForLaterRegisteredCommand` | AC #9 | Take snapshot after first registration; register second command; confirm snapshot does not contain second command's description (i.e., `TryGetDescription("second", out _)` returns `false` on the first snapshot). |

7. Implementation notes:
   - Use `Assert.That(result, Is.True/False)` and `Assert.That(description, Is.EqualTo(...))` following NUnit 3 constraint syntax.
   - For attribute-scan tests, call `_system.Scan(typeof(ScanTargets))` (or `Scan(typeof(ScanTargets), new ScanOptions())` if required).
   - No LINQ in test code.
   - Keep callbacks minimal: use `_ => { }` (no-op) for non-execution tests.
   - Use `new CommandParameterInfo[0]` (or `Array.Empty<CommandParameterInfo>()`) for parameter-less commands.

**Validation:**

- Build check: `dotnet build` — zero errors.
- Full test run: `dotnet test tests/kmCommands.Tests/kmCommands.Tests.csproj` — **112 tests pass** (103 pre-existing + 9 new), zero failures.
- Each of the 9 new tests must pass; confirm test names appear in output.

- taskReviewer review request:
  - Review scope: `tests/kmCommands.Tests/CommandDescriptionTests.cs` — new file with 9 tests.
  - Primary checks: All 9 AC items have at least one test. Case-insensitive lookup test covers both `"MYCMD"` and `"mycmd"` variants. `Empty` singleton test uses `CommandMetadataSnapshot.Empty` directly, not a snapshot from `_system`. Snapshot isolation test takes snapshot *before* second registration. No LINQ. `[SetUp]`/`[TearDown]` consistent. `ScanTargets` inner class is `private static`.
  - Required evidence: `dotnet test` output showing 112/112 passing with `CommandDescriptionTests` class listed.
  - Blocking conditions: any AC without a test; any test skipped or erroring; `Empty` test not using the singleton; case-insensitive test checking only one case variant; test regression in pre-existing 103.

- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — docs sync is the dedicated next task (Task 6).
- Update `.github/instructions/projectOverview.instructions.md` required: No — deferred to Task 6.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (build + 112 tests passing)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `test(command-help-text)`
- Suggested commit message: `test(command-help-text): add CommandDescriptionTests covering all 9 acceptance criteria`

---

### Task 6: Docs Sync and Project Overview Update

- [ ] Not started

**Objective:**

Update `docs/architecture.md` to mention `TryGetDescription` in the Discovery API section (if such a section exists). Update `docs/commands.md` to document the `Description` property on `[Command]` and the 4-arg `Register()` overload. Update `.github/instructions/projectOverview.instructions.md` to reflect the new public API capability.

**Inputs:**

- Requirements refs: Goals #1–#5, FR #6 (snapshot retrieval method), In Scope section
- Design refs: API/Contract Sketch; Architecture Overview; areas "In scope" list

**Implementation Steps:**

1. Open `docs/architecture.md`.
   - Locate the section describing the Discovery Layer or `CommandMetadataSnapshot` API.
   - Add a mention of `TryGetDescription(name, out string description)` alongside the existing `TryGetParameters` reference. Describe it as: returns `true`+description string for commands registered with a non-null description; returns `false`+`null` for commands with no description or names not in the snapshot; lookup is case-insensitive.
   - If no discovery section exists, add a short one noting `GetCommandNames()`, `TryGetCommandParameters()`, `GetSnapshot()`, and now `TryGetDescription()`.

2. Open `docs/commands.md`.
   - Add a section (or sub-section under command registration) documenting:
     - The optional `Description` property on `[Command]`: `[Command("name", Description = "What this command does")]`
     - The 4-arg `Register()` overload: `Register(name, parameters, callback, "Optional description")`
     - Note that omitting the description is always valid; the snapshot returns `null` via `TryGetDescription`.

3. Open `.github/instructions/projectOverview.instructions.md`.
   - In the **API Layer Summary** section, add `TryGetDescription(name, out description)` to the Discovery API bullet.
   - In the **Implementation Direction** section, update the `CommandMetadataSnapshot.cs` entry to note `TryGetDescription()` method.
   - In the **Implementation Direction** section, update the `CommandSystem.cs` entry to note the 4-arg `Register()` overload.
   - In the **Implementation Direction** section, update the `CommandAttribute.cs` entry to note the `Description` property.

**Validation:**

- Build check: `dotnet build` — zero errors (docs changes do not affect build; this ensures no accidental `.cs` edits).
- Full test run: `dotnet test tests/kmCommands.Tests/kmCommands.Tests.csproj` — all 112 tests still pass (guard against accidental changes).
- Doc review: confirm `TryGetDescription` is mentioned in `docs/architecture.md`; confirm `Description` property usage is shown in `docs/commands.md`.

- taskReviewer review request:
  - Review scope: `docs/architecture.md`, `docs/commands.md`, `.github/instructions/projectOverview.instructions.md`.
  - Primary checks: `TryGetDescription` mentioned in architecture discovery section. `Description` property and 4-arg `Register` documented in commands guide. `projectOverview.instructions.md` API Layer Summary and Implementation Direction sections updated. No stale references remain.
  - Required evidence: `dotnet test` showing 112/112 passing (no accidental source changes); diff showing only docs changes.
  - Blocking conditions: `TryGetDescription` absent from architecture doc; API Layer Summary in projectOverview not updated; test regression caused by accidental source edit.

- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/architecture.md`, `docs/commands.md` — both explicitly required.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes.
- Sections to update:
  - `## API Layer Summary` → Discovery API bullet
  - `## Implementation Direction` → `CommandMetadataSnapshot.cs`, `CommandSystem.cs`, `CommandAttribute.cs` entries

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (build + 112 tests passing)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] `docs/architecture.md` updated with `TryGetDescription` in Discovery section
- [ ] `docs/commands.md` updated with `Description` property and 4-arg overload
- [ ] `.github/instructions/projectOverview.instructions.md` synced

**Commit Note:**

- Suggested commit scope: `docs(command-help-text)`
- Suggested commit message: `docs(command-help-text): document Description property, 4-arg Register, and TryGetDescription in architecture and commands guides`

---

## Reviewer Handoff Block

The following checklist mirrors the **Final Review Contract** from `design.md`. A `taskReviewer` agent running a final integration pass must verify all items before approving merge.

### Public API Correctness

- [ ] `Register(name, params, cb)` (3-arg) compiles unchanged and delegates to 4-arg with `null`.
- [ ] `Register(name, params, cb, description)` (4-arg) stores description on `CommandDefinition`.
- [ ] `Register(name, params, cb, null)` stores `null` description — same behavior as 3-arg path.
- [ ] `Register(name, params, cb, "")` stores `""` description; snapshot `TryGetDescription` returns `true`+`""`.
- [ ] `[Command("x", Description = "y")]` compiles; scanner forwards `"y"` to `CommandDefinition`.
- [ ] `[Command("x")]` without `Description` → scanner forwards `null`.

### Snapshot Correctness

- [ ] `BuildSnapshot()` builds `_descriptions` with `OrdinalIgnoreCase` comparer.
- [ ] `BuildSnapshot()` does not store `null` descriptions in `_descriptions`.
- [ ] `TryGetDescription` on `Empty` returns `false`, sets `out null`.
- [ ] `TryGetDescription` on a valid snapshot is case-insensitive.
- [ ] `TryGetDescription` with `null`/empty name returns `false`, `out null`.
- [ ] Snapshot taken before second registration does not contain second command's description.

### Design Invariants

- [ ] `CommandMetadataSnapshot._descriptions` is never `null` (constructed with empty dict for `Empty`).
- [ ] `_descriptions` uses `StringComparer.OrdinalIgnoreCase`.
- [ ] `CommandDefinition.Description` is immutable after construction (get-only property).
- [ ] No execution-path files modified (`ExecutionHandler`, `ArgumentConverter` unchanged).
- [ ] `CommandParameterInfo`, `RegistrationResult`, `RegistrationError`, `ExecutionResult`, `GetCommandNames()`, `TryGetCommandParameters()` — all unmodified.

### Test Evidence

- [ ] All 103 pre-existing tests pass without modification.
- [ ] All 9 `CommandDescriptionTests` tests pass.
- [ ] Coverage includes: manual registration with/without/empty description, attribute scan with/without description, case-insensitive lookup, `Empty` sentinel, snapshot isolation.
- [ ] No LINQ in new test code; no non-NUnit reflection.
- [ ] `[SetUp]`/`[TearDown]` bracketing consistent across all `CommandDescriptionTests` methods.

### Blocking Conditions for Final Approval

- Any pre-existing test fails after the feature change.
- Any new test is skipped or failing.
- `CommandMetadataSnapshot.Empty` not updated to the new 3-arg constructor (would be a compile error, but reconfirm).
- Any new external dependency added to `src/`.
- Any `UnityEngine` reference introduced in `src/`.
- `docs/architecture.md` or `docs/commands.md` missing description-related updates.
- `.github/instructions/projectOverview.instructions.md` not updated.

---

## Coverage Check

### Requirements Coverage

- [ ] FR #1 — Manual registration with description → Task 2 (4-arg overload), Task 5 (AC #1 test)
- [ ] FR #2 — Manual registration without description (3-arg unchanged) → Task 2 (delegation wrapper), Task 5 (AC #2 test)
- [ ] FR #3 — Attribute registration with description → Task 1 (attribute property), Task 3 (scanner), Task 5 (AC #4 test)
- [ ] FR #4 — Attribute registration without description → Task 3 (null forwarded), Task 5 (AC #5 test)
- [ ] FR #5 — Snapshot captures description → Task 4 (BuildSnapshot), Task 5 (AC #1–#5)
- [ ] FR #6 — Snapshot retrieval method (`TryGetDescription`) → Task 4 (CommandMetadataSnapshot), Task 5 (AC #6–#8)
- [ ] FR #7 — `null` for absent description → Task 4, Task 5 (AC #2, #5, #7, #8)
- [ ] FR #8 — No mutation after registration → Task 1 (`Description` get-only property)
- [ ] FR #9 — Case-insensitive lookup → Task 4 (OrdinalIgnoreCase), Task 5 (AC #6)
- [ ] FR #10 — Empty-string description stored as-is (not normalized) → Task 4 (non-null check only), Task 5 (AC #3)
- [ ] FR #11 — Snapshot isolation → Task 4 (snapshot is a detached copy), Task 5 (AC #9)
- [ ] AC #10 — No regression in 103 existing tests → Task 2 (delegation), Task 5 (full test run)
- [ ] AC #11 — 3-arg `Register` backward-compatible → Task 2

### Design Coverage

- [ ] `CommandDefinition` — `Description` field + constructor update → Task 1
- [ ] `CommandAttribute` — `Description` named property → Task 1
- [ ] `CommandSystem.Register()` — 3→4-arg delegation refactor → Task 2
- [ ] `AttributeScanner.ProcessMethod()` — `attr.Description` forwarded → Task 3
- [ ] `CommandRegistry.BuildSnapshot()` — descriptions collected → Task 4
- [ ] `CommandMetadataSnapshot` — `_descriptions` field, updated constructor, `Empty` update, `TryGetDescription` → Task 4
- [ ] `CommandDescriptionTests.cs` — 9 tests, all ACs covered → Task 5
- [ ] `docs/architecture.md` update → Task 6
- [ ] `docs/commands.md` update → Task 6
- [ ] `.github/instructions/projectOverview.instructions.md` → Task 6

### Gaps or Follow-Ups

- No gaps identified. All requirements and design slices are covered.
- AC #10 and AC #11 (backward-compat) are verified by the existing test suite running unchanged; no additional tests needed per design.
