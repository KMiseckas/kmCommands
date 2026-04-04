# kmCommands — Project Vision

## Project Goal

A lightweight, platform-agnostic C# command-system library for Unity 2021+ that enables runtime command registration, argument parsing, and execution with a clean, stable API. Targets `netstandard2.0`. Core is engine-agnostic with no `UnityEngine` dependency.

---

## Project Notes

- No Unity UI, rendering, or input handling in core library.
- No `MonoBehaviour` or scene lifecycle dependencies.
- IL2CPP/AOT-safe patterns required throughout.
- Minimize allocations in parse/execute hot paths.
- Avoid LINQ in runtime paths.
- Public API stability is a hard requirement — external Unity clients consume this.
- Lifecycle must be explicit: `Initialize()` / `Shutdown()`.
- Unity-facing concerns (input, display, keybinding) stay outside the library.

---

## Features

### ✅ Manual Command Registration

Register commands at runtime with a name, typed parameter definitions, and a callback delegate.

- [x] Register by name
- [x] Typed parameter descriptors (`CommandParameterInfo`)
- [x] Structured registration result (`RegistrationResult`)
- [x] Duplicate name detection

---

### ✅ Argument Parsing

Convert string tokens to typed .NET values before invoking the callback.

- [x] Built-in support: `int`, `float`, `bool`, `string`
- [x] Invariant culture numeric parsing
- [x] Strict bool parsing (`"true"` / `"false"` only)
- [x] Unsupported type detected and reported at registration time

---

### ✅ Command Execution

Execute a registered command by name with string arguments and receive a structured result.

- [x] Structured execution result (`ExecutionResult`)
- [x] Argument count validation
- [x] Type conversion errors reported
- [x] Callback exception wrapping

---

### ✅ Command Registry

Internal store for all registered command definitions.

- [x] Case-insensitive name lookup
- [x] Dictionary-backed storage
- [x] Command exists check

---

### 🔲 Attribute-Based Registration

Register commands using C# attributes on static methods — no manual `Register()` calls needed.

- [ ] Define attribute (`[Command("name")]`)
- [ ] Scan a target type or assembly at `Initialize()`
- [ ] Auto-map method parameters to `CommandParameterInfo`
- [ ] Skip unsupported parameter types gracefully
- [ ] AOT/IL2CPP safe (no runtime codegen)

---

### 🔲 Command Chaining

Execute multiple commands in a single input string using a delimiter.

- [ ] Define chain delimiter (e.g. `;`)
- [ ] Split input into command segments
- [ ] Execute each segment in sequence
- [ ] Return aggregated or per-command results
- [ ] Option: stop-on-failure vs continue-on-failure

---

### 🔲 Command Metadata / Discovery API

Expose registered command data for autocompletion, help displays, and tooling.

- [ ] Get all registered command names
- [ ] Get parameter info for a specific command
- [ ] Read-only metadata snapshot of the registry

---

### 🔲 Command Description / Help Text

Attach a human-readable description to a command at registration.

- [ ] Optional description string at registration
- [ ] Exposed via metadata/discovery API
- [ ] Usable by Unity UI layer for help output

---

### 🔲 Optional Parameters / Default Values

Allow commands to declare parameters as optional with a declared fallback value.

- [ ] Mark parameter as optional in `CommandParameterInfo`
- [ ] Provide default value at registration
- [ ] Execution succeeds when optional args are omitted

---

### 🔲 Custom Type Converters

Allow consumers to register converters for types beyond the built-in set.

- [ ] API to register a custom converter for a `System.Type`
- [ ] Custom converters extend or override built-ins
- [ ] Consistent error reporting when conversion fails

---

### 🔲 Command Aliases

Register additional names that route to the same command.

- [ ] Register alias to an existing command name
- [ ] Alias inherits same parameters and callback
- [ ] Aliases appear in metadata/discovery output
