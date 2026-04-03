# Architecture

## Overview

kmCommands is a pure C# command-system library targeting `netstandard2.0`. It is designed to be consumed by a Unity integration layer without depending on `UnityEngine` itself.

The library exposes a single public entry point (`CommandSystem`) that the consumer instantiates and controls. All internal components are hidden behind the `kmCommands.Core` namespace.

```
┌─────────────────────────────────────────────────────┐
│                   Consumer Code                     │
│  (Unity layer or any .NET host)                     │
└─────────────┬───────────────────────────────────────┘
              │ public API  (kmCommands namespace)
              ▼
┌─────────────────────────────────────────────────────┐
│                  CommandSystem                      │
│  Initialize() / Shutdown()                          │
│  Register(name, parameters, callback)               │
│  Execute(commandName, args)                         │
└──────┬──────────────┬───────────────┬───────────────┘
       │              │               │  (kmCommands.Core namespace)
       ▼              ▼               ▼
┌────────────┐ ┌──────────────┐ ┌────────────────┐
│  Command   │ │  Argument    │ │  Execution     │
│  Registry  │ │  Converter   │ │  Handler       │
└────────────┘ └──────────────┘ └────────────────┘
```

## Namespaces

| Namespace | Contents | Visibility |
|---|---|---|
| `kmCommands` | `CommandSystem`, `CommandCallback`, `CommandParameterInfo`, `RegistrationResult`, `ExecutionResult`, error enums | Public |
| `kmCommands.Core` | `CommandRegistry`, `ArgumentConverter`, `ExecutionHandler`, `CommandDefinition` | Internal |

## Components

### CommandSystem

The public entry point. The consumer creates an instance, calls `Initialize()`, then uses `Register` and `Execute`. `Shutdown()` clears all state and allows re-initialization.

- **Lifecycle:** Idempotent `Initialize()` and `Shutdown()`. Calling either method multiple times or out of order is safe.
- **Owns:** `CommandRegistry`, `ArgumentConverter`, `ExecutionHandler`. All are nulled on `Shutdown()`.
- **Thread safety:** Not thread-safe. All calls must originate from the same thread (main thread in Unity).

### CommandRegistry

An `internal sealed class` backed by a `Dictionary<string, CommandDefinition>` with `StringComparer.OrdinalIgnoreCase`. Command names are stored with their original casing but matched case-insensitively.

### ArgumentConverter

An `internal sealed class` that converts string tokens to .NET types using a `Dictionary<Type, TryConvertFunc>`. Ships with built-in converters for `int`, `float`, `bool`, and `string`. All numeric parsing uses `CultureInfo.InvariantCulture`.

Supported built-in types:

| Type | Notes |
|---|---|
| `int` | Parsed with `NumberStyles.Integer`, `InvariantCulture` |
| `float` | Parsed with `NumberStyles.Float`, `InvariantCulture` |
| `bool` | Strict: `"True"` / `"False"` only (case-insensitive) |
| `string` | Always succeeds; returns the token as-is |

### ExecutionHandler

An `internal sealed class` that orchestrates the full execution path:

1. Validate command name is not null/empty.
2. Look up the command in the registry.
3. Validate argument count matches the command's parameter count.
4. Convert each string argument to the declared parameter type.
5. Invoke the callback with the converted `object[]`.
6. Catch any exception thrown by the callback and wrap it in the result.

### CommandDefinition

An `internal sealed class` that stores a command's name, parameter signature (`CommandParameterInfo[]`), and callback delegate. Created at registration time and stored in the registry.

## Data Types

### CommandCallback

```csharp
public delegate void CommandCallback(object[] args);
```

Arguments are delivered as a pre-converted `object[]`. Each element is typed per the command's parameter signature — cast to the expected type before use.

### CommandParameterInfo

```csharp
public sealed class CommandParameterInfo
{
    public string Name { get; }
    public Type Type { get; }
}
```

Describes a single command parameter. `Name` is used in error messages. `Type` must be a type supported by the `ArgumentConverter`.

### Result Types

Both result types are `readonly struct` — no heap allocation.

```
RegistrationResult { bool Success, RegistrationError Error, string ErrorMessage }
ExecutionResult    { bool Success, ExecutionError Error, string ErrorMessage, Exception Exception }
```

`Exception` on `ExecutionResult` is only non-null when `Error == CallbackThrewException`.

## Execution Flow

```
CommandSystem.Execute("set_health", ["player1", "100"])
  │
  ├─ [gate] IsInitialized? No → ExecutionResult.Fail(NotInitialized)
  │
  └─ ExecutionHandler.Execute(...)
       ├─ name null/empty? → Fail(NullOrEmptyCommandName)
       ├─ registry lookup failed? → Fail(CommandNotFound)
       ├─ arg count mismatch? → Fail(ArgumentCountMismatch)
       ├─ per argument: TryConvert failed? → Fail(ArgumentConversionFailed)
       ├─ callback throws? → Fail(CallbackThrewException, ex)
       └─ → ExecutionResult.Ok()
```

## Registration Flow

```
CommandSystem.Register("set_health", parameters, callback)
  │
  ├─ [gate] IsInitialized? No → RegistrationResult.Fail(NotInitialized)
  ├─ name null/empty? → Fail(NullOrEmptyName)
  ├─ parameters null? → Fail(NullParameters)
  ├─ callback null? → Fail(NullCallback)
  ├─ any parameter type unsupported? → Fail(UnsupportedParameterType)
  ├─ registry duplicate? → Fail(DuplicateCommandName)
  └─ → RegistrationResult.Ok()
```

## Key Design Decisions

**Instance-based, no static state.** `CommandSystem` is a plain class. The consumer owns the lifecycle. This is domain-reload safe (Unity editor re-enters play mode without stale state).

**`object[]` callback arguments.** The only AOT-safe way to deliver heterogeneous typed arguments through a single delegate without runtime code generation. Boxing is acceptable because commands fire at human-input frequency, not per-frame.

**`readonly struct` results.** `RegistrationResult` and `ExecutionResult` avoid heap allocation. Internal factory methods (`Ok()`, `Fail(...)`) keep construction logic hidden from consumers.

**Case-insensitive command names.** `OrdinalIgnoreCase` matching. Command-console UX expects case-insensitivity. Original casing is preserved for future metadata/display use.

**`InvariantCulture` numeric parsing.** Prevents locale-dependent decimal separator issues across platforms and regions.

**No exceptions on expected error paths.** All foreseeable failures return structured results. Exceptions are only thrown from `CommandParameterInfo` constructor on null arguments (programming-error guards, not runtime conditions).

## IL2CPP / AOT Compatibility

- No `System.Reflection.Emit`, no `dynamic`, no runtime code generation.
- No generic virtual dispatch in hot paths.
- Static converter methods in `ArgumentConverter` — no closures.
- All generic types used are explicitly instantiated.
- No LINQ in `src/`.
