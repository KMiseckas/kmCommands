# Command Authoring Guide

## What Is a Command?

A command is a named operation that can be invoked at runtime with typed arguments. In kmCommands, a command consists of:

| Part       | Type                     | Description                                                                                                             |
| ---------- | ------------------------ | ----------------------------------------------------------------------------------------------------------------------- |
| Name       | `string`                 | Unique identifier. Lookup is case-insensitive.                                                                          |
| Parameters | `CommandParameterInfo[]` | Ordered list of name + type pairs describing expected arguments.                                                        |
| Callback   | `CommandCallback`        | Delegate invoked when the command executes. Returns `null` for void commands or the return value for non-void commands. |

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
        return null; // void commands return null
    },
    "Optional human-readable description of what this command does.");

if (!result.Success)
{
    // result.Error and result.ErrorMessage describe what went wrong
}
```

Omitting the description argument is valid — the existing 3-argument overload registers the command with a `null` description:

```csharp
system.Register("reload_config", Array.Empty<CommandParameterInfo>(), args => null);
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
        return null;
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
        return null;
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
        return null;
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
    return null; // void command returns null
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

| `ExecutionError`           | Condition                                                 |
| -------------------------- | --------------------------------------------------------- |
| `None`                     | Success                                                   |
| `NotInitialized`           | `Initialize()` not called                                 |
| `NullOrEmptyCommandName`   | Command name is null or `""`                              |
| `CommandNotFound`          | No command registered with that name                      |
| `ArgumentCountMismatch`    | Wrong number of string tokens                             |
| `ArgumentConversionFailed` | A token failed to convert to the declared type            |
| `CallbackThrewException`   | Callback threw an exception — see `result.Exception`      |
| `InstanceNull`             | The instance bound to an instance command is null or GC'd |

## ExecutionResult Return Value

`ExecutionResult` exposes two additional properties for commands that return a value (e.g., property getters or non-void instance methods):

| Property         | Type     | Description                                                                                     |
| ---------------- | -------- | ----------------------------------------------------------------------------------------------- |
| `ReturnValue`    | `object` | The boxed return value from the callback, or `null` for void commands and failed executions.    |
| `HasReturnValue` | `bool`   | `true` when `ReturnValue` is non-null (i.e., the callback returned a value). `false` otherwise. |

For void commands, `ReturnValue` is always `null` and `HasReturnValue` is `false`:

```csharp
ExecutionResult result = system.Execute("reload_config", null);
// result.ReturnValue == null, result.HasReturnValue == false
```

Return values are also captured in `CommandHistoryEntry.ReturnValue`.

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

### Scanning at Initialize Time

Instead of calling `Initialize()` then `Scan()` separately, the scanning overloads of `Initialize()` combine both steps into a single call and return the aggregated `ScanResult`:

```csharp
// Scan types at init time
ScanResult result = system.Initialize(
    new[] { typeof(PlayerCommands), typeof(DebugCommands) },
    new ScanOptions { DevMode = isDevBuild });

// Scan an assembly at init time
ScanResult result = system.Initialize(
    new[] { Assembly.GetExecutingAssembly() },
    new ScanOptions { DevMode = isDevBuild });

// Scan both types and assemblies at init time
ScanResult result = system.Initialize(
    new[] { typeof(PlayerCommands) },
    new[] { Assembly.GetExecutingAssembly() },
    new ScanOptions { DevMode = isDevBuild });
```

All three overloads also accept an optional `historyCapacity` parameter (defaults to `CommandSystem.DefaultHistoryCapacity`):

```csharp
ScanResult result = system.Initialize(
    new[] { typeof(PlayerCommands) },
    new ScanOptions { DevMode = isDevBuild },
    historyCapacity: 128);
```

**Idempotency:** If the system is already initialized, the overload returns immediately. The returned `ScanResult` will have `IsAlreadyInitialized == true` and zero entries — no scan is performed:

```csharp
ScanResult result = system.Initialize(new[] { typeof(PlayerCommands) });
if (result.IsAlreadyInitialized)
{
    // System was already initialized; nothing was scanned.
}
```

`IsAlreadyInitialized == true` is distinct from a zero-entry scan result (`Entries.Length == 0`, `IsAlreadyInitialized == false`), which occurs when an empty or null array is passed to a freshly initialized system.

`ScanResult` holds the per-command outcomes of a scan:

| Member                 | Type          | Description                                                                                                                                                    |
| ---------------------- | ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Entries`              | `ScanEntry[]` | One entry per discovered command method.                                                                                                                       |
| `HasErrors`            | `bool`        | `true` if any entry has `Result.Success == false`.                                                                                                             |
| `IsAlreadyInitialized` | `bool`        | `true` when returned by a scanning `Initialize()` overload on an already-initialized system. No scan was run. Always `false` for results returned by `Scan()`. |

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

---

## Instance Command Registration

Instance commands let you register commands bound to a specific object instance. Each command is automatically namespaced under the instance key using a dot separator (`instanceKey.commandName`). When the instance is destroyed or no longer needed, a single `UnregisterInstance` call removes all associated commands.

### `RegisterInstance`

```csharp
// Shortest form — auto-discovers all public methods and properties
ScanResult result = system.RegisterInstance(target, "player");

// With explicit scan mode and options
ScanResult result = system.RegisterInstance(
    target,
    "player",
    new ScanOptions { DevMode = isDevBuild },
    InstanceScanMode.Auto);
```

| Parameter     | Type               | Description                                                                                 |
| ------------- | ------------------ | ------------------------------------------------------------------------------------------- |
| `target`      | `object`           | The instance to bind. Must not be `null`.                                                   |
| `instanceKey` | `string`           | Unique namespace key. Must not be null, empty, or contain `.`. Case-insensitive for lookup. |
| `options`     | `ScanOptions`      | Controls `IsDevOnly` filtering. Defaults to `new ScanOptions()` (dev mode off).             |
| `mode`        | `InstanceScanMode` | Controls which members are auto-discovered. Defaults to `InstanceScanMode.Auto`.            |

**Command naming:** A public method `Heal(int)` on an instance registered with key `"player"` produces the command `"player.Heal"`.

**Instance key rules:**

- Must not be null or empty.
- Must not contain a `.` character (reserved as the key/command separator).
- Must be unique — re-registering the same key returns `RegistrationError.DuplicateInstanceKey`.

### `InstanceScanMode`

| Value            | Behavior                                                                                                        |
| ---------------- | --------------------------------------------------------------------------------------------------------------- |
| `Auto` (default) | Registers all public instance methods and properties, plus any `[Command]`-decorated private/internal methods.  |
| `AttributeOnly`  | Registers only members explicitly decorated with `[Command]`. Public members without the attribute are skipped. |

### `ScanOptions.ScanUpTo` — Inheritance-Chain Boundary

By default, `RegisterInstance` only discovers members declared directly on the target type (`DeclaredOnly`). Set `ScanOptions.ScanUpTo` to scan up the inheritance chain, stopping before the specified boundary type (exclusive):

```csharp
// Scan PlayerController and all user-defined base classes,
// stopping before MonoBehaviour (whose members are never included).
ScanResult result = system.RegisterInstance(
    target,
    "player",
    new ScanOptions { DevMode = isDevBuild, ScanUpTo = typeof(MonoBehaviour) });
```

`ScanUpTo` rules:

- When `null` (default), only the concrete type's own members are scanned.
- The boundary type itself is **not** included — scanning stops before reaching it.
- `object` is always excluded, regardless of `ScanUpTo`.
- If `ScanUpTo` is not in the target's hierarchy, scanning walks all the way up to `object` (safe — no members of `object` are registered).
- `ScanOptions.DevMode` still applies at every level: auto-scanned members from base classes require DevMode on, just like top-level members.

### Auto-Discovered Members

In `Auto` mode, the scanner registers:

- **Public instance methods** — namespaced as `key.MethodName`. Generic methods, `ref`/`out` parameters, and methods with unsupported parameter types produce failed `ScanEntry` results rather than being silently skipped.
- **Public property getters** — registered as `key.get_PropertyName`. The getter command takes no arguments and returns the property value.
- **Public property setters** — registered as `key.set_PropertyName` if the property type is a supported converter type. Takes one argument (the new value).
- **`[Command]`-decorated instance methods** at any access level (public, private, protected, internal).

Not registered in auto mode:

- Methods inherited from `System.Object` (`GetHashCode`, `Equals`, `ToString`, `GetType`).
- Static methods (use the attribute-based static scan for those).
- Indexer properties.
- Property setters whose type has no registered converter.

### Dev-Only Commands on Instances

The `[Command]` attribute's `IsDevOnly` flag works the same way on instance methods as on static ones:

```csharp
public class PlayerController
{
    [Command("player_debug_state", IsDevOnly = true)]
    private void DumpDebugState()
    {
        // Only registered when ScanOptions.DevMode == true
    }
}
```

### `UnregisterInstance`

Removes all commands registered under the given key and releases the bound instance reference:

```csharp
UnregisterResult result = system.UnregisterInstance("player");

if (result.Success)
{
    // result.RemovedCount — how many commands were removed
}
else
{
    // result.ErrorMessage — why it failed
}
```

After a successful unregister, all commands previously under `player.` are gone: `GetCommandNames()`, `TryGetCommandParameters()`, `GetSnapshot()`, and `Execute()` all behave as if those commands were never registered.

### `UnregisterResult`

| Member         | Type     | Description                                                       |
| -------------- | -------- | ----------------------------------------------------------------- |
| `Success`      | `bool`   | `true` when the instance was found and all commands were removed. |
| `RemovedCount` | `int`    | Number of commands removed. `0` on failure.                       |
| `ErrorMessage` | `string` | Human-readable failure reason, or `null` on success.              |

### `ExecutionError.InstanceNull`

When a bound instance becomes null or is garbage-collected while commands are still registered, executing any of its commands returns `ExecutionError.InstanceNull`:

```csharp
ExecutionResult result = system.Execute("player.Heal", new[] { "50" });
if (result.Error == ExecutionError.InstanceNull)
{
    // The bound instance was destroyed.
    // Remove its commands to prevent further errors:
    system.UnregisterInstance("player");
}
```

This error is only possible for instance commands (`IsInstanceCommand == true`). Static commands that throw `NullReferenceException` internally produce `ExecutionError.CallbackThrewException` instead.

### Lifecycle Example (Unity MonoBehaviour)

```csharp
using kmCommands;

public class PlayerController : MonoBehaviour
{
    private CommandSystem _commands;

    void Start()
    {
        _commands.RegisterInstance(this, "player");
    }

    void OnDestroy()
    {
        _commands.UnregisterInstance("player");
    }

    public void Heal(int amount)
    {
        Health += amount;
    }

    public int Health { get; private set; } = 100;
}
```

After `Start`, the commands `player.Heal`, `player.get_Health` are available. After `OnDestroy`, they are removed.

### Registration Guard Conditions

| Condition                      | `ScanResult.Entries[0].Result.Error`     |
| ------------------------------ | ---------------------------------------- |
| System not initialized         | `RegistrationError.NotInitialized`       |
| `target` is null               | `RegistrationError.NullTarget`           |
| `instanceKey` is null or empty | `RegistrationError.InvalidInstanceKey`   |
| `instanceKey` contains `.`     | `RegistrationError.InvalidInstanceKey`   |
| Same key already registered    | `RegistrationError.DuplicateInstanceKey` |

---

## Instance Command DevMode Safety

### Auto-Scanned Members Are Dev-Only by Default

When `RegisterInstance` runs in `InstanceScanMode.Auto`, any public member discovered without a `[Command]` attribute is **implicitly dev-only**. It is only registered when `ScanOptions.DevMode` is `true`.

This prevents accidental exposure of internal APIs in release builds. In a project using `Auto` mode, calling `RegisterInstance(target, key)` (default options) in a production build registers **zero** auto-scanned methods or properties — only explicitly decorated members are registered.

```csharp
public class GameManager
{
    // ✅ Registered in release builds — explicit opt-in via [Command]
    [Command("restart")]
    public void RestartGame() { ... }

    // ✅ Registered in dev builds only — explicit dev flag
    [Command("dump_state", IsDevOnly = true)]
    public void DumpState() { ... }

    // ⚠ Only registered when DevMode = true (implicitly dev-only)
    public void ResetSession() { ... }

    // ⚠ Only accessible when DevMode = true (implicitly dev-only)
    public int FrameCount { get; }
}
```

### `[Command]` Is the Release-Safe Opt-In

Placing `[Command("name")]` on an instance method — without `IsDevOnly = true` — is the explicit consent mechanism that registers it **regardless of DevMode**. This is the only way to include a member in release builds through `RegisterInstance`.

```csharp
// Explicit release-safe registration — always registered
[Command("heal")]
public void Heal(int amount) { Health += amount; }
```

### `[CommandIgnore]` Attribute

Place `[CommandIgnore]` on a public method or property to exclude it from all scan modes. It overrides `[Command]` — if both are present, the member is skipped entirely.

```csharp
public class PlayerController
{
    // Never registered — ignored even in DevMode
    [CommandIgnore]
    public void InternalReset() { ... }

    // Also ignored — [CommandIgnore] wins over [Command]
    [Command("internal_op")]
    [CommandIgnore]
    public void InternalOp() { ... }

    // Auto-scanned as a dev-only property
    public float Speed { get; set; }
}
```

`[CommandIgnore]` has no effect on non-public members — they are already excluded from auto-scan.

### Property Naming Convention

Auto-scanned properties produce two commands using the C# accessor naming convention:

| Property             | Getter command  | Setter command  |
| -------------------- | --------------- | --------------- |
| `public int Speed`   | `key.get_Speed` | `key.set_Speed` |
| `public string Name` | `key.get_Name`  | `key.set_Name`  |

Read-only properties produce only a getter command. Write-only properties produce only a setter command. Setter commands are omitted if the property's type has no registered converter.

---

## Instance Command Performance Notes

### `DynamicInvoke` Allocation Cost

Instance command callbacks with one or more parameters use `Delegate.DynamicInvoke` internally, which:

- **Boxes value-type arguments** on each call (e.g., `int`, `float`, `bool` become heap-allocated objects).
- **Allocates an internal `object[]` argument array** per invocation.

This is acceptable for user-triggered commands (developer consoles, cheat menus, automation scripts) but is a known allocation hotspot if commands are invoked at high frequency. If your workflow triggers instance commands in tight loops, consider wrapping frequently called operations in manually registered `Register()` commands with explicit delegate callbacks that avoid boxing.

---

## Instance Command Lifecycle

### Strong Reference Warning

`RegisterInstance` stores a **strong reference** to the target object inside `InstanceRegistry`. If `UnregisterInstance` is never called (e.g., `OnDestroy` is missing in a Unity MonoBehaviour), the registered object **cannot be garbage-collected** for the lifetime of the `CommandSystem`.

The `ExecutionError.InstanceNull` error is a symptom — not a substitute for proper cleanup:

```csharp
// OnDestroy must always call UnregisterInstance to release the strong reference
void OnDestroy()
{
    _commandSystem.UnregisterInstance("player");
}
```

Failing to unregister leads to:

- Memory leaks — the target object's entire object graph is kept alive.
- Continued `InstanceNull` errors on any further execution attempts for that key.

---

## Pre-Scan Caching with `[CommandHost]` and `ScanCommandHosts`

### The Problem

Every call to `RegisterInstance` walks the target type's members via reflection and validates their parameter signatures. For classes that are registered and unregistered frequently (e.g., a `PlayerController` that is instantiated once per scene), this reflection cost is repeated unnecessarily.

### The Solution

Decorate types with `[CommandHost]` and call `ScanCommandHosts` once at startup. kmCommands caches each type's member metadata into a `TypeCommandProfile`. Subsequent `RegisterInstance` calls for matching types skip all reflection and go directly to delegate creation.

### Usage

```csharp
// Mark the class as a command host
[CommandHost]
public class PlayerController
{
    [Command("heal")]
    public void Heal(int amount) { Health += amount; }

    public int Health { get; set; } = 100;
}

// At startup — pre-scan known command-host types
system.Initialize();
system.ScanCommandHosts(new[] { typeof(PlayerController) });

// Later — RegisterInstance is reflection-free for PlayerController
ScanResult result = system.RegisterInstance(
    player, "player", new ScanOptions { DevMode = isDevBuild });
```

### Assembly-Level Pre-Scan

If you want all `[CommandHost]` types in an assembly pre-scanned without listing them explicitly:

```csharp
system.ScanCommandHosts(new[] { typeof(PlayerController).Assembly });
```

Only types decorated with `[CommandHost]` are processed. Types without the attribute are silently skipped.

### Behavior Rules

- `ScanCommandHosts` caches **all** members (attribute-decorated and auto-scan eligible) **without** applying DevMode filtering. DevMode is resolved at `RegisterInstance` time, not at pre-scan time.
- `ScanOptions.ScanUpTo` **is** applied at `ScanCommandHosts` time. Pass matching options to both `ScanCommandHosts` and `RegisterInstance`.
- Passing a type **without** `[CommandHost]` to the `Type[]` overload produces no cache entry and no error (silent skip).
- `Shutdown()` clears the profile cache.
- If `RegisterInstance` is called for a type that has not been pre-scanned, it falls back to the standard reflection path automatically.

### `ScanCommandHosts` Overloads

```csharp
// Pre-scan explicit types (only [CommandHost]-decorated types are cached)
ScanResult result = system.ScanCommandHosts(new[] { typeof(PlayerController) });

// Pre-scan with ScanOptions (for ScanUpTo boundary)
ScanResult result = system.ScanCommandHosts(
    new[] { typeof(PlayerController) },
    new ScanOptions { ScanUpTo = typeof(MonoBehaviour) });

// Pre-scan all [CommandHost] types in assemblies
ScanResult result = system.ScanCommandHosts(new[] { Assembly.GetExecutingAssembly() });

// Assembly overload with options
ScanResult result = system.ScanCommandHosts(
    new[] { Assembly.GetExecutingAssembly() },
    new ScanOptions { ScanUpTo = typeof(MonoBehaviour) });
```

---

## Command Suggestions

The suggestion API provides ranked command name completions for a partial input string, bundling each match's parameter signature and description into one `CommandSuggestion` value. It is designed to feed a UI autocompletion layer without requiring the consumer to do further registry lookups.

### `CommandSuggestion`

Each result is a `CommandSuggestion` readonly struct:

| Property      | Type                     | Description                                                              |
| ------------- | ------------------------ | ------------------------------------------------------------------------ |
| `CommandName` | `string`                 | The registered command name.                                             |
| `Parameters`  | `CommandParameterInfo[]` | Parameter descriptors. Never null — empty array for zero-param commands. |
| `Description` | `string`                 | Description from registration. Never null — `string.Empty` when none.    |

`CommandSuggestion` is a `readonly struct`. Only the library produces populated instances; consumers may only observe them.

### `GetSuggestions(string prefix)`

Returns all registered commands whose names begin with `prefix` (case-insensitive ordinal).

```csharp
CommandSuggestion[] suggestions = system.GetSuggestions("he");
for (int i = 0; i < suggestions.Length; i++)
{
    string name = suggestions[i].CommandName;        // e.g. "health", "help"
    CommandParameterInfo[] parms = suggestions[i].Parameters;
    string desc = suggestions[i].Description;
    // render suggestion in UI...
}
```

- `null` or empty `prefix` returns all registered commands.
- Returns `Array.Empty<CommandSuggestion>()` when not initialized, when no commands match, or after `Shutdown()`. Never returns `null`.
- Results are returned in the order they were matched (alphabetical for the built-in matcher, since command names are pre-sorted in the registry).

### `GetSuggestions(string prefix, ISuggestionMatcher matcher)`

Per-call override that uses the supplied `matcher` instead of the global or built-in default.

```csharp
CommandSuggestion[] suggestions = system.GetSuggestions("he", myCustomMatcher);
```

- Passing `null` as `matcher` falls back to the global matcher (set via `SetSuggestionMatcher`) then the built-in prefix matcher.
- The library preserves the order returned by the matcher — results are never re-sorted after the matcher returns.

### `SetSuggestionMatcher(ISuggestionMatcher matcher)`

Sets the global `ISuggestionMatcher` used by the single-argument `GetSuggestions(prefix)` overload.

```csharp
// Use a custom matcher globally
system.SetSuggestionMatcher(new FuzzyMatcher());

// Revert to built-in prefix matcher
system.SetSuggestionMatcher(null);
```

- Accepts `null` to revert to the built-in `PrefixSuggestionMatcher`.
- Safe to call before or after `Initialize()` — no `IsInitialized` guard.
- `Shutdown()` resets the global matcher to `null` (built-in default).

### `ISuggestionMatcher`

Consumer-implementable interface for custom matching strategies (fuzzy, scored, etc.):

```csharp
public class FuzzyMatcher : ISuggestionMatcher
{
    public IList<string> Match(string prefix, string[] commandNames)
    {
        List<string> results = new List<string>();
        for (int i = 0; i < commandNames.Length; i++)
        {
            if (IsFuzzyMatch(prefix, commandNames[i]))
                results.Add(commandNames[i]);
        }
        return results;
    }

    private bool IsFuzzyMatch(string prefix, string name) { /* ... */ }
}
```

- `prefix` — the partial input; null or empty means "return all".
- `commandNames` — a sorted snapshot of all registered names at call time.
- Return value must not be `null` (an empty list is returned for no matches).
- The library never re-sorts the returned list — return order is preserved exactly.

### `CommandMetadataSnapshot.GetSuggestions`

Snapshot instances expose the same two overloads, working from their captured state:

```csharp
CommandMetadataSnapshot snapshot = system.GetSnapshot();

// Uses built-in prefix matcher
CommandSuggestion[] suggestions = snapshot.GetSuggestions("he");

// Per-call custom matcher
CommandSuggestion[] suggestions = snapshot.GetSuggestions("he", myCustomMatcher);
```

- No `IsInitialized` guard — the snapshot is self-contained. `CommandMetadataSnapshot.Empty` returns `Array.Empty<CommandSuggestion>()` naturally.
- Snapshots do not have a global matcher field; the per-call `matcher` parameter falls back directly to the built-in default.

### Lifecycle Behavior

| Event / State                      | `GetSuggestions` return value                 |
| ---------------------------------- | --------------------------------------------- |
| Before `Initialize()`              | `Array.Empty<CommandSuggestion>()`            |
| After `Shutdown()`                 | `Array.Empty<CommandSuggestion>()`            |
| `SetSuggestionMatcher(matcher)`    | Subsequent calls use `matcher`                |
| `SetSuggestionMatcher(null)`       | Reverts to built-in `PrefixSuggestionMatcher` |
| `Shutdown()` after setting matcher | Matcher reset to `null`                       |

---

## Configuration File Support

`CommandSystem` can be initialised from a JSON configuration file instead of (or in addition to) using code-only `Initialize()` calls. This is useful when you want to keep initialisation settings in a deployable asset rather than baked into code.

> **Security note:** Configuration files must never contain secrets or credentials (API keys, tokens, passwords, etc.).

### `CommandConfig`

`CommandConfig` is a public class that holds the initialisation settings with coded defaults:

| Property          | Type   | Default                             | Description                                        |
| ----------------- | ------ | ----------------------------------- | -------------------------------------------------- |
| `HistoryCapacity` | `int`  | `CommandSystem.DefaultHistoryCapacity` | Max history entries. Values < 1 are clamped to 1. |
| `DevMode`         | `bool` | `false`                             | Enables dev-mode — dev-only commands are included. |

### Writing a Config File

```json
{
    "historyCapacity": 128,
    "devMode": true
}
```

Key names are case-insensitive. Unknown keys produce a warning instead of an error, so config files are forward-compatible as the schema grows.

### `CommandConfig.FromFile(string filePath)`

Reads a JSON file and returns a `ConfigResult`:

```csharp
ConfigResult result = CommandConfig.FromFile("commands.json");

if (!result.Success)
{
    Debug.LogError(string.Format("Config error ({0}): {1}", result.Error, result.ErrorMessage));
    return;
}

// Log any unknown-key warnings
for (int i = 0; i < result.Warnings.Length; i++)
{
    Debug.LogWarning(result.Warnings[i]);
}

// Initialise with the parsed config
_commands.Initialize(result.Config);
```

### `CommandConfig.FromJson(string json)`

Parses a raw JSON string instead of reading a file. Useful when the config is embedded in another asset or fetched from a remote source:

```csharp
string json = LoadConfigFromAsset(); // your source
ConfigResult result = CommandConfig.FromJson(json);
```

### `CommandSystem.Initialize(CommandConfig config)`

Applies `config.HistoryCapacity` and `config.DevMode`, then initialises the system. Behaviour is identical to:

```csharp
system.Initialize(historyCapacity: config.HistoryCapacity, devMode: config.DevMode);
```

- Calling when already initialised is a no-op.
- Passing `null` is a no-op (system is not initialised).
- Call `Shutdown()` before re-initialising with a different config.

### `ConfigResult`

Both factory methods return a `ConfigResult` readonly struct:

| Member         | Type            | Description                                                          |
| -------------- | --------------- | -------------------------------------------------------------------- |
| `Success`      | `bool`          | `true` when parsing succeeded.                                       |
| `Config`       | `CommandConfig` | The populated config on success; `null` on failure.                  |
| `Error`        | `ConfigError`   | The failure code; `ConfigError.None` on success.                     |
| `ErrorMessage` | `string`        | Human-readable failure description; `null` on success.               |
| `Warnings`     | `string[]`      | Zero or more warnings (e.g., unknown keys). Never `null` on success. |

### `ConfigError` Enum

| Value           | Cause                                                                        |
| --------------- | ---------------------------------------------------------------------------- |
| `None`          | Success.                                                                     |
| `InvalidJson`   | JSON was null, empty, or structurally malformed.                             |
| `TypeMismatch`  | A known key had the wrong JSON value type (e.g. `"devMode": 42`).            |
| `FileReadError` | File path was invalid, the file was not found, or an I/O error occurred.     |

### Minimal Usage Example

```csharp
// In Awake() or the equivalent bootstrap entry point:
var result = CommandConfig.FromFile("Assets/StreamingAssets/commands.json");

if (result.Success)
{
    _commands.Initialize(result.Config);
}
else
{
    Debug.LogError(result.ErrorMessage);
    _commands.Initialize(); // fall back to defaults
}
```

### Partial Config

Any key can be omitted. Omitted keys use the same defaults as `new CommandConfig()`:

```json
{ "historyCapacity": 256 }
```

An empty object `{}` is valid and applies all defaults — identical to calling `Initialize()` without arguments.

### After `Shutdown()`

Config state is consumed only during `Initialize`. After `Shutdown()`, calling `Initialize(config)` again with a fresh config works correctly.
