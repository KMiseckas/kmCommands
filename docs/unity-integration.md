# Unity Integration Quickstart

## Overview

kmCommands has no dependency on `UnityEngine`. The Unity layer acts as a thin adapter: it initializes the system, registers commands linked to game logic, and calls `Execute` when the player submits input.

## Step 1 — Add the DLL

Copy `kmCommands.dll` from `bin/Debug/netstandard2.0/` (or a release build) into your Unity project's `Assets/Plugins/` folder. Unity will automatically reference it.

## Step 2 — Create a Command Manager

Create a MonoBehaviour (or plain C# class) that owns the `CommandSystem` instance.

```csharp
using kmCommands;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    private CommandSystem _commands;

    private void Awake()
    {
        _commands = new CommandSystem();
        _commands.Initialize();
        RegisterCommands();
    }

    private void OnDestroy()
    {
        _commands.Shutdown();
        _commands = null;
    }

    private void RegisterCommands()
    {
        // Register commands here (see commands.md for the authoring guide)
    }

    public void Submit(string commandName, string[] args)
    {
        ExecutionResult result = _commands.Execute(commandName, args);

        if (!result.Success)
        {
            Debug.LogWarning(string.Format("[CommandSystem] {0}", result.ErrorMessage));
        }
    }
}
```

## Step 3 — Register Commands

Register commands in `RegisterCommands()`. Each command needs a unique name, a parameter signature, and a callback.

```csharp
private void RegisterCommands()
{
    // Zero-argument command
    _commands.Register(
        "quit",
        System.Array.Empty<CommandParameterInfo>(),
        args => Application.Quit());

    // Command with a string and an int parameter
    _commands.Register(
        "set_health",
        new[]
        {
            new CommandParameterInfo("target", typeof(string)),
            new CommandParameterInfo("value",  typeof(int))
        },
        args =>
        {
            string target = (string)args[0];
            int    value  = (int)args[1];
            // apply to your game logic
        });

    // Command with a float parameter
    _commands.Register(
        "set_timescale",
        new[] { new CommandParameterInfo("scale", typeof(float)) },
        args =>
        {
            Time.timeScale = (float)args[0];
        });

    // Command with a bool parameter
    _commands.Register(
        "set_godmode",
        new[] { new CommandParameterInfo("enabled", typeof(bool)) },
        args =>
        {
            bool enabled = (bool)args[0];
            // apply to player
        });
}
```

## Step 4 — Execute Commands

Call `Execute` with the command name and an array of string tokens. The library converts the tokens to the declared types before invoking the callback.

```csharp
// From a UI input field or console script:
public void OnCommandSubmitted(string commandName, string[] args)
{
    ExecutionResult result = _commands.Execute(commandName, args);

    if (result.Success)
    {
        Debug.Log(string.Format("[CommandSystem] '{0}' executed successfully.", commandName));
    }
    else
    {
        Debug.LogWarning(string.Format("[CommandSystem] Error ({0}): {1}",
            result.Error, result.ErrorMessage));
    }
}
```

## Handling Registration Errors

`Register` returns a `RegistrationResult`. Check it during development to catch misconfigured commands early.

```csharp
RegistrationResult reg = _commands.Register("foo", parameters, callback);

if (!reg.Success)
{
    Debug.LogError(string.Format("[CommandSystem] Registration failed ({0}): {1}",
        reg.Error, reg.ErrorMessage));
}
```

Common registration errors:

| Error | Cause |
|---|---|
| `NotInitialized` | `Initialize()` was not called yet |
| `NullOrEmptyName` | Command name is null or empty string |
| `NullParameters` | Parameters array is null (use `Array.Empty<CommandParameterInfo>()`) |
| `NullCallback` | Callback delegate is null |
| `DuplicateCommandName` | A command with this name is already registered |
| `UnsupportedParameterType` | A parameter uses a type not supported by the converter (see `architecture.md`) |

## Handling Execution Errors

Common execution errors:

| Error | Cause |
|---|---|
| `NotInitialized` | `Initialize()` was not called |
| `CommandNotFound` | No command registered with that name |
| `ArgumentCountMismatch` | Wrong number of string tokens provided |
| `ArgumentConversionFailed` | A token could not be converted to the declared parameter type |
| `CallbackThrewException` | The callback threw — check `result.Exception` |

## Lifecycle Notes

- **Idempotent init/shutdown:** Calling `Initialize()` when already initialized is a no-op. Same for `Shutdown()`.
- **Domain reload safe:** `Shutdown()` clears all registered commands. After a Unity domain reload, call `Initialize()` again and re-register commands.
- **Thread safety:** All calls must be on the main thread.

## Pattern — Splitting Raw Input

kmCommands receives a command name and a pre-split `string[]`. The splitting step belongs to your Unity layer. A simple helper:

```csharp
public void SubmitRaw(string rawInput)
{
    if (string.IsNullOrWhiteSpace(rawInput))
        return;

    string[] parts = rawInput.Trim().Split(' ');
    string commandName = parts[0];

    string[] args = parts.Length > 1
        ? System.Array.Extract(parts, 1, parts.Length - 1)  // or manual copy
        : System.Array.Empty<string>();

    _commands.Execute(commandName, args);
}
```

Or more explicitly:

```csharp
public void SubmitRaw(string rawInput)
{
    if (string.IsNullOrWhiteSpace(rawInput))
        return;

    string trimmed = rawInput.Trim();
    int spaceIndex = trimmed.IndexOf(' ');

    string commandName;
    string[] args;

    if (spaceIndex < 0)
    {
        commandName = trimmed;
        args = System.Array.Empty<string>();
    }
    else
    {
        commandName = trimmed.Substring(0, spaceIndex);
        string rest = trimmed.Substring(spaceIndex + 1);
        args = rest.Split(' ');
    }

    _commands.Execute(commandName, args);
}
```
