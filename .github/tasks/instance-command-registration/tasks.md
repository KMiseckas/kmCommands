# Instance Command Registration — Tasks

## Status

- [ ] Planned
- [ ] In Progress
- [ ] Completed

## Inputs

- Requirements: `.github/tasks/instance-command-registration/requirements.md`
- Design: `.github/tasks/instance-command-registration/design.md`

## Branch

- Name: `feat_instance-command-registration`
- Rationale: `feat_` — new user-facing capability adding instance-bound command registration

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- Include doc updates in `docs/` whenever behaviour, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status -> Completed` may be checked only after all tasks and `## Coverage Check` items are checked.

---

## Task List

### Task 1: Callback Return Value Plumbing

- [ ] Not started

**Objective:**

Change `CommandCallback` from `void`-returning to `object`-returning. Propagate the return value through `ExecutionResult`, `CommandHistoryEntry`, and `CommandHistoryBuffer`. Update `AttributeScanner` to handle void vs. non-void static methods. Migrate all existing tests. This is the most cross-cutting change and all subsequent tasks depend on it.

**Inputs:**

- Requirements refs: R13, R14
- Design refs: §Modified Components — `CommandCallback`, `CommandDefinition`, `ExecutionHandler`, `ExecutionResult`, `CommandHistoryEntry`, `CommandHistoryBuffer`; §Implementation Notes — CommandCallback Breaking Change Migration; §Task Planning Handoff — Slice 1

**Implementation Steps:**

1. Change `CommandCallback` delegate signature in `src/CommandCallback.cs` from `void CommandCallback(object[] args)` to `object CommandCallback(object[] args)`.
2. Update `CommandDefinition` in `src/Core/CommandDefinition.cs` — the `Callback` property type is now `CommandCallback` (already is; no struct change, just inherits the new signature).
3. Update `ExecutionResult` in `src/Results/ExecutionResult.cs`:
   - Add `public object ReturnValue { get; }` property.
   - Add `public bool HasReturnValue { get; }` property.
   - Update the private constructor to accept `object returnValue` and `bool hasReturnValue`.
   - Update `ExecutionResult.Ok()` to `ExecutionResult.Ok(object returnValue = null)` — sets `HasReturnValue = returnValue != null`.
   - All `Fail` factories remain unchanged (both new properties default to `null` / `false`).
4. Update `ExecutionHandler.Execute` in `src/Core/ExecutionHandler.cs`:
   - Change `definition.Callback(convertedArgs);` to `object returnValue = definition.Callback(convertedArgs);`.
   - Change `return ExecutionResult.Ok();` to `return ExecutionResult.Ok(returnValue);`.
5. Update `CommandHistoryEntry` in `src/CommandHistoryEntry.cs`:
   - Add `public object ReturnValue { get; }` property.
   - Update the internal constructor to accept and store `object returnValue`.
6. Update `CommandHistoryBuffer` in `src/Core/CommandHistoryBuffer.cs`:
   - Update `Record(string commandName, string[] args)` to `Record(string commandName, string[] args, object returnValue)`.
   - Pass `returnValue` through to `new CommandHistoryEntry(commandName, argsCopy, returnValue)`.
7. Update `CommandSystem.Execute` — pass `result.ReturnValue` to `_historyBuffer.Record(commandName, args, result.ReturnValue)`.
8. Update `AttributeScanner.BuildCallback` in `src/Core/AttributeScanner.cs`:
   - Zero-param void: `Action del = ...; return args => { del(); return null; };`
   - N-param void: `Delegate d = ...; return args => { d.DynamicInvoke(args); return null; };`
   - N-param non-void: `Delegate d = ...; return args => d.DynamicInvoke(args);`
   - Check `method.ReturnType == typeof(void)` to select the path.
   - Update `GetActionDelegateType` helper name/usage as needed; add `GetFuncDelegateType` helper for non-void cases (supports 1–4 params, matching existing pattern).
9. Migrate all existing tests in `tests/kmCommands.Tests/` — update every `CommandCallback` lambda that returns `void` to return `object null`. Affected files include `CommandSystemTests.cs`, `CommandExecutionTests.cs`, `AttributeScannerTests.cs`, `AutoScanAtInitializeTests.cs`, `ExecutionHandlerTests.cs`, and any others that create callbacks. Search for `CommandCallback` and `args =>` patterns.

**Validation:**

- Unit tests: All 186 existing tests pass after migration. No new tests are strictly required for this task beyond verifying the existing suite still passes.
- Additional checks:
  - Manually verify `ExecutionResult.ReturnValue` is `null` and `HasReturnValue` is `false` for a void callback.
  - Verify `AttributeScanner` non-void static method test: register a `[Command]`-decorated static method that returns an `int`; execute it; confirm `ReturnValue` is the expected value.
- QA quick pass (`taskReviewer`): Yes — focused on delegate signature change and test migration completeness.
- taskReviewer review request:
  - Review scope: `CommandCallback.cs`, `ExecutionResult.cs`, `ExecutionHandler.cs`, `CommandHistoryEntry.cs`, `CommandHistoryBuffer.cs`, `CommandSystem.cs` (Execute + Record call), `AttributeScanner.cs` (BuildCallback), all migrated test files.
  - Primary checks: No void `CommandCallback` lambda remains in tests or source; `ExecutionResult.Ok()` correctly sets `HasReturnValue`; `AttributeScanner` correctly handles both void and non-void static methods; `CommandHistoryBuffer.Record` signature updated consistently.
  - Required evidence: Full test suite run with 0 failures (186+).
  - Blocking conditions: Any existing test failing; `HasReturnValue` set incorrectly for void commands; `AttributeScanner` non-void path missing.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: required before marking complete

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md` — update callback delegate example snippets to return `object`; note the `ReturnValue` / `HasReturnValue` properties on `ExecutionResult`. `docs/architecture.md` — note `CommandCallback` now returns `object`.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update: API Layer Summary — `CommandCallback` delegate signature; `ExecutionResult` description; `CommandHistoryEntry` description.

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

- Suggested commit scope: `src/`, `tests/`
- Suggested commit message: `feat(instance-command-registration): change CommandCallback to return object; add ReturnValue to ExecutionResult and CommandHistoryEntry`

---

### Task 2: Registry Removal and Instance Infrastructure

- [ ] Not started

**Objective:**

Add `CommandRegistry.TryRemove`, the new `InstanceRegistry` internal component, new `RegistrationError` enum values, and the public `UnregisterResult` type. These are the building blocks needed before the scanner can be written.

**Inputs:**

- Requirements refs: R4, R5, R2 (NullTarget / InvalidInstanceKey errors)
- Design refs: §Modified Components — `CommandRegistry`, `RegistrationError`; §New Internal Components — `InstanceRegistry`; §API/Contract Sketch — `UnregisterResult`; §Components and Responsibilities — InstanceRegistry Detail; §CommandRegistry.TryRemove

**Implementation Steps:**

1. Add `internal bool TryRemove(string name)` to `CommandRegistry` in `src/Core/CommandRegistry.cs`:
   ```csharp
   internal bool TryRemove(string name)
   {
       return _commands.Remove(name);
   }
   ```
2. Add new enum values to `RegistrationError` in `src/Results/RegistrationResult.cs`:
   - `NullTarget` — target object passed to RegisterInstance was null.
   - `DuplicateInstanceKey` — an instance with the same key is already registered.
   - `InvalidInstanceKey` — key is null/empty or contains a dot.
3. Create `src/Core/InstanceRegistry.cs` — internal sealed class with:
   - `Dictionary<string, List<string>> _keyToNames` (OrdinalIgnoreCase)
   - `Dictionary<string, object> _keyToTarget` (OrdinalIgnoreCase)
   - `bool TryReserveKey(string key, object target)`
   - `void TrackCommand(string key, string fullCommandName)`
   - `bool TryGetCommandNames(string key, out List<string> names)`
   - `void RemoveKey(string key)`
   - `void Clear()`
4. Create `src/Results/UnregisterResult.cs` — public readonly struct:
   - `bool Success { get; }`
   - `int RemovedCount { get; }`
   - `string ErrorMessage { get; }`
   - Private constructor.
   - `internal static UnregisterResult Ok(int removedCount)`
   - `internal static UnregisterResult Fail(string message)`

**Validation:**

- Unit tests: New focused unit tests for `InstanceRegistry` and `CommandRegistry.TryRemove`:
  - `TryRemove` on existing command → returns `true`, command no longer in registry.
  - `TryRemove` on unknown name → returns `false`.
  - `InstanceRegistry.TryReserveKey` with new key → `true`.
  - `InstanceRegistry.TryReserveKey` with duplicate key → `false`.
  - `InstanceRegistry.TrackCommand` then `TryGetCommandNames` → correct list.
  - `InstanceRegistry.RemoveKey` → key and commands gone.
  - `InstanceRegistry.Clear` → empty state.
  - `UnregisterResult.Ok` / `Fail` factory methods return correct values.
- Additional checks: All 186+ existing tests still pass (no regressions from `RegistrationError` enum additions).
- QA quick pass (`taskReviewer`): Yes.
- taskReviewer review request:
  - Review scope: `CommandRegistry.cs` (TryRemove), `RegistrationResult.cs` (new enum values), `InstanceRegistry.cs` (new file), `UnregisterResult.cs` (new file).
  - Primary checks: `TryRemove` uses the case-insensitive dictionary correctly; `InstanceRegistry` dictionaries use `OrdinalIgnoreCase`; `UnregisterResult` is immutable; no accidental public exposure of internals.
  - Required evidence: New unit tests passing; 186+ existing tests passing.
  - Blocking conditions: `TryRemove` case-sensitivity mismatch; `InstanceRegistry` leaking internal types publicly.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments reviewed: required before marking complete

**Documentation Sync:**

- Docs to update in `docs/`: None required for this task — purely internal infrastructure; no public API surface yet except `UnregisterResult` and `RegistrationError` enum additions which are documented in the next task.
- Update `.github/instructions/projectOverview.instructions.md` required: No (internal file additions; public API changes documented in Task 4).

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

- Suggested commit scope: `src/Core/`, `src/Results/`
- Suggested commit message: `feat(instance-command-registration): add InstanceRegistry, UnregisterResult, TryRemove, and new RegistrationError values`

---

### Task 3: InstanceScanner and InstanceCallbackBuilder

- [ ] Not started

**Objective:**

Implement the two new internal components that do the core discovery work: `InstanceCallbackBuilder` (builds AOT-safe instance-bound delegates) and `InstanceScanner` (discovers members on a type and registers them). Also create the public `InstanceScanMode` enum. Add `IsInstanceCommand` flag to `CommandDefinition`.

**Inputs:**

- Requirements refs: R7, R8, R9, R10, R11, R17, R18
- Design refs: §New Internal Components — InstanceScanner, InstanceCallbackBuilder; §Components and Responsibilities — all three sections; §Implementation Notes — InstanceCallbackBuilder Detail, InstanceScanner Detail; §Auto-Scan Deduplication

**Implementation Steps:**

1. Add `public bool IsInstanceCommand { get; }` to `CommandDefinition` in `src/Core/CommandDefinition.cs`. Update the constructor to accept and store it (default `false` for existing callers). Update `AttributeScanner` and `CommandSystem.Register` to pass `false`; instance paths will pass `true`.
2. Create `src/InstanceScanMode.cs` — public enum with `Auto = 0` and `AttributeOnly = 1`.
3. Create `src/Core/InstanceCallbackBuilder.cs` — internal static class with:
   - `BuildMethodCallback(object target, MethodInfo method, ParameterInfo[] parameters) → CommandCallback`
     - Zero-param void fast path: `Action` delegate, returns `null`.
     - Zero-param non-void fast path: `Func<TReturn>` delegate via `Delegate.CreateDelegate`.
     - N-param void: `GetActionDelegateType` helper, `DynamicInvoke`, returns `null`.
     - N-param non-void: `GetFuncDelegateType` helper, `DynamicInvoke`, returns value.
   - `BuildGetterCallback(object target, PropertyInfo property) → CommandCallback`
     - `Func<TReturn>` via `GetGetMethod()`, `DynamicInvoke(null)`.
   - `BuildSetterCallback(object target, PropertyInfo property) → CommandCallback`
     - `Action<T>` via `GetSetMethod()`, `DynamicInvoke(args)`, returns `null`.
   - Private `GetActionDelegateType(Type[] paramTypes)` — supports 1–4 params (same pattern as `AttributeScanner`).
   - Private `GetFuncDelegateType(Type[] paramTypes, Type returnType)` — supports 0–4 params.
4. Create `src/Core/InstanceScanner.cs` — internal sealed class:
   - Constructor: `InstanceScanner(CommandRegistry, ArgumentConverter, InstanceRegistry)`.
   - `internal ScanResult Scan(object target, string instanceKey, ScanOptions options, InstanceScanMode mode)`:
     - Step 1: `ScanAttributeDecoratedMethods` — `Public | NonPublic | Instance | DeclaredOnly`; only methods with `[Command]`; skip static; apply `IsDevOnly` filter; validate params (no ref/out/in, no generics, type support); build full name `key.attr.Name`; call `InstanceCallbackBuilder.BuildMethodCallback`; create `CommandDefinition` with `IsInstanceCommand = true`; `TryRegister`; track in `InstanceRegistry`.
     - Step 2 (only if `mode == Auto`): `ScanPublicMethods` — `Public | Instance | DeclaredOnly`; skip `IsSpecialName`, abstract, generic, ref/out/in params, unsupported types, already has `[Command]`; command name = `method.Name`; similar pipeline.
     - Step 3 (only if `mode == Auto`): `ScanPublicProperties` — `Public | Instance | DeclaredOnly`; skip indexers; for each readable property with public getter: register `get_PropName` (0 params, `IsInstanceCommand = true`); for each writable property with public setter and supported type: register `set_PropName` (1 param, `IsInstanceCommand = true`).
     - Collect all `ScanEntry` results; return `new ScanResult(entries.ToArray())`.

**Validation:**

- Unit tests: New `InstanceScannerTests.cs` (or included in `InstanceCommandRegistrationTests.cs`):
  - `[Command]`-decorated private method discovered with `attr.Name`.
  - `[Command]` with `IsDevOnly = true` skipped when `DevMode = false`; included when `DevMode = true`.
  - Public instance method auto-scanned as `key.MethodName`.
  - Public read-write property produces both `key.get_X` and `key.set_X`.
  - Public read-only property produces only `key.get_X`.
  - Public write-only property produces only `key.set_X`.
  - Private method without `[Command]` not registered.
  - Static method not registered.
  - `ToString`, `GetHashCode`, `Equals` not registered (inherited from `object`).
  - Method with generic parameters: produces failed `ScanEntry` and is skipped.
  - Method with ref/out/in parameter: produces failed `ScanEntry` and is skipped.
  - Method with unsupported parameter type: produces failed `ScanEntry`.
  - Indexer property not registered.
  - `[Command]`-decorated method not double-registered by auto-scan.
  - `InstanceScanMode.AttributeOnly` suppresses auto-scan of public methods and properties.
  - Registered commands have `IsInstanceCommand == true`; manually registered commands have `false`.
  - Callback execution via `InstanceCallbackBuilder`: void method returns `null`; non-void method returns the value.
  - Property getter callback returns the property value.
  - Property setter callback sets the value (verify side effect).
- Additional checks: 186+ existing tests still pass.
- QA quick pass (`taskReviewer`): Yes.
- taskReviewer review request:
  - Review scope: `InstanceScanMode.cs`, `InstanceCallbackBuilder.cs`, `InstanceScanner.cs`, `CommandDefinition.cs` (IsInstanceCommand).
  - Primary checks: `DeclaredOnly` flag correctly excludes inherited members; `IsSpecialName` filter correctly skips property accessors in method scan (they are scanned separately via property scan); `Delegate.CreateDelegate` with instance target is AOT-safe; getter for a `float` property with a private setter only produces `get_`; `[Command]` methods not duplicated by auto-scan; `IsInstanceCommand` flag set correctly on all paths.
  - Required evidence: All new unit tests passing; 186+ existing tests passing.
  - Blocking conditions: `object`-inherited methods appearing in registered commands; double-registration of `[Command]` methods; `IsInstanceCommand` flag not set for instance-path commands; AOT-unsafe code patterns (Emit, DynamicMethod).
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments addressed: required before marking complete

**Documentation Sync:**

- Docs to update in `docs/`: None required at this task level — internals only; public API documented in Task 4.
- Update `.github/instructions/projectOverview.instructions.md` required: No.

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

- Suggested commit scope: `src/Core/`, `src/`
- Suggested commit message: `feat(instance-command-registration): add InstanceScanner, InstanceCallbackBuilder, and InstanceScanMode`

---

### Task 4: CommandSystem Public API and ExecutionHandler InstanceNull Handling

- [ ] Not started

**Objective:**

Wire the new components into `CommandSystem` via the public `RegisterInstance` / `UnregisterInstance` API. Update `ExecutionHandler` to catch `TargetInvocationException` wrapping `NullReferenceException` and return `ExecutionError.InstanceNull` for instance commands. Integrate `InstanceRegistry` into `Shutdown`. Add `ExecutionError.InstanceNull`.

**Inputs:**

- Requirements refs: R1, R2, R3, R4, R5, R6, R12, R15, R16
- Design refs: §API/Contract Sketch — CommandSystem Public API Additions; §NullReferenceException Handling in ExecutionHandler; §Shutdown Integration; §ExecutionError enum addition; §RegisterInstance Flow; §UnregisterInstance Flow

**Implementation Steps:**

1. Add `ExecutionError.InstanceNull` to the `ExecutionError` enum in `src/Results/ExecutionResult.cs`.
2. Update `ExecutionHandler.Execute` in `src/Core/ExecutionHandler.cs` — replace the single `catch (Exception ex)` with the three-catch pattern:
   ```csharp
   catch (TargetInvocationException ex) when (definition.IsInstanceCommand && ex.InnerException is NullReferenceException)
   {
       return ExecutionResult.Fail(
           ExecutionError.InstanceNull,
           string.Format("Command '{0}' failed: the bound instance is null or destroyed.", commandName),
           ex.InnerException);
   }
   catch (TargetInvocationException ex)
   {
       return ExecutionResult.Fail(
           ExecutionError.CallbackThrewException,
           string.Format("Command '{0}' callback threw an exception: {1}",
               commandName, ex.InnerException != null ? ex.InnerException.Message : ex.Message),
           ex.InnerException ?? ex);
   }
   catch (Exception ex)
   {
       return ExecutionResult.Fail(
           ExecutionError.CallbackThrewException,
           string.Format("Command '{0}' callback threw an exception: {1}", commandName, ex.Message),
           ex);
   }
   ```
   Note: `ExecutionHandler` needs access to `CommandDefinition.IsInstanceCommand` — it already has the `definition` local variable from the registry lookup.
3. Add `private InstanceRegistry _instanceRegistry;` field to `CommandSystem`.
4. In `InitializeCore`, construct `_instanceRegistry = new InstanceRegistry();` alongside the other components.
5. In `Shutdown`, add `_instanceRegistry?.Clear(); _instanceRegistry = null;` (alongside existing null-outs).
6. Add `private InstanceScanner _instanceScanner;` field to `CommandSystem`.
7. In `InitializeCore`, construct `_instanceScanner = new InstanceScanner(_registry, _converter, _instanceRegistry);`.
8. In `Shutdown`, set `_instanceScanner = null;`.
9. Add `RegisterInstance` overloads to `CommandSystem`:

   ```csharp
   public ScanResult RegisterInstance(object target, string instanceKey)
   {
       return RegisterInstance(target, instanceKey, default, InstanceScanMode.Auto);
   }

   public ScanResult RegisterInstance(
       object target,
       string instanceKey,
       ScanOptions options,
       InstanceScanMode mode = InstanceScanMode.Auto)
   ```

   - Guard: not initialized → `ScanResult.SystemFailure(RegistrationError.NotInitialized, ...)`.
   - Guard: `target == null` → `ScanResult.SystemFailure(RegistrationError.NullTarget, ...)`.
   - Guard: `string.IsNullOrEmpty(instanceKey)` → `ScanResult.SystemFailure(RegistrationError.InvalidInstanceKey, ...)`.
   - Guard: `instanceKey.Contains('.')` → `ScanResult.SystemFailure(RegistrationError.InvalidInstanceKey, ...)`.
   - Guard: `!_instanceRegistry.TryReserveKey(instanceKey, target)` → `ScanResult.SystemFailure(RegistrationError.DuplicateInstanceKey, ...)`.
   - Delegate to `_instanceScanner.Scan(target, instanceKey, options, mode)` and return.

10. Add `UnregisterInstance` to `CommandSystem`:
    ```csharp
    public UnregisterResult UnregisterInstance(string instanceKey)
    ```

    - Guard: not initialized → `UnregisterResult.Fail("CommandSystem has not been initialized.")`.
    - Guard: `string.IsNullOrEmpty(instanceKey)` → `UnregisterResult.Fail("Instance key must not be null or empty.")`.
    - If `!_instanceRegistry.TryGetCommandNames(instanceKey, out List<string> names)` → `UnregisterResult.Fail(string.Format("No instance registered with key '{0}'.", instanceKey))`.
    - Remove each name: `for (int i = 0; i < names.Count; i++) { _registry.TryRemove(names[i]); }`.
    - `_instanceRegistry.RemoveKey(instanceKey)`.
    - Return `UnregisterResult.Ok(names.Count)`.

**Validation:**

- Unit tests: Add to a new or existing `InstanceCommandRegistrationTests.cs`:
  - `RegisterInstance` before `Initialize` → `NotInitialized` failure.
  - `RegisterInstance` with null target → `NullTarget` failure.
  - `RegisterInstance` with null/empty key → `InvalidInstanceKey` failure.
  - `RegisterInstance` with key containing `.` → `InvalidInstanceKey` failure.
  - `RegisterInstance` with duplicate key → `DuplicateInstanceKey` failure; first registration intact (commands still executable).
  - `RegisterInstance` success → commands appear in `GetCommandNames()`, `TryGetCommandParameters()`, `GetSnapshot()`.
  - Execute instance command → success.
  - Execute property getter → `ReturnValue` populated.
  - Execute after instance GC'd → `ExecutionError.InstanceNull`.
  - Static command throwing `NullReferenceException` → `ExecutionError.CallbackThrewException` (not `InstanceNull`).
  - `UnregisterInstance` before `Initialize` → failure.
  - `UnregisterInstance` with unknown key → graceful failure, `RemovedCount == 0`.
  - `UnregisterInstance` success → all commands gone from all discovery APIs; `Execute` returns `CommandNotFound`.
  - `UnregisterResult.RemovedCount` equals expected count.
  - `Shutdown` then `Initialize` cycle clears instance state (re-registering same key works after re-init).
- Additional checks: 186+ existing tests still pass.
- QA quick pass (`taskReviewer`): Yes.
- taskReviewer review request:
  - Review scope: `CommandSystem.cs` (RegisterInstance overloads, UnregisterInstance, InitializeCore, Shutdown), `ExecutionHandler.cs` (three-catch block), `ExecutionResult.cs` (InstanceNull enum value).
  - Primary checks: All input validation guards are in correct order; `TargetInvocationException` unwrapping correctly distinguishes `InstanceNull` from other exceptions; `IsInstanceCommand` flag gates the `InstanceNull` branch so static commands are unaffected; `Shutdown` properly nulls all new fields; `UnregisterInstance` removes from both `InstanceRegistry` and `CommandRegistry`; discovery APIs are clean after unregister.
  - Required evidence: All new integration tests passing; 186+ existing tests passing; explicit test showing static-command `NullReferenceException` produces `CallbackThrewException`.
  - Blocking conditions: `InstanceNull` reported for non-instance commands; stale commands remaining in registry after unregister; `Initialize` guard missing on `RegisterInstance` / `UnregisterInstance`.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments addressed: required before marking complete

**Documentation Sync:**

- Docs to update in `docs/`:
  - `docs/commands.md` — add section on `RegisterInstance` / `UnregisterInstance` usage; property command naming (`get_` / `set_`); `InstanceScanMode`; `UnregisterResult`; `ExecutionError.InstanceNull` and what it means for lifecycle management.
  - `docs/architecture.md` — update component map to include `InstanceScanner`, `InstanceCallbackBuilder`, `InstanceRegistry`; note `CommandDefinition.IsInstanceCommand` flag; note `ExecutionHandler` three-catch pattern.
  - `docs/unity-integration.md` — add `MonoBehaviour`-hosted command example: `RegisterInstance(this, "player")` in `Start`; `UnregisterInstance("player")` in `OnDestroy`.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update:
  - Key Paths — new files: `src/InstanceScanMode.cs`, `src/Results/UnregisterResult.cs`, `src/Core/InstanceScanner.cs`, `src/Core/InstanceCallbackBuilder.cs`, `src/Core/InstanceRegistry.cs`.
  - API Layer Summary — `RegisterInstance` / `UnregisterInstance` API; `InstanceScanMode`; `UnregisterResult`; `ExecutionError.InstanceNull`; `CommandCallback` now returns `object`; `ExecutionResult.ReturnValue` / `HasReturnValue`; `CommandHistoryEntry.ReturnValue`.
  - Current Repository State — `[Command]` on instance methods; instance command registration now fully implemented.

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

- Suggested commit scope: `src/`, `docs/`, `.github/instructions/`
- Suggested commit message: `feat(instance-command-registration): add RegisterInstance/UnregisterInstance API; InstanceNull error handling; update docs`

---

### Task 5: Full Integration Tests

- [ ] Not started

**Objective:**

Write end-to-end integration tests covering all acceptance criteria from the requirements. These tests exercise the full stack: `CommandSystem` → `InstanceScanner` → `CommandRegistry` → `ExecutionHandler` → `CommandHistoryBuffer`. Verify all 18 requirements are covered.

**Inputs:**

- Requirements refs: R1–R18 (all — final coverage verification)
- Design refs: §Testing Strategy — Unit Tests; §Acceptance Overview; §Final Review Contract — Required Test Evidence

**Implementation Steps:**

1. Create `tests/kmCommands.Tests/InstanceCommandRegistrationTests.cs` (or extend if partially created in Task 4):
   - Organise into `[TestFixture]` regions matching the design's test categories: Registration, Auto-scan, Unregister, Execution, History, Discovery.
   - Declare small focused test classes (`PlayerForTests`, `EnemyForTests`, etc.) as private nested types within the test class to avoid polluting the test namespace.
2. Ensure the following scenarios are explicitly covered with one test each minimum:

   **Registration:**
   - R1: `RegisterInstance` before `Initialize` → `NotInitialized`.
   - R2a: `RegisterInstance(null, "player")` → `NullTarget`.
   - R2b: `RegisterInstance(obj, null)` and `RegisterInstance(obj, "")` → `InvalidInstanceKey`.
   - R3: Second `RegisterInstance` with same key → `DuplicateInstanceKey`; first registration intact.
   - R6: Key with `.` in it → `InvalidInstanceKey`.
   - R9 + R10: `[Command]`-decorated instance method registered with correct name; `IsDevOnly = true` respects `ScanOptions.DevMode`.
   - R11: `InstanceScanMode.AttributeOnly` suppresses auto-scan.

   **Auto-scan (R7, R8, R18):**
   - R7: `ToString`, `GetHashCode`, `GetType`, `Equals` not registered.
   - R8: Property naming — read-write (`get_` + `set_`), read-only (`get_` only), write-only (`set_` only).
   - R18: Method with `ref` param skipped with descriptive `ScanEntry`; method with unsupported type skipped.

   **Unregister (R4, R5):**
   - R4: `UnregisterInstance` removes all commands; subsequent `Execute` returns `CommandNotFound`.
   - R5: `UnregisterInstance` with unknown key → graceful failure.

   **Execution (R12, R13):**
   - R13: Non-void instance method → `ExecutionResult.ReturnValue` set; void method → `null`.
   - R12: Execute on dead instance → `ExecutionError.InstanceNull`.

   **History (R14):**
   - R14: History entry after instance command execution has `ReturnValue` matching return.

   **Discovery (R15, R16):**
   - R15: Instance commands in `GetCommandNames()`, `TryGetCommandParameters()`, `GetSnapshot()`.
   - R16: After `UnregisterInstance`, commands absent from all three APIs.

3. Run full test suite — all 186+ pre-existing tests plus all new tests must pass.
4. Confirm `ScanResult.HasErrors` remains `false` for a clean `RegisterInstance` call.
5. Confirm `ScanResult.Entries` contains `ScanEntry` records for each registered command.

**Validation:**

- Unit tests: All new tests plus 186+ existing tests pass.
- Additional checks: Code coverage should touch `InstanceScanner`, `InstanceCallbackBuilder`, `InstanceRegistry`, `UnregisterResult`, and the new `ExecutionHandler` catch paths.
- QA quick pass (`taskReviewer`): Yes — this is the final coverage gate.
- taskReviewer review request:
  - Review scope: `InstanceCommandRegistrationTests.cs` (full file); all 18 requirements mapped to at least one test.
  - Primary checks: Every numbered requirement (R1–R18) has a matching test; discovery API tests cover all three methods; history `ReturnValue` test verifies the actual stored value not just a `null` check; `InstanceNull` test verifies the error code explicitly (not just `!Success`); no test hardcodes internal command counts that could break with new auto-scan targets.
  - Required evidence: Full test suite run output showing 0 failures and the count of new tests added.
  - Blocking conditions: Any R1–R18 requirement with no test; any test that passes trivially without testing the intended behaviour; any regression in the 186+ base tests.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step
- Comments/check comments addressed: required before marking complete

**Documentation Sync:**

- Docs to update in `docs/`: None additional — documentation was updated in Task 4.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes
- If Yes, sections to update: Current Repository State — update test count to reflect new total; note `InstanceCommandRegistrationTests.cs` exists.

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

- Suggested commit scope: `tests/`
- Suggested commit message: `test(instance-command-registration): add full integration tests covering R1–R18`

---

## Coverage Check

- Requirements coverage:
  - [ ] Every requirement is mapped to at least one task
  - [ ] No requirement is left unplanned

- Design coverage:
  - [ ] Key design components are mapped to tasks
  - [ ] Critical design constraints are represented in validation gates

- Gaps or follow-ups:
  - Auto-key convenience overload (`RegisterInstance(object target)` returning the generated key) — explicitly deferred: not required by any current requirement; add if consumer feedback demands it.
  - Broadcasting (all instances sharing a type key) — explicitly deferred per requirements Out of Scope section.
  - `InstanceScanMode` may gain additional values in future (e.g. `PropertiesOnly`) — design is open-closed; no action required now.

### Requirements-to-Task Mapping

| Requirement                                               | Task(s)                                              |
| --------------------------------------------------------- | ---------------------------------------------------- |
| R1 — RegisterInstance fails before Initialize             | T4                                                   |
| R2 — Null target / empty key rejected                     | T4                                                   |
| R3 — Duplicate instanceKey rejected                       | T4                                                   |
| R4 — UnregisterInstance removes all commands atomically   | T2 (infrastructure), T4 (API)                        |
| R5 — UnregisterInstance unknown key = graceful failure    | T4                                                   |
| R6 — Dot not permitted in instanceKey                     | T4                                                   |
| R7 — Auto-scan declared-only (no inherited members)       | T3 (scanner), T5 (verified)                          |
| R8 — get*/set* property naming                            | T3 (scanner), T5 (verified)                          |
| R9 — [Command] on instance methods respected              | T3 (scanner), T5 (verified)                          |
| R10 — ScanOptions passed to RegisterInstance              | T4 (API), T5 (verified)                              |
| R11 — InstanceScanMode.AttributeOnly suppresses auto-scan | T3 (scanner), T5 (verified)                          |
| R12 — NullReferenceException → InstanceNull error         | T4 (ExecutionHandler)                                |
| R13 — ExecutionResult.ReturnValue for non-void callbacks  | T1 (plumbing), T4 (wired)                            |
| R14 — CommandHistoryEntry.ReturnValue                     | T1 (plumbing), T5 (verified)                         |
| R15 — Instance commands in discovery APIs                 | T4 (API), T5 (verified)                              |
| R16 — UnregisterInstance removes from discovery           | T4 (API), T5 (verified)                              |
| R17 — IL2CPP/AOT safe (no Emit/DynamicMethod)             | T3 (InstanceCallbackBuilder pattern), T3 review gate |
| R18 — ref/out/in/generic params skipped with entry        | T3 (scanner), T5 (verified)                          |

### Design-to-Task Mapping

| Design Component                                                       | Task   |
| ---------------------------------------------------------------------- | ------ |
| `CommandCallback` → `object`-returning                                 | T1     |
| `ExecutionResult.ReturnValue` / `HasReturnValue`                       | T1     |
| `CommandHistoryEntry.ReturnValue`                                      | T1     |
| `CommandHistoryBuffer.Record` updated                                  | T1     |
| `AttributeScanner.BuildCallback` void/non-void                         | T1     |
| `CommandRegistry.TryRemove`                                            | T2     |
| `InstanceRegistry`                                                     | T2     |
| `UnregisterResult`                                                     | T2     |
| `RegistrationError` new values                                         | T2     |
| `InstanceScanMode`                                                     | T3     |
| `InstanceCallbackBuilder`                                              | T3     |
| `InstanceScanner`                                                      | T3     |
| `CommandDefinition.IsInstanceCommand`                                  | T3     |
| `ExecutionError.InstanceNull`                                          | T4     |
| `ExecutionHandler` three-catch block                                   | T4     |
| `CommandSystem.RegisterInstance` API                                   | T4     |
| `CommandSystem.UnregisterInstance` API                                 | T4     |
| `InitializeCore` / `Shutdown` updated                                  | T4     |
| End-to-end acceptance criteria                                         | T5     |
| Docs update (`commands.md`, `architecture.md`, `unity-integration.md`) | T4     |
| `projectOverview.instructions.md` update                               | T4, T5 |
