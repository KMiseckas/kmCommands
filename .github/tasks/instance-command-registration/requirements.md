# Instance Command Registration

## Status

Draft

## Branch

- Name: `feat_instance-command-registration`
- Rationale: new capability — adds instance-bound command registration to the existing static-only command system

## Summary

Extend kmCommands to support registering commands bound to specific object instances. This enables Unity consumers to expose instance methods (e.g. on a `MonoBehaviour`) as commands without any static boilerplate, while keeping the core library completely free of `UnityEngine` dependencies. Commands registered this way follow a `instanceKey.commandName` naming scheme, are discoverable through the existing metadata API, and can be mass-removed when an instance is destroyed.

This feature also introduces **return value support** across the command system: instance method returns and property getter values are surfaced in `ExecutionResult` and recorded in `CommandHistoryEntry`.

---

## Goals

- Allow any object instance to be registered as a command source via a single call.
- Support clean lifecycle teardown: unregistering all instance commands when an object is destroyed.
- Extend return value support to `ExecutionResult` and `CommandHistoryEntry` to accommodate getter commands and non-void instance methods.
- Keep the core library engine-agnostic — no `UnityEngine` types anywhere in `src/`.
- Maintain IL2CPP / AOT safety throughout.

---

## In Scope

### Registration API

- `RegisterInstance(object target, string instanceKey)` — scans the target's type, discovers commands, and registers them under `instanceKey.commandName` names. Returns a result type indicating per-command outcomes (analogous to `ScanResult`).
- `RegisterInstance(object target, string instanceKey, ScanOptions options)` — overload with dev-mode filtering.
- `UnregisterInstance(string instanceKey)` — removes all commands currently registered under the given instance key, in one call. Returns a result indicating how many commands were removed (or whether the key was unknown).
- Consumer is the authority on instance identity; the library stores the key string as-is with no interpretation.

### Command Naming

- All instance commands are named `instanceKey.commandName` — dot separator is fixed and reserved for this scheme.
- `commandName` for attribute-decorated methods comes from `[Command("name")]`.
- `commandName` for auto-scanned public methods is the method name (original casing preserved; see auto-scan rules below).
- `commandName` for auto-scanned properties follows the explicit scheme:
  - Readable property → `get_PropertyName` (0 args; returns a value)
  - Writable property → `set_PropertyName` (1 arg; no return value)
  - Read-write property → both `get_PropertyName` and `set_PropertyName` registered separately

### Attribute-Based Discovery on Instances

- `[Command]` attribute is respected on instance methods at `RegisterInstance()` time, using the same `Name` and `IsDevOnly` rules as the existing static scanner.
- `IsDevOnly` filtering applies uniformly — dev-only instance commands are skipped unless `ScanOptions.DevMode` is `true`.
- Non-void instance methods decorated with `[Command]` are accepted; their return value is captured (see Return Values below).

### Auto-Scan Behaviour

- By default, `RegisterInstance` auto-scans the target type and registers all discovered public instance members.
- Auto-scan is **declared-only**: only members declared on the target's own type are included — inherited members from `object` (e.g. `ToString`, `GetHashCode`, `GetType`, `Equals`) are excluded.
- Scanned members: `public` instance methods and `public` instance properties.
- Private, protected, internal, and static members are ignored by auto-scan.
- Explicit `[Command]` attribution or manual `Register()` is the only way to expose non-public members.
- Consumer can **opt out of auto-scan** per `RegisterInstance` call via a flag or enum, falling back to attribute-only discovery on that type.

### Return Values

- `ExecutionResult` gains a field (or property) to hold an optional boxed return value, populated when a command callback produces a return value.
- `CommandHistoryEntry` gains a field to capture the return value from the execution that produced the entry.  
- For void methods and setter commands, the return value field is `null` / a sentinel "no value" representation.
- The return value is boxed (one allocation per returned value) — acceptable since this is not a hot path.
- Existing `ExecutionResult` consumers that do not read the new field are unaffected in behaviour.

### Null / Destroyed Instance Handling

- If the stored instance reference is `null` or has been GC'd at execution time, the library catches any resulting `NullReferenceException` and returns a structured `ExecutionResult` failure (specific error code: `InstanceNull` or equivalent).
- The consumer remains responsible for calling `UnregisterInstance` when an object is intentionally destroyed.

### Discovery API Compatibility

- Instance commands appear in `GetCommandNames()`, `TryGetCommandParameters()`, and `GetSnapshot()` under their full `instanceKey.commandName` names.
- `UnregisterInstance` removes them from all discovery results.

---

## Out of Scope

- Broadcasting (invoking all instances sharing a type key) — deferred to design time; not in this PR.
- Indexer properties — excluded from auto-scan.
- Generic instance methods — excluded from auto-scan (AOT risks).
- Ref / out / in method parameters — excluded from auto-scan (no conversion support).
- Weak reference storage of the target — the library holds a strong reference; lifetime management is the consumer's responsibility.
- Any `UnityEngine` API, `MonoBehaviour` base class handling, or scene lifecycle logic — strictly consumer-side concerns.
- Aliases for instance commands — covered by the separate Command Aliases feature.
- Thread safety — not added; all calls remain single-threaded (same constraint as the rest of the library).

---

## Requirements

1. `RegisterInstance(object target, string instanceKey)` must be callable after `Initialize()` and fail gracefully (structured result) if called before initialization.
2. `RegisterInstance` must reject a `null` target or a `null`/empty instance key with a descriptive failure result — no exceptions thrown at the call site.
3. Registering two instances with the same `instanceKey` must fail with a duplicate-key error on the second call; the first registration is left intact.
4. `UnregisterInstance(string instanceKey)` must remove every command registered under that key atomically from the consumer's perspective; subsequent `Execute` calls for those names must return `CommandNotFound`.
5. `UnregisterInstance` called with an unknown key must return a graceful "not found" result rather than throwing.
6. Command names produced by `RegisterInstance` must follow the `instanceKey.commandName` dot-separated scheme; the dot character must not be permitted in a plain `instanceKey` string (validate at call site).
7. Auto-scan must skip all members declared on `System.Object` and any other base class above the target's own declared type.
8. Auto-scan must produce `get_`/`set_` prefixed commands for properties; read-only properties produce only `get_`; write-only properties produce only `set_`; read-write produce both.
9. `[Command]` attribute on instance methods must be respected at `RegisterInstance()` time; `Name` and `IsDevOnly` behave identically to the static scan path.
10. The consumer must be able to pass a `ScanOptions` value to `RegisterInstance` to control `IsDevOnly` filtering.
11. A flag or enum parameter on `RegisterInstance` must allow the consumer to suppress auto-scan and rely solely on `[Command]`-decorated methods for that instance.
12. If an instance command's callback throws `NullReferenceException` at execution time, the library must catch it and return `ExecutionResult` with a dedicated error code rather than propagating the exception.
13. `ExecutionResult` must expose the return value of a command callback when one exists; the field must default to `null` (or a "no value" sentinel) for void callbacks and set commands.
14. History entries recorded for instance commands must include the return value alongside the existing name and args snapshot.
15. Instance commands must appear in `GetCommandNames()`, `TryGetCommandParameters()`, and `GetSnapshot()` under their full `instanceKey.commandName` names.
16. After `UnregisterInstance`, the removed commands must no longer appear in any discovery API output.
17. All new code paths must be IL2CPP / AOT safe — no `System.Reflection.Emit`, no `DynamicMethod`, no runtime code generation.
18. Method parameters involving `ref`, `out`, `in`, or generic type parameters must be silently skipped during auto-scan with a descriptive entry in the registration result.

---

## Acceptance Overview

- A consumer can call `RegisterInstance(myPlayerObject, "player")` and immediately execute `player.Heal`, `player.get_Health`, or `player.set_Health` (for a `Health` property) via `Execute(...)`.
- Destroying the player object and calling `UnregisterInstance("player")` removes all three of those commands; subsequent executions return `CommandNotFound`.
- Attempting to execute a command on a GC'd instance (without unregistering) returns a structured failure result with an `InstanceNull` error code.
- A second `RegisterInstance(otherObject, "player")` call returns a duplicate-key failure without affecting the first registration.
- Return value from `player.get_Health` is accessible on the returned `ExecutionResult` and is visible in the history buffer via `GetHistory()`.
- All instance commands appear in `GetSnapshot()` and `GetCommandNames()`.

---

## Testing Expectations

- Unit tests: **Required**
- Notes:
  - All new API surface must have unit test coverage — registration, unregistering, auto-scan, attribute discovery, opt-out, `get_`/`set_` property naming, null-target error, duplicate key error, `NullReferenceException` at execute time, return value in `ExecutionResult`, return value in history entry, discovery API visibility before and after unregister.
  - Tests run on `net8.0` under NUnit, same as the existing test suite; no Unity runtime needed.

---

## Open Questions

- **Return value field type on `ExecutionResult`**: Should it be `object ReturnValue` (simplest, one allocation per non-void call) or a new `CommandReturnValue` discriminated wrapper? Answer at design time — both satisfy the requirements above.
- **`InstanceNull` error code placement**: New enum value on `ExecutionError`, or a sub-code inside the existing callback-exception error? Resolve at design time.
- **`RegisterInstance` result type**: Reuse `ScanResult` (it already carries per-command `ScanEntry` outcomes) or introduce a new `InstanceRegistrationResult`? Resolve at design time based on whether per-instance context needs to be distinct.

---

## PR Scope

This work is intended to ship in one pull request with multiple commits.
