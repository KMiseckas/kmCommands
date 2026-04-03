# Planned Features

Tracking future features not yet implemented. Each item is a candidate for a separate PR / GitHub issue.

## Command Aliases

- Register one or more aliases for a command name.
- Aliases resolve to the same command definition and callback.

## Attribute-Based Command Registration

- Define a `[Command]` attribute (and related parameter attributes) for methods.
- Reflection scanner discovers attributed methods from specified assemblies/types.
- Scanner produces command descriptors that feed into the manual registration API.
- Results are cached to avoid repeated scanning.

## Command Parsing

- Parse a raw input string into command name + argument tokens.
- Handle quoted strings, escape characters, and whitespace rules.

## Command Chaining

- Support multiple commands in a single input line (e.g., separated by `;` or `&&`).
- Execute chained commands sequentially, with configurable stop-on-error behavior.

## Metadata / Autocomplete API

- Expose registered command names, descriptions, parameter signatures, and usage hints.
- Support querying commands by prefix for autocomplete scenarios.

## Middleware / Extensibility Hooks

- Allow inserting pre-execution and post-execution hooks (e.g., logging, permissions).
- Support custom argument type parsers/converters beyond built-in primitives.

## Optional Parameters and Default Values

- Support parameters with default values that can be omitted at invocation.
- Named parameter syntax (e.g., `--flag value`).

## Command Help System

- Built-in `help` command or API to display command usage and descriptions.
- Per-command and global help output.

## Individual Command Deregistration

- `Unregister(name)` to remove a single command at runtime.
- `DeregisterAll()` / `Clear()` as an explicit bulk operation distinct from `Shutdown()`.
