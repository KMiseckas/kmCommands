# Configuration File Support

## Status

Draft

## Branch

- Name: `feat_config-file-support`
- Rationale: `feat_` — new capability that did not previously exist; consumers can now drive system configuration from a JSON file instead of code-only `Initialize()` calls.

## Summary

Allow consumers to author a JSON file that captures `CommandSystem` initialisation settings, then pass a file path or raw JSON string to `Initialize()`. The library parses the file and applies the settings before the system becomes active. Unknown keys produce warnings rather than errors so config files remain forward-compatible as the schema grows over time.

v1 scope is intentionally minimal: only the settings that correspond to currently-implemented features are included in the config schema. Settings for unimplemented features (command chaining delimiter, case-sensitivity mode, expression evaluation flags, etc.) are deferred and will be added to the schema when those features ship.

## Goals

- Let consumers replace repetitive code-level `Initialize(historyCapacity, devMode)` calls with a declarative JSON file.
- Produce clear, actionable warnings when a config file contains unrecognised keys so consumers catch typos early.
- Keep the parser minimal, dependency-free, and AOT/IL2CPP-safe.
- Preserve all existing `Initialize()` overload behaviour — the config path is a new entry point, not a replacement.

## In Scope

- A new `CommandConfig` public class that holds all configurable values with coded defaults:
  - `HistoryCapacity` (int, default: `CommandSystem.DefaultHistoryCapacity`)
  - `DevMode` (bool, default: `false`)
- A `CommandConfig.FromJson(string json)` static factory that parses a JSON string and returns a populated `CommandConfig` (or a result carrying parse errors/warnings).
- A `CommandConfig.FromFile(string filePath)` static factory that reads the file and delegates to `FromJson`.
- A new `Initialize(CommandConfig config)` overload on `CommandSystem` that applies the config and initialises the system — equivalent in behaviour to `Initialize(historyCapacity: config.HistoryCapacity, devMode: config.DevMode)`.
- Unknown JSON keys produce one warning entry per unknown key; the config is still applied with defaults for the unknown values.
- Parse errors (malformed JSON, wrong value type for a known key) produce a descriptive error result; the system is not initialised.
- A `ConfigResult` public result type (or equivalent) returned from `FromJson` / `FromFile` that signals success with optional warnings, or failure with an error message.
- `Initialize(CommandConfig config)` returns `void` (consistent with the basic `Initialize()` overload); parse errors are surfaced via `ConfigResult` before `Initialize()` is called — the two steps are explicit and separate.
- Config file must be applied once at initialisation; no live-reload; consistent with the existing lifecycle constraint.
- Documentation note that config files must never contain secrets or credentials.

## Out of Scope

- YAML format (deferred).
- Config settings for unimplemented features (command chaining delimiter, case-sensitivity, expression evaluation, etc.).
- Inline scan target declarations in the config (type/assembly names as strings require reflection and complicate AOT safety).
- Live-reload or file-watching.
- Config serialisation / writing back to disk.
- `Initialize(string filePath)` shorthand overload on `CommandSystem` — consumers call `CommandConfig.FromFile()` explicitly, which keeps error handling visible and avoids overload ambiguity with the existing `string`-accepting API surface.

## Requirements

1. **`CommandConfig` class**: A public class in the `kmCommands` namespace. Holds `HistoryCapacity` (int) and `DevMode` (bool) with coded defaults matching the existing `Initialize()` defaults.
2. **`CommandConfig.FromJson(string json)`**: Static factory. Parses the provided JSON string. Returns a `ConfigResult<CommandConfig>` (or equivalent) carrying either:
   - A populated `CommandConfig` and zero or more warning strings (unknown keys), or
   - A failure with a descriptive error message (malformed JSON, wrong type for a known key).
3. **`CommandConfig.FromFile(string filePath)`**: Static factory. Reads the file at `filePath` and delegates to `FromJson`. File-not-found or read errors are reported as a failure in the returned result.
4. **`CommandSystem.Initialize(CommandConfig config)`**: New overload. Applies `config.HistoryCapacity` and `config.DevMode`. Behaviour is identical to calling `Initialize(historyCapacity: config.HistoryCapacity, devMode: config.DevMode)` directly. Calling this after the system is already initialised is a no-op (consistent with all other `Initialize()` overloads). Null config is treated as a no-op or ArgumentNull — design to decide which is cleaner.
5. **Unknown-key warnings**: Each unrecognised JSON key at the top level produces one warning string in the `ConfigResult`. Warnings do not prevent the config from being applied.
6. **Malformed / type-mismatch errors**: A non-parseable JSON document, or a value of the wrong type for a known key (e.g. `"devMode": 42`), causes `ConfigResult` to carry a failure message. The `CommandConfig` produced is null or otherwise unusable.
7. **Coded defaults**: A `CommandConfig` constructed with `new CommandConfig()` (no parsing) has the same defaults as calling `Initialize()` with no arguments.
8. **No third-party dependencies**: The JSON parser must be a minimal hand-rolled or built-in implementation. No `Newtonsoft.Json`, `System.Text.Json`, or other package.
9. **AOT/IL2CPP safety**: No reflection-based generic deserialisation, no `dynamic`, no expression trees. The parser references only known property names by string comparison.
10. **`Shutdown()` resets config state**: After `Shutdown()`, `Initialize(CommandConfig config)` can be called again with a fresh config.

## Acceptance Overview

- A consumer can write a `commands.json` file with `{ "historyCapacity": 128, "devMode": true }`, call `CommandConfig.FromFile("commands.json")`, and pass the result to `CommandSystem.Initialize(config)` to start the system with those settings.
- A config file with only `{ "historyCapacity": 256 }` initialises with capacity 256 and `devMode` defaulting to `false`.
- An empty config object `{}` initialises with all defaults — identical to calling plain `Initialize()`.
- A config with `{ "historyCapacity": 64, "unknownKey": "foo" }` produces a successful `ConfigResult` with one warning for `unknownKey`; config is applied.
- A malformed JSON string produces a failed `ConfigResult` with a descriptive error; `Initialize(config)` must not be called (or is a no-op if called with a null/invalid config).
- A file path that does not exist produces a failed `ConfigResult` with a clear file-not-found error message.
- Existing `Initialize()` overloads are unaffected and still work as before.

## Testing Expectations

- Unit tests: Required
- Notes: All key behaviours can be validated without Unity:
  - `FromJson` with valid, minimal, full, and unknown-key inputs
  - `FromJson` with malformed JSON and wrong-type values
  - `FromFile` with a missing path
  - `Initialize(CommandConfig)` applying capacity and devMode settings correctly
  - `Initialize(CommandConfig)` idempotency (already-initialised no-op)
  - Default values of `new CommandConfig()` match `DefaultHistoryCapacity` and `false`

## Open Questions

- None — scope is locked per user answers.

## PR Scope

This work is intended to ship in one pull request with multiple commits.
