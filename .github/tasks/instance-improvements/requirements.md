# Instance Command Registration — Improvements

## Status

Draft

## Branch

- Name: `feat_instance-improvements`
- Rationale: new capabilities and behavior fixes extending the completed `feat_instance-command-registration` feature

## Reference

All items in this document originate from `.github/recommendations.md` under the **Instance Command Registration** section.

---

## Summary

The initial instance command registration feature (`feat_instance-command-registration`) shipped core functionality for registering instance-bound commands. A post-implementation review identified a set of missing behaviors, new capabilities, documentation gaps, and a test coverage gap. This work addresses all of them, coordinated in one PR.

The changes span four themes:

1. **DevMode safety** — auto-scan currently ignores `ScanOptions.DevMode`, creating a release-safety hole; a system-wide initialization-time DevMode flag is also absent.
2. **Scan control** — no mechanism exists to exclude specific members from auto-scan or to walk inherited members from user-defined base classes.
3. **Startup performance** — there is no way to pre-scan and cache per-type member metadata at startup to avoid repeated reflection at `RegisterInstance()` time.
4. **Documentation and tests** — several behaviors are undocumented, and one integration test path is uncovered.

---

## Goals

- Close the release-safety gap: auto-scanned members must never appear in release builds without explicit opt-in.
- Give consumers fine-grained control over which members are discoverable and to what depth.
- Enable startup-time pre-scanning so `RegisterInstance()` calls become delegate-binding-only for pre-declared types.
- Provide a single initialization-time DevMode flag so consumers do not need to thread `ScanOptions` through every call site.
- Document all behaviors and tradeoffs that are currently invisible or surprising to consumers.
- Achieve integration-level test coverage for the 4-arg `RegisterInstance` public overload.

---

## In Scope

### 1. Auto-Scan DevMode Filtering (Behavior Fix)

Auto-scanned public members (those with no `[Command]` attribute) are currently registered regardless of `ScanOptions.DevMode`. This must change:

- Auto-scanned public members are **implicitly dev-only**: they are skipped unless `ScanOptions.DevMode` is `true`.
- A public member annotated with `[Command]` and `IsDevOnly = false` (the default) is the explicit, per-member release-safe consent mechanism and must be registered regardless of DevMode.
- A public member annotated with `[Command(IsDevOnly = true)` is filtered by DevMode as before.
- This rule applies to both method and property auto-scan paths inside `InstanceScanner`.

### 2. `[CommandIgnore]` Attribute

Introduce a `[CommandIgnore]` attribute that opts a specific public method or property out of auto-scanning:

- Placing `[CommandIgnore]` on a member causes `InstanceScanner` to skip it in both auto-scan and attribute-only scan modes.
- The attribute is a no-op on non-public members (they are already excluded from auto-scan).
- Target: `AttributeTargets.Method | AttributeTargets.Property`.
- Lives in the public API surface (`src/`).

### 3. `[CommandHost]` Attribute and Per-Type Startup Pre-Scan

Introduce a `[CommandHost]` class-level attribute and a corresponding pre-scanning path:

- `[CommandHost]` is placed on a class to declare it as a command-hosting type.
- A new `ScanCommandHosts(Type[])` and `ScanCommandHosts(Assembly[])` API on `CommandSystem` pre-scans decorated types at startup, performs all `GetMethods()` / `GetProperties()` reflection, validates members, and caches a per-type profile (validated member list with `MethodInfo` / `PropertyInfo` references and resolved `CommandParameterInfo[]` arrays).
- Subsequent `RegisterInstance()` calls for a pre-scanned type skip the reflection and validation pass; only `Delegate.CreateDelegate` binding (which is per-instance and cannot be cached) occurs.
- If `RegisterInstance()` is called for a type that has no cached profile, the existing runtime scanning path is used unchanged (backward compatible).
- The cached profile is cleared on `Shutdown()`.
- The attribute lives in the public API surface (`src/`).

### 4. Configurable Scan-Depth Boundary (`ScanOptions.ScanUpTo`)

Extend `ScanOptions` with an optional `Type ScanUpTo` field:

- When `ScanUpTo` is `null` (default), behavior is identical to current: only members declared directly on the target type are discovered (`DeclaredOnly`).
- When `ScanUpTo` is set, `InstanceScanner` walks the inheritance chain from the concrete type up to (but not including) the boundary type, accumulating members from each level.
- Members declared on the boundary type itself and any type above it are excluded.
- Common use case: Unity consumer sets `ScanUpTo = typeof(MonoBehaviour)` so intermediate user-defined base classes are included while the MonoBehaviour API surface is excluded.
- This is a `ScanOptions` field; consumers do not need a new overload to use it.

### 5. System-Wide DevMode Flag at `Initialize()`

Add an initialization-time DevMode parameter so consumers can set a single flag rather than threading `ScanOptions` through every call site:

- All `Initialize()` overloads (including the scan-at-init variants) accept an optional `bool devMode = false` parameter.
- The stored DevMode value is applied as the effective default for all subsequent `Scan()`, `RegisterInstance()`, and internal scan-at-init operations that do not receive an explicit `ScanOptions` argument.
- When an explicit `ScanOptions` is provided by the caller, it takes precedence over the system-wide default.
- The flag is cleared on `Shutdown()`.

### 6. Documentation Updates

Update `docs/commands.md` and `docs/unity-integration.md` to cover:

- Auto-scanned members are implicitly dev-only; `[Command]` without `IsDevOnly` is the release-safe explicit opt-in.
- How to use the system-wide DevMode init flag.
- The recommended Unity pattern for enabling DevMode (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`).
- `DynamicInvoke` allocation cost on instance method callbacks: value-type args are boxed on each call; flag as a known allocation hotspot for consumers who trigger commands at high frequency.
- Strong reference: `InstanceRegistry` holds a strong reference to the registered target. Forgetting to call `UnregisterInstance()` in `OnDestroy` will prevent garbage collection; `InstanceNull` errors are a symptom, not a substitute for cleanup.
- Property command naming convention: auto-scanned readable properties register as `instanceKey.get_PropertyName`; writable properties as `instanceKey.set_PropertyName`.

### 7. Integration Test: 4-Arg `RegisterInstance` with `ScanOptions`

Add integration-level tests in `InstanceCommandRegistrationTests` covering the 4-arg `RegisterInstance(target, key, options, mode)` overload:

- Verify that `IsDevOnly` filtering works end-to-end from `CommandSystem` when `ScanOptions.DevMode = false`.
- Verify that dev-only commands become registered when `ScanOptions.DevMode = true`.
- Cover both `InstanceScanMode.Auto` and `InstanceScanMode.AttributeOnly` paths via the public overload.

---

## Out of Scope

- Changes to the static attribute-scanner (`AttributeScanner`) — scope is instance scanning only.
- Changing the `DynamicInvoke` allocation behavior (acknowledged tradeoff; documentation only).
- Changing the strong-reference strategy in `InstanceRegistry` (documentation only; WeakReference support is a separate future concern).
- Changing the property command naming scheme (`get_X` / `set_X`) — documentation only.
- Unity-specific integration code (no `UnityEngine` in `src/`).
- Console UI or input handling.

---

## Requirements

- R1: Auto-scanned public members must be skipped when `ScanOptions.DevMode` is `false` (or not set). This applies to both method and property scan paths.
- R2: A public member annotated with `[Command]` and `IsDevOnly = false` must be registered regardless of DevMode.
- R3: A `[CommandIgnore]` attribute must exist and cause `InstanceScanner` to skip the decorated member in all scan modes.
- R4: A `[CommandHost]` attribute must exist on the public API and allow type-level pre-scanning via `ScanCommandHosts()`.
- R5: After a type is pre-scanned via `ScanCommandHosts()`, `RegisterInstance()` for that type must not invoke `GetMethods()` or `GetProperties()` reflection.
- R6: `ScanOptions.ScanUpTo` must allow inheritance-chain walking from the concrete type up to (not including) the boundary type.
- R7: When `ScanUpTo` is `null`, behavior is unchanged from current.
- R8: `Initialize()` overloads must accept a `devMode` parameter and store it as the system-wide effective DevMode default.
- R9: An explicit `ScanOptions` supplied by the caller takes precedence over the system-wide DevMode default.
- R10: `docs/commands.md` and `docs/unity-integration.md` must document all behaviors listed in the Documentation Updates scope item.
- R11: Integration tests must cover the 4-arg `RegisterInstance` overload end-to-end for DevMode filtering and scan mode.

---

## Acceptance Overview

- `RegisterInstance()` (auto-scan, DevMode off) does not expose any command whose source is an un-attributed public member.
- `RegisterInstance()` (auto-scan, DevMode on) exposes un-attributed public members as commands.
- `[Command]` without `IsDevOnly` always registers, independent of DevMode.
- `[CommandIgnore]` on a public method or property prevents registration in all modes.
- `ScanCommandHosts(types)` called at startup populates a per-type profile; subsequent `RegisterInstance()` for those types skips the reflection pass (verifiable by the absence of `GetMethods`/`GetProperties` calls in tests or by timing data).
- `ScanOptions { ScanUpTo = SomeBaseType }` causes `RegisterInstance()` to include members from intermediate base classes up to (not including) the boundary.
- `CommandSystem.Initialize(devMode: true)` causes all subsequent operations to behave as if `ScanOptions.DevMode = true` was passed.
- All listed documentation topics are present in the docs.
- Integration tests for the 4-arg `RegisterInstance` overload pass and cover the two DevMode states.

---

## Testing Expectations

- Unit tests: Required
- Notes:
  - Auto-scan DevMode filter behavior requires unit tests in `InstanceScannerTests` covering the new auto-scan-skips-when-devmode-off path.
  - `[CommandIgnore]` requires unit tests verifying skip in both scan modes.
  - `ScanUpTo` requires unit tests with a multi-level type hierarchy.
  - `ScanCommandHosts()` pre-scan cache requires unit/integration tests confirming profile reuse and correct commands produced.
  - System-wide DevMode requires tests on `CommandSystem` verifying precedence rules (explicit `ScanOptions` vs. stored flag).
  - Integration tests for 4-arg `RegisterInstance` overload are required (see R11).

---

## Open Questions

- None at this stage. All items were explicitly specified in `.github/recommendations.md`. Any behavioral edge cases not resolved here are deferred to the design step.

---

## PR Scope

- This work is intended to ship in one pull request with multiple commits.
