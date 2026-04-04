# Command Metadata / Discovery API

## Status

Draft

## Branch

- Name: `feature/command-metadata-discovery`
- Rationale: `feat_` — new public capability exposing read-only command discovery to consumers.

## Summary

Add a read-only discovery API to `CommandSystem` that allows Unity UI layers and tooling to inspect the set of registered commands and their parameter signatures at runtime. No new registration or execution behavior is introduced — this feature solely exposes data that already exists inside the command registry.

## Goals

- Allow consumers to retrieve the names of all currently registered commands.
- Allow consumers to retrieve the parameter descriptors (`CommandParameterInfo`) for a specific command by name.
- Provide a read-only snapshot of the full registry state that the Unity UI layer can use for autocompletion, help displays, and tooling without needing to re-query after every keystroke.
- Keep the API surface small, stable, and consistent with existing `CommandSystem` conventions.

## In Scope

- A method or property on `CommandSystem` to retrieve all registered command names.
- A method on `CommandSystem` to retrieve `CommandParameterInfo[]` (or equivalent read-only collection) for a named command.
- A read-only metadata snapshot type that captures command names and their parameter info at a point in time (i.e., not a live view that changes as registration happens mid-frame).
- Case-insensitive name lookup consistent with the existing registry behavior.
- Graceful handling when a requested command name does not exist (structured result or null-safe return, consistent with project conventions).
- Unit tests in `tests/kmCommands.Tests/` covering: querying all names, querying parameters for a known command, querying a non-existent command, and snapshot accuracy.

## Out of Scope

- Help text or description strings — that is a separate planned feature ("Command Description / Help Text").
- Command aliases — a separate planned feature.
- Live/reactive registry observation (events, callbacks on registration change).
- Any Unity UI, rendering, input handling, or autocomplete widget implementation.
- Writing to or mutating the registry through the discovery API.
- Filtering or searching commands by type, argument count, or other criteria (beyond exact name lookup).
- Exposing internal implementation details beyond what `CommandParameterInfo` already describes.

## Requirements

1. `CommandSystem` must expose a way to retrieve all currently registered command names as a read-only collection of strings.
2. `CommandSystem` must expose a way to retrieve the parameter descriptors for a specific command given its name (case-insensitive).
3. The parameter retrieval API must return a defined result when the requested command does not exist — either a structured failure indicator consistent with the existing result pattern, or a documented null/empty return, decided at design time.
4. `CommandSystem` must expose a way to obtain a read-only metadata snapshot of the entire registry — capturing command names and their associated `CommandParameterInfo` at the moment of the call.
5. The snapshot must be a value that can be stored and inspected by the consumer without risk of mutation by subsequent registrations.
6. All new public API methods must be callable only after `Initialize()` has been called, and must behave safely if called after `Shutdown()` (i.e., return empty or a defined failure state, not throw).
7. No new `UnityEngine` dependency may be introduced anywhere in `src/`.
8. All allocations introduced by the discovery API must be bounded by registry size and must not occur in execution hot paths (the discovery API is expected to be called during UI preparation, not per-frame during command execution).
9. All new source files in `src/` must carry the required copyright header.
10. IL2CPP/AOT-safe patterns must be used throughout; no runtime code generation, no unconstrained `dynamic` or reflection-heavy per-call patterns.

## Acceptance Overview

- A Unity client can call a single `CommandSystem` method to retrieve all command names and populate an autocomplete list.
- A Unity client can call a single `CommandSystem` method with a command name to retrieve the expected parameters for that command and display them as hints.
- A Unity client can take a metadata snapshot once at a stable point (e.g. after all `Register()`/`Scan()` calls complete) and reference it later without worrying that subsequent registrations corrupt the copy.
- Calling any discovery method before `Initialize()` or after `Shutdown()` does not throw an exception.
- All new public API passes the existing test suite without modification, and new unit tests cover the above behaviors.

## Testing Expectations

- Unit tests: **Required**
- Notes: All behaviors described in Acceptance Overview are unit-testable without a Unity runtime. Tests should cover: (a) retrieving all names from a registry with multiple registered commands, (b) retrieving parameters for a known command, (c) querying a non-existent command name, (d) snapshot isolation — registering a new command after snapshotting does not alter an already-taken snapshot, (e) calling discovery methods before `Initialize()` or after `Shutdown()` does not throw. Tests go in `tests/kmCommands.Tests/` using NUnit, targeting `net8.0`.

## Open Questions

1. **Snapshot type shape** — Should the snapshot be a new dedicated type (e.g. `CommandRegistrySnapshot`) or a simple `IReadOnlyDictionary<string, CommandParameterInfo[]>`? This is a design-time decision; either approach satisfies the requirement. A named type is friendlier to extend later (e.g. for help text when that feature is added).
2. **Parameter collection type** — Should `CommandParameterInfo[]` be returned as a plain array, `IReadOnlyList<CommandParameterInfo>`, or wrapped in a new struct? Arrays are AOT-safe and allocation-free to pass; a read-only wrapper would prevent misuse by the caller. Design should decide.
3. **Pre-initialize behavior** — Should methods called before `Initialize()` return empty results or a distinct error state? The preference stated in requirements is "no throw," but the exact return shape is a design decision.
4. **Snapshot freshness contract** — Should the snapshot explicitly document that it reflects the registry state at the moment of the call only, or should there be a version/timestamp on it? Probably documentation only, but worth confirming at design time.

## PR Scope

- This work is intended to ship in one pull request with multiple commits.
