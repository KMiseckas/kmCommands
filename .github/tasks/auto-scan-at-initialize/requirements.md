# Auto-Scan at Initialize

## Status

Planned

## Branch

- Name: `feat_auto-scan-at-initialize`
- Rationale: `feat_` — this is a new capability extending the `Initialize()` API to support declaring scan targets at startup.

---

## Summary

Allow consumers to pass scan targets (types, assemblies, or both) directly to `Initialize()` so that attribute-based command registration runs automatically during startup. This removes the need for explicit follow-up `Scan()` calls in consumer bootstrap code and gives the consumer access to the init-time scan results for inspection.

---

## Goals

- Provide `Initialize()` overloads that accept scan targets alongside the existing optional history capacity.
- Run attribute scans as part of initialization — before `Initialize()` returns.
- Expose the scan results from init-time scans so consumers can inspect per-command outcomes.
- Preserve full compatibility with subsequent `Register()` and `Scan()` calls after init.
- Support dev-mode filtering via `ScanOptions` supplied at init time.

---

## In Scope

- New `Initialize()` overload(s) that accept one or more scan targets (types and/or assemblies) and optionally a `ScanOptions` value.
- History capacity remains configurable in the new overloads (consistent with the existing `Initialize(int historyCapacity)` pattern).
- Init-time scan results accessible to the caller immediately after `Initialize()` returns.
- Dev-mode filtering: when `ScanOptions.DevMode` is `false`, commands marked `[Command(IsDevOnly = true)]` are skipped — identical to the behavior of the existing `Scan()` API.
- The new overloads must remain idempotent: calling `Initialize(...)` when already initialized is a no-op (no scan is run, results reflect that).

## Out of Scope

- Changes to the existing `Scan(Type, ScanOptions)` or `Scan(Assembly, ScanOptions)` methods — they must remain unchanged.
- Changes to `Shutdown()` behavior.
- Storing or re-exposing init-time scan results after additional `Scan()` calls (results are per-call, not accumulated).
- Any Unity-layer concerns (no `UnityEngine` dependency introduced).
- Automatic re-scanning on subsequent `Initialize()` calls on an already-initialized system.
- Background or deferred scanning.

---

## Requirements

### Overload Shape

1. At least one new `Initialize()` overload must accept scan targets — covering `Type[]`, `Assembly[]`, or a combined form — alongside an optional history capacity parameter.
2. A `ScanOptions` parameter must be accepted alongside the scan targets to enable dev-mode filtering at init time.
3. The new overloads must follow the same idempotency rule as existing overloads: if the system is already initialized, the call is a no-op.
4. History capacity behavior in the new overloads must match the existing `Initialize(int historyCapacity)` semantics: values less than 1 are clamped to 1; omitting the parameter uses `DefaultHistoryCapacity` (64).

### Scan Execution

5. All declared scan targets are processed in the order supplied before `Initialize()` returns.
6. Scan execution at init time must use the same underlying attribute scanning logic as `Scan(Type, ScanOptions)` and `Scan(Assembly, ScanOptions)`.
7. Commands registered via init-time scanning must behave identically to commands registered via explicit post-init `Scan()` calls.

### Result Exposure

8. The new overloads must return or otherwise expose the aggregated scan results so the consumer can inspect per-command outcomes without making a follow-up call.
9. If no scan targets are supplied (e.g., empty arrays), the returned result must reflect that no commands were scanned (zero entries, no errors).
10. If the system is already initialized, the no-op path must return a result that clearly indicates initialization was skipped (distinct from a successful scan with zero entries).

### Compatibility

11. Subsequent `Register()`, `RegisterConverter()`, and `Scan()` calls after an init-time scan must work without interference.
12. Commands registered at init time are visible to all discovery APIs (`GetCommandNames()`, `TryGetCommandParameters()`, `GetSnapshot()`) immediately after `Initialize()` returns.
13. Init-time scan results must not be retained or re-exposed by the system after `Initialize()` returns — consumers are responsible for storing the result if they need it later.

### Non-Functional

14. No allocation must be introduced into the `Execute()` hot path by this feature.
15. The implementation must remain IL2CPP/AOT safe: no runtime code generation, no reflection beyond what `AttributeScanner` already performs.
16. No `UnityEngine` or other engine namespace may be introduced in `src/`.
17. The new overload signatures must not break any existing callers — existing `Initialize()` and `Initialize(int)` overloads must remain available and unchanged.
18. No LINQ in any new runtime path added by this feature.

---

## Acceptance Overview

- A consumer can call a single `Initialize(...)` overload with one or more `Type` or `Assembly` targets and receive back a result describing which commands were registered and which (if any) failed — without any additional `Scan()` call.
- A consumer enabling dev mode by passing a `ScanOptions` with `DevMode = true` sees dev-only commands registered; with `DevMode = false` those commands are skipped.
- After init-time scanning, subsequent `Register()` and `Scan()` calls succeed and produce correct results.
- All existing `Initialize()` and `Initialize(int)` call sites remain valid and unaffected.
- Calling any new `Initialize(...)` overload on an already-initialized system is a safe no-op.
- Discovery APIs (`GetCommandNames()`, `GetSnapshot()`) reflect init-time scan results immediately after `Initialize()` returns.

---

## Testing Expectations

- Unit tests: **Required**
- Notes:
  - Cover the happy path: init with a type array, init with an assembly array; verify registered commands are discoverable.
  - Cover dev-mode filtering: commands with `IsDevOnly = true` excluded when `DevMode = false`, included when `DevMode = true`.
  - Cover idempotency: calling the new overload on an already-initialized system is a no-op; verify no double-registration.
  - Cover result exposure: returned scan result correctly reports per-command outcomes for successful and failed entries.
  - Cover empty-targets case: passing empty `Type[]` or `Assembly[]` returns a result with zero entries and no error.
  - Cover already-initialized no-op result: result clearly distinguishes "already initialized" from "zero commands scanned."
  - Cover compatibility: post-init `Register()` and `Scan()` work correctly after an init-time scan.
  - Tests live in `tests/kmCommands.Tests/` and target the `net8.0` framework, consistent with the existing test suite.

---

## Open Questions

- **Combined overload vs. separate overloads**: should the API provide `Initialize(Type[], ScanOptions)`, `Initialize(Assembly[], ScanOptions)`, and `Initialize(Type[], Assembly[], ScanOptions)` separately, or a single overload accepting a dedicated scan-targets descriptor struct? The design step should evaluate which shape is cleaner given `netstandard2.0` constraints and AOT safety.
- **Result type for the new overloads**: should the new overloads return a single `ScanResult` (aggregated across all targets), a collection of `ScanResult` (one per target), or a new wrapper type? The design step should decide based on caller ergonomics.
- **Already-initialized return value**: what concrete value indicates "no-op due to already initialized" — a new `ScanResult` factory method (e.g., `ScanResult.AlreadyInitialized()`), a nullable return, or an out-parameter? Design step to decide.

---

## PR Scope

This work is intended to ship in one pull request with multiple commits.
