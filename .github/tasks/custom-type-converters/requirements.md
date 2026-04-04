# Custom Type Converters

## Status

Draft

## Branch

- Name: `feature/custom-type-converters`
- Rationale: `feat_` — new capability that extends the existing argument-conversion system

## Summary

Allow library consumers to register their own string-to-type converters for types beyond the built-in set (`int`, `float`, `bool`, `string`). Custom converters integrate into the existing argument-parsing pipeline, so commands that declare parameters of custom types work transparently through the normal `Execute()` path. Consumers may also override built-in converter behavior when needed.

## Goals

- Let consumers declare and use command parameters of any `System.Type`, not just the four built-in primitives.
- Provide a clear, explicit registration API that fits naturally alongside `Initialize()` / `Shutdown()`.
- Surface conversion failures through the existing `ExecutionResult` / `ExecutionError` channel — no new error plumbing required by the caller.
- Ensure the feature is IL2CPP/AOT-safe and allocation-efficient in the execute path.

## In Scope

- A public API on `CommandSystem` for registering a converter delegate for a given `System.Type`.
- Converters follow the same `bool TryConvert(string input, out object result)` contract used internally.
- Custom converters extend the supported-type set; a converter registered for an already-supported type overrides the built-in behavior for that type.
- Registration of a converter is valid before or after `Initialize()`, but the behavior and lifecycle rules must be clearly defined (see Requirements).
- Validation at registration time: null type and null delegate must be rejected with a clear error result.
- When conversion of a parameter fails (custom or built-in converter returns `false`), `ExecutionResult` carries `ExecutionError.ArgumentConversionFailed` — identical to the existing failure path.
- Commands declared with a parameter type that has no registered converter are rejected at command-registration time (`RegistrationResult`) — existing behavior is preserved and extended to include consumer-registered types as "known" types.
- `CommandMetadataSnapshot` and the discovery API are not affected by this feature (parameter type information is already carried as `System.Type`).

## Out of Scope

- Converters for generics, arrays, or collection types.
- Deferred / lazy converter lookup.
- Per-command converter overrides (converter is registered per `System.Type`, globally).
- Converter priority beyond the override-on-duplicate rule.
- Async converters.
- UI, rendering, or Unity-specific converter helpers.
- Removing a previously registered converter after registration.

## Requirements

1. `CommandSystem` exposes a public method to register a custom type converter. The method accepts a `System.Type` and a converter delegate, and returns a result that indicates success or a specific registration-time error.
2. The converter delegate signature must match the existing internal `TryConvert` contract: given a `string` input it attempts conversion and returns `bool` (`true` = success), writing the converted value to an `out object` parameter.
3. The delegate type used in the public API must be a named, public delegate type so consumers can satisfy it without anonymous-method hacks or casts in AOT contexts.
4. Registering a converter for a type that already has a built-in or previously registered converter replaces the prior converter for that type (last-write wins).
5. Registering a converter with a `null` type or a `null` delegate must return a failure result immediately and must not modify internal state.
6. Converters registered **before** `Initialize()` must survive the `Initialize()` call — i.e. `Initialize()` must not clear custom converters.
7. `Shutdown()` clears all custom converters along with the rest of command-system state. After `Shutdown()`, the system reverts to the built-in-only converter set.
8. Commands whose parameter types have no registered converter (built-in or custom) continue to be rejected at command-registration time with the existing `RegistrationResult` error path.
9. When a custom converter returns `false` during argument conversion, `ExecutionResult.Error` is `ExecutionError.ArgumentConversionFailed` — no new error values are introduced.
10. The converter registration API is safe to call from any thread with respect to read-during-execute scenarios; the design must not create a data-race on the converter store during an active `Execute()` call (exact synchronization mechanism is deferred to design).
11. No allocations are introduced on the execute hot path beyond what is already incurred by the existing conversion lookup.

## Acceptance Overview

- A consumer can register a custom converter for a user-defined `System.Type` and successfully execute a command that accepts a parameter of that type.
- A consumer can override the built-in `int` converter (or any other built-in) by registering a replacement converter for the same type.
- Registering a converter with a `null` type or `null` delegate returns a failure result and does not crash or corrupt state.
- After `Shutdown()`, custom converters are cleared; re-registering after re-`Initialize()` works correctly.
- Commands with unsupported parameter types continue to be rejected at registration, even after custom converters are in use for other types.
- `Execute()` with a failing custom converter returns `ExecutionError.ArgumentConversionFailed` in the result.
- No existing tests regress.

## Testing Expectations

- Unit tests: **Required**
- Notes:
  - Register a custom converter and verify a command using that parameter type executes successfully.
  - Verify that registering a `null` type or `null` delegate returns a failure result.
  - Verify that overriding a built-in converter produces the consumer-defined conversion behavior.
  - Verify that `Shutdown()` clears custom converters.
  - Verify that custom converters registered before `Initialize()` are still active after `Initialize()`.
  - Verify that a command with a parameter type lacking any converter (built-in or custom) is rejected at registration.
  - Verify that a failed custom converter produces `ExecutionError.ArgumentConversionFailed`.
  - Tests must be in the existing `kmCommands.Tests` NUnit project targeting `net8.0`.

## Open Questions

- None — scope is sufficiently defined to proceed to design.

## PR Scope

- This work is intended to ship in one pull request with multiple commits.
