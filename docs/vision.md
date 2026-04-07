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
- [x] Auto-map method parameters to `CommandParameterInfo`
- [x] Skip unsupported parameter types gracefully
- [x] AOT/IL2CPP safe (no runtime codegen)

**Note — release vs. debug command filtering**: two candidate approaches; design should pick whichever is more user-friendly:

- _Call-site `#if`_: Unity layer wraps the scan call in `#if UNITY_EDITOR || DEVELOPMENT_BUILD` — simple, no lib changes, but places the burden on every consumer.
- _`IsDevOnly` attribute flag_: `[Command("name", IsDevOnly = true)]` — lib skips those commands unless the consumer initialises in dev mode; cleaner for the user, requires a dev-mode initialisation concept in the lib.

---

### ✅ Auto-Scan at Initialize

Allow consumers to declare scan targets (types or assemblies) at `Initialize()` time so that attribute-based registration runs automatically during startup — removing the need for explicit follow-up `Scan()` calls in consumer bootstrap code.

- [x] `Initialize(Type[])` and `Initialize(Assembly[])` overloads (or a combined overload) that accept scan targets alongside the optional history capacity
- [x] Scan results from init-time scanning available for inspection after `Initialize()` returns
- [x] Compatible with subsequent manual `Register()` and explicit `Scan()` calls after init
- [x] Dev-mode opt-in via `ScanOptions` passed alongside scan targets at init time

---

### ✅ Command History

Maintain an in-memory ring buffer of successfully executed commands for consumer inspection.

- [x] In-memory ring buffer with configurable max entry count
- [x] Consumer reads buffer via history API (`GetHistory()`, `HistoryCount`, `ClearHistory()`)
- [x] Buffer capacity configurable at `Initialize(int historyCapacity)`; clamped to ≥ 1
- [x] `DefaultHistoryCapacity` constant (64) used by no-arg `Initialize()`

---

### ✅ Rich History Entries

Extend `CommandHistoryEntry` to record richer execution context beyond the current command name and argument snapshot.

- [x] Timestamp (UTC) recorded at execution time
- [x] Raw input string as originally passed to `Execute()`, before any processing
- [x] Result status (success or the specific `ExecutionError` value)
- [x] Error detail string when execution fails

---

### 🔲 History Writer Adapter

An injectable `IHistoryWriter` adapter interface that lets consumers forward history entries to any external sink without coupling the core library to `System.IO`.

- [ ] Define `IHistoryWriter` interface with a single `Write(CommandHistoryEntry entry)` method
- [ ] Library calls `IHistoryWriter.Write()` on each entry append; no I/O without an injected implementation
- [ ] Adapter is optional — library works fully without one; missing or null adapter is never an error
- [ ] Adapter injectable at `Initialize()` time alongside capacity configuration
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

### 🔲 Commands as Command Arguments

Allow a command invocation to be used as an argument to another command, so the return value of the inner command is resolved first and passed as a typed argument to the outer command (e.g. `destroy(getPlayer(1))`).

- [ ] Nested command invocation syntax — inner call wrapped in parentheses inside the outer call's argument position
- [ ] Inner command is executed first; its return value is passed as the argument to the outer command
- [ ] Type compatibility between inner return value and outer parameter type is validated before execution
- [ ] Nesting depth limit defined at design time to prevent unbounded recursion
- [ ] Errors in inner command execution propagate as a structured failure in the outer result — outer command is not invoked
- [ ] AOT/IL2CPP safe — no runtime code generation or dynamic dispatch

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

### ✅ Custom Type Converters

Allow consumers to register converters for types beyond the built-in set.

- [x] API to register a custom converter for a `System.Type`
- [x] Custom converters extend or override built-ins
- [x] Consistent error reporting when conversion fails

---

### 🔲 Command Aliases

Register additional names that route to the same command.

- [ ] Register alias to an existing command name
- [ ] Alias inherits same parameters and callback
- [ ] Aliases appear in metadata/discovery output

---

### ✅ Instance Command Registration

Register commands bound to a specific object instance, enabling instance method callbacks without static boilerplate. Intended to support MonoBehaviour-hosted commands on the Unity side without any `UnityEngine` dependency in the library.

- [x] `RegisterInstance(object target, string instanceKey)` API — consumer supplies a stable string key to identify the instance
- [x] `[Command]` attribute on instance methods discovered at `RegisterInstance()` time (extends attribute-based registration)
- [x] Command names follow the scheme `"instanceKey.commandName"` (e.g. `"player.heal"`) — dot separator is fixed; instanceKey and commandName each follow normal naming rules
- [x] `UnregisterInstance(string instanceKey)` — removes all commands associated with that instance (critical for scene unload and object destruction)
- [x] Consumer is responsible for mapping Unity identity (GameObject name, tag, instance ID) to the instance key string; library does not interpret identity
- [x] When multiple instances of the same type exist, the consumer must supply a unique key per instance (e.g. `"enemy_1"`, `"enemy_2"`)
- [x] Broadcasting (call all instances sharing a type key) may be an explicit opt-in — deferred to design time
- [x] Auto-scan public instance methods and readable/writable properties of a registered type — these become instance commands without requiring a `[Command]` attribute
- [x] Private and protected members are ignored by auto-scan; explicit `[Command]` attribute or manual `Register()` call required to expose them
- [x] Writable property → setter command; readable property → getter command; read-write property → both registered
- [x] Consumer can opt out of auto-scan per registration if they want attribute-only discovery on a given type

---

### ✅ Autocomplete / Command Suggestions

Return ranked command name suggestions and parameter signatures from a partial input string. Enough for a Unity UI layer to render a dropdown or inline hint without doing its own registry work.

- [x] `GetSuggestions(string prefix)` API — returns command names that start with the given prefix (case-insensitive)
- [x] Each suggestion includes the command's `CommandParameterInfo` list so the UI can display the full signature
- [x] Built-in prefix-match implementation in the library — no third-party dependency required
- [x] Consumer can supply an `ISuggestionMatcher` to replace or extend matching (e.g. fuzzy match, ranked scoring)
- [x] Works on both the live registry and a `CommandMetadataSnapshot`
- [x] Returns an empty list (never null) when there are no matches

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

### 🔲 AI Infrastructure (Dev-Only)

Foundation layer for all LLM-backed features. Strictly developer tooling — must never be present in release builds. All AI types, interfaces, and code paths are stripped at compile time unless the consumer defines `KMCOMMANDS_AI`.

**Provider interface**: `ILlmProvider` is a minimal async text-in / text-out contract. The consumer wires up their preferred backend (OpenAI, Anthropic, local model, etc.) — no bundled provider, no model-specific types in the library.

**Settings**: an `AiSettings` struct holds AI-specific configuration (max iterations cap, context token budget hint, etc.) and is passed at `Initialize()` time or supplied via `CommandConfig`. Auth tokens are part of the consumer's `ILlmProvider` implementation — never a library concern and never serialised.

**Queue**: AI operations are async-first. A dedicated AI command queue accepts dispatched requests so Unity main-thread code can fire-and-forget without blocking the frame.

- [ ] `ILlmProvider` interface — single `Task<string> CallAsync(string prompt)` method; AOT-safe, no generic type parameters
- [ ] `AiSettings` struct — `MaxIterations` (int), `ContextTokenBudget` (int hint, default TBD); injectable at `Initialize()` time or via `CommandConfig`
- [ ] Auth token held entirely within the consumer's `ILlmProvider` implementation — never accepted, stored, or logged by the library
- [ ] `ILlmProvider` injectable alongside (or after) `Initialize()`; null provider is never an error — AI call sites return a clear `NotConfigured` diagnostic result
- [ ] Compile-time gate: `KMCOMMANDS_AI` symbol — all AI types stripped from builds that do not define it
- [ ] Runtime guard at every AI call site — returns `NotConfigured` no-op result if symbol absent or provider not set
- [ ] Async AI command queue — queues in-flight AI operations; `Shutdown()` cancels pending work and clears the provider reference
- [ ] Shared AI result type(s) covering: `Success`, `NotConfigured`, `ProviderError`, `ParseFailure`, `CapReached`, `Cancelled`
- [ ] Token and provider config must not appear in any serialised state, asset, or config file that could ship in a release build
- [ ] Documentation must warn: do not enable in release builds; do not hard-code tokens in source; consumer is responsible for rate limiting and cost management

---

### 🔲 Natural Language Command Dispatch (Dev-Only)

Requires **AI Infrastructure** to be in place. Consumer passes a free-text string; the library builds a structured prompt from the live registry, sends it to the configured `ILlmProvider`, parses the response, and dispatches the resolved command through the normal execution path.

**Prompt construction**: the library serialises the registry to a compact JSON block (command names, parameter names/types, descriptions) and wraps the user's input in a pre-defined envelope. Consumer can inject an `IPromptFormatter` to customise the system prompt or user message wrapper; the built-in formatter is used when none is set.

**Response envelope**: the LLM is instructed (via the system prompt) to return a flat JSON object: `{ "command": "commandName", "args": ["arg1", "arg2"] }`. The library owns and parses this schema — the consumer's `ILlmProvider` returns raw text and is unaware of it.

- [ ] `ExecuteNaturalLanguageAsync(string input, CancellationToken)` on `CommandSystem` — returns `Task<NlCommandResult>`
- [ ] `NlCommandResult` — carries resolved command name, resolved args, the underlying `ExecutionResult`, and an `NlCommandError` status (`None`, `NotConfigured`, `ProviderError`, `ParseFailure`, `CommandNotFound`, `ExecutionFailed`, `Cancelled`)
- [ ] Built-in registry-to-JSON context builder — serialises command names, parameter names/types, and descriptions into a compact JSON block; no third-party serialiser
- [ ] Default system prompt template built into the library — instructs the LLM to emit the response envelope; no consumer action required to get basic behaviour
- [ ] `IPromptFormatter` interface — optional consumer-supplied hook to replace or wrap the default system prompt and/or user message; null/unset uses the built-in template
- [ ] Response envelope parser — hand-rolled flat-JSON parse of `{ "command": ..., "args": [...] }`; AOT-safe; failure → `ParseFailure` result
- [ ] Resolved command is dispatched through the existing `Execute()` path — argument conversion, validation, and history recording behave identically to a direct call
- [ ] Consumer is responsible for rate limiting, cost management, and compliance with their LLM provider's terms of service

---

### 🔲 AI Agent Loop (Dev-Only)

Requires **AI Infrastructure** and **Natural Language Command Dispatch** to be in place. Given a goal string, the library iteratively prompts the configured `ILlmProvider` with the full registry context and a record of previously executed commands, generating and executing a sequence of commands until the LLM signals completion or the iteration cap is reached.

**Context growth**: each iteration appends the executed command name, args, and execution outcome to the running context so the LLM can reason about progress toward the goal.

**Done signal**: the LLM response envelope for agent-loop iterations is `{ "done": bool, "commands": [{ "name": "...", "args": [...] }] }`. The library owns this schema; the system prompt instructs the LLM to emit it. Multiple commands may be returned per iteration and are executed in sequence before the next LLM call.

- [ ] `RunAgentLoopAsync(string goal, CancellationToken)` on `CommandSystem` — returns `Task<AgentLoopResult>`
- [ ] `AgentLoopResult` — carries per-iteration summaries (commands attempted, outcomes), final status (`Completed`, `CapReached`, `ProviderError`, `ParseFailure`, `Cancelled`, `NotConfigured`), and total iteration count
- [ ] Iteration cap from `AiSettings.MaxIterations`; loop terminates with `CapReached` status when hit
- [ ] `CancellationToken` respected between iterations — cancellation returns `Cancelled` result with work done so far
- [ ] Per-iteration context builder appends executed command names, args, and outcome summaries to the running prompt; `AiSettings.ContextTokenBudget` hint used to trim oldest entries when context grows large
- [ ] Multi-command response per iteration — all commands in `"commands"` array executed in sequence; any execution failure recorded in result but loop continues unless `"done": true`
- [ ] Consumer is responsible for rate limiting, cost management, and compliance with their LLM provider's terms of service

---

### ✅ Configuration File Support

Allow `CommandSystem` behaviour to be driven by an external JSON file loaded at initialisation, reducing the need for repetitive manual setup in the consumer's bootstrap code.

- [x] Consumer passes a config file path (or raw string content) to `Initialize()`; library parses and applies it
- [x] Supported format: JSON only — minimal built-in parser with no third-party dependencies; YAML deferred to a future iteration
- [x] Settings in scope for v1 (current implemented features only): `historyCapacity` (int) and `devMode` (bool)
- [x] Settings for future features (chain delimiter, case-sensitivity mode, expression evaluation defaults, etc.) are added to the config schema when those features ship — not in v1
- [x] Config is applied once at initialisation; no live-reload unless consumer calls `Shutdown()` + `Initialize()` again
- [x] Unknown keys in config produce a warning result rather than a hard error — forward compatibility
- [x] Config file must never contain secrets (tokens, credentials); documentation must state this explicitly
- [x] All config values have coded defaults so a missing or empty file is never an error

---

## Unity Companion Package Ideas

These are not core library features. Each would live in a separate Unity-only package with a `UnityEngine` / `UnityEditor` dependency. Listed here as potential future work.

**kmCommands.Unity.InstanceCommands** — Pre-built `RegisterInstance` helpers for common Unity component types (`Transform`, `Rigidbody`, `Camera`, etc.). Exposes things like `setPosition`, `setRotation`, `setTimeScale` without the consumer writing boilerplate. Relies entirely on the existing Instance Command Registration and auto-scan — no core library changes required.

**kmCommands.Unity.StaticCommands** — A curated set of static command wrappers around common Unity engine functions: `Application.Quit`, `SceneManager.LoadScene`, `Time.timeScale`, `Physics.gravity`, etc. Registered automatically when the package is present.

**kmCommands.Unity.EditorCommands** — Editor-only commands (`UnityEditor` assembly) useful for in-editor dev tooling: things like `Selection`, `AssetDatabase` operations, play-mode toggling. Stripped from all builds outside the Editor.
