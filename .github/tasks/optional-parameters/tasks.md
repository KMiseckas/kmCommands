# Tasks: Optional Parameters / Default Values

**Branch:** `feature/optional-parameters`
**Requirements:** `.github/tasks/optional-parameters/requirements.md`
**Design:** `.github/tasks/optional-parameters/design.md`

---

## Task List

### T01 — Extend `CommandParameterInfo` with optional parameter support

**Files changed:** `src/CommandParameterInfo.cs`

**What to implement:**

- Add `public bool IsOptional { get; }` read-only property. Auto-initialises to `false` for instances created by the existing constructor.
- Add `public object DefaultValue { get; }` read-only property. Auto-initialises to `null` for instances created by the existing constructor.
- Add new three-argument constructor `CommandParameterInfo(string name, Type type, object defaultValue)` with the following behaviour:
  - Throws `ArgumentNullException` for null `name`, null `type`, or null `defaultValue`.
  - Throws `ArgumentException` (parameter name: `defaultValue`) if `type.IsAssignableFrom(defaultValue.GetType())` is false — i.e. the supplied default value's runtime type is not assignable to the declared parameter type.
  - Sets `DefaultValue = defaultValue` and `IsOptional = true`.
- The existing two-argument constructor must remain byte-for-byte unchanged.

**Acceptance gate:**

- Project builds with no errors.
- `new CommandParameterInfo("x", typeof(int))` yields `IsOptional == false` and `DefaultValue == null`.
- `new CommandParameterInfo("x", typeof(int), 42)` constructs without exception, `IsOptional == true`, `DefaultValue` equals the boxed `42`.
- `new CommandParameterInfo("x", typeof(int), "wrong")` throws `ArgumentException`.
- `new CommandParameterInfo("x", typeof(int), (object)null)` throws `ArgumentNullException`.

---

### T02 — Add `OptionalParameterBeforeRequired` to `RegistrationError`

**Files changed:** `src/Results/RegistrationResult.cs`

**What to implement:**

- Append a new value `OptionalParameterBeforeRequired` to the `RegistrationError` enum **after** the existing `InvalidMethod` entry.
- Do not reorder, rename, or remove any existing enum values.

**Acceptance gate:**

- `RegistrationError.OptionalParameterBeforeRequired` is accessible and compiles without ambiguity.
- All existing `RegistrationError` values retain their prior implicit integer assignments.
- Project builds with no errors.

---

### T03 — Validate optional-before-required ordering in `CommandSystem.Register()`

**Files changed:** `src/CommandSystem.cs`

**What to implement:**

- Add a single forward pass immediately after the existing per-parameter `IsTypeSupported` loop and before `CommandDefinition` construction:

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

- No other changes to `Register()`.

**Acceptance gate:**

- `Register("cmd", [required, optional], cb)` returns a successful `RegistrationResult`.
- `Register("cmd", [optional, required], cb)` returns `RegistrationResult` with `Success == false` and `Error == RegistrationError.OptionalParameterBeforeRequired`.
- `Register("cmd", [optional, optional], cb)` succeeds (all-optional).
- All existing `Register()` tests pass.

---

### T04 — Cache `RequiredParameterCount` in `CommandDefinition`

**Files changed:** `src/Core/CommandDefinition.cs`

**What to implement:**

- Add `internal int RequiredParameterCount { get; }` read-only property.
- Compute and assign it once in the constructor using a plain `for` loop — no LINQ:

```csharp
int required = 0;
for (int i = 0; i < parameters.Length; i++)
{
    if (!parameters[i].IsOptional)
        required++;
}
RequiredParameterCount = required;
```

**Acceptance gate:**

- `CommandDefinition` compiles with no errors.
- A definition constructed with 2 required + 1 optional parameter has `RequiredParameterCount == 2` and `Parameters.Length == 3`.
- A definition constructed with 3 required parameters has `RequiredParameterCount == 3` (equal to `Parameters.Length`).
- A definition with 0 required + 2 optional has `RequiredParameterCount == 0`.

---

### T05 — Update execution argument handling in `ExecutionHandler`

**Files changed:** `src/Core/ExecutionHandler.cs`

**What to implement:**

#### 5a. Argument count range check

Replace the current single-equality guard with a range check against `RequiredParameterCount` and `Parameters.Length`:

```csharp
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

When `requiredCount == totalCount` (all required), the message format is identical to the current format — no existing test assertions break.

#### 5b. Default value injection in the conversion loop

Replace the `expectedCount`-bounded conversion loop with a `totalCount`-bounded loop that injects defaults for omitted trailing arguments:

```csharp
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

**Acceptance gate:**

- All-required commands: execution behaviour and error message format unchanged.
- `Execute("cmd", allArgs)` (all required + all optional supplied): succeeds.
- `Execute("cmd", requiredArgsOnly)`: succeeds; callback receives declared default values for omitted positions.
- `Execute("cmd", subsetArgs)` (required + some optional supplied): succeeds.
- `Execute("cmd", tooFewArgs)` (below `RequiredParameterCount`): returns `ExecutionError.ArgumentCountMismatch`.
- `Execute("cmd", tooManyArgs)` (above `Parameters.Length`): returns `ExecutionError.ArgumentCountMismatch`.
- Default injection does not invoke `ArgumentConverter` for omitted positions (verified by checking received value type in callback, not `"42"` string).
- No `IndexOutOfRangeException` when optional args are omitted.

---

### T06 — Write `OptionalParameterTests.cs`

**Files changed:** `tests/kmCommands.Tests/OptionalParameterTests.cs` _(new file)_

**What to implement:**

Create a new NUnit test class covering all 15 acceptance criteria from `requirements.md` plus the two error-message format variants from the design. Implement each test as a separate `[Test]` method named as listed below. Follow existing test conventions: `[SetUp]` calling `CommandSystem.Initialize()` and `[TearDown]` calling `CommandSystem.Shutdown()`.

| AC                             | Test method name                                          |
| ------------------------------ | --------------------------------------------------------- |
| AC-1                           | `RequiredParam_HasIsOptionalFalse_AndNullDefaultValue`    |
| AC-2                           | `OptionalParam_HasIsOptionalTrue_AndExpectedDefaultValue` |
| AC-3                           | `OptionalParam_TypeMismatch_ThrowsArgumentException`      |
| AC-4                           | `OptionalParam_NullDefault_ThrowsArgumentNullException`   |
| AC-5                           | `Register_AllRequired_Succeeds`                           |
| AC-6                           | `Register_TrailingOptional_Succeeds`                      |
| AC-7                           | `Register_OptionalBeforeRequired_ReturnsError`            |
| AC-8                           | `Register_AllOptional_Succeeds`                           |
| AC-9                           | `Execute_AllArguments_Succeeds`                           |
| AC-10                          | `Execute_OnlyRequiredArgs_Succeeds`                       |
| AC-11                          | `Execute_SubsetOfOptionalArgs_Succeeds`                   |
| AC-12                          | `Execute_TooFewArgs_ReturnsArgumentCountMismatch`         |
| AC-13                          | `Execute_TooManyArgs_ReturnsArgumentCountMismatch`        |
| AC-14                          | `Execute_OmittedOptional_InjectsDefaultDirectly`          |
| AC-15                          | `Execute_MixedArgs_CallbackReceivesCorrectValues`         |
| Error message range format     | `Execute_TooFewArgs_ErrorMessageShowsRange`               |
| Error message unchanged format | `Execute_TooFewArgs_AllRequired_ErrorMessageUnchanged`    |

**Implementation notes:**

- Use a captured `object[]` in callback lambdas to inspect values received by the command.
- For AC-14: register a param of type `typeof(int)` with `DefaultValue = 42`; after execution, assert the received object is `(int)42`, not the string `"42"`.
- For AC-15: register a 3-parameter command `(required int, optional string "hello", optional bool true)`, call with only the `int` argument, and assert the callback receives `[suppliedInt, "hello", true]` in order.
- No LINQ. No reflection beyond what NUnit itself requires.

**Acceptance gate:**

- All 17 new tests pass.
- All 103 pre-existing tests continue to pass.
- File compiles with no errors or warnings.

---

### T07 — Full validation gate

**Files changed:** _(none — verification only)_

**What to run:**

```
dotnet test tests/kmCommands.Tests/kmCommands.Tests.csproj --configuration Debug
```

**Acceptance gate:**

- Total tests passed: **≥ 120** (103 pre-existing + 17 new).
- Zero test failures.
- Zero test errors.
- Build produces zero compile errors and zero new warnings introduced by this feature.

---

## Acceptance Criteria Coverage Map

Every acceptance criterion from `requirements.md` maps to at least one implementation task and at least one test task.

| Criterion                                                            | Covered by    |
| -------------------------------------------------------------------- | ------------- |
| AC-1: Required param — `IsOptional=false`, `DefaultValue=null`       | T01, T06      |
| AC-2: Optional param — `IsOptional=true`, correct `DefaultValue`     | T01, T06      |
| AC-3: Mismatched default type → `ArgumentException`                  | T01, T06      |
| AC-4: Null default → `ArgumentNullException`                         | T01, T06      |
| AC-5: All-required registration succeeds (regression)                | T06           |
| AC-6: Trailing optional params — registration succeeds               | T03, T06      |
| AC-7: Optional before required → `OptionalParameterBeforeRequired`   | T02, T03, T06 |
| AC-8: All-optional registration succeeds                             | T03, T06      |
| AC-9: Execute with all args (required + optional) succeeds           | T05, T06      |
| AC-10: Execute with only required args succeeds                      | T04, T05, T06 |
| AC-11: Execute omitting subset of trailing optional args succeeds    | T04, T05, T06 |
| AC-12: Too few args (below required) → `ArgumentCountMismatch`       | T05, T06      |
| AC-13: Too many args (above total) → `ArgumentCountMismatch`         | T05, T06      |
| AC-14: Omitted optional — default injected without string conversion | T05, T06      |
| AC-15: Correct mix of caller values and defaults in callback order   | T05, T06      |

All 15 acceptance criteria are covered.

---

## Reviewer Handoff

A reviewer must confirm all items below before approving this feature for merge.

### `src/CommandParameterInfo.cs`

- [ ] Existing two-argument constructor is byte-for-byte unchanged.
- [ ] New three-argument constructor rejects null `name`, null `type`, and null `defaultValue` individually with `ArgumentNullException`.
- [ ] Type check direction is `type.IsAssignableFrom(defaultValue.GetType())` — not reversed.
- [ ] `IsOptional` default-initialises to `false` and `DefaultValue` to `null` when the two-arg constructor is used (C# auto-init on read-only properties not set in that constructor).

### `src/Results/RegistrationResult.cs`

- [ ] `OptionalParameterBeforeRequired` is appended after `InvalidMethod` with no existing values reordered or renumbered.

### `src/CommandSystem.cs`

- [ ] Ordering validation is positioned after the `IsTypeSupported` loop and before `CommandDefinition` construction.
- [ ] A single forward pass only — no nested loops.
- [ ] Error message names the offending parameter and its index.

### `src/Core/CommandDefinition.cs`

- [ ] `RequiredParameterCount` is computed eagerly in the constructor (not lazily).
- [ ] Computed using a plain `for` loop counting `!IsOptional` — no LINQ.

### `src/Core/ExecutionHandler.cs`

- [ ] Range check: `actualCount < requiredCount || actualCount > totalCount` (two comparisons, no extra allocation).
- [ ] Error message uses unchanged format `"expects N argument(s)"` when `requiredCount == totalCount`.
- [ ] Error message uses range format `"expects between R and T argument(s)"` when `requiredCount != totalCount`.
- [ ] Default injection: `i >= actualCount` branch writes `param.DefaultValue` without calling `_converter.TryConvert`.
- [ ] `object[]` is allocated with size `totalCount`; no additional heap allocations introduced in the hot path.

### `src/CommandMetadataSnapshot.cs`

- [ ] No changes to this file.

### `src/Core/AttributeScanner.cs`

- [ ] No changes to this file.

### Tests (`tests/kmCommands.Tests/OptionalParameterTests.cs`)

- [ ] All 15 acceptance criteria from `requirements.md` have at least one dedicated test method.
- [ ] Both error-message format variants (range and unchanged) are tested.
- [ ] All 103 pre-existing tests pass unchanged (verified by T07 output).
- [ ] No LINQ in test code; no non-NUnit reflection.
- [ ] `CommandSystem.Initialize()` / `Shutdown()` bracketing is consistent across all test methods.
