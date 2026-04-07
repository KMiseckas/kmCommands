# Rich History Entries

## Status

Draft

## Feature Slug

`rich-history-entries`

## Branch

- Name: `feat_rich-history-entries`
- Base: `origin/main`
- Rationale: `feat_` — new capability extending an existing type with additional data fields

---

## Summary

`CommandHistoryEntry` currently records only the command name, a snapshot of argument tokens, and the callback return value for **successful** executions. This feature extends the struct with four additional pieces of execution context: a UTC timestamp recorded at execution time, the raw input tokens as passed to `Execute()` before any processing, the result status (`ExecutionError` value), and an error detail string for failed executions. The scope decision on whether to record **failed** executions (in addition to successful ones) is the primary open question that must be resolved before design begins.

---

## Goals

- Add a UTC timestamp to each `CommandHistoryEntry` so consumers can correlate commands with wall-clock time.
- Add the raw input representation to each entry so consumers can reconstruct exactly what was submitted.
- Add a result status field (`ExecutionError`) so consumers can distinguish success entries from recorded failure entries without re-executing or parsing error messages.
- Add an error detail string to entries that represent failed executions.
- Keep all new fields fully AOT/IL2CPP-safe and allocation-minimal.
- Maintain backward compatibility of the public history API (`GetHistory()`, `HistoryCount`, `ClearHistory()`).

---

## In Scope

- New properties on `CommandHistoryEntry`: `Timestamp` (`System.DateTime`, UTC), `RawInput` (see Open Questions), `Status` (`ExecutionError`), `ErrorDetail` (string, null on success).
- Extension of `CommandHistoryBuffer.Record()` to accept and store the new fields.
- Adjustment of the `Execute()` call site in `CommandSystem` to pass the new data when recording entries.
- Decision on whether failed executions are recorded (see Open Questions — must be resolved before implementation).
- Unit test coverage for all new fields on both success and failure entries.

---

## Out of Scope

- `IHistoryWriter` adapter interface — this is a separate tracked feature in the vision document.
- Persistence of history entries to disk or any external sink.
- Filtering or querying the history buffer by status, time range, or any other criterion.
- Changes to `GetHistory()`, `HistoryCount`, or `ClearHistory()` method signatures.
- Changes to history buffer capacity or eviction behavior.
- Serialisation of `CommandHistoryEntry` to JSON or any other format.
- Command chaining integration (separate tracked feature).

---

## Functional Requirements

1. `CommandHistoryEntry` must expose a `Timestamp` property of type `System.DateTime` that carries the UTC time at which the entry was recorded. The value must be sourced via `System.DateTime.UtcNow` at the moment of recording.

2. `CommandHistoryEntry` must expose a `RawInput` property that captures the input tokens exactly as received by `Execute(string commandName, string[] args)` before any processing (name lookup, argument conversion, or dispatch). See Open Question OQ-1 for the exact representation to adopt.

3. `CommandHistoryEntry` must expose a `Status` property of type `ExecutionError`. On a successful execution this value must be `ExecutionError.None`. On a failed execution this value must be the specific `ExecutionError` that caused the failure.

4. `CommandHistoryEntry` must expose an `ErrorDetail` property of type `string`. On a successful execution this value must be `null`. On a failed execution this value must be the `ErrorMessage` string from the `ExecutionResult`.

5. `CommandHistoryEntry` must remain a `readonly struct` with an `internal` constructor. No new public constructors may be introduced.

6. All new properties must be get-only (no setters).

7. If failed executions are recorded (see Open Question OQ-2), the history buffer must record an entry for every call to `Execute()` that returns a non-success result, in addition to all successful calls. The recording must happen regardless of the failure reason (e.g. `CommandNotFound`, `ArgumentConversionFailed`, `InstanceNull`, etc.), with the exception of `NotInitialized` (which is a guard condition, not a dispatch failure — see OQ-3).

8. `CommandHistoryBuffer.Record()` must accept the new fields. The internal method signature must be extended consistently with the new `CommandHistoryEntry` constructor parameters.

9. The `Execute()` method in `CommandSystem` must pass all four new values to the buffer recording call at each recording site.

10. `Args` snapshot behaviour is unchanged: the stored arg array is always an isolated copy of the passed array, never a reference to the caller's array.

---

## API Changes

### `CommandHistoryEntry` (public `readonly struct`)

New get-only properties:

| Property | Type | Description |
|---|---|---|
| `Timestamp` | `System.DateTime` | UTC time recorded at the moment the entry was created. |
| `RawInput` | `string` or `string[]` (see OQ-1) | Input tokens as passed to `Execute()`, before any processing. |
| `Status` | `ExecutionError` | `ExecutionError.None` on success; specific error value on failure. |
| `ErrorDetail` | `string` | `null` on success; the `ErrorMessage` from `ExecutionResult` on failure. |

The `internal` constructor gains four additional parameters corresponding to the new properties. Parameter order in the constructor is an implementation detail left to design.

### `CommandHistoryBuffer` (internal)

`Record()` gains four additional parameters matching the new `CommandHistoryEntry` fields.

### `CommandSystem` (public)

`Execute()` call site(s) for `_historyBuffer.Record(...)` updated to pass the new parameters. No change to `Execute()`'s public signature.

No changes to `GetHistory()`, `HistoryCount`, or `ClearHistory()`.

---

## Backward Compatibility

- The `GetHistory()`, `HistoryCount`, and `ClearHistory()` method signatures are unchanged.
- The `CommandHistoryEntry` struct gains new properties; existing code that only reads `CommandName`, `Args`, or `ReturnValue` continues to compile and run without modification.
- The `internal` constructor signature changes — this affects only `CommandHistoryBuffer` (internal) and any test code that constructs entries directly. Test helpers that construct entries must be updated.
- If failed executions become recorded (OQ-2 resolves `yes`), consumers that iterate `GetHistory()` and assume all entries are successful must be updated — this is a **behavioural breaking change** that must be clearly communicated in the release notes.

---

## Acceptance Criteria

1. `CommandHistoryEntry` compiles as a `readonly struct` with the four new get-only properties.
2. After a successful `Execute()` call, the recorded entry's `Timestamp` is a UTC `DateTime` value close to the execution time (within a reasonable tolerance, e.g. 1 second).
3. After a successful `Execute()`, `Status` is `ExecutionError.None` and `ErrorDetail` is `null`.
4. After a successful `Execute()`, `RawInput` captures the input as defined by the resolution of OQ-1.
5. After a failed `Execute()` (if OQ-2 resolves to record failures), the recorded entry's `Status` matches the `ExecutionError` from the returned `ExecutionResult`.
6. After a failed `Execute()`, `ErrorDetail` matches the `ErrorMessage` from the returned `ExecutionResult`.
7. Mutating the caller's arg array after `Execute()` does not affect the stored `RawInput` representation (isolation guarantee must be maintained for any string-array component of `RawInput`).
8. All existing `CommandHistoryTests` tests continue to pass without modification to test assertions (only test setup helper changes for entry construction are acceptable).
9. All new fields are covered by unit tests in `tests/kmCommands.Tests/CommandHistoryTests.cs`.
10. No allocation of new objects occurs on the `Execute()` hot path beyond what is already allocated (args copy, entry struct). `DateTime.UtcNow` is a value-type read — acceptable.

---

## Testing Expectations

- Unit tests: **Required**
- Location: `tests/kmCommands.Tests/CommandHistoryTests.cs`
- Coverage required:
  - `Timestamp` is a valid UTC `DateTime` close to the time of execution (both success and, if OQ-2 = yes, failure).
  - `Status` is `ExecutionError.None` for success entries.
  - `Status` matches the specific `ExecutionError` for failure entries (if OQ-2 = yes).
  - `ErrorDetail` is `null` for success entries.
  - `ErrorDetail` matches `ErrorMessage` for failure entries (if OQ-2 = yes).
  - `RawInput` is correct for both zero-arg and multi-arg calls (per OQ-1 resolution).
  - Arg-mutation isolation is preserved for the `RawInput` field (if it involves a string array).
  - Existing test assertions for `CommandName`, `Args`, and `ReturnValue` remain passing.

---

## Open Questions

### OQ-1 — Raw input representation

`Execute(string commandName, string[] args)` accepts the command name and arguments as separate parameters. The vision says "raw input string as originally passed to `Execute()`". Two candidate representations exist:

- **Option A — `string RawInput`**: A single reconstructed string formed by joining `commandName` and `args` (e.g. `"mycmd arg1 arg2"`). Matches the vision's "string" wording and is more useful for display/logging.
- **Option B — `string[] RawInput`**: Store `commandName` prepended to `args` as a `string[]` snapshot. Avoids string allocation on the hot path; more faithful to the actual call signature.

**Decision needed before design.** Current lean is Option A (string join) to match vision wording, but Option B avoids a per-execution string allocation. Design should evaluate allocation impact and pick one.

### OQ-2 — Record failed executions?

Currently, only **successful** executions are recorded. The vision checklist includes "Result status (success or the specific `ExecutionError` value)" and "Error detail string when execution fails", which strongly implies failed executions must also be recorded.

However, recording failed executions changes observable behaviour and adds an entry for every bad command name, argument mismatch, or conversion error. Three sub-questions:

- **OQ-2a**: Should **all** failure modes be recorded, or only those that reached the dispatch/execution stage (i.e. exclude `NotInitialized`)?
- **OQ-2b**: Should history capacity count both successes and failures, or should the ring buffer distinguish them?
- **OQ-2c**: What is the expected consumer experience when `HistoryCount` grows due to bad-input failures? Is a separate "error history" preferable?

**Recommended resolution**: Record all failures that pass the `IsInitialized` guard (i.e. exclude `NotInitialized`). A single unified buffer is simpler. This is reflected in FR-7 above but must be confirmed.

### OQ-3 — `NotInitialized` guard: record or skip?

If OQ-2 = yes, should a `NotInitialized` failure be recorded? The buffer doesn't exist when not initialized, so recording is not possible in that state anyway. This is a non-issue in practice (the buffer is null), but the requirement should be explicit: `NotInitialized` calls are never recorded.

---

## PR Scope

This work is intended to ship in one pull request with multiple commits.
