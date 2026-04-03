# Command Authoring Guide

## What Is a Command?

A command is a named operation that can be invoked at runtime with typed arguments. In kmCommands, a command consists of:

| Part | Type | Description |
|---|---|---|
| Name | `string` | Unique identifier. Lookup is case-insensitive. |
| Parameters | `CommandParameterInfo[]` | Ordered list of name + type pairs describing expected arguments. |
| Callback | `CommandCallback` | Delegate invoked when the command executes. Receives pre-converted arguments. |

## Registering a Command

Call `CommandSystem.Register` after calling `Initialize()`.

```csharp
RegistrationResult result = system.Register(
    "command_name",
    new[]
    {
        new CommandParameterInfo("paramName", typeof(ParamType))
    },
    args =>
    {
        // your logic here
    });

if (!result.Success)
{
    // result.Error and result.ErrorMessage describe what went wrong
}
```

## Parameter Types

The following types are supported out of the box:

| .NET Type | Example token | Notes |
|---|---|---|
| `typeof(int)` | `"42"`, `"-10"`, `"0"` | Integer only. `"1.5"` fails. |
| `typeof(float)` | `"1.5"`, `"-3.14"`, `"10"` | Uses `.` as decimal separator regardless of thread culture. |
| `typeof(bool)` | `"true"`, `"True"`, `"false"`, `"FALSE"` | Strict `True`/`False` only. `"1"`, `"yes"` are not accepted. |
| `typeof(string)` | `"anything"` | Always succeeds. Returns the token as-is. |

Attempting to register a command with an unsupported parameter type returns `RegistrationError.UnsupportedParameterType`.

## Zero-Argument Commands

Pass `Array.Empty<CommandParameterInfo>()` for commands that take no arguments.

```csharp
system.Register(
    "reload_config",
    System.Array.Empty<CommandParameterInfo>(),
    args =>
    {
        // no arguments — args is an empty array
        ReloadConfig();
    });
```

Execute with `null` or an empty array — both are treated identically:

```csharp
system.Execute("reload_config", null);
system.Execute("reload_config", System.Array.Empty<string>());
```

## Single-Parameter Command

```csharp
system.Register(
    "set_level",
    new[] { new CommandParameterInfo("level", typeof(int)) },
    args =>
    {
        int level = (int)args[0];
        LoadLevel(level);
    });

// Execute:
system.Execute("set_level", new[] { "3" });
```

## Multi-Parameter Command

```csharp
system.Register(
    "spawn",
    new[]
    {
        new CommandParameterInfo("prefab",  typeof(string)),
        new CommandParameterInfo("x",       typeof(float)),
        new CommandParameterInfo("y",       typeof(float)),
        new CommandParameterInfo("count",   typeof(int))
    },
    args =>
    {
        string prefab = (string)args[0];
        float  x      = (float)args[1];
        float  y      = (float)args[2];
        int    count  = (int)args[3];

        SpawnPrefab(prefab, x, y, count);
    });

// Execute:
system.Execute("spawn", new[] { "enemy", "10.5", "0.0", "3" });
```

## Accessing Arguments in the Callback

Arguments arrive in `args` in the same order as declared in the parameter signature. Cast each element to the declared type — the conversion has already been validated before the callback fires.

```csharp
args =>
{
    string name  = (string)args[0];   // declared as typeof(string)
    int    value = (int)args[1];      // declared as typeof(int)
    float  scale = (float)args[2];    // declared as typeof(float)
    bool   flag  = (bool)args[3];     // declared as typeof(bool)
}
```

## Command Names

- Names are case-insensitive at registration and execution time.
- `"SetHealth"`, `"sethealth"`, and `"SETHEALTH"` all refer to the same command.
- The name is stored with the casing provided at registration.
- Names must be unique. Registering a name that already exists (regardless of case) returns `RegistrationError.DuplicateCommandName`.

## Handling Callback Exceptions

If the callback throws an unhandled exception, the system catches it and returns it in the result. The command system remains operational — other commands can still execute.

```csharp
system.Register(
    "risky_op",
    System.Array.Empty<CommandParameterInfo>(),
    args =>
    {
        throw new InvalidOperationException("something went wrong");
    });

ExecutionResult result = system.Execute("risky_op", null);

if (result.Error == ExecutionError.CallbackThrewException)
{
    // result.Exception holds the original exception
    Debug.LogException(result.Exception);
}
```

## Checking All Error Conditions

### Registration Errors

| `RegistrationError` | Condition |
|---|---|
| `None` | Success |
| `NotInitialized` | `Initialize()` not called |
| `NullOrEmptyName` | Name is null or `""` |
| `NullParameters` | Parameters array is null |
| `NullCallback` | Callback is null |
| `DuplicateCommandName` | Name already registered |
| `UnsupportedParameterType` | Parameter type has no built-in converter |

### Execution Errors

| `ExecutionError` | Condition |
|---|---|
| `None` | Success |
| `NotInitialized` | `Initialize()` not called |
| `NullOrEmptyCommandName` | Command name is null or `""` |
| `CommandNotFound` | No command registered with that name |
| `ArgumentCountMismatch` | Wrong number of string tokens |
| `ArgumentConversionFailed` | A token failed to convert to the declared type |
| `CallbackThrewException` | Callback threw an exception — see `result.Exception` |

## Organizing Command Registration

For larger projects, split registration into logical groups rather than one large method:

```csharp
private void RegisterCommands()
{
    RegisterDebugCommands();
    RegisterGameplayCommands();
    RegisterAudioCommands();
}

private void RegisterDebugCommands()
{
    _commands.Register("quit",        Array.Empty<CommandParameterInfo>(), _ => Application.Quit());
    _commands.Register("log_fps",     Array.Empty<CommandParameterInfo>(), _ => LogFPS());
    _commands.Register("set_timescale",
        new[] { new CommandParameterInfo("scale", typeof(float)) },
        args => Time.timeScale = (float)args[0]);
}
```

## Re-Registering After Shutdown

`Shutdown()` clears all registered commands. After calling `Initialize()` again, re-register all commands before accepting input.

```csharp
system.Shutdown();
system.Initialize();
RegisterAllCommands(); // must be called again
```
