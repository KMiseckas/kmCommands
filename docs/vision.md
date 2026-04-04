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

### ✅ Attribute-Based Registration

Register commands using C# attributes on static methods — no manual `Register()` calls needed.

- [x] Define attribute (`[Command("name")]`)
- [x] Scan a target type or assembly at `Initialize()`
- [x] Auto-map method parameters to `CommandParameterInfo`
- [x] Skip unsupported parameter types gracefully
- [x] AOT/IL2CPP safe (no runtime codegen)

**Note — release vs. debug command filtering**: two candidate approaches; design should pick whichever is more user-friendly:

- _Call-site `#if`_: Unity layer wraps the scan call in `#if UNITY_EDITOR || DEVELOPMENT_BUILD` — simple, no lib changes, but places the burden on every consumer.
- _`IsDevOnly` attribute flag_: `[Command("name", IsDevOnly = true)]` — lib skips those commands unless the consumer initialises in dev mode; cleaner for the user, requires a dev-mode initialisation concept in the lib.

---

### 🔲 Command History

Maintain an in-memory ring buffer of executed commands, results, and system events. An injectable adapter interface lets consumers optionally persist history to any sink (file, network, etc.) without coupling the core to `System.IO`.

- [ ] In-memory ring buffer with configurable max entry count
- [ ] Entries record: timestamp, raw input string, resolved command name, result status, error detail
- [ ] Consumer reads buffer via a history API (e.g. `GetHistory()`, paged or full)
- [ ] Injectable `IHistoryWriter` adapter — library calls it on each entry append; no I/O without an injected implementation
- [ ] Adapter is optional — library works fully without one; missing adapter is never an error
- [ ] Buffer capacity and adapter are configurable at `Initialize()`
- [ ] Unity layer implements `IHistoryWriter` for file logging, Editor console forwarding, network sinks, etc.

---

### 🔲 Command Chaining

Execute multiple commands in a single input string using a delimiter.

- [ ] Define chain delimiter (e.g. `;`)
- [ ] Split input into command segments
- [ ] Execute each segment in sequence
- [ ] Return aggregated or per-command results
- [ ] Option: stop-on-failure vs continue-on-failure

---

### ✅ Command Metadata / Discovery API

Expose registered command data for autocompletion, help displays, and tooling.

- [x] Get all registered command names
- [x] Get parameter info for a specific command
- [x] Read-only metadata snapshot of the registry

---

### ✅ Command Description / Help Text

Attach a human-readable description to a command at registration.

- [x] Optional description string at registration
- [x] Exposed via metadata/discovery API
- [x] Usable by Unity UI layer for help output

---

### ✅ Optional Parameters / Default Values

Allow commands to declare parameters as optional with a declared fallback value.

- [x] Mark parameter as optional in `CommandParameterInfo`
- [x] Provide default value at registration
- [x] Execution succeeds when optional args are omitted

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

---

### 🔲 Instance Command Registration

Register commands bound to a specific object instance, enabling instance method callbacks without static boilerplate. Intended to support MonoBehaviour-hosted commands on the Unity side without any `UnityEngine` dependency in the library.

- [ ] `RegisterInstance(object target, string instanceKey)` API — consumer supplies a stable string key to identify the instance
- [ ] `[Command]` attribute on instance methods discovered at `RegisterInstance()` time (extends attribute-based registration)
- [ ] Command names follow the scheme `"instanceKey.commandName"` (e.g. `"player.heal"`) — dot separator is fixed; instanceKey and commandName each follow normal naming rules
- [ ] `UnregisterInstance(string instanceKey)` — removes all commands associated with that instance (critical for scene unload and object destruction)
- [ ] Consumer is responsible for mapping Unity identity (GameObject name, tag, instance ID) to the instance key string; library does not interpret identity
- [ ] When multiple instances of the same type exist, the consumer must supply a unique key per instance (e.g. `"enemy_1"`, `"enemy_2"`)
- [ ] Broadcasting (call all instances sharing a type key) may be an explicit opt-in — deferred to design time
- [ ] Auto-scan public instance methods and readable/writable properties of a registered type — these become instance commands without requiring a `[Command]` attribute
- [ ] Private and protected members are ignored by auto-scan; explicit `[Command]` attribute or manual `Register()` call required to expose them
- [ ] Writable property → setter command; readable property → getter command; read-write property → both registered
- [ ] Consumer can opt out of auto-scan per registration if they want attribute-only discovery on a given type

---

### 🔲 Autocomplete / Command Suggestions

Return ranked command name suggestions and parameter signatures from a partial input string. Enough for a Unity UI layer to render a dropdown or inline hint without doing its own registry work.

- [ ] `GetSuggestions(string prefix)` API — returns command names that start with the given prefix (case-insensitive)
- [ ] Each suggestion includes the command's `CommandParameterInfo` list so the UI can display the full signature
- [ ] Built-in prefix-match implementation in the library — no third-party dependency required
- [ ] Consumer can supply an `ISuggestionMatcher` to replace or extend matching (e.g. fuzzy match, ranked scoring)
- [ ] Works on both the live registry and a `CommandMetadataSnapshot`
- [ ] Returns an empty list (never null) when there are no matches

---

### 🔲 Expression Evaluation in Arguments

Evaluate arithmetic expressions in argument strings before type conversion, so values like `2+2`, `10*0.5`, or `(100-20)/4` resolve to their computed result.

- [ ] Opt-in per parameter via a flag on `CommandParameterInfo` (off by default — see note below)
- [ ] Supported operators: `+`, `-`, `*`, `/`, `%`, parentheses, unary negation
- [ ] Consumer-registered named variable values (e.g. `"health"`, `"maxSpeed"`) usable inside expressions
- [ ] Named variable registry is mutable at runtime so consumers can keep it in sync with game state
- [ ] AOT/IL2CPP safe — recursive descent evaluator only; no `System.Linq.Expressions` or `Emit`
- [ ] Expression errors reported in `ExecutionResult` before the callback is invoked

**Note**: Expression parsing adds a string scan pass per argument. Opt-in per parameter avoids overhead for commands that never use it and prevents ambiguity in string arguments that legitimately contain `+` or `-` characters.

---

### 🔲 LLM / AI Integration (Dev-Only)

Natural language command dispatch and autonomous AI agent loops backed by external LLMs. Strictly developer tooling — must never be present in release builds.

**Natural language parsing**: consumer passes a free-text string; an LLM resolves it to a recognised command + arguments, which the library then executes through its normal execution path.

**AI agent loop**: given a goal string, an LLM is provided the full command registry as context and autonomously generates and executes a sequence of commands to accomplish the goal.

**Richer context via MonoBehaviour auto-scan**: the public-method/property auto-scan from Instance Command Registration significantly expands the command surface available to an LLM agent without requiring the consumer to manually register every game function — more registered commands means more options for the LLM to compose goal-achieving sequences.

- [ ] Natural language string → command resolution via LLM, returned through normal `ExecutionResult`
- [ ] AI agent loop: iterative goal → command sequence generation using live registry context
- [ ] Consumer provides their own LLM API token via a dedicated initialisation call — token is held only in memory, never written to disk, never logged anywhere
- [ ] LLM provider is pluggable via interface — no bundled provider; consumer wires up their preferred backend (e.g. OpenAI, Anthropic, local model)
- [ ] Feature gated behind a compile-time symbol (e.g. `KMCOMMANDS_AI`) — all AI types and code are stripped from builds that do not define it
- [ ] Secondary runtime guard at every AI call site — no-op with a clear diagnostic result if built without the compile symbol or if not in a dev context
- [ ] Token and provider config must not appear in any serialised state, asset, or scene file that could ship in a release build
- [ ] Agent loop has a configurable max-iteration cap (default TBD at design time); loop terminates with a diagnostic result when the cap is reached
- [ ] Consumer is responsible for rate limiting, cost management, and compliance with their LLM provider's terms of service
- [ ] Documentation must warn: do not enable in release builds; do not hard-code tokens in source

---

### 🔲 Configuration File Support

Allow `CommandSystem` behaviour to be driven by an external JSON or YAML file loaded at initialisation, reducing the need for repetitive manual setup in the consumer's bootstrap code.

- [ ] Consumer passes a config file path (or raw string content) to `Initialize()`; library parses and applies it
- [ ] Supported formats: JSON and YAML — both handled by a minimal built-in parser with no third-party dependencies
- [ ] Configurable settings include: chain delimiter, case-sensitivity mode, expression evaluation defaults, and other behavioural flags added by future features
- [ ] Config is applied once at initialisation; no live-reload unless consumer calls `Shutdown()` + `Initialize()` again
- [ ] Unknown keys in config produce a warning result rather than a hard error — forward compatibility
- [ ] Config file must never contain secrets (tokens, credentials); documentation must state this explicitly
- [ ] All config values have coded defaults so a missing file is never an error
