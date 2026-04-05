# Command History

## Status

Draft

## Branch

- Name: `plan/command-history`
- Rationale: Planning artifact only; implementation will land on `feat/command-history`.

## Summary

Add a command history system to the kmCommands core library. When a command is executed via `Execute()`, the command name and argument tokens are recorded into an in-memory history buffer. The history buffer has a configurable maximum capacity and evicts the oldest entry when full. Callers can retrieve history entries, query their count, and clear the buffer. The history is reset when `Shutdown()` is called.

This is a pure core-library feature. No UI, input handling, rendering, or Unity-specific code is in scope.

## Goals

- Record executed commands (name + string argument tokens) as part of the existing `Execute()` flow.
- Support configurable history capacity with defined eviction behavior.
- Expose history retrieval and clear operations on `CommandSystem`.
- Reset history state cleanly on `Shutdown()`.
- Remain IL2CPP/AOT safe, allocation-conscious, and engine-agnostic.

## In Scope

- A `CommandHistoryEntry` value type (or equivalent) holding a command name and a snapshot of its argument tokens.
- An internal history buffer with capped capacity and eviction of the oldest entry when the cap is reached.
- A way to configure the history capacity before or at initialization (exact API shape is deferred to design).
- `CommandSystem` API additions:
  - Retrieve all current history entries as a snapshot.
  - Query the current number of recorded entries.
  - Clear the history buffer.
- Recording behavior on `Execute()` — specifically which executions are recorded (see Open Questions).
- History cleared by `Shutdown()`.
- History buffer starts empty after each `Initialize()` call.
- Unit tests covering all new behaviors.

## Out of Scope

- UI or console rendering of history.
- Keyboard/controller navigation through history (up-arrow recall, etc.).
- Persistence of history across sessions (no file I/O, no serialization).
- History search or filtering.
- History entry metadata beyond command name and argument tokens (e.g., timestamps, execution duration).
- Any `UnityEngine` dependency.
- Modifications to existing `ExecutionResult` shape.

## Requirements

1. **History entry shape.** Each history entry must capture the exact command name (as passed to `Execute()`) and a copy of the argument tokens array. The stored copy must be immutable from the caller's perspective.

2. **History buffer.** The system must maintain an ordered history buffer where the most recently recorded entry is retrievable. Entries must be ordered from oldest to newest.

3. **Configurable capacity.** The maximum number of entries stored in the history buffer must be configurable. Once the buffer is at capacity, recording a new entry must discard the oldest entry to make room (circular/ring-buffer behavior).

4. **Default capacity.** A sensible default capacity must be defined and documented. The exact value is a design decision; it must be a positive integer.

5. **Minimum capacity constraint.** The configured capacity must be at least 1. Attempting to configure a capacity less than 1 must be handled gracefully (exact behavior — error result vs. clamping — deferred to design).

6. **Retrieval API.** `CommandSystem` must expose a method to retrieve a snapshot of current history entries. The snapshot must not reflect subsequent writes to the live buffer. Return type must be an array or equivalent non-allocating-on-read structure consistent with existing API conventions (e.g., `GetCommandNames()` returns `string[]`).

7. **Entry count API.** `CommandSystem` must expose a property or method to query the current number of recorded entries without allocating.

8. **Clear API.** `CommandSystem` must expose a method to clear all entries from the history buffer.

9. **Execute integration.** History recording must occur inside the existing `Execute()` flow. History must not be recorded if the system is not initialized.

10. **Shutdown resets history.** Calling `Shutdown()` must discard all history entries and release the buffer. After a subsequent `Initialize()`, history must start empty.

11. **Pre-init behavior.** Calling history retrieval, count, or clear operations before `Initialize()` (or after `Shutdown()`) must not throw. Retrieval must return an empty result; count must return zero; clear must be a no-op.

12. **IL2CPP / AOT safety.** All new code must be IL2CPP safe. No reflection, `System.Reflection.Emit`, `dynamic`, LINQ, or runtime code generation in the history implementation.

13. **Allocation discipline.** History recording must not allocate on the hot path beyond the entry storage itself. The argument snapshot copy may allocate once per execution.

## Non-Functional Requirements

- **Compatibility.** Must compile against `netstandard2.0` and remain compatible with Unity 2021+.
- **Thread safety.** Consistent with existing `CommandSystem` contract: no thread-safety guarantees are required. All calls are expected on a single thread.
- **No external dependencies.** No new runtime dependencies may be introduced.
- **API stability.** New public API surface must follow existing naming and return-type conventions to minimize future breaking-change risk.
- **Performance.** History buffer must use a fixed-size internal structure (e.g., circular array) to avoid unbounded allocation growth. Capacity must be allocated once at initialization.

## Acceptance Overview

- After `Initialize()`, the history count is 0.
- After calling `Execute()` with a valid command, the history count increases by 1 and the entry reflects the name and args used.
- After filling the buffer to capacity and executing one more command, the oldest entry is gone and the count stays at capacity.
- After `ClearHistory()` (or equivalent), the history count returns to 0 and retrieval returns an empty result.
- After `Shutdown()` followed by `Initialize()`, the history count is 0.
- Calling retrieval/count/clear before `Initialize()` does not throw.

## Testing Expectations

- **Unit tests: Required**
- New behaviors must be covered by NUnit tests in `tests/kmCommands.Tests/`.
- Expected test coverage areas:
  - History is empty after `Initialize()`.
  - A successful execution adds one entry with correct name and args.
  - Entries are ordered oldest-first.
  - Buffer evicts oldest entry when capacity is exceeded.
  - Default capacity is respected.
  - Configuring a custom capacity works correctly.
  - `ClearHistory()` resets count to 0 and retrieval to empty.
  - `Shutdown()` resets history; subsequent `Initialize()` starts empty.
  - Pre-init calls (retrieval, count, clear) do not throw.
  - Argument snapshot is independent of the caller's original array (mutation of caller's args after execute does not affect stored entry).

## Open Questions

1. **Which executions are recorded?** Should history record only executions that result in `ExecutionResult.Success == true`, or should all calls to `Execute()` be recorded (including failures such as `CommandNotFound`, `ArgumentConversionFailed`, etc.)? Recording only successful executions is the working assumption; recording all calls would require a separate design decision.

2. **Capacity configuration point.** How is the history capacity configured? Options include: an overload of `Initialize(int historyCapacity)`, a separate `SetHistoryCapacity(int)` method callable before `Initialize()`, or a configuration struct passed to `Initialize()`. The design step must select one approach that minimizes breaking changes to existing callers.

3. **Sub-capacity behavior on pre-init clear.** If `ClearHistory()` is called before `Initialize()`, should it buffer a "pending clear" intent or simply be a no-op? Working assumption: no-op, since there is nothing to clear.

4. **Entry struct vs. class.** Should `CommandHistoryEntry` be a `struct` (value type) or `class` (reference type)? Given allocation discipline goals, a `struct` is preferred — this is noted here for the design step to confirm.

## PR Scope

- This work is intended to ship in one pull request with multiple commits.
