# Instance Command Registration — Improvements Tasks

## Status

- [ ] Planned
- [ ] In Progress
- [ ] Completed

## Inputs

- Requirements: `.github/tasks/instance-improvements/requirements.md`
- Design: `.github/tasks/instance-improvements/design.md`

## Branch

- Name: `feat_instance-improvements`
- Rationale: `feat_` — new capabilities and behavior fixes extending the completed instance command registration feature

## Global Execution Notes

- Work is implemented in order, task by task (Tasks 1–4 are naturally sequential; Tasks 5–6 depend on 1+3; Task 7 can be done last).
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status -> Completed` may be checked only after all tasks and `## Coverage Check` items are checked.

---

## Task List

### Task 1: Auto-Scan DevMode Filtering and `[CommandIgnore]`

- [ ] Not started

**Objective:**

Close the release-safety gap in `InstanceScanner` and introduce the `[CommandIgnore]` exclusion attribute. After this task, auto-scanned public members are only registered when DevMode is on, and decorated members can be explicitly excluded from all scan modes.

**Inputs:**

- Requirements refs: R1, R2, R3
- Design refs: §1 Auto-Scan DevMode Filtering, §2 `CommandIgnoreAttribute`

**Implementation Steps:**

1. Create `src/CommandIgnoreAttribute.cs`:
   - `[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]`
   - `public sealed class CommandIgnoreAttribute : Attribute { }`
   - Add required source file header.

2. Modify `InstanceScanner.ScanAttributeDecoratedMethods`:
   - After finding `[Command]` on a method, check for `[CommandIgnore]`. If present, `continue` — skip without adding a `ScanEntry`.

3. Modify `InstanceScanner.ScanPublicMethods`:
   - Add `ScanOptions options` parameter.
   - In the loop, after the `[Command]` skip check, add: `if (method.GetCustomAttribute<CommandIgnoreAttribute>() != null) continue;`
   - Add: `if (!options.DevMode) continue;` (implicitly dev-only guard).

4. Modify `InstanceScanner.ScanPublicProperties`:
   - Add `ScanOptions options` parameter.
   - In the loop, after the indexer check, add: `if (property.GetCustomAttribute<CommandIgnoreAttribute>() != null) continue;`
   - Add: `if (!options.DevMode) continue;` (implicitly dev-only guard).

5. Update all call sites of `ScanPublicMethods` and `ScanPublicProperties` inside `InstanceScanner.Scan` to pass `options`.

**Validation:**

- Unit tests in `tests/kmCommands.Tests/InstanceScannerTests.cs`:
  - `AutoScan_DevModeOff_SkipsPublicMethods` — scanning in Auto mode with DevMode off registers zero auto-scanned methods.
  - `AutoScan_DevModeOff_SkipsPublicProperties` — scanning in Auto mode with DevMode off registers no getter/setter commands from un-attributed properties.
  - `AutoScan_DevModeOn_RegistersPublicMethods` — scanning with DevMode on registers public methods.
  - `AutoScan_DevModeOn_RegistersPublicProperties` — scanning with DevMode on produces getter/setter commands.
  - `AutoScan_ExplicitCommand_AlwaysRegistered_RegardlessOfDevMode` — a `[Command]` method without `IsDevOnly` always registers regardless of DevMode.
  - `CommandIgnore_OnMethod_SkipsInAutoScan` — `[CommandIgnore]` method absent from result.
  - `CommandIgnore_OnMethod_SkipsInAttributeScan` — `[CommandIgnore]` on a also-`[Command]`-decorated method causes it to be skipped.
  - `CommandIgnore_OnProperty_SkipsGetterAndSetter` — `[CommandIgnore]` property produces no commands.
- Run the full test suite; all 272+ pre-existing tests must pass.

- QA quick pass (`taskReviewer`): yes
- taskReviewer review request:
  - Review scope: `src/CommandIgnoreAttribute.cs` (new), `src/Core/InstanceScanner.cs` (modified), `tests/kmCommands.Tests/InstanceScannerTests.cs` (new tests).
  - Primary checks: (a) Auto-scanned public method/property skipped when DevMode off. (b) `[Command]` without `IsDevOnly` still registers when DevMode off. (c) `[Command(IsDevOnly=true)]` still skips when DevMode off. (d) `[CommandIgnore]` prevents registration in both auto-scan and attribute-only modes. (e) `[CommandIgnore]` + `[Command]` together → skipped, no `ScanEntry` produced. (f) No regression in existing tests.
  - Required evidence: new unit test file/section with green runs; full test suite green.
  - Blocking conditions: any existing test fails; auto-scanned method appears when DevMode off; `[Command]` method missing when DevMode off and `IsDevOnly = false`.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md` — add "Instance Command DevMode Safety" section (items 1–4 from design §6): auto-scanned members are dev-only by default; `[Command]` is the release-safe opt-in; `[CommandIgnore]` usage; property naming convention. (Full prose doc with Unity examples from design §6.)
- Update `.github/instructions/projectOverview.instructions.md` required: **Yes**
- Sections to update:
  - Add `CommandIgnoreAttribute` to the public API surface list under `src/`.
  - Update `InstanceScanner` description to note DevMode filtering on auto-scan paths and `[CommandIgnore]` check.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (unit tests green)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated (`docs/commands.md` — DevMode safety section)
- [ ] `.github/instructions/projectOverview.instructions.md` synced

**Commit Note:**

- Suggested commit scope: `src/CommandIgnoreAttribute.cs`, `src/Core/InstanceScanner.cs`, `tests/kmCommands.Tests/InstanceScannerTests.cs`, `docs/commands.md`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `feat(instance-improvements): auto-scan DevMode filtering and CommandIgnore attribute`

---

### Task 2: `ScanOptions.ScanUpTo` Inheritance-Chain Boundary

- [ ] Not started

**Objective:**

Allow consumers to control how deep `RegisterInstance` walks the inheritance chain by adding `ScanOptions.ScanUpTo`. When `null` (default), behavior is `DeclaredOnly` — identical to current. When set, `InstanceScanner` scans each type in the chain from the concrete type up to (not including) the boundary.

**Inputs:**

- Requirements refs: R6, R7
- Design refs: §4 `ScanOptions.ScanUpTo`, `GetScanTypes` helper, updated `Scan` method

**Implementation Steps:**

1. Modify `src/ScanOptions.cs`:
   - Add `public Type ScanUpTo { get; set; }` property with XML doc (see design §4 for doc text).

2. Add `GetScanTypes(Type concreteType, Type scanUpTo)` private static helper to `InstanceScanner`:
   - When `scanUpTo == null`, return `new[] { concreteType }`.
   - Otherwise walk from `concreteType` up `BaseType`, stopping when `current == null`, `current == scanUpTo`, or `current == typeof(object)`. Return the collected `Type[]`.

3. Modify `InstanceScanner.Scan`:
   - Replace the single `Type type = target.GetType();` with `Type[] scanTypes = GetScanTypes(target.GetType(), options.ScanUpTo);`
   - Wrap the existing three scan calls in a `for` loop over `scanTypes`, passing each level's `Type type = scanTypes[t]` to `ScanAttributeDecoratedMethods`, `ScanPublicMethods`, `ScanPublicProperties`.
   - `BindingFlags.DeclaredOnly` is already used in all three methods — no change needed there; each level independently discovers its own members.

**Validation:**

- Unit tests in `InstanceScannerTests.cs`:
  - `ScanUpTo_Null_DiscoversDeclaredMembersOnly` — no members from a base class appear when `ScanUpTo` is null.
  - `ScanUpTo_MidHierarchy_IncludesIntermediateBaseMembers` — members from an intermediate base class appear, members from the boundary class do not.
  - `ScanUpTo_BoundaryTypeNotInHierarchy_ScansAll` — when `ScanUpTo` is not in the hierarchy, the walk proceeds to just below `object` (all user types scanned).
  - `ScanUpTo_EqualsConcreteType_ScansNothing` — degenerate case: empty command set returned.
  - `ScanUpTo_DevModeOff_InheritedAutoScanMembersStillSkipped` — combined: base class public methods still require DevMode on (R1 still applies per level).
- Full suite: all 272+ pre-existing tests pass.

- QA quick pass (`taskReviewer`): yes
- taskReviewer review request:
  - Review scope: `src/ScanOptions.cs` (modified), `src/Core/InstanceScanner.cs` (modified: `GetScanTypes` helper + `Scan` loop), `tests/kmCommands.Tests/InstanceScannerTests.cs` (new tests).
  - Primary checks: (a) `ScanUpTo = null` is functionally identical to current behavior. (b) Members from the boundary type itself are excluded. (c) `object` is never scanned regardless of `ScanUpTo`. (d) DevMode filtering from Task 1 still applies per hierarchy level. (e) No duplicate commands when the same method name exists at multiple levels (each level uses `DeclaredOnly` so duplication is impossible — verify).
  - Required evidence: new ScanUpTo unit tests green; all pre-existing tests green.
  - Blocking conditions: any existing auto-scan test fails; boundary type's own members appear in results; `object` methods appear in results.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md` — update the instance scanning section to mention `ScanOptions.ScanUpTo` and document the Unity use case (`ScanUpTo = typeof(MonoBehaviour)`).
- Update `.github/instructions/projectOverview.instructions.md` required: **Yes**
- Sections to update:
  - Update `ScanOptions` entry to note `ScanUpTo` property.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (unit tests green)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated (`docs/commands.md` — `ScanUpTo` section)
- [ ] `.github/instructions/projectOverview.instructions.md` synced

**Commit Note:**

- Suggested commit scope: `src/ScanOptions.cs`, `src/Core/InstanceScanner.cs`, `tests/kmCommands.Tests/InstanceScannerTests.cs`, `docs/commands.md`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `feat(instance-improvements): ScanOptions.ScanUpTo inheritance-chain boundary`

---

### Task 3: System-Wide DevMode Flag on `Initialize()`

- [ ] Not started

**Objective:**

Add an initialization-time `devMode` parameter to all `Initialize()` overloads. The stored flag is applied as the effective DevMode default for all subsequent `Scan()`, `RegisterInstance()`, and scan-at-init operations that do not explicitly set `ScanOptions.DevMode = true`. Cleared on `Shutdown()`.

**Inputs:**

- Requirements refs: R8, R9
- Design refs: §5 System-Wide DevMode Flag, `ResolveEffectiveOptions` helper, OR-semantic decision

**Implementation Steps:**

1. Add `private bool _devMode;` field to `CommandSystem`.

2. Add `private ScanOptions ResolveEffectiveOptions(ScanOptions callerOptions)` helper:
   ```
   if (_devMode && !callerOptions.DevMode) callerOptions.DevMode = true;
   return callerOptions;
   ```

3. Modify all six `Initialize()` overloads to accept `bool devMode = false` as an additional optional parameter (place last in signature to preserve backward compatibility with default arguments):
   - `Initialize()` → `Initialize(bool devMode = false)`
   - `Initialize(int historyCapacity)` → `Initialize(int historyCapacity, bool devMode = false)`
   - `Initialize(Type[] types, ScanOptions options, int historyCapacity)` → add `bool devMode = false` after `historyCapacity`
   - `Initialize(Assembly[] assemblies, ScanOptions options, int historyCapacity)` → same pattern
   - `Initialize(Type[] types, Assembly[] assemblies, ScanOptions options, int historyCapacity)` → same pattern
   - In all overloads: set `_devMode = devMode;` before calling `InitializeCore(...)`.
   - In the scan-at-init overloads: pass `ResolveEffectiveOptions(options)` instead of `options` to `RunInitTimeScans(...)`.

4. Apply `ResolveEffectiveOptions` at all call sites where `ScanOptions` flows through `CommandSystem`:
   - `Scan(Type type, ScanOptions options)` — resolve before passing to `_attributeScanner.ScanType`.
   - `Scan(Assembly assembly, ScanOptions options)` — resolve before passing to `_attributeScanner.ScanAssembly`.
   - `RegisterInstance(..., ScanOptions options, ...)` — resolve before passing to `_instanceScanner.Scan` (and later to `ScanFromProfile` in Task 4).
   - `ScanCommandHosts(...)` overloads (Task 4) — resolve before passing to `_instanceScanner.BuildProfile`.

5. Modify `Shutdown()`: add `_devMode = false;` in the cleanup block.

**Validation:**

- Unit tests in `tests/kmCommands.Tests/CommandSystemTests.cs`:
  - `Initialize_WithDevModeTrue_SetsEffectiveDevMode` — initialize with `devMode: true`, then call `RegisterInstance` with a type that has public auto-scan methods; confirm those methods are registered (DevMode was inherited from system flag).
  - `Initialize_WithDevModeFalse_DefaultBehavior` — initialize without devMode; auto-scanned members absent.
  - `Initialize_WithDevModeTrue_ExplicitScanOptions_DevModeTrue_Works` — passing `new ScanOptions { DevMode = true }` still works (OR semantic).
  - `Shutdown_ClearsDevMode` — initialize with `devMode: true`, shutdown, re-initialize without devMode; confirm devMode is off.
  - `Scan_UsesSystemDevMode` — `Initialize(devMode: true)` followed by `Scan(type)` (no options) registers dev-only commands.
- Full suite: all 272+ pre-existing tests pass.

- QA quick pass (`taskReviewer`): yes
- taskReviewer review request:
  - Review scope: `src/CommandSystem.cs` (`_devMode` field, `ResolveEffectiveOptions`, all six `Initialize()` overloads, `Scan()`, `RegisterInstance()`, `Shutdown()`), `tests/kmCommands.Tests/CommandSystemTests.cs` (new tests).
  - Primary checks: (a) `Initialize(devMode: true)` visible effect on `RegisterInstance` with default `ScanOptions`. (b) `Initialize(devMode: false)` (or no argument) produces identical behavior to the current codebase. (c) OR semantic: once system DevMode is on, no per-call way to turn it off. (d) `Shutdown()` clears flag. (e) All scan-at-init overloads also respect the flag. (f) No regression in any existing test.
  - Required evidence: new CommandSystem DevMode unit tests green; full test suite green.
  - Blocking conditions: any existing test fails; devMode flag not cleared on Shutdown; per-call `ScanOptions` ignores system flag OR semantic.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/unity-integration.md` — add "DevMode Configuration" section documenting the system-wide `devMode` parameter and the recommended Unity macro pattern (design §6 items 7–8).
- Update `.github/instructions/projectOverview.instructions.md` required: **Yes**
- Sections to update:
  - Update all `Initialize()` overload descriptions in the API Layer Summary to note the `devMode` parameter.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (unit tests green)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated (`docs/unity-integration.md` — DevMode configuration section)
- [ ] `.github/instructions/projectOverview.instructions.md` synced

**Commit Note:**

- Suggested commit scope: `src/CommandSystem.cs`, `tests/kmCommands.Tests/CommandSystemTests.cs`, `docs/unity-integration.md`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `feat(instance-improvements): system-wide devMode flag on Initialize()`

---

### Task 4: `[CommandHost]` Attribute, `TypeCommandProfile`, and `ScanCommandHosts()`

- [ ] Not started

**Objective:**

Introduce startup pre-scanning for types decorated with `[CommandHost]`. Pre-scanning builds an immutable `TypeCommandProfile` (validated member metadata) per type via `ScanCommandHosts()`. Subsequent `RegisterInstance()` calls for pre-scanned types skip all `GetMethods()`/`GetProperties()` reflection — only `Delegate.CreateDelegate` binding occurs.

**Inputs:**

- Requirements refs: R4, R5
- Design refs: §3a `CommandHostAttribute`, §3b `TypeCommandProfile`, §3c `TypeCommandProfileCache`, §3d profile building, §3e `ScanFromProfile`, §3f `ScanCommandHosts` API, §3g `RegisterInstance` cache integration

**Implementation Steps:**

1. Create `src/CommandHostAttribute.cs`:
   - `[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]`
   - `public sealed class CommandHostAttribute : Attribute { }`
   - Required source file header.

2. Create `src/Core/TypeCommandProfile.cs`:
   - Sealed internal class with `MethodEntry[]` (attribute-decorated), `AutoScanMethods[]` (`MethodEntry[]`), `AutoScanProperties[]` (`PropertyEntry[]`).
   - `MethodEntry` readonly struct: `MethodInfo`, `ParameterInfo[] ReflectedParams`, `CommandParameterInfo[] Parameters`, `string CommandName`, `string Description`, `bool IsDevOnly`.
   - `PropertyEntry` readonly struct: `PropertyInfo`, `bool CanRead`, `bool CanWrite`, `bool SetterTypeSupported`.
   - All arrays immutable after construction.

3. Create `src/Core/TypeCommandProfileCache.cs`:
   - `Dictionary<Type, TypeCommandProfile>` backing store.
   - `TryGet`, `Add`, `Clear` methods.

4. Add `internal TypeCommandProfile BuildProfile(Type type, ScanOptions options)` to `InstanceScanner`:
   - Mirrors the reflection + validation logic of `Scan`, but produces `TypeCommandProfile` entries instead of registering commands or creating delegates.
   - For attribute methods: walk the same scan types as `Scan` (using `GetScanTypes` from Task 2), check `[CommandIgnore]`, validate params, build a `MethodEntry` per valid member.
   - For auto-scan methods: check `[CommandIgnore]`, validate (skip generics, ref params, unsupported types), build `MethodEntry` — **do not** apply DevMode filter here; DevMode filtering happens at `ScanFromProfile` time.
   - For auto-scan properties: check `[CommandIgnore]`, compute `SetterTypeSupported`, build `PropertyEntry`.

5. Add `internal ScanResult ScanFromProfile(object target, string instanceKey, ScanOptions options, InstanceScanMode mode, TypeCommandProfile profile)` to `InstanceScanner` (code given in design §3e):
   - Step 1: loop `profile.AttributeMethods` — apply `IsDevOnly` + DevMode filter, call `InstanceCallbackBuilder.BuildMethodCallback`, register.
   - Step 2 (Auto mode only): loop `profile.AutoScanMethods` — apply implicit DevMode filter, build callback, register; loop `profile.AutoScanProperties` — apply implicit DevMode filter, build getter/setter callbacks, register.

6. Add `_profileCache` field of type `TypeCommandProfileCache` to `CommandSystem`. Initialize in `InitializeCore()`. Add `_profileCache?.Clear(); _profileCache = null;` to `Shutdown()`.

7. Add four `ScanCommandHosts` overloads to `CommandSystem` (design §3f):
   - `ScanCommandHosts(Type[])` — delegates to `ScanCommandHosts(types, default)`.
   - `ScanCommandHosts(Type[], ScanOptions)` — validates initialized, resolves effective options, skips non-`[CommandHost]` types, calls `_instanceScanner.BuildProfile`, stores in `_profileCache`.
   - `ScanCommandHosts(Assembly[])` — delegates to `ScanCommandHosts(assemblies, default)`.
   - `ScanCommandHosts(Assembly[], ScanOptions)` — finds all `[CommandHost]`-decorated types in assemblies, delegates to `ScanCommandHosts(Type[], options)`.

8. Modify `CommandSystem.RegisterInstance(object, string, ScanOptions, InstanceScanMode)`:
   - After resolving effective options and passing guards, check `_profileCache.TryGet(target.GetType(), out TypeCommandProfile profile)`.
   - On cache hit: call `_instanceScanner.ScanFromProfile(target, instanceKey, effective, mode, profile)`.
   - On cache miss: call `_instanceScanner.Scan(target, instanceKey, effective, mode)` (existing path — unchanged).

9. Apply `ResolveEffectiveOptions` to `ScanCommandHosts` call sites (rely on the helper added in Task 3).

**Validation:**

- Unit/integration tests in `InstanceScannerTests.cs` and `InstanceCommandRegistrationTests.cs`:
  - `BuildProfile_ProducesCorrectAttributeMethodEntries` — `BuildProfile` on a target type returns `AttributeMethods` entries matching `[Command]`-decorated methods.
  - `BuildProfile_ProducesCorrectAutoScanMethodEntries` — public non-attributed methods appear in `AutoScanMethods` regardless of DevMode (DevMode is not applied at profile-build time).
  - `BuildProfile_Respects_CommandIgnore` — `[CommandIgnore]` methods are absent from all profile arrays.
  - `BuildProfile_Respects_ScanUpTo` — members from above the boundary are absent from the profile.
  - `ScanFromProfile_AttributeMethod_RegistersCorrectly` — executing a command registered via `ScanFromProfile` produces the expected result.
  - `ScanFromProfile_DevModeOff_SkipsAutoScanEntries` — auto-scan profile entries not registered when DevMode off.
  - `ScanFromProfile_DevModeOn_RegistersAutoScanEntries` — auto-scan entries registered when DevMode on.
  - `ScanCommandHosts_NonCommandHostType_SilentlySkipped` — passing a type without `[CommandHost]` produces no cache entry and no error.
  - `ScanCommandHosts_CommandHostType_CachesProfile` — after `ScanCommandHosts`, `RegisterInstance` for that type succeeds and produces the expected commands.
  - `RegisterInstance_ForPreScannedType_SkipsReflection` — verify by inspecting that `GetMethods`/`GetProperties` are not called; practically tested by confirming identical command output from a pre-scanned type vs. a cold-scan type.
- Full suite: all 272+ pre-existing tests pass.

- QA quick pass (`taskReviewer`): yes
- taskReviewer review request:
  - Review scope: `src/CommandHostAttribute.cs`, `src/Core/TypeCommandProfile.cs`, `src/Core/TypeCommandProfileCache.cs`, `src/Core/InstanceScanner.cs` (`BuildProfile` + `ScanFromProfile`), `src/CommandSystem.cs` (`_profileCache`, `ScanCommandHosts`, `RegisterInstance` cache check).
  - Primary checks: (a) Profile built correctly — attribute methods, auto-scan methods, auto-scan properties all captured. (b) `[CommandIgnore]` respected in `BuildProfile`. (c) `ScanUpTo` respected in `BuildProfile`. (d) DevMode NOT applied at `BuildProfile` time (filtering deferred to `ScanFromProfile`). (e) `ScanFromProfile` produces same command set as `Scan` for identical target + options. (f) `ScanCommandHosts` skips non-decorated types silently. (g) `Shutdown` clears profile cache. (h) No regression.
  - Required evidence: all new tests green; identical command output from pre-scanned vs. cold-scan paths; full suite green.
  - Blocking conditions: any existing test fails; `ScanFromProfile` produces different commands than direct `Scan` for same type + options; DevMode applied at build time causing wrong filtering later.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md` — add `[CommandHost]` usage note and `ScanCommandHosts()` pattern. `docs/architecture.md` — update internal component table to include `TypeCommandProfile` and `TypeCommandProfileCache`.
- Update `.github/instructions/projectOverview.instructions.md` required: **Yes**
- Sections to update:
  - Add `CommandHostAttribute` to public API surface.
  - Add `TypeCommandProfile` and `TypeCommandProfileCache` to `src/Core/` key paths.
  - Add `ScanCommandHosts()` to the API Layer Summary.
  - Update `InstanceScanner` description to note `BuildProfile` and `ScanFromProfile` paths.
  - Update `CommandSystem` description to note `_profileCache` field and cache integration.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (unit and integration tests green)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated (`docs/commands.md`, `docs/architecture.md`)
- [ ] `.github/instructions/projectOverview.instructions.md` synced

**Commit Note:**

- Suggested commit scope: `src/CommandHostAttribute.cs`, `src/Core/TypeCommandProfile.cs`, `src/Core/TypeCommandProfileCache.cs`, `src/Core/InstanceScanner.cs`, `src/CommandSystem.cs`, `tests/kmCommands.Tests/InstanceScannerTests.cs`, `tests/kmCommands.Tests/InstanceCommandRegistrationTests.cs`, `docs/commands.md`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `feat(instance-improvements): CommandHost attribute, TypeCommandProfile pre-scan cache, ScanCommandHosts API`

---

### Task 5: Integration Tests — 4-Arg `RegisterInstance` with `ScanOptions`

- [ ] Not started

**Objective:**

Add integration-level tests verifying the full end-to-end path through `CommandSystem.RegisterInstance(target, key, options, mode)` for DevMode filtering and scan mode behavior. These tests cover the public 4-arg overload directly and exercise the behavior changes from Tasks 1 and 3.

**Inputs:**

- Requirements refs: R11
- Design refs: §7 Integration Tests

**Implementation Steps:**

1. Add to `tests/kmCommands.Tests/InstanceCommandRegistrationTests.cs` — add helper target class `ExplicitCommandTarget` with a `[Command("explicit_cmd")]` method and a public `PublicAutoMethod`.

2. Add the following five tests (all using the existing `_system` fixture in that file):
   - `RegisterInstance_4Arg_DevModeOff_SkipsAutoScannedMembers` — `InstanceScanMode.Auto` + `DevMode = false` → `RegularMethod` absent.
   - `RegisterInstance_4Arg_DevModeOn_IncludesAutoScannedMembers` — `InstanceScanMode.Auto` + `DevMode = true` → `RegularMethod` present.
   - `RegisterInstance_4Arg_DevModeOff_RegistersExplicitCommandAttribute` — `DevMode = false` + `[Command("explicit_cmd")]` → command present.
   - `RegisterInstance_4Arg_AttributeOnlyMode_DevModeOff` — `InstanceScanMode.AttributeOnly` + `DevMode = false` → `dev_cmd` absent (IsDevOnly), `RegularMethod` absent (not an attribute method).
   - `RegisterInstance_4Arg_AttributeOnlyMode_DevModeOn` — `InstanceScanMode.AttributeOnly` + `DevMode = true` → `dev_cmd` present, `RegularMethod` absent (still no attribute — AttributeOnly mode).

3. Reuse existing `DevOnlyTarget` helper class already defined in that file (has `[Command("dev_cmd", IsDevOnly = true)]` and a public `RegularMethod()`).

**Validation:**

- All five new tests pass.
- All pre-existing tests in `InstanceCommandRegistrationTests.cs` continue to pass.
- Full suite: all 272+ pre-existing tests plus new ones pass.

- QA quick pass (`taskReviewer`): yes
- taskReviewer review request:
  - Review scope: `tests/kmCommands.Tests/InstanceCommandRegistrationTests.cs` (new tests + `ExplicitCommandTarget` class).
  - Primary checks: (a) Each test exercises a distinct path through the 4-arg overload. (b) Assertions are precise — use `Has.Member` / `Has.No.Member` on `GetCommandNames()`. (c) DevMode filtering at the end-to-end boundary (not just unit level). (d) `AttributeOnly` mode correctly prevents auto-scan membership even with DevMode on. (e) Tests are isolable — each test registers under a unique key.
  - Required evidence: all five new tests green; all pre-existing integration tests green; full suite green.
  - Blocking conditions: any pre-existing test fails; a test passes for the wrong reason (e.g., assertion too loose); both DevMode states not covered.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: none required for this task — tests only.
- Update `.github/instructions/projectOverview.instructions.md` required: **No** — no new production types or API surface.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (all five new tests green; full suite green)
- [ ] Unit tests passed or exception documented
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or N/A documented (N/A — tests only)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A)

**Commit Note:**

- Suggested commit scope: `tests/kmCommands.Tests/InstanceCommandRegistrationTests.cs`
- Suggested commit message: `test(instance-improvements): integration tests for 4-arg RegisterInstance with ScanOptions`

---

### Task 6: Documentation — Performance, Lifecycle, and Property Naming Notes

- [ ] Not started

**Objective:**

Complete the remaining documentation items from requirements R10 that were not already covered by Tasks 1–3: `DynamicInvoke` allocation warning, strong reference lifecycle warning, and property naming convention. These are doc-only changes; no production code is touched.

**Inputs:**

- Requirements refs: R10 (items not yet covered: `DynamicInvoke` cost, strong reference warning, property naming convention)
- Design refs: §6 Documentation Updates items 5–6

**Implementation Steps:**

1. `docs/commands.md` — append or update sections:
   - **Performance Notes:** `DynamicInvoke` allocation cost — instance callbacks with 1+ parameters box value-type args and allocate an internal array per call. Acceptable for user-triggered commands; flag as allocation hotspot for high-frequency invocation.
   - **Instance Lifecycle / Memory:** Strong reference held by `InstanceRegistry`. Forgetting `UnregisterInstance()` in `OnDestroy` prevents garbage collection. `InstanceNull` is a symptom, not a substitute for cleanup.
   - **Property Command Naming:** readable properties register as `instanceKey.get_PropName`, writable as `instanceKey.set_PropName`. Follows C# accessor naming; consumers should be aware when building command UI.

2. No changes to `src/` or tests.

**Validation:**

- Visual review: all three topics present in `docs/commands.md` with clear prose.
- Full suite: no test changes — all 272+ tests pass (unchanged).

- QA quick pass (`taskReviewer`): yes
- taskReviewer review request:
  - Review scope: `docs/commands.md` additions.
  - Primary checks: (a) DynamicInvoke note is accurate — describes per-call boxing behavior. (b) Strong reference warning clearly states `UnregisterInstance` is required, not optional. (c) Property naming example (`instanceKey.get_Health`, `instanceKey.set_Health`) is present. (d) No incorrect claims made about the library's behavior.
  - Required evidence: doc review confirmation; full test suite still green (no code changed).
  - Blocking conditions: factually incorrect statement about memory or performance behavior.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve before marking complete.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md` (items 5–6 from design §6).
- Update `.github/instructions/projectOverview.instructions.md` required: **No** — no new production types or API changes.

**Completion Gate:**

- [ ] Implementation done
- [ ] Validation passed (docs reviewed; test suite green)
- [ ] Unit tests passed or exception documented (N/A — doc-only task)
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated (`docs/commands.md`)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A)

**Commit Note:**

- Suggested commit scope: `docs/commands.md`
- Suggested commit message: `docs(instance-improvements): DynamicInvoke cost, strong reference warning, property naming convention`

---

## Coverage Check

- Requirements coverage:
  - [ ] Every requirement is mapped to at least one task
  - [ ] No requirement is left unplanned

- Design coverage:
  - [ ] Key design components are mapped to tasks
  - [ ] Critical design constraints are represented in validation gates

| Requirement | Task(s) |
|-------------|---------|
| R1 — Auto-scanned members skipped when DevMode off | Task 1 |
| R2 — `[Command]` without `IsDevOnly` always registers | Task 1 |
| R3 — `[CommandIgnore]` attribute causes skip in all modes | Task 1 |
| R4 — `[CommandHost]` attribute + `ScanCommandHosts()` | Task 4 |
| R5 — Pre-scanned types skip `GetMethods`/`GetProperties` | Task 4 |
| R6 — `ScanOptions.ScanUpTo` inheritance-chain walk | Task 2 |
| R7 — `ScanUpTo = null` preserves DeclaredOnly behavior | Task 2 |
| R8 — `Initialize()` overloads accept `devMode` parameter | Task 3 |
| R9 — System-wide DevMode applied via OR semantic | Task 3 |
| R10 — `docs/commands.md` and `docs/unity-integration.md` updated | Tasks 1, 2, 3 (partial); Task 6 (remainder) |
| R11 — Integration tests cover 4-arg `RegisterInstance` | Task 5 |

| Design Component | Task(s) |
|-----------------|---------|
| `CommandIgnoreAttribute` | Task 1 |
| `InstanceScanner` DevMode filter on auto-scan paths | Task 1 |
| `ScanOptions.ScanUpTo` + `GetScanTypes` helper | Task 2 |
| `InstanceScanner.Scan` hierarchy loop | Task 2 |
| `CommandSystem._devMode` + `ResolveEffectiveOptions` | Task 3 |
| All `Initialize()` overloads updated | Task 3 |
| `CommandHostAttribute` | Task 4 |
| `TypeCommandProfile` + `TypeCommandProfileCache` | Task 4 |
| `InstanceScanner.BuildProfile` | Task 4 |
| `InstanceScanner.ScanFromProfile` | Task 4 |
| `CommandSystem.ScanCommandHosts()` overloads | Task 4 |
| `RegisterInstance` cache-check integration | Task 4 |
| Integration tests for 4-arg `RegisterInstance` | Task 5 |
| Remaining docs (DynamicInvoke, lifecycle, property naming) | Task 6 |

- Gaps or follow-ups:
  - Design §6 documents items across Tasks 1, 2, 3, and 6 — no gap; each doc item is anchored to the task that introduces the corresponding feature.
  - The OR semantic for DevMode (design §5 decision) is documented in Task 3.
  - Profile `ScanUpTo` locking behavior (design Risks #2) is a doc note in Task 4.
