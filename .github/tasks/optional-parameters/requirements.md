# Requirements: Optional Parameters / Default Values

**Status:** Draft
**Feature:** Optional Parameters / Default Values
**Slug:** `optional-parameters`
**Branch:** `feature/optional-parameters`

---

## Summary

Allow commands to declare one or more parameters as optional with a type-safe declared fallback value. When a caller omits trailing arguments that correspond to optional parameters, the library supplies the declared defaults and executes the command successfully.

---

## Scope

### In Scope

- Extend `CommandParameterInfo` to carry an optional default value and an `IsOptional` flag.
- Add a new `CommandParameterInfo` constructor that accepts a default value, marking the parameter optional.
- Add a new `RegistrationError` value (`OptionalParameterBeforeRequired`) for the case where an optional parameter appears before a required one in the parameter list.
- Validate at registration that all optional parameters appear after all required parameters.
- Update execution argument-count validation to allow the caller to supply fewer arguments than the total parameter count, down to the number of required parameters.
- When optional arguments are omitted at execution time, pad the converted argument array with the declared default values (no string conversion performed on defaults).
- The existing `ArgumentCountMismatch` error continues to cover: too few args (below the required minimum) and too many args (above the total parameter count).

### Out of Scope

- Attribute-based optional parameter support (`[Command]` on static methods with C# default parameter values).
- Named / keyword argument syntax at the call site.
- `null` as a valid default value for any parameter type (including `string`).
- Any change to the attribute scanner or `AttributeScanner.cs`.
- Any new public API beyond changes to `CommandParameterInfo` and `RegistrationError`.

---

## Goals

1. A consumer can register a command where one or more trailing parameters carry a declared default value.
2. Calling `Execute` with fewer arguments than the total parameter count succeeds when the missing arguments correspond to optional parameters.
3. The library rejects a registration where an optional parameter precedes a required parameter.
4. Default values are stored as `object` on `CommandParameterInfo` — no runtime type-building, IL2CPP/AOT safe.
5. No additional heap allocation per execution beyond what the existing argument array already requires.

## Non-Goals

- Providing a convenience mechanism for attribute-scanned methods to auto-detect C# `optional` / default parameters.
- Changing the `Execute` public signature.
- Supporting variadic / params-style parameter lists.

---

## Acceptance Criteria

Each criterion must be covered by at least one unit test.

### `CommandParameterInfo`

1. A `CommandParameterInfo` constructed without a default value has `IsOptional == false` and `DefaultValue == null`.
2. A `CommandParameterInfo` constructed with a default value has `IsOptional == true` and `DefaultValue` equal to the supplied value.
3. Constructing a `CommandParameterInfo` with a default value whose runtime type does not match the declared `Type` throws `ArgumentException`.
4. Constructing a `CommandParameterInfo` with a `null` default value throws `ArgumentNullException` (null defaults are not permitted).

### Registration

5. Registering a command whose parameter list contains only required parameters succeeds (no regression).
6. Registering a command whose parameter list ends with one or more optional parameters succeeds.
7. Registering a command where an optional parameter appears before a required parameter fails with `RegistrationResult.Success == false` and `RegistrationError.OptionalParameterBeforeRequired`.
8. Registering a command with all-optional parameters (no required parameters) succeeds.

### Execution — argument count

9. Executing a command and supplying all arguments (required + optional) succeeds.
10. Executing a command and omitting all optional arguments (supplying only the required ones) succeeds.
11. Executing a command and omitting a subset of trailing optional arguments succeeds.
12. Executing a command with fewer arguments than the required-parameter count fails with `ExecutionError.ArgumentCountMismatch`.
13. Executing a command with more arguments than the total parameter count fails with `ExecutionError.ArgumentCountMismatch`.

### Execution — default value injection

14. When an optional argument is omitted, the default value declared on `CommandParameterInfo` is passed to the callback without string conversion.
15. The callback receives the correct mix of caller-supplied (converted) values and default values in parameter order.

---

## Testing Expectations

- Unit tests only; no integration or Unity-environment tests required.
- Tests belong in `tests/kmCommands.Tests/`.
- New tests may live in existing test files where they fit naturally (e.g., `CommandExecutionTests.cs`, `CommandSystemTests.cs`) or in a new focused file (e.g., `OptionalParameterTests.cs`).
- All 103 existing tests must continue to pass.
- The NUnit test project targets `net8.0`.

---

## Assumptions

1. `CommandParameterInfo` remains a `sealed class`; the new constructor is the opt-in path for optional parameters.
2. The default value is stored as a plain `object` field on `CommandParameterInfo` — no generic wrapper — to keep the public API simple and AOT-safe.
3. Type-safety enforcement (default value type must match `CommandParameterInfo.Type`) is a constructor-time guard, not deferred to registration.
4. "Omitting" optional arguments means supplying a shorter `args` array, not passing `null` tokens or empty strings for those positions.
5. `string` is a supported parameter type but `null` is explicitly excluded as a valid default for all types, keeping defaulting unambiguous.
6. The required-parameter count for a command is the number of `CommandParameterInfo` entries with `IsOptional == false`.
7. `CommandDefinition` stores the parameter array as-is; the execution path derives required vs. total counts from `IsOptional` at execution time or caches them at registration — design decision deferred to `design.md`.

---

## Open Questions

1. **`CommandMetadataSnapshot` / `TryGetParameters`** — should the snapshot surface `IsOptional` and `DefaultValue` through the existing `CommandParameterInfo` references, or does the discovery API need any additional changes? Current assumption: snapshot already returns the `CommandParameterInfo` objects by reference, so no extra changes are needed. Confirm during design.

2. **`ArgumentCountMismatch` error message** — the current message format is `"expects {N} argument(s) but received {M}"`. With optional parameters, `N` is now a range. Should the message be updated to reflect the required/total range? Treating this as a design-time decision; not blocking requirements.
