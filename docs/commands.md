# Command Authoring Guide

## What Is a Command?

A command is a named operation that can be invoked at runtime with typed arguments. In kmCommands, a command consists of:

| Part       | Type                     | Description                                                                   |
| ---------- | ------------------------ | ----------------------------------------------------------------------------- |
| Name       | `string`                 | Unique identifier. Lookup is case-insensitive.                                |
| Parameters | `CommandParameterInfo[]` | Ordered list of name + type pairs describing expected arguments.              |
| Callback   | `CommandCallback`        | Delegate invoked when the command executes. Receives pre-converted arguments. |

## Registering a Command

Call `CommandSystem.Register` after calling `Initialize()`. An optional `description` string can be attached to the command for use by help UIs or autocomplete.

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
    },
    "Optional human-readable description of what this command does.");

if (!result.Success)
{
    // result.Error and result.ErrorMessage describe what went wrong
}
```

Omitting the description argument is valid — the existing 3-argument overload registers the command with a `null` description:

```csharp
system.Register("reload_config", Array.Empty<CommandParameterInfo>(), args => { });
```

## Parameter Types

The following types are supported out of the box:

| .NET Type        | Example token                            | Notes                                                        |
| ---------------- | ---------------------------------------- | ------------------------------------------------------------ |
| `typeof(int)`    | `"42"`, `"-10"`, `"0"`                   | Integer only. `"1.5"` fails.                                 |
| `typeof(float)`  | `"1.5"`, `"-3.14"`, `"10"`               | Uses `.` as decimal separator regardless of thread culture.  |
| `typeof(bool)`   | `"true"`, `"True"`, `"false"`, `"FALSE"` | Strict `True`/`False` only. `"1"`, `"yes"` are not accepted. |
| `typeof(string)` | `"anything"`                             | Always succeeds. Returns the token as-is.                    |

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

| `RegistrationError`        | Condition                                                                        |
| -------------------------- | -------------------------------------------------------------------------------- |
| `None`                     | Success                                                                          |
| `NotInitialized`           | `Initialize()` not called                                                        |
| `NullOrEmptyName`          | Name is null or `""`                                                             |
| `NullParameters`           | Parameters array is null; also returned by `RegisterConverter` when type is null |
| `NullCallback`             | Callback is null                                                                 |
| `NullConverter`            | Converter delegate passed to `RegisterConverter` is null                         |
| `DuplicateCommandName`     | Name already registered                                                          |
| `UnsupportedParameterType` | Parameter type has no registered converter (built-in or custom)                  |
| `InvalidMethod`            | Method decorated with `[Command]` is not static (scan only)                      |

### Execution Errors

| `ExecutionError`           | Condition                                            |
| -------------------------- | ---------------------------------------------------- |
| `None`                     | Success                                              |
| `NotInitialized`           | `Initialize()` not called                            |
| `NullOrEmptyCommandName`   | Command name is null or `""`                         |
| `CommandNotFound`          | No command registered with that name                 |
| `ArgumentCountMismatch`    | Wrong number of string tokens                        |
| `ArgumentConversionFailed` | A token failed to convert to the declared type       |
| `CallbackThrewException`   | Callback threw an exception — see `result.Exception` |

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

---

## Attribute-Based Registration

In addition to manual `Register()` calls, commands can be declared close to their implementation using the `[Command]` attribute. The `CommandSystem.Scan()` method then discovers and registers all attributed static methods on a type or across an entire assembly.

### The `[Command]` Attribute

Apply `[Command]` to any `static` method. Provide a command name as the first argument.

```csharp
using kmCommands;

public static class PlayerCommands
{
    [Command("heal")]
    public static void Heal(int amount)
    {
        Player.Health += amount;
    }

    [Command("teleport")]
    public static void Teleport(float x, float y, float z)
    {
        Player.Position = new Vector3(x, y, z);
    }

    [Command("reload_scene")]
    public static void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
```

**Rules:**

- The attribute must be applied to a `static` method. Non-static methods produce a `RegistrationError.InvalidMethod` failure in the scan result.
- Supported parameter types are the same as for manual registration: `int`, `float`, `bool`, `string`. Methods with unsupported parameter types are skipped with a `RegistrationError.UnsupportedParameterType` failure.
- Parameters are auto-mapped from the method signature in declaration order — no manual `CommandParameterInfo` construction needed.
- Commands with up to 4 parameters are supported.

### Dev-Only Commands with `IsDevOnly`

Mark internal/debug commands with `IsDevOnly = true`. They are only registered when the scan runs in dev mode.

```csharp
[Command("dump_state", IsDevOnly = true)]
public static void DumpState()
{
    // Only available in debug/development builds
}
```

### Command Descriptions with `Description`

Attach a human-readable description that consumers (e.g., help UIs) can display:

```csharp
[Command("heal", Description = "Restores the player's health by the specified amount.")]
public static void Heal(int amount)
{
    Player.Health += amount;
}
```

Omitting `Description` is valid; the command's description will be `null`.

### `ScanOptions` and Dev Mode

Pass a `ScanOptions` struct to control dev-mode filtering. `DevMode` defaults to `false`.

```csharp
bool isDevBuild = /* your build-config logic */;
ScanOptions options = new ScanOptions { DevMode = isDevBuild };
```

When `DevMode = false` (default), `IsDevOnly = true` commands are silently skipped — they produce no entry in `ScanResult.Entries`. When `DevMode = true`, they are registered and behave identically to regular commands.

### Type-Scoped Scan

Scan a single class to register all its attributed static methods:

```csharp
system.Initialize();

ScanOptions options = new ScanOptions { DevMode = isDevBuild };
ScanResult result = system.Scan(typeof(PlayerCommands), options);

if (result.HasErrors)
{
    foreach (ScanEntry entry in result.Entries)
    {
        if (!entry.Result.Success)
            Console.WriteLine($"[kmCommands] {entry.CommandName}: {entry.Result.ErrorMessage}");
    }
}
```

### Assembly-Wide Scan

Scan all types in an assembly at once. Useful for registering all commands in a project without enumerating each class manually.

```csharp
ScanResult result = system.Scan(System.Reflection.Assembly.GetExecutingAssembly(), options);
```

First-registered-wins: if two types define a command with the same name, the first encountered is registered and the second produces a `DuplicateCommandName` failure entry.

### `ScanResult` and `ScanEntry`

`ScanResult` holds the per-command outcomes of a scan:

| Member      | Type          | Description                                        |
| ----------- | ------------- | -------------------------------------------------- |
| `Entries`   | `ScanEntry[]` | One entry per discovered command method.           |
| `HasErrors` | `bool`        | `true` if any entry has `Result.Success == false`. |

`ScanEntry` describes a single outcome:

| Member        | Type                 | Description                                                                       |
| ------------- | -------------------- | --------------------------------------------------------------------------------- |
| `CommandName` | `string`             | The command name from the attribute, or `string.Empty` for system-level failures. |
| `Result`      | `RegistrationResult` | The registration outcome (`Success`, `Error`, `ErrorMessage`).                    |

System-level failures (e.g., scan called before `Initialize()`, or with a null argument) are returned as a `ScanResult` with a single `ScanEntry` whose `CommandName` is `string.Empty`.

```csharp
ScanResult result = system.Scan(typeof(MyCommands));

for (int i = 0; i < result.Entries.Length; i++)
{
    ScanEntry entry = result.Entries[i];
    if (!entry.Result.Success)
    {
        // entry.CommandName — which command failed
        // entry.Result.Error — the RegistrationError enum value
        // entry.Result.ErrorMessage — a human-readable description
    }
}
```

### Attribute-Based vs. Manual Registration

Both approaches register commands into the same `CommandRegistry` and produce identical runtime behavior. The choice is a code-organization preference.

| Aspect                       | Attribute-based (`[Command]` + Scan)       | Manual (`Register()`)              |
| ---------------------------- | ------------------------------------------ | ---------------------------------- |
| Command declaration location | Next to the implementation                 | Centralized registration site      |
| Parameter setup              | Inferred from method signature             | Explicit `CommandParameterInfo[]`  |
| Dev-only filtering           | `IsDevOnly = true` on attribute            | Implement in consumer code         |
| Error feedback               | `ScanResult.Entries`                       | `RegistrationResult` per call      |
| Suitable for                 | Large sets of commands spread across types | Small sets or dynamic registration |

Both can coexist. Attribute-scanned commands and manually registered commands share the same namespace and are subject to the same duplicate-name rules.

---

## Discovery API

The discovery API lets consumers inspect what commands are registered — names, parameter signatures, and stable snapshots — without executing anything or modifying the registry.

All three methods follow the same safety contract: they never throw, and they return empty results (not `null`) if called before `Initialize()` or after `Shutdown()`.

### `GetCommandNames()`

Returns a sorted array of all currently registered command names.

```csharp
string[] names = system.GetCommandNames();
for (int i = 0; i < names.Length; i++)
    autocomplete.Add(names[i]);
```

- Names are sorted by ordinal case-insensitive order for deterministic output.
- Returns `Array.Empty<string>()` when not initialized or when no commands are registered.
- **Allocates per call** (new `string[]`). For repeated reads, prefer `GetSnapshot()` instead.

### `TryGetCommandParameters(string name, out CommandParameterInfo[] parameters)`

Retrieves the parameter descriptors for a specific command by name. Lookup is case-insensitive.

```csharp
if (system.TryGetCommandParameters(inputName, out CommandParameterInfo[] parms))
{
    for (int i = 0; i < parms.Length; i++)
        ShowHint(parms[i].Name, parms[i].Type);
}
```

- Returns `true` and sets `parameters` if the command exists.
- Returns `false` and sets `parameters = null` if the system is not initialized, `name` is null or empty, or no matching command is found.
- The returned array is the **same instance stored in the registry** — do not mutate it.
- **Zero allocation** on the happy path.

### `GetSnapshot()`

Captures a stable, immutable point-in-time copy of the full registry state.

```csharp
// Take once after all Register()/Scan() calls complete
CommandMetadataSnapshot snapshot = system.GetSnapshot();

// Reference later without re-querying
string[] allNames = snapshot.CommandNames;

if (snapshot.TryGetParameters(selectedCommand, out CommandParameterInfo[] p))
    RenderParameterPanel(p);
```

- The snapshot is **isolated**: subsequent `Register()` or `Scan()` calls do not affect an already-taken snapshot.
- `CommandNames` is a sorted array of names captured at snapshot time.
- `TryGetParameters(name, out parameters)` on the snapshot behaves like the live method but reads from the captured copy. Lookup is case-insensitive.
- `TryGetDescription(name, out description)` retrieves the optional description attached at registration. Returns `true` and sets `description` when the command was registered with a non-null description; returns `false` and sets `description = null` for commands registered without a description or names not in the snapshot. Lookup is case-insensitive.
- Returns `CommandMetadataSnapshot.Empty` (an empty singleton) when not initialized.
- **Allocates once** per call, bounded by registry size. Safe to store and reference across multiple frames.

```csharp
CommandMetadataSnapshot snapshot = system.GetSnapshot();

// Retrieve description for a help UI
if (snapshot.TryGetDescription(selectedCommand, out string desc))
    ShowHelpText(desc);
```

### Before `Initialize()` / After `Shutdown()`

| Method                       | Pre-init / post-shutdown return value |
| ---------------------------- | ------------------------------------- |
| `GetCommandNames()`          | `Array.Empty<string>()`               |
| `TryGetCommandParameters(…)` | `false`, `parameters = null`          |
| `GetSnapshot()`              | `CommandMetadataSnapshot.Empty`       |

No exception is thrown in any case.

---

## Custom Type Converters

By default kmCommands supports `int`, `float`, `bool`, and `string` as parameter types. Use `RegisterConverter` to add support for any additional `System.Type`.

### Registering a Converter

```csharp
// Example custom type
struct Vector2Custom { public float X; public float Y; }

// Register the converter before or after Initialize()
bool TryParseVector2(string input, out object result)
{
    string[] parts = input.Split(',');
    if (parts.Length == 2
        && float.TryParse(parts[0], out float x)
        && float.TryParse(parts[1], out float y))
    {
        result = new Vector2Custom { X = x, Y = y };
        return true;
    }
    result = null;
    return false;
}

RegistrationResult r = system.RegisterConverter(typeof(Vector2Custom), TryParseVector2);
if (!r.Success)
{
    // r.Error and r.ErrorMessage describe what went wrong
}
```

After the converter is registered, commands can declare parameters of that type:

```csharp
system.Initialize();
system.RegisterConverter(typeof(Vector2Custom), TryParseVector2);

system.Register(
    "move",
    new[] { new CommandParameterInfo("pos", typeof(Vector2Custom)) },
    args =>
    {
        Vector2Custom pos = (Vector2Custom)args[0];
        MovePlayer(pos);
    });

system.Execute("move", new[] { "3.0,4.5" });
```

### Overriding a Built-In Converter

Registering a converter for a type that already has one replaces it (last-write wins). This applies to built-ins too:

```csharp
// Replace the built-in int converter with a hex-aware one
system.RegisterConverter(typeof(int), (string input, out object result) =>
{
    if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        && int.TryParse(input.Substring(2),
            System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out int hex))
    {
        result = hex;
        return true;
    }
    result = null;
    return false;
});
```

### `TypeConverterDelegate` Signature

```csharp
public delegate bool TypeConverterDelegate(string input, out object result);
```

The delegate must return `true` on a successful conversion and write the converted value to `result`. Return `false` and set `result = null` on failure — this causes `Execute()` to return `ExecutionError.ArgumentConversionFailed`.

### Lifecycle Rules

- Converters registered **before** `Initialize()` are buffered and activated when `Initialize()` runs.
- Converters registered **after** `Initialize()` take effect immediately.
- `Shutdown()` clears all custom converters. After a new `Initialize()` cycle, re-register any converters needed.
- `RegisterConverter` itself is safe to call at any point in the lifecycle — before or after `Initialize()`.

---

## Command History

kmCommands maintains an in-memory history of successfully executed commands. Each successful `Execute()` call appends an entry to the history buffer. Failed executions (command not found, argument conversion failure, callback exception, etc.) are **not** recorded.

### History Capacity

The buffer has a fixed maximum capacity. When the buffer is full and a new entry is recorded, the **oldest entry is discarded** (ring-buffer eviction).

The default capacity is `CommandSystem.DefaultHistoryCapacity` (value: `64`). To configure a different capacity, use the `Initialize(int historyCapacity)` overload:

```csharp
// Default capacity (64)
system.Initialize();

// Custom capacity — stores the last 20 successful executions
system.Initialize(20);
```

Values less than `1` are clamped to `1`. Capacity cannot be changed after initialization — call `Shutdown()` then `Initialize(newCapacity)` to resize.

### `HistoryCount`

Returns the current number of entries in the buffer. Does not allocate.

```csharp
int count = system.HistoryCount;
```

Returns `0` when the system is not initialized.

### `GetHistory()`

Returns a snapshot of all current history entries, ordered **oldest to newest**.

```csharp
CommandHistoryEntry[] history = system.GetHistory();
for (int i = 0; i < history.Length; i++)
{
    string name = history[i].CommandName;
    string[] args = history[i].Args;
    // display or store entry...
}
```

- The returned array is a **new snapshot** — it is independent of the live buffer. Subsequent executions do not affect a previously captured array.
- Returns `Array.Empty<CommandHistoryEntry>()` when not initialized or when the buffer is empty.

### `ClearHistory()`

Clears all entries from the history buffer.

```csharp
system.ClearHistory();
```

No-op when the system is not initialized.

### Lifecycle Behavior

| Event                               | History state   |
| ----------------------------------- | --------------- |
| After `Initialize()`                | Empty           |
| After `Execute()` (success)         | Entry appended  |
| After `Execute()` (failure)         | Unchanged       |
| After `ClearHistory()`              | Empty           |
| After `Shutdown()`                  | Buffer released |
| After `Shutdown()` + `Initialize()` | Empty           |

### Before `Initialize()` / After `Shutdown()`

| Member           | Pre-init / post-shutdown return value |
| ---------------- | ------------------------------------- |
| `HistoryCount`   | `0`                                   |
| `GetHistory()`   | `Array.Empty<CommandHistoryEntry>()`  |
| `ClearHistory()` | no-op (does not throw)                |
