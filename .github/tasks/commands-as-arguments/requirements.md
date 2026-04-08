# Commands as Command Arguments

## Status

Draft

## Branch

- Name: `feat_commands-as-arguments`
- Rationale: `feat_` — new capability enabling nested command invocation as argument values

## Summary

Allow a command invocation to be used as an argument to another command. The inner command is wrapped in a delimiter pair (prefix + suffix) within the args array, executed first, and its return value is passed as the resolved argument to the outer command. Nesting depth is configurable with a default of 4.

## Goals

- Enable consumers to write `destroy $(getPlayer 1)$` (or equivalent delimiter-wrapped form) rather than having to pre-execute the inner command manually and pass its result as a literal.
- Keep the existing `Execute(name, string[] args)` call shape — no new raw-string `Execute` overload required.
- Integrate with the autocomplete/suggestion API so the UI can offer inner command suggestions after the opening delimiter is typed.
- Record each inner command execution in history independently, the same way a direct `Execute()` call would be.
- Prevent unbounded recursion via a configurable depth limit (default 4) set at `Initialize()` time.

## In Scope

- A delimited token syntax (`prefix`…`suffix`) that marks an element of the `string[] args` array as an inner command invocation rather than a literal value.
- Recursive resolution: a nested call can itself contain delimited inner commands, up to the depth limit.
- Pre-execution validation: type compatibility between the inner command's declared return type and the outer parameter's expected type is checked before execution begins; a mismatch is returned as a structured error without invoking either command.
- Structured error propagation: any failure during inner command resolution (command not found, argument error, callback exception, or depth exceeded) halts outer-command execution and is surfaced in the outer `ExecutionResult` — the outer command callback is never invoked on partial resolution.
- History recording for each inner command, identical to a direct `Execute()` invocation (entry per inner command, own timestamp, own status).
- Autocomplete/suggestion support: when the consumer calls `GetSuggestions(prefix)` and the active token starts with the opening delimiter, the suggestion engine treats the content after the delimiter as a nested command prefix and returns matching inner command suggestions.
- Configurable depth limit via `CommandConfig` JSON key (e.g. `nestedCommandDepth`) and matching `Initialize()` parameter path; clamped to ≥ 1; default 4.
- Default delimiter pair defined as a library constant (exact characters decided at design time; must not conflict with existing argument characters such as spaces, quotes, or the dot instance-key separator).

## Out of Scope

- A new `Execute(string rawInput)` overload that parses a combined name + args string.
- Command Chaining (semicolon-delimited multi-command execution) — that is a separate feature.
- Expression Evaluation — arithmetic in arguments (`2+2`) is a separate feature.
- Consumer-configurable delimiter characters (delimiter pair is fixed by the library; design time decision).
- Broadcasting inner commands to multiple instances.
- Any UI rendering or input-handling changes — those are the consumer's responsibility.

## Requirements

- The library MUST define a fixed delimiter pair (prefix token and suffix token) to identify a nested command invocation within an argument position.
- When an arg element begins with the opening delimiter and ends with the closing delimiter, the library MUST extract the inner command name and its own arguments, resolve them recursively, and substitute the return value into the outer argument position before type conversion.
- The library MUST validate type compatibility (inner command declared return type vs. outer parameter declared type) before executing any command in the chain. A type mismatch MUST produce a structured `ExecutionResult` failure; neither the inner nor the outer command callback is invoked.
- The library MUST enforce the nesting depth limit. Attempts that would exceed the limit MUST return a structured `ExecutionResult` failure; no partial execution occurs.
- The depth limit MUST be configurable at `Initialize()` time via `CommandConfig` (new JSON key) and clamped to ≥ 1. The default value MUST be 4 when not specified.
- Each inner command execution MUST be recorded in the history buffer as an independent `CommandHistoryEntry`, with its own timestamp and status, exactly as a direct `Execute()` call would be.
- The outer command entry MUST also be recorded in history (including failure outcomes), consistent with the existing history recording contract.
- `GetSuggestions(prefix)` MUST detect when the active token starts with the opening delimiter and return suggestions scoped to inner command names based on the content following the delimiter.
- All parsing and resolution MUST be AOT/IL2CPP-safe — no runtime code generation, no `System.Linq.Expressions`, no `Emit`.
- The feature MUST be covered by deterministic unit tests in `tests/kmCommands.Tests/`.

## Acceptance Overview

- `Execute("destroy", new[] { "$(getPlayer 1)$" })` (or equivalent delimiter form) resolves `getPlayer` with arg `"1"`, takes its return value, passes it to `destroy`, and returns a success `ExecutionResult`. Both inner and outer entries appear in history.
- A type mismatch between inner return type and outer parameter returns a failure `ExecutionResult` and neither callback fires; no history entry is recorded for either.
- Nesting beyond the depth limit returns a structured depth-exceeded failure; no command in the chain executes.
- `GetSuggestions("$")` (opening delimiter alone) returns all registered command names as nested-command suggestions.
- `GetSuggestions("$(get")` returns all commands whose names start with `"get"`.
- Setting `nestedCommandDepth = 2` in config and nesting 3 levels deep fails deterministically.
- Depth default (4) applies when `nestedCommandDepth` is absent from config.

## Testing Expectations

- Unit tests: Required
- Notes: All resolution paths (success, inner-not-found, type mismatch, depth exceeded, partial nesting) must have deterministic test coverage. Suggestion behavior with the opening delimiter must also be tested.

## Open Questions

- Exact delimiter characters are to be determined at design time. The pair must not conflict with spaces, the dot instance-key separator, or any characters valid in command names or string arguments. Candidates include `$(…)$`, `{(…)}`, or `[[…]]`.
- Should `nestedCommandDepth = 0` be clamped to 1 (allow one level) or treated as "nesting disabled" (reject any delimited token)? Currently specified as clamped to ≥ 1; clarify at design time if a "disabled" state is wanted.
- Inner commands that declare `void` / `null` return values are not useful as arguments. Design should decide whether to reject void-return inner commands at validation time or at depth-resolution time, and what error code to expose.

## PR Scope

- This work is intended to ship in one pull request with multiple commits.
