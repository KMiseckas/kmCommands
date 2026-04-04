# Attribute-Based Registration

## Status

Draft

## Branch

- Name: `feat_attribute-based-registration`
- Rationale: `feat_` — this is a new capability that adds an attribute-driven registration path alongside the existing manual API.

---

## Summary

Allow developers to register commands by decorating static methods with a `[Command]` attribute and scanning a type or assembly at initialization time. This removes the need to write manual `Register()` calls for each command while remaining fully IL2CPP/AOT safe.

---

## Goals

- Let developers declare commands close to their implementation using a C# attribute.
- Provide a scan API that can discover and register all attributed commands in a target type or assembly.
- Keep the feature additive — manual registration must continue to work unchanged.
- Avoid placing debug/dev-only filtering burden on every consumer.
- Maintain IL2CPP/AOT safety: no runtime code generation, no `Emit`, no dynamic proxies.

---

## In Scope

- A `[Command]` attribute applicable to static methods, carrying at minimum a command name.
- An `IsDevOnly` flag on the attribute (`[Command("name", IsDevOnly = true)]`).
- A scan entry point on `CommandSystem` that accepts an explicit `System.Type` to scan (type-scoped discovery).
- Assembly-wide scan support (accept a `System.Reflection.Assembly` argument).
- Auto-mapping of supported method parameter types (`int`, `float`, `bool`, `string`) to `CommandParameterInfo` entries.
- Graceful skip of methods with unsupported parameter types, with a reported failure in the returned result.
- A dev-mode concept in the library — a flag or mode passed at `Initialize()` or scan time — that controls whether `IsDevOnly` commands are registered.
- Integration with the existing `RegistrationResult` type to surface per-command outcomes from a scan.
- Unit tests covering all new behaviors (see Testing Expectations).

---

## Out of Scope

- Any changes to the existing `Register()` / `Execute()` / `Initialize()` / `Shutdown()` signatures or behavior.
- Scanning instance methods or non-static methods.
- Discovering commands at runtime outside initialization (deferred or lazy discovery).
- UI, rendering, input handling, or `UnityEngine` dependencies anywhere in `src/`.
- A Unity-specific adapter or MonoBehaviour wrapper — Unity integration remains the consumer's responsibility.
- Command chaining or metadata/discovery API features (separate vision items).
- Stripping or code-generation based on build configuration inside the library.

---

## Requirements

1. **`[Command]` attribute**
   - Must be applicable to `static` methods only (enforced by convention; runtime behavior when applied to instance methods is explicitly out of scope).
   - Must accept a `string` command name as its first positional argument.
   - Must expose an optional `IsDevOnly` property (default `false`).
   - Must be defined in the `src/` layer with no `UnityEngine` dependency.

2. **Parameter auto-mapping**
   - When scanning a method, each parameter must be mapped to a `CommandParameterInfo` in declaration order.
   - Supported types: `int`, `float`, `bool`, `string` (matching the existing `ArgumentConverter` support set).
   - A method that contains at least one unsupported parameter type must be skipped and reported as a failure; it must not be partially registered.

3. **Type-scoped scan**
   - `CommandSystem` must expose a way to scan a single `System.Type` and register all attributed static methods found on it.
   - The call must return a result that indicates per-command registration outcomes (success or failure with reason).

4. **Assembly-wide scan**
   - `CommandSystem` must expose a way to scan a `System.Reflection.Assembly` and register all attributed static methods found across all types.
   - Per-command outcome reporting applies here as well.

5. **Dev-mode filtering**
   - The library must provide a mechanism for the consumer to declare that the current context is a dev/debug context.
   - When not in dev mode, commands decorated with `IsDevOnly = true` must be silently skipped during scanning (not registered, not reported as failures).
   - When in dev mode, `IsDevOnly = true` commands must be registered and behave identically to non-dev commands.
   - The dev-mode flag must be passed explicitly by the consumer (e.g., at `Initialize()` or at scan time) — it must not be inferred from build symbols inside the library.

6. **IL2CPP / AOT safety**
   - No use of `System.Reflection.Emit`, `DynamicMethod`, expression tree compilation, or any other runtime code generation.
   - Reflection used during scanning is limited to startup/initialization time, not per-frame or per-execute hot paths.
   - Discovered method references must be converted to `CommandCallback` delegates in an AOT-safe way.

7. **Additive, non-breaking**
   - All currently passing tests must continue to pass unchanged.
   - Manually registered commands must behave identically to attribute-registered commands at execution time.
   - No changes to `ExecutionResult`, `RegistrationResult`, `CommandParameterInfo`, or `CommandCallback` types unless strictly required to represent scan results.

---

## Acceptance Overview

- A static method decorated with `[Command("myCmd")]` on a scanned type is callable via `CommandSystem.Execute("myCmd", args)` after a scan.
- A method with an unsupported parameter type is not registered and its failure is captured in the scan result.
- A method decorated with `[Command("debugCmd", IsDevOnly = true)]` is registered only when the consumer opts into dev mode at scan time.
- An assembly-wide scan discovers and registers all attributed static methods across all types in the assembly.
- All pre-existing manual registration tests pass without modification.
- No `UnityEngine` namespace appears anywhere in `src/`.

---

## Testing Expectations

- **Unit tests: Required**
- Tests must be added to `tests/kmCommands.Tests/` and target `net8.0`.
- Coverage expectations:
  - Scanning a type with a single attributed static method registers and executes correctly.
  - Scanning a type with multiple attributed static methods registers all of them.
  - A method with an unsupported parameter type is skipped and the failure is present in the result.
  - A method with no parameters is scanned and registered correctly (command with no args).
  - `IsDevOnly = true` command is excluded when dev mode is off.
  - `IsDevOnly = true` command is included when dev mode is on.
  - A duplicate-name collision during scan is reported as a failure (consistent with existing registry behavior).
  - Assembly-wide scan discovers attributed methods across multiple types.
  - Pre-existing manual registration tests are unchanged and all pass.

---

## Open Questions

1. **Dev-mode API surface**: Should the dev-mode flag be a boolean parameter on the scan method itself, a mode passed to `CommandSystem.Initialize()`, or a separate `ScanOptions` struct? This is left to design.
2. **Scan result type**: Should scan return a single aggregate `RegistrationResult`, a `IReadOnlyList<RegistrationResult>`, or a new dedicated `ScanResult` type? Caller needs per-command feedback. Leave to design.
3. **Attribute placement validation**: If `[Command]` is placed on a non-static method, should scanning silently skip it, log a warning via a registered handler, or throw? Behavior at scan time for invalid placement is unspecified here.
4. **Naming conflicts across type and assembly scans**: If the same command name appears in two types during an assembly scan, which registration wins, or are both reported as failures? Leave to design.

---

## PR Scope

This work is intended to ship in one pull request with multiple commits.
