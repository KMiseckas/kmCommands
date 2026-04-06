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

### Alternative: Attribute-Based Registration at Initialize Time

If you decorate your command methods with `[Command]`, you can scan at initialization and skip the separate registration step entirely:

```csharp
using kmCommands;
using System.Reflection;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    private CommandSystem _commands;

    private void Awake()
    {
        _commands = new CommandSystem();

        bool isDevBuild = Debug.isDebugBuild;
        ScanOptions options = new ScanOptions { DevMode = isDevBuild };

        // Initialize and scan an assembly in one call
        ScanResult result = _commands.Initialize(
            new[] { Assembly.GetExecutingAssembly() },
            options);

        if (result.HasErrors)
        {
            for (int i = 0; i < result.Entries.Length; i++)
            {
                if (!result.Entries[i].Result.Success)
                    Debug.LogWarning(string.Format("[CommandSystem] {0}: {1}",
                        result.Entries[i].CommandName, result.Entries[i].Result.ErrorMessage));
            }
        }
    }
}
```

Subsequent `Register()` and `Scan()` calls work normally after an init-time scan.

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

| Error                      | Cause                                                                          |
| -------------------------- | ------------------------------------------------------------------------------ |
| `NotInitialized`           | `Initialize()` was not called yet                                              |
| `NullOrEmptyName`          | Command name is null or empty string                                           |
| `NullParameters`           | Parameters array is null (use `Array.Empty<CommandParameterInfo>()`)           |
| `NullCallback`             | Callback delegate is null                                                      |
| `DuplicateCommandName`     | A command with this name is already registered                                 |
| `UnsupportedParameterType` | A parameter uses a type not supported by the converter (see `architecture.md`) |

## Handling Execution Errors

Common execution errors:

| Error                      | Cause                                                         |
| -------------------------- | ------------------------------------------------------------- |
| `NotInitialized`           | `Initialize()` was not called                                 |
| `CommandNotFound`          | No command registered with that name                          |
| `ArgumentCountMismatch`    | Wrong number of string tokens provided                        |
| `ArgumentConversionFailed` | A token could not be converted to the declared parameter type |
| `CallbackThrewException`   | The callback threw — check `result.Exception`                 |

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

---

## Instance Commands (MonoBehaviour Lifecycle)

Use `RegisterInstance` to bind a MonoBehaviour's public methods and properties as commands at runtime. Call `UnregisterInstance` in `OnDestroy` so commands are removed when the object is destroyed.

```csharp
using kmCommands;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // _commands is typically a shared singleton or injected reference
    private CommandSystem _commands;

    public int Health { get; private set; } = 100;

    private void Start()
    {
        // Auto-discovers: Heal(), get_Health, and any [Command]-decorated members
        ScanResult result = _commands.RegisterInstance(this, "player");

        if (result.HasErrors)
        {
            for (int i = 0; i < result.Entries.Length; i++)
            {
                if (!result.Entries[i].Result.Success)
                    Debug.LogWarning(string.Format("[kmCommands] {0}: {1}",
                        result.Entries[i].CommandName, result.Entries[i].Result.ErrorMessage));
            }
        }
    }

    private void OnDestroy()
    {
        _commands.UnregisterInstance("player");
    }

    public void Heal(int amount)
    {
        Health = Mathf.Clamp(Health + amount, 0, 100);
    }
}
```

After `Start()`, the following commands are available:

- `player.Heal` — executes `Heal(int amount)`
- `player.get_Health` — returns the current `Health` value

After `OnDestroy()`, all `player.*` commands are removed from the registry.

### Handling `ExecutionError.InstanceNull`

If a command is executed after the bound instance is destroyed (or before `UnregisterInstance` is called in a cleanup edge case), the system returns `ExecutionError.InstanceNull`:

```csharp
ExecutionResult result = _commands.Execute("player.Heal", new[] { "10" });
if (result.Error == ExecutionError.InstanceNull)
{
    Debug.LogWarning("[kmCommands] Player instance was destroyed. Cleaning up...");
    _commands.UnregisterInstance("player");
}
```

### Attribute-Only Mode

Use `InstanceScanMode.AttributeOnly` when you do not want all public methods exposed — only those explicitly decorated with `[Command]`:

```csharp
_commands.RegisterInstance(this, "player", default, InstanceScanMode.AttributeOnly);
```

With `AttributeOnly`, only methods and properties decorated with `[Command]` are registered. Public methods without the attribute are ignored.

---

## DevMode Configuration

### System-Wide DevMode Flag

All `Initialize()` overloads accept an optional `devMode` parameter. When `true`, the system-wide DevMode flag is set and applies to all subsequent `Scan()` and `RegisterInstance()` operations — you do not need to pass `ScanOptions { DevMode = true }` at every call site.

```csharp
// Enable DevMode for the whole session
_commands.Initialize(devMode: true);

// RegisterInstance and Scan now behave as if DevMode = true
_commands.RegisterInstance(playerController, "player");
_commands.Scan(typeof(DebugCommands));
```

When `devMode: false` (the default), auto-scanned public members are excluded from all registrations and only explicitly `[Command]`-decorated members with `IsDevOnly = false` are registered.

### Recommended Unity Pattern

Use Unity preprocessor directives to enable DevMode only in editor and development builds:

```csharp
void Awake()
{
    bool isDev = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    isDev = true;
#endif
    _commands.Initialize(devMode: isDev);
    RegisterCommands();
}
```

This ensures:

- **Release builds:** Only explicitly `[Command]`-decorated methods (without `IsDevOnly`) are registered. Auto-scanned and dev-only commands are excluded.
- **Development builds and editor:** All public members and `IsDevOnly = true` commands are also registered for testing and iteration.

### Per-Call Override

Even when the system DevMode is `false`, you can pass `ScanOptions { DevMode = true }` to a specific call to override:

```csharp
// Force DevMode on for this specific registration
_commands.RegisterInstance(debugHelper, "debug", new ScanOptions { DevMode = true });
```

DevMode uses an OR rule: a call-site `DevMode = true` wins over a system-wide `false`, and a system-wide `true` wins over a call-site `false`.

---

## Initialising from a Config File

As an alternative to code-only `Initialize()` calls, you can load settings from a JSON file using `CommandConfig.FromFile`. This is useful when settings such as history buffer size or dev mode should be controlled at deployment time without recompiling.

> **Security note:** Config files must never contain secrets or credentials.

### JSON Config Format

```json
{
    "historyCapacity": 128,
    "devMode": true
}
```

Key names are case-insensitive. Any key can be omitted — omitted keys fall back to the same defaults as `new CommandConfig()`. Unknown keys produce a per-key warning string in `ConfigResult.Warnings` but do not prevent the config from being applied.

### Usage in `Awake()`

```csharp
using kmCommands;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    private CommandSystem _commands;

    private void Awake()
    {
        _commands = new CommandSystem();

        ConfigResult cfg = CommandConfig.FromFile(
            Application.streamingAssetsPath + "/commands.json");

        if (cfg.Success)
        {
            // Log any unknown-key warnings
            for (int i = 0; i < cfg.Warnings.Length; i++)
                Debug.LogWarning("[kmCommands] Config warning: " + cfg.Warnings[i]);

            _commands.Initialize(cfg.Config);
        }
        else
        {
            Debug.LogError(string.Format(
                "[kmCommands] Config load failed ({0}): {1}", cfg.Error, cfg.ErrorMessage));
            _commands.Initialize(); // fall back to defaults
        }
    }

    private void OnDestroy()
    {
        _commands.Shutdown();
    }
}
```

### `ConfigError` Values

| Value           | Cause                                                                    |
| --------------- | ------------------------------------------------------------------------ |
| `InvalidJson`   | JSON was null, empty, or structurally malformed.                         |
| `TypeMismatch`  | A known key had the wrong JSON type (e.g. `"devMode": 42`).              |
| `FileReadError` | File path was invalid, not found, or an I/O error occurred.              |

### After `Shutdown()`

Config state is consumed only during `Initialize`. After `Shutdown()`, call `CommandConfig.FromFile` again (or use the same `CommandConfig` object) and pass it to `Initialize` to re-initialise with the same settings.
