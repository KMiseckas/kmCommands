# Recommendations

> Action points identified by Feature Investigator. Remove items as they are implemented or explicitly discarded.

---

## Instance Command Registration

### Missing or Incomplete

- [ ] **Auto-scan ignores `ScanOptions.DevMode` — auto-scanned members must become implicitly dev-only:** `ScanPublicMethods` and `ScanPublicProperties` in `InstanceScanner` receive no `ScanOptions` and apply no dev-mode filter. **Decision:** auto-scanned public members (no `[Command]` attribute) are treated as implicitly dev-only and must only be registered when `DevMode = true`. Applying `[Command]` without `IsDevOnly` is the explicit, per-method release-safe consent mechanism. This is the release-safe default: a release build contains only what the developer explicitly marks.

- [ ] **No per-method exclusion attribute for auto-scan:** There is no `[CommandIgnore]` (or equivalent) attribute to opt a specific public method or property out of auto-scanning, forcing the consumer to choose between `InstanceScanMode.AttributeOnly` (annotate everything) or accepting all public members as commands in release builds.

- [ ] **`[CommandHost]` attribute for type-level pre-scanning and delegate-binding cache:** Introduce a `[CommandHost]` class-level attribute in the core library. At `Initialize()` time (via dedicated overload or `ScanCommandHosts(Type[]/Assembly[])`) the library pre-scans `[CommandHost]`-decorated types and builds a per-type `TypeCommandProfile` — the validated member list with `MethodInfo`/`PropertyInfo` references and resolved `CommandParameterInfo[]` arrays — at startup, not at `RegisterInstance()` time. Subsequent `RegisterInstance()` calls for a cached type skip `GetMethods()`/`GetProperties()` and all parameter-validation reflection; only `Delegate.CreateDelegate` binding (which must remain per-instance) occurs. The Unity integration layer uses this attribute to declare which MonoBehaviour types participate.

- [ ] **Add configurable scan-depth boundary to `ScanOptions`:** Currently `InstanceScanner` uses `BindingFlags.DeclaredOnly`, so inherited public members from user-defined base classes are never discovered. **Decision:** add an optional `Type scanUpTo` field to `ScanOptions` (or an overload of `RegisterInstance`). When set, `InstanceScanner` walks the inheritance chain from the concrete type up to (but not including) the boundary type. When `null` (default), current `DeclaredOnly` behaviour is preserved. The Unity integration layer sets `scanUpTo = typeof(UnityEngine.MonoBehaviour)` so intermediate user base classes are scanned while MonoBehaviour's own API surface is excluded.

- [ ] **Add a system-wide `DevMode` flag at `Initialize()` time:** Introduce an init-level mode parameter (e.g., `bool devMode = false` on `Initialize()` overloads or a stored property on `CommandSystem`) that sets the effective `DevMode` for all subsequent `Scan()`, `RegisterInstance()`, and scan-at-init operations without requiring the consumer to construct `new ScanOptions { DevMode = true }` at every call site. The Unity integration layer passes `devMode: true` via `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

- [ ] **Release vs. dev-mode guidance is absent from docs:** `docs/commands.md` and `docs/unity-integration.md` must document (a) that auto-scanned public members are dev-only by default, (b) that `[Command]` without `IsDevOnly` is the explicit release-safe consent mechanism, (c) how to use the system-wide `DevMode` init flag, and (d) the recommended Unity macro pattern (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`) for wiring dev mode.

### Improvements

- [ ] **`DynamicInvoke` causes allocations on every non-zero-param execution:** `InstanceCallbackBuilder` uses `del.DynamicInvoke(args)` for all callbacks with 1+ parameters (and for non-void zero-param callbacks). This boxes value type arguments and allocates an internal argument array on each call. The architecture docs acknowledge this as an intentional trade-off (`DynamicInvoke` on a pre-bound typed delegate is documented as acceptable because commands are user-triggered, not per-frame). Flag this as a known allocation hotspot in docs so consumers consciously understand the cost if they trigger commands at high frequency.

- [ ] **Strong reference in `InstanceRegistry` creates a memory-leak footprint in Unity:** `InstanceRegistry._keyToTarget` holds a strong reference to the target object. If `UnregisterInstance()` is never called (e.g., consumer forgets `OnDestroy` cleanup), the registered Unity object will not be garbage collected. Docs should clearly warn that `UnregisterInstance()` must be called to release the strong reference, and that the `InstanceNull` execution error is a symptom, not a substitute for proper cleanup.

- [ ] **Property command naming convention (`get_X` / `set_X`) is undocumented and console-unfriendly:** Auto-scanned properties produce command names like `player.get_Health` and `player.set_Health` — the C# accessor naming convention. This is surprising to end users typing into a dev console. Document this convention in `docs/commands.md`;

- [ ] **No test coverage for `RegisterInstance` + `ScanOptions` passed via `RegisterInstance(..., options, mode)` overload at the integration level:** `InstanceCommandRegistrationTests` tests dev-mode filtering via `InstanceScannerTests` (unit), but the 4-arg `RegisterInstance(target, key, options, mode)` public overload has no integration test that verifies `IsDevOnly` filtering end-to-end from `CommandSystem`.

---
