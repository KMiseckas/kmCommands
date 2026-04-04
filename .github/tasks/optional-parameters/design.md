# Design: Optional Parameters / Default Values

**Status:** Draft
**Feature:** Optional Parameters / Default Values
**Slug:** `optional-parameters`
**Requirements:** `.github/tasks/optional-parameters/requirements.md`

---

## Summary

This feature extends `CommandParameterInfo` to carry an optional default value and an `IsOptional`
flag. When a caller omits trailing arguments that correspond to optional parameters, the execution
handler injects the declared defaults directly into the converted-argument array, bypassing string
conversion entirely.

Five files change: `CommandParameterInfo`, `RegistrationResult`, `CommandSystem`, `CommandDefinition`,
and `ExecutionHandler`. No new public types are introduced. `CommandMetadataSnapshot` and
`AttributeScanner` are untouched.

---

## Architecture: What Changes and Why

| File                                | Change                                       | Reason                                    |
| ----------------------------------- | -------------------------------------------- | ----------------------------------------- |
| `src/CommandParameterInfo.cs`       | New constructor + two new properties         | Carries optional default value            |
| `src/Results/RegistrationResult.cs` | New `RegistrationError` enum value           | Represents new validation failure         |
| `src/CommandSystem.cs`              | New validation pass in `Register()`          | Enforces optional-after-required ordering |
| `src/Core/CommandDefinition.cs`     | New cached `RequiredParameterCount` property | Avoids per-execution parameter iteration  |
| `src/Core/ExecutionHandler.cs`      | Argument count check + default injection     | Core runtime behavior                     |

---

## Component-Level Implementation Notes

### 1. `src/CommandParameterInfo.cs`

Add two new read-only properties and one new constructor. The existing constructor is **unchanged**.

```csharp
/// <summary>
/// <c>true</c> if this parameter has a declared default value and may be omitted at call time.
/// </summary>
public bool IsOptional { get; }

/// <summary>
/// The declared default value for this parameter, or <c>null</c> if <see cref="IsOptional"/> is <c>false</c>.
/// The runtime type is guaranteed to be assignable to <see cref="Type"/> (enforced at construction).
/// </summary>
public object DefaultValue { get; }

/// <summary>
/// Initializes a new optional <see cref="CommandParameterInfo"/> with a declared default value.
/// </summary>
/// <param name="name">The parameter name. Must not be null.</param>
/// <param name="type">The parameter type. Must not be null.</param>
/// <param name="defaultValue">
/// The default value to inject when this argument is omitted at call time.
/// Must not be null. Must be assignable to <paramref name="type"/>.
/// </param>
/// <exception cref="ArgumentNullException">
/// Thrown if <paramref name="name"/>, <paramref name="type"/>, or <paramref name="defaultValue"/> is null.
/// </exception>
/// <exception cref="ArgumentException">
/// Thrown if <paramref name="defaultValue"/>'s runtime type is not assignable to <paramref name="type"/>.
/// </exception>
public CommandParameterInfo(string name, Type type, object defaultValue)
{
    Name = name ?? throw new ArgumentNullException(nameof(name));
    Type = type ?? throw new ArgumentNullException(nameof(type));

    if (defaultValue == null)
        throw new ArgumentNullException(nameof(defaultValue));

    if (!type.IsAssignableFrom(defaultValue.GetType()))
        throw new ArgumentException(
            string.Format(
                "Default value of type '{0}' is not assignable to parameter type '{1}'.",
                defaultValue.GetType().Name, type.Name),
            nameof(defaultValue));

    DefaultValue = defaultValue;
    IsOptional = true;
}
```

**Notes:**

- `IsOptional` is `false` and `DefaultValue` is `null` for instances created via the existing
  two-argument constructor (C# auto-initialises `bool` to `false` and reference types to `null`).
- `type.IsAssignableFrom(defaultValue.GetType())` is the correct direction: it answers "can a value
  of `defaultValue`'s type be assigned where `type` is expected?" This handles subclass defaults
  correctly (e.g., a `string` assigned to a `string` parameter, an `int` assigned to an `int`
  parameter). It does not permit boxing mismatches.
- `null` is explicitly rejected as a default for all types, including `string`. This keeps default
  injection unambiguous and prevents NREs in registered callbacks.
- AOT/IL2CPP safe: `object` storage, no generics, no `Expression`, no `Emit`.

---

### 2. `src/Results/RegistrationResult.cs`

Append one new value to `RegistrationError`:

```csharp
/// <summary>
/// An optional parameter (one with a default value) appears before a required parameter
/// in the command's parameter list. All optional parameters must trail all required parameters.
/// </summary>
OptionalParameterBeforeRequired
```

Append after the existing `InvalidMethod` entry to preserve existing enum integer values.

---

### 3. `src/CommandSystem.cs` — `Register()` validation

Registration validation already lives in `CommandSystem.Register()`. A new pass is added **after** the
existing per-parameter `IsTypeSupported` loop and **before** constructing `CommandDefinition`:

```csharp
bool seenOptional = false;
for (int i = 0; i < parameters.Length; i++)
{
    if (parameters[i].IsOptional)
    {
        seenOptional = true;
    }
    else if (seenOptional)
    {
        return RegistrationResult.Fail(
            RegistrationError.OptionalParameterBeforeRequired,
            string.Format(
                "Required parameter '{0}' at index {1} appears after an optional parameter. " +
                "All optional parameters must follow all required parameters.",
                parameters[i].Name, i));
    }
}
```

**Why here, not in `CommandRegistry`?**
`CommandRegistry.TryRegister()` performs only duplicate-name detection. All other semantic
validation (null checks, type support, ordering) is already concentrated in `CommandSystem.Register()`.
Adding the new check here preserves that pattern and avoids mixing responsibilities.

---

### 4. `src/Core/CommandDefinition.cs`

Cache `RequiredParameterCount` at construction time to avoid iterating the parameter array on every
`Execute()` call. The parameter array and its `IsOptional` flags are immutable after registration,
so caching is safe.

```csharp
internal sealed class CommandDefinition
{
    internal string Name { get; }
    internal CommandParameterInfo[] Parameters { get; }
    internal CommandCallback Callback { get; }
    internal int RequiredParameterCount { get; }

    internal CommandDefinition(string name, CommandParameterInfo[] parameters, CommandCallback callback)
    {
        Name = name;
        Parameters = parameters;
        Callback = callback;

        int required = 0;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (!parameters[i].IsOptional)
                required++;
        }
        RequiredParameterCount = required;
    }
}
```

`Parameters.Length` (total count) is already O(1) from the array. `RequiredParameterCount` is now
also O(1) without per-call iteration.

---

### 5. `src/Core/ExecutionHandler.cs`

Two changes: (a) argument count validation broadens to a range, (b) the conversion loop injects
defaults for omitted trailing positions.

#### 5a. Argument count validation

Replace the current single-equality check:

```csharp
// BEFORE
int expectedCount = definition.Parameters.Length;
int actualCount = args != null ? args.Length : 0;

if (actualCount != expectedCount)
{
    return ExecutionResult.Fail(
        ExecutionError.ArgumentCountMismatch,
        string.Format(
            "Command '{0}' expects {1} argument(s) but received {2}.",
            commandName, expectedCount, actualCount),
        null);
}
```

With a range check:

```csharp
// AFTER
int totalCount    = definition.Parameters.Length;
int requiredCount = definition.RequiredParameterCount;
int actualCount   = args != null ? args.Length : 0;

if (actualCount < requiredCount || actualCount > totalCount)
{
    string expectedDesc = requiredCount == totalCount
        ? requiredCount.ToString()
        : string.Format("between {0} and {1}", requiredCount, totalCount);

    return ExecutionResult.Fail(
        ExecutionError.ArgumentCountMismatch,
        string.Format(
            "Command '{0}' expects {1} argument(s) but received {2}.",
            commandName, expectedDesc, actualCount),
        null);
}
```

**Backward compatibility:** When all parameters are required, `requiredCount == totalCount`, so the
message format remains `"expects N argument(s) but received M"` — identical to the existing format.
No existing test assertions on this string are broken.

#### 5b. Default value injection in the conversion loop

Replace the current conversion loop:

```csharp
// BEFORE
object[] convertedArgs = expectedCount > 0
    ? new object[expectedCount]
    : Array.Empty<object>();

for (int i = 0; i < expectedCount; i++)
{
    CommandParameterInfo param = definition.Parameters[i];

    if (!_converter.TryConvert(param.Type, args[i], out object converted))
    {
        return ExecutionResult.Fail(...);
    }

    convertedArgs[i] = converted;
}
```

With:

```csharp
// AFTER
object[] convertedArgs = totalCount > 0
    ? new object[totalCount]
    : Array.Empty<object>();

for (int i = 0; i < totalCount; i++)
{
    CommandParameterInfo param = definition.Parameters[i];

    if (i >= actualCount)
    {
        // Argument omitted — inject declared default directly, no string conversion.
        convertedArgs[i] = param.DefaultValue;
        continue;
    }

    if (!_converter.TryConvert(param.Type, args[i], out object converted))
    {
        return ExecutionResult.Fail(
            ExecutionError.ArgumentConversionFailed,
            string.Format(
                "Failed to convert argument '{0}' at index {1}: cannot convert '{2}' to {3}.",
                param.Name, i, args[i], param.Type.Name),
            null);
    }

    convertedArgs[i] = converted;
}
```

**Notes:**

- The `i >= actualCount` branch is only reachable when `actualCount < totalCount`, i.e. some
  optional arguments were omitted. The range guard above ensures `i < totalCount` and
  `param.IsOptional == true` for those positions (enforced at registration).
- Default values bypass `ArgumentConverter.TryConvert` entirely. They were already type-validated
  at `CommandParameterInfo` construction time — no additional boxing or conversion occurs.
- Allocation: one `object[]` of size `totalCount`. Identical to the current allocation. No new
  allocations added to the hot path.

---

### 6. `src/CommandMetadataSnapshot.cs` — No Changes Required

**Open Question 1 resolved:** `TryGetParameters` returns `CommandParameterInfo[]` — an array of
references to the same `CommandParameterInfo` objects stored in `CommandDefinition.Parameters`.
Since `IsOptional` and `DefaultValue` are properties on `CommandParameterInfo`, consumers who call
`TryGetParameters` will see the new properties automatically.

`CommandRegistry.BuildSnapshot()` already performs a structural copy of the array
(`Array.Copy(def.Parameters, paramsCopy, ...)`) while keeping the same `CommandParameterInfo`
object references. No change to this behavior is needed.

---

## Data Flow: Optional Parameter Lifecycle

```
Registration
─────────────
Consumer calls CommandSystem.Register("move", parameters, callback)
  │
  ├─ Null / empty name guard
  ├─ Null parameters guard
  ├─ Null callback guard
  ├─ Per-parameter: IsTypeSupported check
  ├─ NEW: Optional-before-required ordering check
  │       → returns RegistrationError.OptionalParameterBeforeRequired on violation
  ├─ CommandDefinition constructed
  │   → RequiredParameterCount computed and cached
  └─ CommandRegistry.TryRegister(definition)
      → returns RegistrationError.DuplicateCommandName on conflict

Execution
─────────────
Consumer calls CommandSystem.Execute("move", args)
  │
  ├─ Null/empty command name guard
  ├─ Registry lookup → CommandNotFound
  ├─ Argument count range check
  │   totalCount    = definition.Parameters.Length
  │   requiredCount = definition.RequiredParameterCount  ← cached
  │   if args.Length < requiredCount || args.Length > totalCount
  │       → ExecutionError.ArgumentCountMismatch
  │           message: "expects between R and T arg(s)" if R != T
  │           message: "expects N arg(s)"               if R == T
  ├─ Conversion loop (i = 0 .. totalCount - 1)
  │   if i >= args.Length  → inject param.DefaultValue  ← no conversion
  │   else                 → ArgumentConverter.TryConvert(param.Type, args[i])
  │                           → ExecutionError.ArgumentConversionFailed on failure
  └─ definition.Callback(convertedArgs)
      → ExecutionError.CallbackThrewException on exception
      → ExecutionResult.Ok() on success
```

---

## Resolution of Open Questions

### OQ-1: `CommandMetadataSnapshot` / `TryGetParameters`

**Resolved: No changes needed.**

`CommandMetadataSnapshot.TryGetParameters` returns the same `CommandParameterInfo` object
references that were registered. Because `IsOptional` and `DefaultValue` are instance properties
on `CommandParameterInfo`, any consumer inspecting a snapshot's parameters will automatically see
the new fields. `BuildSnapshot` copies the array but not the objects — this is correct and
sufficient.

### OQ-2: `ArgumentCountMismatch` error message format

**Resolved: Conditional format, backward-compatible.**

- When `requiredCount == totalCount` (all-required command): message is unchanged —
  `"expects N argument(s) but received M"`.
- When `requiredCount < totalCount` (some optional params): message becomes
  `"expects between R and T argument(s) but received M"`.

This avoids breaking existing test assertions on the error message string, while giving callers
actionable feedback when optional parameters are involved.

---

## Design Decisions and Rationale

| Decision                                                            | Rationale                                                                                                                                                       |
| ------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Cache `RequiredParameterCount` in `CommandDefinition`               | Parameter array is fixed-size and immutable after registration. Caching avoids iterating `O(n)` parameters on every `Execute()` call in the hot path.           |
| New validation in `CommandSystem.Register()`, not `CommandRegistry` | All semantic validation already lives in `CommandSystem.Register()`. `CommandRegistry.TryRegister()` is intentionally narrow (duplicate detection only).        |
| `type.IsAssignableFrom(defaultValue.GetType())` direction           | Correct semantic: "can `defaultValue`'s type be assigned to a storage location of `type`?" Handles direct matches and subclasses.                               |
| `null` disallowed as default for all types                          | Avoids ambiguity in the execution path (is `null` a missing argument or a supplied null?). Keeps callback contracts clear.                                      |
| Default values bypass `ArgumentConverter`                           | Defaults are type-validated at construction. Running them through string conversion would require `defaultValue.ToString()` roundtrips — lossy and unnecessary. |
| `object DefaultValue` (not generic wrapper)                         | No generic type parameter on `CommandParameterInfo` needed. AOT/IL2CPP safe. Consistent with existing `CommandCallback` pattern.                                |
| No changes to `AttributeScanner`                                    | Explicitly out of scope per requirements. Attribute-based optional parameter detection is a separate future feature.                                            |
| `OptionalParameterBeforeRequired` appended after `InvalidMethod`    | Preserves existing enum integer assignments for all current values.                                                                                             |

---

## Testing Strategy

### New Test File

`tests/kmCommands.Tests/OptionalParameterTests.cs`

A focused test class covering all 15 acceptance criteria from `requirements.md`, plus error-message
format verification and regression coverage.

### Test Coverage Map

| Acceptance Criterion                                                       | Test Method (suggested)                                   |
| -------------------------------------------------------------------------- | --------------------------------------------------------- |
| AC-1: Required param has `IsOptional=false`, `DefaultValue=null`           | `RequiredParam_HasIsOptionalFalse_AndNullDefaultValue`    |
| AC-2: Optional param has `IsOptional=true`, correct `DefaultValue`         | `OptionalParam_HasIsOptionalTrue_AndExpectedDefaultValue` |
| AC-3: Mismatched default type throws `ArgumentException`                   | `OptionalParam_TypeMismatch_ThrowsArgumentException`      |
| AC-4: Null default throws `ArgumentNullException`                          | `OptionalParam_NullDefault_ThrowsArgumentNullException`   |
| AC-5: All-required registration succeeds (regression)                      | `Register_AllRequired_Succeeds`                           |
| AC-6: Trailing optional params — registration succeeds                     | `Register_TrailingOptional_Succeeds`                      |
| AC-7: Optional before required — returns `OptionalParameterBeforeRequired` | `Register_OptionalBeforeRequired_ReturnsError`            |
| AC-8: All-optional params — registration succeeds                          | `Register_AllOptional_Succeeds`                           |
| AC-9: Execute with all args (required + optional) succeeds                 | `Execute_AllArguments_Succeeds`                           |
| AC-10: Execute with only required args succeeds                            | `Execute_OnlyRequiredArgs_Succeeds`                       |
| AC-11: Execute omitting subset of trailing optional args succeeds          | `Execute_SubsetOfOptionalArgs_Succeeds`                   |
| AC-12: Too few args (below required) fails with `ArgumentCountMismatch`    | `Execute_TooFewArgs_ReturnsArgumentCountMismatch`         |
| AC-13: Too many args (above total) fails with `ArgumentCountMismatch`      | `Execute_TooManyArgs_ReturnsArgumentCountMismatch`        |
| AC-14: Omitted optional default injected without string conversion         | `Execute_OmittedOptional_InjectsDefaultDirectly`          |
| AC-15: Correct mix of caller values and defaults in callback order         | `Execute_MixedArgs_CallbackReceivesCorrectValues`         |
| Error message — range format when optional params present                  | `Execute_TooFewArgs_ErrorMessageShowsRange`               |
| Error message — unchanged format for all-required commands                 | `Execute_TooFewArgs_AllRequired_ErrorMessageUnchanged`    |

### Regression Checks

- All 103 existing tests must continue to pass unchanged.
- Existing `CommandExecutionTests.cs` and `CommandSystemTests.cs` argument-count error message
  assertions will continue to pass because the message format is unchanged for all-required commands
  (`requiredCount == totalCount`).

### Test Implementation Notes

- Use a captured `object[]` in the callback lambda to inspect what values the command received.
- For AC-14 type verification, register a param of type `int` with `DefaultValue = 42` (boxed int),
  then assert the received object is `(int)42`, not a string `"42"`.
- For AC-15, register a 3-parameter command `(required int, optional string "hello", optional bool true)`,
  call with only the `int` argument, and assert the callback receives `[suppliedInt, "hello", true]`.

---

## Review Contract

A reviewer must confirm:

1. **`CommandParameterInfo`**
   - Existing two-argument constructor is byte-for-byte unchanged.
   - `IsOptional` is `false` and `DefaultValue` is `null` when the two-arg constructor is used.
   - New constructor rejects `null` name, `null` type, `null` default, and type-mismatched default.
   - `type.IsAssignableFrom(defaultValue.GetType())` is the assignability check direction (not reversed).

2. **`RegistrationError`**
   - New value `OptionalParameterBeforeRequired` is appended without reordering existing values.

3. **`CommandSystem.Register()`**
   - New ordering validation appears after the existing `IsTypeSupported` loop.
   - Validation is a single forward pass (no nested loops).
   - Error message names the offending parameter and its index.

4. **`CommandDefinition`**
   - `RequiredParameterCount` is computed once in the constructor, not lazily.
   - Computation is a simple loop counting `!IsOptional` entries; no LINQ.

5. **`ExecutionHandler`**
   - Range check uses `< requiredCount || > totalCount` (two comparisons, no extra allocation).
   - Error message format is conditional: range form only when `requiredCount != totalCount`.
   - Default injection: `i >= actualCount` branch writes `param.DefaultValue` without calling
     `_converter.TryConvert`.
   - No new heap allocations introduced beyond the existing `object[]` argument array.

6. **`CommandMetadataSnapshot`**
   - No changes to this file.

7. **`AttributeScanner`**
   - No changes to this file.

8. **Tests**
   - All 15 acceptance criteria have at least one test.
   - All 103 pre-existing tests still pass.
   - No test uses LINQ or reflection beyond what NUnit itself requires.
