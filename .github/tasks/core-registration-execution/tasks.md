# Core Registration and Execution Tasks

## Status

- [ ] Planned
- [x] In Progress
- [ ] Completed

## Inputs

- Requirements: `.github/tasks/core-registration-execution/requirements.md`
- Design: `.github/tasks/core-registration-execution/design.md`

## Branch

- Name: `feat_core-registration-execution`
- Rationale: This is new capability — the foundational runtime of the library does not exist yet. No prior behavior is being corrected or restructured; it is net-new feature work.

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- Do not add behavior beyond the task scope, even if it seems convenient.
- All `src/` files must carry the required license header before commit.
- No LINQ anywhere in `src/`. No `UnityEngine` references in `src/`.
- All public types must have XML documentation comments.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes (Task 7).

---

## Task List

### Task 1: Project Scaffolding and Test Infrastructure

- [ ] Completed (gate evidence pending)

**Objective:**

Establish the test project, wire it into the solution, and add `InternalsVisibleTo` so internal types are available to tests. No library source code is written in this task.

**Inputs:**

- Requirements refs: Test project setup, `InternalsVisibleTo` requirement.
- Design refs: "Test Project Setup" section; "File Structure" section (`tests/kmCommands.Tests/`).

**Implementation Steps:**

1. Create `tests/kmCommands.Tests/kmCommands.Tests.csproj` targeting `net6.0` with NUnit, NUnit3TestAdapter, and Microsoft.NET.Test.Sdk package references and a project reference to `../../kmCommands.csproj`.
2. Create `src/Properties/AssemblyInfo.cs` with the required license header and `[assembly: InternalsVisibleTo("kmCommands.Tests")]`.
3. Add the test project to `kmCommands.sln` (`dotnet sln add`).
4. Run `dotnet build` from the repo root and confirm both projects build with no errors.
5. Run `dotnet test` and confirm zero tests, zero failures (empty test suite passes).

**Validation:**

- Unit tests: N/A — no logic yet.
- Additional checks: `dotnet build` succeeds. `dotnet test` exits 0.
- QA quick pass (`taskReviewer`): Verify solution structure, project references, and InternalsVisibleTo are correct.
- taskReviewer review request:
  - Review scope: New test project, AssemblyInfo, solution update.
  - Primary checks: Test project targets `net6.0`; references main project; NUnit packages present; `InternalsVisibleTo` declared correctly; solution file includes test project; both projects build cleanly.
  - Required evidence: `dotnet build` output (no errors); `dotnet test` output (zero failures).
  - Blocking conditions: Build fails; `InternalsVisibleTo` missing or incorrect name; test project not in solution.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — no library behavior introduced yet.
- Update `.github/instructions/projectOverview.instructions.md` required: No — project-level facts haven't changed yet (src/ still effectively empty from a library perspective).

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A)

**Commit Note:**

- Suggested commit scope: `build`
- Suggested commit message: `build: add test project, NUnit infrastructure, InternalsVisibleTo`

---

### Task 2: Public Types — Result Enums, Structs, Delegate, and ParameterInfo

- [ ] Completed (gate evidence pending)

**Objective:**

Define all public-facing types that form the library's contract surface: the `CommandCallback` delegate, `CommandParameterInfo`, `RegistrationResult`/`RegistrationError`, and `ExecutionResult`/`ExecutionError`. These have no dependencies on other new library code and can be written and verified independently.

**Inputs:**

- Requirements refs: Structured result types; delegate shape; typed parameter description; XML docs requirement; license header requirement.
- Design refs: "Public Delegate", "Public Parameter Info", "Public Result Types" sections; "File Structure" section.

**Implementation Steps:**

1. Create `src/CommandCallback.cs`:
   - License header.
   - `namespace kmCommands`.
   - `public delegate void CommandCallback(object[] args)` with XML doc.

2. Create `src/CommandParameterInfo.cs`:
   - License header.
   - `namespace kmCommands`.
   - `public sealed class CommandParameterInfo` with `Name` and `Type` properties, constructor as per design (throws `ArgumentNullException` on null — this is a programming-error guard, not a runtime condition), and XML docs on all public members.

3. Create `src/Results/RegistrationResult.cs`:
   - License header.
   - `namespace kmCommands`.
   - `public enum RegistrationError` with values: `None`, `NotInitialized`, `NullOrEmptyName`, `NullParameters`, `NullCallback`, `DuplicateCommandName`, `UnsupportedParameterType`.
   - `public readonly struct RegistrationResult` with `Success`, `Error`, `ErrorMessage` properties; private constructor; `internal static Ok()` and `internal static Fail(RegistrationError, string)` factory methods; XML docs on all public members.

4. Create `src/Results/ExecutionResult.cs`:
   - License header.
   - `namespace kmCommands`.
   - `public enum ExecutionError` with values: `None`, `NotInitialized`, `NullOrEmptyCommandName`, `CommandNotFound`, `ArgumentCountMismatch`, `ArgumentConversionFailed`, `CallbackThrewException`.
   - `public readonly struct ExecutionResult` with `Success`, `Error`, `ErrorMessage`, `Exception` properties; private constructor; `internal static Ok()` and `internal static Fail(ExecutionError, string, Exception)` factory methods; XML docs on all public members.

5. Run `dotnet build` — must succeed with no errors or warnings.

**Validation:**

- Unit tests: None for this task (pure data type definitions with no logic beyond null guards). The null guard in `CommandParameterInfo` constructor is trivially verifiable via a throw test — write it in this task or defer to Task 6.
- Additional checks: `dotnet build` clean. All XML doc tags present. `readonly struct` verified on result types. Internal factory methods not visible in a consumer-facing sense.
- QA quick pass (`taskReviewer`): Spot-check that factory methods are `internal`, not `public`; enum values match requirements; `Exception` property on `ExecutionResult` has accurate XML doc stating it is only non-null for `CallbackThrewException`.
- taskReviewer review request:
  - Review scope: 5 new source files (`CommandCallback.cs`, `CommandParameterInfo.cs`, `RegistrationResult.cs`, `ExecutionResult.cs`, plus enums).
  - Primary checks: `readonly struct` on both result types; `internal` factory methods; all enum variants present and correctly named; XML docs on every public member; license headers present; no LINQ.
  - Required evidence: `dotnet build` clean output.
  - Blocking conditions: Missing enum variants; factory methods `public`; missing XML docs; missing license headers; build errors.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None at this stage — public API docs will be covered in a later PR after full API is implemented.
- Update `.github/instructions/projectOverview.instructions.md` required: No — no project-level facts have changed; src/ content is beginning but not yet functional.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented (N/A — no logic, null guard may be tested in Task 6)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A)

**Commit Note:**

- Suggested commit scope: `feat`
- Suggested commit message: `feat: add public result types, enums, CommandCallback, and CommandParameterInfo`

---

### Task 3: Internal Components — CommandDefinition, CommandRegistry, ArgumentConverter

- [ ] Not started

**Objective:**

Implement the three internal building blocks: the `CommandDefinition` storage model, the `CommandRegistry` dictionary-backed store, and the `ArgumentConverter` with built-in converters for `int`, `float`, `bool`, and `string`. These are independently testable via `InternalsVisibleTo`.

**Inputs:**

- Requirements refs: Command storage and lookup; typed argument conversion for `int`, `float`, `bool`, `string`; extensible converter design; `InvariantCulture` for numeric parsing; case-insensitive name lookup; no LINQ.
- Design refs: "Internal CommandDefinition", "Internal CommandRegistry", "Internal ArgumentConverter" sections.

**Implementation Steps:**

1. Create `src/Core/CommandDefinition.cs`:
   - License header.
   - `namespace kmCommands.Core`.
   - `internal sealed class CommandDefinition` with `Name` (string), `Parameters` (`CommandParameterInfo[]`), `Callback` (`CommandCallback`) properties and constructor.

2. Create `src/Core/CommandRegistry.cs`:
   - License header.
   - `namespace kmCommands.Core`.
   - `internal sealed class CommandRegistry`.
   - `Dictionary<string, CommandDefinition>` with `StringComparer.OrdinalIgnoreCase`.
   - `TryRegister(CommandDefinition)` → returns `false` on duplicate, `true` on success.
   - `TryGetCommand(string, out CommandDefinition)` → dictionary lookup.
   - `Clear()`.
   - `Count` property.
   - No LINQ.

3. Create `src/Core/ArgumentConverter.cs`:
   - License header.
   - `namespace kmCommands.Core`.
   - `internal sealed class ArgumentConverter`.
   - `internal delegate bool TryConvertFunc(string input, out object result)`.
   - `Dictionary<Type, TryConvertFunc>` initialized with 4 entries: `typeof(int)`, `typeof(float)`, `typeof(bool)`, `typeof(string)`.
   - `TryConvert(Type, string, out object)` — dictionary lookup then invoke.
   - `IsTypeSupported(Type)` — dictionary key check.
   - Static private converter methods: `TryConvertInt` uses `int.TryParse` with `NumberStyles.Integer` and `CultureInfo.InvariantCulture`; `TryConvertFloat` uses `float.TryParse` with `NumberStyles.Float` and `CultureInfo.InvariantCulture`; `TryConvertBool` uses `bool.TryParse`; `TryConvertString` always returns true.
   - No LINQ, no closures.

4. Run `dotnet build` — must succeed.

**Validation:**

- Unit tests: Write `ArgumentConverterTests.cs` in the test project covering:
  - `int`: valid integer, invalid string, negative, zero.
  - `float`: valid float `"1.5"`, invalid string, negative.
  - `float` InvariantCulture: temporarily set `Thread.CurrentThread.CurrentCulture` to a comma-decimal culture (e.g., `de-DE`) and confirm `"1.5"` still parses correctly.
  - `bool`: `"true"`, `"True"`, `"false"`, `"False"`, invalid.
  - `string`: any input returns true and the same string.
  - Unsupported type (e.g., `typeof(double)`): `TryConvert` returns false, `IsTypeSupported` returns false.
  - `CommandRegistry`: register new → returns true; register duplicate → returns false; TryGet found and not found; Clear resets count to 0.
- Additional checks: Build clean. No LINQ in new files.
- QA quick pass (`taskReviewer`): Confirm `InvariantCulture` is applied on both int and float converters; confirm static methods don't capture closures; confirm `OrdinalIgnoreCase` on `CommandRegistry`; confirm `IsTypeSupported` is consistent with `TryConvert` for all 4 built-in types.
- taskReviewer review request:
  - Review scope: `CommandDefinition.cs`, `CommandRegistry.cs`, `ArgumentConverter.cs`, `ArgumentConverterTests.cs`.
  - Primary checks: `InvariantCulture` on numeric converters; `OrdinalIgnoreCase` on registry; no LINQ; static converter methods; `IsTypeSupported` covers exactly the 4 built-in types; duplicate registration returns false not throws; InvariantCulture test case present.
  - Required evidence: `dotnet test` output showing all new tests pass.
  - Blocking conditions: InvariantCulture test missing or failing; LINQ found; duplicate registration throws exception.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — internal types.
- Update `.github/instructions/projectOverview.instructions.md` required: No — internal structure; project-level summary does not need to change yet.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A)

**Commit Note:**

- Suggested commit scope: `feat`
- Suggested commit message: `feat: add CommandDefinition, CommandRegistry, and ArgumentConverter`

---

### Task 4: ExecutionHandler

- [ ] Not started

**Objective:**

Implement `ExecutionHandler`, which orchestrates the full execute path: command lookup → argument count validation → argument conversion → callback invocation → structured result. This is the internal engine that `CommandSystem` will delegate to.

**Inputs:**

- Requirements refs: Execution accepts command name + string args; converts each token to declared type; callback invoked; callback exceptions caught and wrapped; argument count mismatch → structured error; conversion failure → structured error with parameter name and index; `null` args treated as empty.
- Design refs: "Execution Flow" flowchart; "Internal ExecutionHandler" section; `Array.Empty<object>()` for zero-arg commands.

**Implementation Steps:**

1. Create `src/Core/ExecutionHandler.cs`:
   - License header.
   - `namespace kmCommands.Core`.
   - `internal sealed class ExecutionHandler`.
   - Constructor: `(CommandRegistry registry, ArgumentConverter converter)`.
   - `internal ExecutionResult Execute(string commandName, string[] args)` implementing the full flow from the design:
     1. Guard: null/empty `commandName` → `Fail(NullOrEmptyCommandName, ...)`.
     2. Registry lookup → `Fail(CommandNotFound, ...)` if not found.
     3. Arg count check: `args?.Length ?? 0` vs `definition.Parameters.Length` → `Fail(ArgumentCountMismatch, ...)`.
     4. Loop over parameters: `ArgumentConverter.TryConvert(param.Type, args[i], out converted)` → on failure: `Fail(ArgumentConversionFailed, ...)` with param name and index in message.
     5. Build `object[]`: use `Array.Empty<object>()` for zero params.
     6. `try { definition.Callback(convertedArgs); }` `catch (Exception ex)` → `Fail(CallbackThrewException, ..., ex)`.
     7. Return `ExecutionResult.Ok()`.
   - No LINQ.

2. Run `dotnet build` — must succeed.

**Validation:**

- Unit tests: Write `ExecutionHandlerTests.cs` (internal tests via `InternalsVisibleTo`) in the test project covering:
  - Successful execution: callback is invoked, returns Success.
  - Zero-arg command with null args: succeeds.
  - Zero-arg command with empty array: succeeds.
  - Command not found: returns `CommandNotFound`.
  - Null/empty command name: returns `NullOrEmptyCommandName`.
  - Too few args: returns `ArgumentCountMismatch`.
  - Too many args: returns `ArgumentCountMismatch`.
  - Wrong type (e.g., `"abc"` for `int`): returns `ArgumentConversionFailed` with message containing parameter name and index.
  - Callback throws: returns `CallbackThrewException` and `Exception` property is the thrown exception.
  - Callback receives correctly typed values (verify via captured output inside callback).
  - Case-insensitive name lookup (`"SET_HEALTH"` resolves same as `"set_health"`).
- Additional checks: `dotnet test` all pass. No LINQ.
- QA quick pass (`taskReviewer`): Verify null args path; verify `Array.Empty<object>()` used for zero-param path; verify exception is both wrapped in result AND the original exception object is preserved on `ExecutionResult.Exception`; verify error messages include param name and index for conversion failures.
- taskReviewer review request:
  - Review scope: `ExecutionHandler.cs`, `ExecutionHandlerTests.cs`.
  - Primary checks: Null args → treated as empty (not throws); `Array.Empty` for zero args; callback exception caught and not re-thrown; `ExecutionResult.Exception` set; arg count mismatch catches both too-few and too-many; conversion failure message contains param name and index.
  - Required evidence: `dotnet test` output with all new tests passing.
  - Blocking conditions: Exception re-thrown instead of wrapped; null args throws `NullReferenceException`; LINQ found; `Exception` property null on `CallbackThrewException` result.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — internal type.
- Update `.github/instructions/projectOverview.instructions.md` required: No — internal structure unchanged at project level.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A)

**Commit Note:**

- Suggested commit scope: `feat`
- Suggested commit message: `feat: add ExecutionHandler with full execute path and error handling`

---

### Task 5: CommandSystem — Entry Point and Public API

- [ ] Not started

**Objective:**

Implement `CommandSystem`, the public-facing entry point that gates all operations behind `Initialize()`, creates and owns internal components, and exposes `Register` and `Execute` as the consumer-facing API. This is the final library source task that completes the functional implementation.

**Inputs:**

- Requirements refs: Explicit `Initialize()` / `Shutdown()` lifecycle; all operations gated; calling before init returns structured error; idempotent lifecycle; `Register` validates inputs and returns `RegistrationResult`; `Execute` delegates to `ExecutionHandler`; no LINQ; no UnityEngine; XML docs.
- Design refs: "CommandSystem" component description; "Registration Flow" flowchart; "Public Entry Point" API sketch; "Lifecycle Idempotency" implementation note; "No Thread Safety" note.

**Implementation Steps:**

1. Create `src/CommandSystem.cs`:
   - License header.
   - `namespace kmCommands`.
   - `public sealed class CommandSystem`.
   - Private fields: `_registry` (`CommandRegistry`), `_converter` (`ArgumentConverter`), `_executionHandler` (`ExecutionHandler`), all null until `Initialize()`.
   - `public bool IsInitialized { get; private set; }`.
   - `public void Initialize()`: idempotent — if already initialized, return. Create `_registry`, `_converter`, `_executionHandler`. Set `IsInitialized = true`.
   - `public void Shutdown()`: idempotent — if not initialized, return. Null all three fields. Set `IsInitialized = false`.
   - `public RegistrationResult Register(string name, CommandParameterInfo[] parameters, CommandCallback callback)`:
     - Guard: not initialized → `RegistrationResult.Fail(NotInitialized, ...)`.
     - Validate: null/empty name → `Fail(NullOrEmptyName, ...)`.
     - Validate: null parameters → `Fail(NullParameters, ...)`.
     - Validate: null callback → `Fail(NullCallback, ...)`.
     - Validate: any `parameters[i].Type` not supported by `_converter.IsTypeSupported` → `Fail(UnsupportedParameterType, ...)` with type name in message.
     - Create `CommandDefinition` and attempt `_registry.TryRegister` → on false: `Fail(DuplicateCommandName, ...)`.
     - Return `RegistrationResult.Ok()`.
   - `public ExecutionResult Execute(string commandName, string[] args)`:
     - Guard: not initialized → `ExecutionResult.Fail(NotInitialized, ...)`.
     - Delegate to `_executionHandler.Execute(commandName, args)`.
   - XML docs on all public members including a thread-safety note on the class-level doc.
   - No LINQ.

2. Run `dotnet build` — must succeed with no errors or warnings.

**Validation:**

- Unit tests: Write `CommandSystemLifecycleTests.cs` and `CommandRegistrationTests.cs` in the test project covering:
  - `CommandSystemLifecycleTests`: `Initialize()` sets `IsInitialized` true; `Shutdown()` sets `IsInitialized` false; double-`Initialize()` is no-op (no exception, `IsInitialized` still true); double-`Shutdown()` is no-op; re-`Initialize()` after `Shutdown()` works; `Register()` before init returns `NotInitialized`; `Execute()` before init returns `NotInitialized`.
  - `CommandRegistrationTests`: Successful registration returns `Success = true`; duplicate name returns `DuplicateCommandName`; null name returns `NullOrEmptyName`; empty string name returns `NullOrEmptyName`; null parameters array returns `NullParameters`; null callback returns `NullCallback`; parameter with unsupported type (e.g., `typeof(double)`) returns `UnsupportedParameterType` with type name in message; mixed valid + invalid parameter types also fails.
- Additional checks: `dotnet test` all pass. No LINQ in `CommandSystem.cs`.
- QA quick pass (`taskReviewer`): Confirm idempotency tests exist for both `Initialize` and `Shutdown`; re-init after shutdown test present; `UnsupportedParameterType` loop validation present; `Execute` before init covered; no internal state leaks between test cases (each test creates its own `CommandSystem` instance).
- taskReviewer review request:
  - Review scope: `CommandSystem.cs`, `CommandSystemLifecycleTests.cs`, `CommandRegistrationTests.cs`.
  - Primary checks: Idempotent init/shutdown; re-init after shutdown; all `RegistrationError` variants exercised; `Execute` before init; unsupported type validation loop; no LINQ; XML docs present including thread-safety note; no `static` state on `CommandSystem`.
  - Required evidence: `dotnet test` output with all new tests passing.
  - Blocking conditions: Static state on `CommandSystem`; idempotency not implemented; `Execute` before init throws instead of returning result; LINQ found; XML docs missing.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None in this task — API docs will be a future concern.
- Update `.github/instructions/projectOverview.instructions.md` required: No — tracked together in Task 7 once the full implementation is complete.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to Task 7)

**Commit Note:**

- Suggested commit scope: `feat`
- Suggested commit message: `feat: add CommandSystem entry point with lifecycle, registration, and execution`

---

### Task 6: End-to-End Execution Tests and Full Test Suite Completion

- [ ] Not started

**Objective:**

Write `CommandExecutionTests.cs` (end-to-end tests through `CommandSystem.Execute`) and complete any remaining test coverage gaps identified in earlier tasks. Confirm the full test suite passes cleanly.

**Inputs:**

- Requirements refs: Callback invoked with correctly typed values; all execution error paths produce structured results; null args treated as empty; case-insensitive command name; re-init after shutdown works with pre-registered commands cleared.
- Design refs: "Testing Strategy" table — `CommandExecutionTests` row; "Code Examples" section for expected consumer behavior.

**Implementation Steps:**

1. Create `tests/kmCommands.Tests/CommandExecutionTests.cs` covering:
   - Successful execution with typed args (`string`, `int`, `float`, `bool`) — verify callback receives correctly typed values via captured output.
   - Zero-arg command with null args input.
   - Zero-arg command with empty string array input.
   - Command not found: `CommandNotFound`.
   - Too few args: `ArgumentCountMismatch`, message contains expected and actual counts.
   - Too many args: `ArgumentCountMismatch`.
   - Wrong type (e.g., `"abc"` for `int`): `ArgumentConversionFailed`, message contains parameter name and index.
   - Callback throws: `CallbackThrewException`, `Exception` property is the original exception.
   - Re-init after shutdown: commands registered before shutdown are gone; new registration works.
   - Case-insensitive lookup: register `"SetHP"`, execute `"sethp"` and `"SETHP"` both succeed.
   - All four supported parameter types resolved in a single command.

2. If the `CommandParameterInfo` null-guard (`ArgumentNullException`) was deferred from Task 2, add those test cases here.

3. Run the complete test suite (`dotnet test`) and confirm all tests across all test classes pass with zero failures.

**Validation:**

- Unit tests: All tests in `CommandExecutionTests.cs` pass. Full suite (`dotnet test`) passes with zero failures.
- Additional checks: No skipped or ignored tests without documented reason.
- QA quick pass (`taskReviewer`): Review that callback-receives-correct-types test actually captures and asserts the typed value (not just `Success == true`); confirm re-init after shutdown clears commands; case-insensitive test covers at least two case variants.
- taskReviewer review request:
  - Review scope: `CommandExecutionTests.cs`, final `dotnet test` output.
  - Primary checks: Typed-value assertion (not just success flag); all `ExecutionError` variants covered; re-init clears commands; case-insensitive cases; null args path; zero-arg path; `Exception` property asserted on `CallbackThrewException`; no LINQ in test code.
  - Required evidence: Full `dotnet test` output showing all test classes, test counts, and zero failures.
  - Blocking conditions: Any assertion that only checks `Success == true` without verifying typed value; any `ExecutionError` variant untested; failing tests; LINQ in test code.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — docs are out of scope for this PR.
- Update `.github/instructions/projectOverview.instructions.md` required: No — tracked in Task 7.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (deferred to Task 7)

**Commit Note:**

- Suggested commit scope: `test`
- Suggested commit message: `test: add end-to-end execution tests and complete full test suite`

---

### Task 7: Update projectOverview.instructions.md

- [ ] Not started

**Objective:**

Update `.github/instructions/projectOverview.instructions.md` to reflect that the project is no longer empty scaffolding — it now has a functional `src/` with implemented systems, a `tests/` project, and a new dependency on NUnit in the test layer.

**Inputs:**

- Requirements refs: projectOverview must stay accurate for all agents and developers.
- Design refs: "File Structure" section; "Test Project Setup" section; "Systems In Action" and "API Layer Summary" tables in project overview.

**Implementation Steps:**

1. Update the "Current Repository State" section:
   - Remove `src/ is currently empty`, `tests/ is currently empty` statements.
   - Replace with accurate current state: `src/` contains the core command system (entry point, registry, argument converter, execution handler, result types). `tests/` contains `kmCommands.Tests` with NUnit.

2. Update the "Folder Hierarchy" table:
   - Update `src/` description to reflect implemented systems.
   - Update `tests/` description to reflect implemented test project.

3. Update the "Dependencies And Target Versions" table:
   - Add row: Test framework = `NUnit` (test project only, `net6.0`).

4. Update the "Systems In Action" table:
   - Mark Command Registry, Argument System, and Execution Engine rows as implemented (or add a status column).
   - Alternatively, add a note at the top of the table indicating which systems are active vs still planned.

5. Update the "Implementation Direction" note at the bottom to reflect what has been implemented and what remains to be built.

6. Confirm the "API Layer Summary" still accurately describes the planned state (it should, since future features are still planned).

7. Review the full document for any other statements that now differ from reality (e.g., reference to `src/` being empty).

**Validation:**

- Unit tests: N/A.
- Additional checks: Read through the updated document end-to-end and confirm no statement contradicts current repo state.
- QA quick pass (`taskReviewer`): Confirm no "currently empty" statements remain for `src/` or `tests/`; dependency table updated; systems table reflects implemented vs planned.
- taskReviewer review request:
  - Review scope: `.github/instructions/projectOverview.instructions.md`.
  - Primary checks: "currently empty" language removed; NUnit test dependency noted; implemented systems identified; planned/future systems still clearly marked; no contradictions.
  - Required evidence: Show diff of changed sections.
  - Blocking conditions: "src/ is currently empty" still present; no mention of test framework; factual inaccuracies about what is implemented.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: Resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: None — `docs/` is still explicitly out of scope for this PR.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes — this task IS the update.
- Sections to update: Current Repository State, Folder Hierarchy, Dependencies And Target Versions, Systems In Action, Implementation Direction.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed
- [ ] Unit tests passed or exception documented (N/A)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (N/A)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented

**Commit Note:**

- Suggested commit scope: `docs`
- Suggested commit message: `docs: update projectOverview to reflect implemented core systems`

---

## Coverage Check

### Requirements Coverage

- [ ] Every requirement is mapped to at least one task
- [ ] No requirement is left unplanned

| Requirement                                                     | Covered By                          |
| --------------------------------------------------------------- | ----------------------------------- |
| `Initialize()` / `Shutdown()` lifecycle gating                  | Task 5, Task 6                      |
| Idempotent lifecycle                                            | Task 5, Task 6                      |
| Calling before init returns error (not throws)                  | Task 5, Task 6                      |
| `Shutdown()` clears all state                                   | Task 5, Task 6                      |
| Manual registration: name + params + callback                   | Task 5, Task 6                      |
| Duplicate name → structured error                               | Task 5, Task 6                      |
| Null/empty name, null params, null callback → structured errors | Task 5, Task 6                      |
| Unsupported parameter type → structured error                   | Task 5, Task 6                      |
| Execution: command name + string args                           | Task 4, Task 6                      |
| String-to-type conversion: `int`, `float`, `bool`, `string`     | Task 3, Task 6                      |
| `InvariantCulture` for numeric parsing                          | Task 3, Task 6                      |
| Extensible converter design for future types                    | Task 3 (dict-based design)          |
| Argument count mismatch → structured error                      | Task 4, Task 6                      |
| Conversion failure → structured error with param name + index   | Task 4, Task 6                      |
| Successful execution invokes callback → success result          | Task 4, Task 5, Task 6              |
| Callback exception caught and wrapped                           | Task 4, Task 6                      |
| `null` args treated as empty                                    | Task 4, Task 6                      |
| Structured result types for registration and execution          | Task 2                              |
| All public types have XML docs                                  | Task 2, Task 5                      |
| No LINQ in runtime paths                                        | All tasks (enforced)                |
| Minimal allocations (`Array.Empty` for zero args)               | Task 4                              |
| IL2CPP / AOT safe                                               | Task 3 (static methods, no codegen) |
| No UnityEngine references in `src/`                             | All src tasks (enforced)            |
| License headers on all `src/` files                             | All src tasks (enforced)            |
| Unit tests covering registration, conversion, execution, errors | Tasks 3, 4, 5, 6                    |
| Test project with NUnit                                         | Task 1                              |
| `InternalsVisibleTo` for test access                            | Task 1                              |

### Design Coverage

- [ ] Key design components are mapped to tasks
- [ ] Critical design constraints are represented in validation gates

| Design Component                                 | Covered By |
| ------------------------------------------------ | ---------- |
| `CommandSystem` entry point                      | Task 5     |
| `CommandRegistry`                                | Task 3     |
| `ArgumentConverter`                              | Task 3     |
| `ExecutionHandler`                               | Task 4     |
| `CommandDefinition`                              | Task 3     |
| `CommandCallback` delegate                       | Task 2     |
| `CommandParameterInfo`                           | Task 2     |
| `RegistrationResult` / `RegistrationError`       | Task 2     |
| `ExecutionResult` / `ExecutionError`             | Task 2     |
| Test project scaffolding                         | Task 1     |
| `InternalsVisibleTo` / `AssemblyInfo.cs`         | Task 1     |
| `commandRegistry` with `OrdinalIgnoreCase`       | Task 3     |
| `InvariantCulture` on numeric converters         | Task 3     |
| `Array.Empty<object>()` for zero-arg commands    | Task 4     |
| Idempotent lifecycle (init/shutdown)             | Task 5     |
| No static state on `CommandSystem`               | Task 5     |
| `readonly struct` for result types               | Task 2     |
| `internal` factory methods on result types       | Task 2     |
| XML docs + thread-safety note on `CommandSystem` | Task 5     |
| projectOverview.instructions.md sync             | Task 7     |

### Gaps or Follow-Ups

- Individual command deregistration (`Unregister`) → tracked in `PLANNED.md`, not in scope.
- Command aliases → tracked in `PLANNED.md`, not in scope.
- `docs/` content (architecture, integration guide) → planned future PR, not in scope.
- Generic overload conveniences for `Register<T1, T2>(...)` → planned future PR, noted in design.
- Thread safety → explicitly noted as out of scope and documented (main-thread Unity usage).
