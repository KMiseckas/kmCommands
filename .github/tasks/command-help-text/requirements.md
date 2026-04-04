# Requirements: Command Description / Help Text

**Status:** Draft
**Feature:** Command Description / Help Text
**Slug:** `command-help-text`
**Branch:** `feat_command-help-text`

---

## Summary

Allow a human-readable description string to be attached to a command at registration time. The description is optional — omitting it is valid and backward-compatible. Once registered, the description must be accessible through all existing discovery APIs (`GetCommandNames` is unaffected; `TryGetCommandParameters` is unaffected; `GetSnapshot()` and `CommandMetadataSnapshot` must expose it; `TryGetCommandParameters` on the snapshot is unaffected). A Unity UI layer (or any consumer) can then use the description to render help text, tooltips, or autocomplete summaries without any changes to the core library.

---

## Branch

- **Name:** `feat_command-help-text`
- **Base:** `origin/main`
- **Rationale:** `feat_` — this is a new capability added to the public API surface.

---

## Goals

1. Commands may carry an optional human-readable description string.
2. The description is available at discovery time via `CommandMetadataSnapshot`.
3. Both registration paths (manual `Register()` and attribute-based `[Command]`) can supply a description.
4. Omitting a description is always valid and does not change existing behavior.
5. No breaking changes to any existing public API.

---

## In Scope

- Add an optional `Description` property to `CommandSystem.Register()` — either through an overload or an updated signature — so callers can supply a description string alongside the name, parameters, and callback.
- Add a `Description` property (optional named argument) to the `[Command]` attribute so attribute-scanned commands can carry a description.
- Store the description on the internal `CommandDefinition` so it travels through the registry.
- Expose `Description` from `CommandMetadataSnapshot`: the snapshot must allow callers to retrieve the description for a given command name, and the snapshot's per-command entry must include it.
- `GetSnapshot()` on `CommandSystem` must capture descriptions at snapshot time.
- A `null` or missing description is exposed as `null` (not an empty string) — callers can test for `null` to detect absent descriptions.

---

## Out of Scope

- Descriptions on individual parameters (`CommandParameterInfo`) — description lives on the command, not individual parameters.
- Validation or sanitization of the description string content (length, allowed characters, etc.).
- Localization or multi-language description support.
- Any change to `CommandParameterInfo`.
- Any change to `ExecutionResult`, `RegistrationResult`, or `RegistrationError`.
- Any change to `GetCommandNames()` or `TryGetCommandParameters()` on `CommandSystem`.
- Rendering, formatting, or displaying the description string — that responsibility belongs to the Unity UI layer.
- Description indexing or search.

---

## Functional Requirements

1. **Manual registration — with description:** `CommandSystem.Register(name, parameters, callback, description)` (or equivalent overload) must accept an optional non-null description string and associate it with the registered command.
2. **Manual registration — without description:** `CommandSystem.Register(name, parameters, callback)` (the existing signature) must continue to work unchanged; the registered command's description must be `null`.
3. **Attribute registration — with description:** `[Command("name", Description = "...")]` must accept an optional description string and pass it through the attribute scanner to the registered command.
4. **Attribute registration — without description:** `[Command("name")]` (existing usage) must continue to work unchanged; the registered command's description must be `null`.
5. **Snapshot captures description:** `CommandSystem.GetSnapshot()` must include each command's description in the returned `CommandMetadataSnapshot`.
6. **Snapshot retrieval method:** `CommandMetadataSnapshot` must expose a way to retrieve the description for a named command — either via a new `TryGetDescription(name, out string description)` method or by returning a richer per-command entry that includes `Description`.
7. **Null for absent description:** When a command was registered without a description, the description value returned from the snapshot must be `null`.
8. **No mutation after registration:** The description is set at registration time and cannot be changed afterward; it is read-only.
9. **Case-insensitive lookup:** Description lookup through the snapshot must be case-insensitive on the command name, consistent with all other snapshot lookups.
10. **Empty-string description:** Registering with an empty-string description is permitted; it is stored and returned as-is (not normalized to `null`).
11. **Snapshot isolation:** Changing the description on a subsequently registered command with the same name (if that were possible) must not affect a snapshot already taken. (Follows existing snapshot isolation semantics.)

---

## Acceptance Criteria

Each criterion must be covered by at least one unit test.

### Manual Registration

1. Registering a command with a non-null, non-empty description string succeeds and the description is retrievable from a subsequent snapshot.
2. Registering a command without a description (existing two-argument `Register` overload) succeeds; the description in a subsequent snapshot is `null`.
3. Registering a command with an empty-string description succeeds; the description in a subsequent snapshot is `""`.

### Attribute Registration

4. A `[Command]` attribute with a `Description` property value set registers the command with that description; it is retrievable from a subsequent snapshot.
5. A `[Command]` attribute without a `Description` property registers the command with a `null` description.

### Snapshot / Discovery

6. `CommandMetadataSnapshot` returned by `GetSnapshot()` allows retrieval of the description for a registered command name (case-insensitive).
7. `CommandMetadataSnapshot` returns `null` as the description for a command registered without one.
8. `CommandMetadataSnapshot.Empty` (the singleton) returns a "not found" / `null` result for any description lookup.
9. Snapshot isolation: a snapshot taken before a second command is registered does not include the second command's description.

### Backward Compatibility

10. All 103 existing tests continue to pass without modification.
11. Code that calls `Register(name, parameters, callback)` (the existing three-parameter form) compiles and behaves identically — no `NotInitialized`, argument count, or other error changes.

---

## Testing Expectations

- Unit tests: **Required**
- Tests belong in `tests/kmCommands.Tests/`.
- New tests should be placed in a new focused file such as `CommandDescriptionTests.cs`.
- Coverage areas:
  - Manual registration with and without description.
  - Attribute-scanned registration with and without description.
  - `CommandMetadataSnapshot` description retrieval (found, not found, empty snapshot).
  - Case-insensitive lookup by command name on the snapshot.
  - Backward compatibility: no regression on existing command registration and execution flows.
- The NUnit test project targets `net8.0`.
- All 103 existing tests must continue to pass.

---

## Assumptions

1. The description is stored on the command definition (not on `CommandParameterInfo`), because it describes the command as a whole.
2. The preferred API shape for snapshot description access is a new `TryGetDescription(name, out string description)` method on `CommandMetadataSnapshot`, mirroring the existing `TryGetParameters` pattern. The design step may revise this.
3. The `Register()` extension will be delivered as an additional overload (preserving the existing three-parameter signature) rather than changing the existing method signature.
4. `Description` on `[Command]` is a settable property (C# named argument syntax: `[Command("name", Description = "...")]`), consistent with the existing `IsDevOnly` property pattern.
5. `AttributeScanner` must be updated to read and forward the `Description` property; this is considered in-scope.
6. Internal `CommandDefinition` will carry a `Description` field; this is an internal change and not a public API concern.
7. `CommandMetadataSnapshot` internal storage must be extended to hold descriptions alongside parameter arrays; exact design is left to the design step.

---

## Open Questions

- None at this time. Scope is clear enough to proceed to design.

---

## PR Scope

- This work is intended to ship in one pull request with multiple commits.
