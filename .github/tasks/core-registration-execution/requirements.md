# Core Registration and Execution

## Status

Draft

## Summary

Implement the foundational entry point class, command registry, manual registration API, typed argument conversion, and callback execution. This establishes the core runtime path: register a command with a typed callback, invoke it by name with arguments, and have the callback fire with correctly-typed parameters.

## Goals

- Establish the central library entry point with explicit lifecycle (`Initialize` / `Shutdown`).
- Allow manual registration of named commands linked to typed callbacks.
- Convert raw string arguments to the types expected by each command's parameters.
- Execute a registered command by name, invoking its callback with converted arguments.
- Return structured results from execution (success, failure, error details).
- Keep the design open for future features (parsing, chaining, aliases, metadata, attribute scanning) without coupling to them now.

## In Scope

- Library entry point class with `Initialize()` and `Shutdown()` lifecycle.
- Command Registry: store and resolve command definitions by name.
- Manual Registration API: register a command definition (name, parameter signature, callback delegate).
- Typed Argument System: convert string tokens to parameter types declared in the command signature (built-in primitive types at minimum: `int`, `float`, `bool`, `string`).
- Execution: look up a command by name, convert provided arguments, invoke the callback, return a result.
- Structured error/result types for registration and execution outcomes.
- Validation at boundaries (duplicate command names, argument count mismatch, type conversion failure, calling APIs before initialization).
- A `PLANNED.md` file documenting deferred features for future tracking.
- Unit tests covering registration, argument conversion, execution success, and error paths.

## Out of Scope

- Attribute-based / reflection-based command discovery and registration.
- Command aliases.
- Command parsing from raw text input (splitting input string into command name + tokens).
- Command chaining (multiple commands in one input line).
- Metadata / autocomplete API.
- Middleware, extensibility hooks, custom argument parsers.
- Any Unity-specific code, UI, input handling, or MonoBehaviour integration.
- Documentation files in `docs/`.

## Requirements

- A central entry point class (e.g., `CommandSystem`) must gate all operations behind an explicit `Initialize()` call and clean up state on `Shutdown()`.
- Calling registration or execution before initialization must return a clear error, not throw.
- Commands are registered with a unique name, a parameter signature (ordered list of name + type pairs), and a callback delegate.
- Attempting to register a duplicate command name must fail with a structured error.
- Execution accepts a command name and an array of string argument tokens.
- The system must convert each string token to the type declared in the command's parameter signature before invoking the callback.
- Supported built-in types for conversion: `int`, `float`, `bool`, `string`.
- The design must allow adding more type converters in the future without breaking changes.
- Argument count mismatch (too few or too many) must produce a structured error, not an exception.
- Type conversion failure must produce a structured error identifying which argument failed and why.
- Successful execution invokes the callback and returns a success result.
- If a callback itself throws, the system must catch the exception and return it wrapped in a failure result.
- All public API types must have XML documentation comments.
- No LINQ in runtime code paths.
- No allocations in the execute path beyond what is strictly necessary.
- IL2CPP / AOT safe: no `Reflection.Emit`, no `dynamic`, no generic virtual dispatch in hot paths.
- Core code must not reference UnityEngine.

## Acceptance Overview

- A consumer can initialize the system, register a command with typed parameters and a callback, execute it by name with string arguments, and observe the callback invoked with correctly-typed values.
- All foreseeable error paths (uninitialized, duplicate name, bad argument count, bad argument type, callback exception) return structured results rather than throwing.
- The system can be shut down and re-initialized cleanly.
- Unit tests pass for all of the above scenarios.

## Testing Expectations

- Unit tests: **Required**
- Notes: Core logic is pure C# with no Unity dependency. All registration, conversion, execution, and error handling paths are deterministically testable. Tests should cover happy paths and each error condition listed in requirements.

## Open Questions

- Should `Shutdown()` implicitly deregister all commands, or should there be a separate `Clear()` / `DeregisterAll()` method? (Current assumption: `Shutdown()` resets all state.)
- Should individual command deregistration (`Unregister(name)`) be part of this PR or deferred? (Current assumption: deferred, but easy to add.)
- Should the callback delegate signature be a specific delegate type (e.g., `CommandCallback`) or use `Action<object[]>`? (This is a design decision for the implementation phase.)

## PR Scope

- This work is intended to ship in one pull request with multiple commits.
