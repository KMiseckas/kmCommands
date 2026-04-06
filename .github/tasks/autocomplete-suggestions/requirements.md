# Autocomplete / Command Suggestions

## Status

Proposed

## Branch

- Name: `feature/autocomplete-suggestions`
- Rationale: `feat_` — new capability; adds a `GetSuggestions` API and `ISuggestionMatcher` extension point to the library

## Summary

This feature adds a command suggestion API to kmCommands that enables a Unity UI layer (or any consumer) to retrieve ranked, typed suggestions from a partial input string. The library handles all registry lookup and matching; the consumer only needs to render the results. The API is available on both the live `CommandSystem` and on `CommandMetadataSnapshot` snapshots.

---

## Goals

- Provide `GetSuggestions(string prefix)` on `CommandSystem` and on `CommandMetadataSnapshot`.
- Return rich suggestion objects that include command name, parameter list, and description — everything a UI needs to show a dropdown or inline hint.
- Ship a built-in case-insensitive prefix matcher with no third-party dependencies.
- Allow consumers to replace or extend matching via an injectable `ISuggestionMatcher` interface (e.g. fuzzy match, ranked scoring).
- Never return null; always return an array (empty when there are no matches).
- Keep the hot path allocation-minimal and IL2CPP/AOT-safe.

---

## In Scope

- A new public `CommandSuggestion` readonly struct that pairs a command name, its `CommandParameterInfo[]`, and its description string (nullable/empty when not set).
- A new public `ISuggestionMatcher` interface with a single matching method that receives the prefix and a read-only view of available command names, and returns an ordered list of matched names.
- `CommandSystem.GetSuggestions(string prefix)` — queries the live registry using the active or default matcher.
- `CommandSystem.GetSuggestions(string prefix, ISuggestionMatcher matcher)` — same, but uses the caller-supplied matcher for this call only (no global state change).
- `CommandMetadataSnapshot.GetSuggestions(string prefix)` — queries the snapshot using the built-in prefix matcher.
- `CommandMetadataSnapshot.GetSuggestions(string prefix, ISuggestionMatcher matcher)` — same, using caller-supplied matcher.
- `CommandSystem.SetSuggestionMatcher(ISuggestionMatcher matcher)` — registers a global matcher; pass `null` to revert to the built-in default.
- Built-in `PrefixSuggestionMatcher` — internal sealed class implementing `ISuggestionMatcher`; case-insensitive, ordinal prefix match; results ordered alphabetically by command name.
- Correct empty-result behavior before `Initialize()` and after `Shutdown()` (returns empty array, never throws).
- `GetSuggestions` with a null or empty prefix returns all registered commands (sorted alphabetically) — enables "show all" use cases.
- All public types target `netstandard2.0`.

## Out of Scope

- Unity UI, rendering, dropdown widgets, or any UnityEngine dependency.
- Fuzzy matching, scored ranking, or any non-prefix algorithm — the `ISuggestionMatcher` interface exists so consumers can supply these; the library ships only a prefix matcher.
- Asynchronous or streaming suggestion APIs.
- Suggestion filtering by DevMode flag (all registered commands, including dev-only ones active in the registry, are eligible).
- Per-parameter token suggestions (e.g. suggesting enum values for an argument) — only command-level name suggestions are in scope.
- Changes to how commands are registered or executed.
- Changes to existing public API signatures.
- Persistence or serialization of suggestion results.

---

## Requirements

### Functional Requirements

**FR-1 — `CommandSuggestion` struct**
A new public readonly struct `CommandSuggestion` must be introduced.
It must expose:
- `CommandName` (string) — the registered command name.
- `Parameters` (`CommandParameterInfo[]`) — the command's parameter list; never null (use empty array when there are no parameters).
- `Description` (string) — the command's description; empty string when no description was registered (never null).

**FR-2 — `ISuggestionMatcher` interface**
A new public interface `ISuggestionMatcher` must be introduced.
It must define a single method that accepts a prefix string and a read-only collection of all available command names, and returns an ordered list (or array) of matched command names.
The interface must be IL2CPP-safe and must not use generics on the method signature in a way that requires runtime specialization.

**FR-3 — Built-in prefix matcher**
The library must ship a built-in implementation of `ISuggestionMatcher`.
This implementation must:
- Match commands whose names start with the given prefix (case-insensitive, ordinal comparison).
- When the prefix is null or empty string, return all command names.
- Return matched names in ascending alphabetical order (case-insensitive, ordinal).
- Allocate only the result collection; no per-call intermediate allocations beyond a temporary list used to collect matches.

**FR-4 — `CommandSystem.GetSuggestions(string prefix)`**
`CommandSystem` must expose `GetSuggestions(string prefix)` returning `CommandSuggestion[]`.
- Uses the globally registered matcher if one has been set; otherwise uses the built-in prefix matcher.
- Returns an empty (non-null) array when called before `Initialize()` or after `Shutdown()`.
- Returns an empty (non-null) array when no commands match.
- Is thread-safe with respect to reads on the live registry (same guarantees as existing `GetCommandNames()` / `GetSnapshot()`).

**FR-5 — `CommandSystem.GetSuggestions(string prefix, ISuggestionMatcher matcher)`**
`CommandSystem` must expose a second `GetSuggestions` overload that accepts an explicit `ISuggestionMatcher`.
- Uses the supplied matcher for this call only; does not mutate global state.
- If `matcher` is null, falls back to the globally registered matcher or the built-in default.
- Same empty-result guarantees as FR-4.

**FR-6 — `CommandSystem.SetSuggestionMatcher(ISuggestionMatcher matcher)`**
`CommandSystem` must expose `SetSuggestionMatcher(ISuggestionMatcher matcher)`.
- Replaces the global matcher used by `GetSuggestions(prefix)`.
- Passing `null` removes the global matcher and reverts to the built-in default.
- Safe to call before `Initialize()`, between `Initialize()` and `Shutdown()`, and after `Shutdown()`.
- `Shutdown()` resets the global matcher to null (built-in default restored on next call).

**FR-7 — `CommandMetadataSnapshot.GetSuggestions(string prefix)`**
`CommandMetadataSnapshot` must expose `GetSuggestions(string prefix)` returning `CommandSuggestion[]`.
- Uses only the built-in prefix matcher (snapshot has no globally registered matcher).
- Returns an empty (non-null) array when the snapshot is empty or no commands match.
- The `CommandMetadataSnapshot.Empty` singleton must return an empty array.

**FR-8 — `CommandMetadataSnapshot.GetSuggestions(string prefix, ISuggestionMatcher matcher)`**
`CommandMetadataSnapshot` must expose a second `GetSuggestions` overload accepting an explicit `ISuggestionMatcher`.
- Uses the supplied matcher for this call only.
- If `matcher` is null, falls back to the built-in prefix matcher.
- Same empty-result guarantees as FR-7.

**FR-9 — Description inclusion**
Each `CommandSuggestion` result must include the command's description if one was registered.
- `CommandSystem.GetSuggestions` must source descriptions from the live registry.
- `CommandMetadataSnapshot.GetSuggestions` must source descriptions via existing `TryGetDescription` logic.

**FR-10 — Null/empty prefix behavior**
When the prefix argument is null or empty string, `GetSuggestions` must return suggestions for all registered commands (subject to matching sort order), not an empty array.

**FR-11 — Matching is name-only**
The `ISuggestionMatcher` contract receives and returns command names only. Resolution of `CommandParameterInfo[]` and descriptions into `CommandSuggestion` structs is performed by the library after the matcher returns, not by the matcher itself.

---

### Non-Functional Requirements

**NFR-1 — No LINQ in hot path**
The built-in matcher and the result-building path in `CommandSystem` and `CommandMetadataSnapshot` must not use LINQ at runtime. Manual loops and array operations only.

**NFR-2 — Allocation discipline**
On a successful call returning N results, allocations must be limited to:
- One `CommandSuggestion[]` of length N (the return value).
- Any internal temporary collection used to gather matched names (reuse via a pooled or stack-local `List<string>` is acceptable).
No per-element object allocations.

**NFR-3 — IL2CPP / AOT safety**
All new types and members must be IL2CPP/AOT-safe:
- No `System.Reflection.Emit`, no `dynamic`, no `Delegate.CreateDelegate` with open generics at match time.
- `ISuggestionMatcher` method signature must not require runtime generic specialization.
- `CommandSuggestion` must be a `readonly struct` to avoid boxing when stored in arrays.

**NFR-4 — No UnityEngine dependency**
No new source file under `src/` may reference or import `UnityEngine` or any Unity namespace.

**NFR-5 — `netstandard2.0` target**
All new source must compile cleanly against `netstandard2.0`.

**NFR-6 — Public API stability**
No existing public member signatures on `CommandSystem`, `CommandMetadataSnapshot`, or any other current public type may be changed or removed. All additions must be additive.

**NFR-7 — Lifecycle safety**
All new `CommandSystem` members must handle pre-`Initialize()` and post-`Shutdown()` states gracefully (return safe defaults; never throw `InvalidOperationException` or `NullReferenceException`).

---

## Acceptance Overview

- `CommandSystem.GetSuggestions("he")` returns suggestions for all commands whose names start with `"he"` (case-insensitive), each containing the correct `Parameters` and `Description`.
- `CommandSystem.GetSuggestions(string.Empty)` (or `null`) returns suggestions for every registered command.
- Calling `GetSuggestions` before `Initialize()` returns an empty array without throwing.
- Calling `GetSuggestions` after `Shutdown()` returns an empty array without throwing.
- A consumer-supplied `ISuggestionMatcher` (e.g. one that returns commands in reverse order) is respected when passed to the two-argument overloads.
- `SetSuggestionMatcher` replaces the default for subsequent no-arg calls; passing `null` restores default behaviour.
- `Shutdown()` resets the global matcher so subsequent calls use the built-in default.
- `CommandMetadataSnapshot.GetSuggestions` behaves equivalently on a captured snapshot, including on `CommandMetadataSnapshot.Empty`.
- `CommandSuggestion.Parameters` is never null; `CommandSuggestion.Description` is never null.
- No `NullReferenceException` for any null string prefix input.

---

## Testing Expectations

- **Unit tests: Required**
- Tests must be added to `tests/kmCommands.Tests/` (NUnit, `net8.0`).
- A new dedicated test file `SuggestionTests.cs` is expected.
- Minimum coverage areas:
  - Prefix match returns correct subset of registered commands.
  - Prefix match is case-insensitive.
  - Null/empty prefix returns all commands (sorted).
  - No-match prefix returns empty array (not null).
  - Pre-`Initialize()` call returns empty array.
  - Post-`Shutdown()` call returns empty array.
  - Two-argument overload uses the supplied matcher (not the global default).
  - `SetSuggestionMatcher` affect subsequent default calls; `null` reverts to built-in.
  - `Shutdown()` resets global matcher.
  - `CommandMetadataSnapshot.GetSuggestions` mirrors `CommandSystem` behavior on a captured snapshot.
  - `CommandMetadataSnapshot.Empty.GetSuggestions` returns empty array.
  - `CommandSuggestion.Parameters` is never null for a zero-parameter command.
  - `CommandSuggestion.Description` is never null for a command with no registered description.
  - A custom `ISuggestionMatcher` returning names in an arbitrary order produces `CommandSuggestion[]` in that same order.
  - Description field is correctly populated when a description is registered.

---

## Open Questions

1. **Sort stability with custom matchers** — When a consumer-supplied `ISuggestionMatcher` returns names in a custom order, the library must preserve that order in the resulting `CommandSuggestion[]`. This is a stated requirement (FR-11 + acceptance), but needs to be confirmed that the design does not silently re-sort after the matcher returns.

2. **Thread safety scope** — Existing `GetCommandNames()` does not document explicit thread-safety guarantees. `GetSuggestions` should offer the same (not stronger) guarantee. The design doc should clarify whether the internal name-list snapshot taken before calling the matcher must be protected by the same lock (if any) used by the registry.

3. **`ISuggestionMatcher` null handling contract** — FR-5 and FR-8 state: if `matcher` is null, fall back to global/default. An alternative is to throw `ArgumentNullException` early. The current requirement captures the lenient behaviour; the design step should confirm which is more appropriate for the public API contract.

---

## PR Scope

- This work is intended to ship in one pull request with multiple commits.
