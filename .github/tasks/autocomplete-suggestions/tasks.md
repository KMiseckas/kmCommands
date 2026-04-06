# Autocomplete / Command Suggestions Tasks

## Status

- [ ] Planned
- [ ] In Progress
- [ ] Completed

## Inputs

- Requirements: `.github/tasks/autocomplete-suggestions/requirements.md`
- Design: `.github/tasks/autocomplete-suggestions/design.md`

## Branch

- Name: `feature/autocomplete-suggestions`
- Rationale: New user-facing capability — adds `GetSuggestions` API and `ISuggestionMatcher` extension point. Requirements Planner selected `feature/` prefix style; preserved verbatim per branch-naming ownership rule.

## Global Execution Notes

- Work is implemented in order, task by task.
- Each completed task should result in one commit.
- Keep commits scoped to the task objective.
- All new `src/` files must begin with the required source header (see projectOverview).
- No LINQ in any new source file (NFR-1).
- No `UnityEngine` imports in any new source file (NFR-4).
- All new source must compile against `netstandard2.0` (NFR-5).
- Include doc updates in `docs/` whenever behavior, API, usage, or architecture changes.
- Keep `.github/instructions/projectOverview.instructions.md` aligned with project-level changes.
- A task checkbox may be set to complete only after all items under its `Completion Gate` are checked.
- `## Status -> Completed` may be checked only after all tasks and `## Coverage Check` items are checked.

---

## Task List

---

### T-1: Add `CommandSuggestion` Public Readonly Struct

- [ ] Not started

**Objective:**

Create the new public `CommandSuggestion` readonly struct that carries a matched command name, its parameter list, and its description. This is the return-value type of all `GetSuggestions` overloads.

**Inputs:**

- Requirements refs: FR-1, NFR-3, NFR-4, NFR-5, NFR-6
- Design refs: "Components and Responsibilities → `CommandSuggestion`", "API / Contract Sketch", "Implementation Notes → `CommandSuggestion` Constructor Visibility"

**Implementation Steps:**

1. Create `src/CommandSuggestion.cs`.
2. Add the required source header comment block at the top of the file.
3. Declare `namespace kmCommands`.
4. Declare `public readonly struct CommandSuggestion` (no class-level attributes needed).
5. Add three auto-property getters (no setters):
   - `public string CommandName { get; }` — the registered command name
   - `public CommandParameterInfo[] Parameters { get; }` — parameter list; never null per FR-1
   - `public string Description { get; }` — description text; never null per FR-1
6. Add a single `internal` constructor:
   ```csharp
   internal CommandSuggestion(string commandName, CommandParameterInfo[] parameters, string description)
   {
       CommandName = commandName;
       Parameters = parameters;
       Description = description;
   }
   ```
   The constructor is `internal` so only library code can produce valid populated instances; external consumers may obtain `default(CommandSuggestion)` (all fields null/empty, which is harmless).
7. Do not add any other members, static factories, or helper methods.

**Validation:**

- Build check: `dotnet build` from the solution root must succeed with zero errors and zero warnings after this file is added.
- Code review: confirm `readonly struct`, `internal` constructor, three get-only properties, no LINQ, no `UnityEngine`, `netstandard2.0` compatible.
- QA quick pass (`taskReviewer`): review the newly added file only.
- taskReviewer review request:
  - Review scope: `src/CommandSuggestion.cs` (new file only).
  - Primary checks: declared as `readonly struct`; constructor is `internal`; `Parameters` property type is `CommandParameterInfo[]`; `Description` property type is `string`; no LINQ; no `UnityEngine`; source header present; compiles cleanly.
  - Required evidence: successful `dotnet build` output.
  - Blocking conditions: missing `readonly`, constructor not `internal`, any LINQ/UnityEngine import, build error.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: N/A (new file — no prior review thread).

**Documentation Sync:**

- Docs to update in `docs/`: None at this step — defer to T-10.
- Update `.github/instructions/projectOverview.instructions.md` required: No (new type will be documented in T-10 after API is complete).

**Completion Gate:**

- [ ] Implementation done
- [ ] `dotnet build` passes with zero errors
- [ ] Code review confirms `readonly struct`, `internal` constructor, three properties, no LINQ/UnityEngine
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-10)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — deferred to T-10)

**Commit Note:**

- Suggested commit scope: `src/CommandSuggestion.cs`
- Suggested commit message: `feat(autocomplete): add CommandSuggestion readonly struct`

---

### T-2: Add `ISuggestionMatcher` Public Interface

- [ ] Not started

**Objective:**

Create the new public `ISuggestionMatcher` interface that defines the matching contract used by all `GetSuggestions` overloads and consumer-injectable custom matchers.

**Inputs:**

- Requirements refs: FR-2, NFR-3, NFR-4, NFR-5, NFR-6
- Design refs: "Components and Responsibilities → `ISuggestionMatcher`", "API / Contract Sketch", "IL2CPP Safety Notes"

**Implementation Steps:**

1. Create `src/ISuggestionMatcher.cs`.
2. Add the required source header comment block at the top of the file.
3. Add `using System.Collections.Generic;` — required for `IList<string>`.
4. Declare `namespace kmCommands`.
5. Declare `public interface ISuggestionMatcher`.
6. Add a single method:
   ```csharp
   IList<string> Match(string prefix, string[] commandNames);
   ```

   - `prefix` — the partial input string to match against; may be null or empty (means "return all").
   - `commandNames` — a sorted snapshot of all registered command names at call time.
   - Returns an ordered list of matched command names; may be empty; must not return null (callers guard, but implementors should not return null).
7. Do not add any other members, default implementations, or overloads.
8. Do not use any generic type parameters on the method signature (IL2CPP/AOT safety — NFR-3).

**Validation:**

- Build check: `dotnet build` must succeed with zero errors after this file is added.
- Code review: confirm non-generic method signature, no `UnityEngine`, source header present, `netstandard2.0` compatible (no `default interface member` syntax).
- QA quick pass (`taskReviewer`): review the newly added file only.
- taskReviewer review request:
  - Review scope: `src/ISuggestionMatcher.cs` (new file only).
  - Primary checks: `Match` signature uses only concrete types (`string`, `string[]`, `IList<string>`); no generic type parameters on the method; no `UnityEngine`; source header present; compiles cleanly on `netstandard2.0`.
  - Required evidence: successful `dotnet build` output.
  - Blocking conditions: generic method type parameter on `Match`; default interface member syntax; any LINQ/UnityEngine import; build error.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: N/A (new file).

**Documentation Sync:**

- Docs to update in `docs/`: None at this step — defer to T-10.
- Update `.github/instructions/projectOverview.instructions.md` required: No (deferred to T-10).

**Completion Gate:**

- [ ] Implementation done
- [ ] `dotnet build` passes with zero errors
- [ ] Code review confirms non-generic method, no default interface syntax, no LINQ/UnityEngine
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-10)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — deferred to T-10)

**Commit Note:**

- Suggested commit scope: `src/ISuggestionMatcher.cs`
- Suggested commit message: `feat(autocomplete): add ISuggestionMatcher interface`

---

### T-3: Add `PrefixSuggestionMatcher` Internal Sealed Class

- [ ] Not started

**Depends on:** T-2 (`ISuggestionMatcher` must exist)

**Objective:**

Create the built-in default `ISuggestionMatcher` implementation. This class is the stateless singleton used by `CommandSystem` and `CommandMetadataSnapshot` when no external matcher is configured.

**Inputs:**

- Requirements refs: FR-3, NFR-1, NFR-2, NFR-3, NFR-4, NFR-5
- Design refs: "Components and Responsibilities → `PrefixSuggestionMatcher`", "Implementation Notes → `PrefixSuggestionMatcher` Sort Guarantee", "Code Examples → `PrefixSuggestionMatcher` implementation sketch", "Allocation Profile Analysis", "IL2CPP Safety Notes"

**Implementation Steps:**

1. Create `src/Core/PrefixSuggestionMatcher.cs`.
2. Add the required source header comment block at the top of the file.
3. Add `using System;` and `using System.Collections.Generic;`.
4. Declare `namespace kmCommands.Core`.
5. Declare `internal sealed class PrefixSuggestionMatcher : ISuggestionMatcher`.
6. Implement `public IList<string> Match(string prefix, string[] commandNames)`:
   - If `commandNames` is null or `commandNames.Length == 0`, return `Array.Empty<string>()`.
   - Let `matchAll = string.IsNullOrEmpty(prefix)`.
   - Allocate `List<string> results = new List<string>()`.
   - Loop with `for (int i = 0; i < commandNames.Length; i++)`:
     - If `matchAll` OR `commandNames[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase)`, add `commandNames[i]` to `results`.
   - Return `results`.
   - **Do not call `results.Sort(...)` or any sort after the loop** — the input `commandNames` is pre-sorted by the registry; the output is already alpha-sorted by traversal order (design invariant from "Sort Preservation Invariant").
7. Do not add any instance fields, constructors, or other members — the class is stateless.
8. No LINQ, no reflection, no `UnityEngine` imports.

**Validation:**

- Build check: `dotnet build` must succeed with zero errors.
- Code review: confirm no sort call after loop, `OrdinalIgnoreCase` comparison, null/empty prefix treated as "match all", `Array.Empty<string>()` used for empty-input guard, no LINQ, no fields, no `UnityEngine`, source header present.
- Manual trace: mentally trace `Match("he", ["health", "help", "jump"])` → `["health", "help"]`; trace `Match("", ["health", "help", "jump"])` → `["health", "help", "jump"]`; trace `Match(null, ["health"])` → `["health"]`; trace `Match("zzz", ["health"])` → `[]`.
- QA quick pass (`taskReviewer`): review new file only.
- taskReviewer review request:
  - Review scope: `src/Core/PrefixSuggestionMatcher.cs` (new file only).
  - Primary checks: no sort after loop; `OrdinalIgnoreCase` used; `string.IsNullOrEmpty(prefix)` triggers match-all; guard for null/empty `commandNames`; no LINQ; no fields (stateless); source header present; compiles cleanly.
  - Required evidence: successful `dotnet build` output; manual trace results as documented above.
  - Blocking conditions: sort call after loop; wrong comparison type; null/empty prefix not matched to all commands; LINQ usage; build error.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: N/A (new file).

**Documentation Sync:**

- Docs to update in `docs/`: None at this step — defer to T-10.
- Update `.github/instructions/projectOverview.instructions.md` required: No (deferred to T-10).

**Completion Gate:**

- [ ] Implementation done
- [ ] `dotnet build` passes with zero errors
- [ ] No sort call after the result loop; `OrdinalIgnoreCase` confirmed
- [ ] Null/empty prefix match-all behavior confirmed by trace
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-10)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — deferred to T-10)

**Commit Note:**

- Suggested commit scope: `src/Core/PrefixSuggestionMatcher.cs`
- Suggested commit message: `feat(autocomplete): add PrefixSuggestionMatcher built-in matcher`

---

### T-4: Extend `CommandSystem` — Matcher Fields and `SetSuggestionMatcher`

- [ ] Not started

**Depends on:** T-2 (`ISuggestionMatcher`), T-3 (`PrefixSuggestionMatcher`)

**Objective:**

Add the global matcher state to `CommandSystem`: the `_defaultMatcher` static singleton, the `_suggestionMatcher` instance field, and the `SetSuggestionMatcher(ISuggestionMatcher)` public method. This is the state infrastructure that `GetSuggestions` (T-5) will rely on.

**Inputs:**

- Requirements refs: FR-6, NFR-6, NFR-7
- Design refs: "Components and Responsibilities → `CommandSystem`", "Implementation Notes → Static Default Matcher", "Implementation Notes → `SetSuggestionMatcher` Signature", "Implementation Notes → Matcher Resolution"

**Implementation Steps:**

1. Open `src/CommandSystem.cs`.
2. Locate the block of `private` instance fields (near the top of the class, alongside `_registry`, `_instanceRegistry`, etc.).
3. Add the following two fields in that block:
   ```csharp
   private static readonly ISuggestionMatcher _defaultMatcher = new Core.PrefixSuggestionMatcher();
   private ISuggestionMatcher _suggestionMatcher;
   ```

   - `_defaultMatcher` is `static readonly` — one allocation per AppDomain load, shared by all `CommandSystem` instances.
   - `_suggestionMatcher` is a nullable instance field; `null` means "use default".
4. Add the new public method after the existing public API methods (placement: near other setter-style helpers or lifecycle members):
   ```csharp
   public void SetSuggestionMatcher(ISuggestionMatcher matcher)
   {
       _suggestionMatcher = matcher;
   }
   ```

   - Accepts `null` (reverts to built-in default on the next `GetSuggestions` call — FR-6).
   - No `IsInitialized` guard — this method is lifecycle-safe to call at any time (NFR-7).
   - No other logic needed.
5. Do not add XML doc comments unless a matching style is already used on other public methods in this file (mirror existing conventions).

**Validation:**

- Build check: `dotnet build` must succeed with zero errors.
- Code review: confirm `_defaultMatcher` is `private static readonly`; confirm `_suggestionMatcher` is `private` (non-static); confirm `SetSuggestionMatcher` assigns without any guard; source file retains its existing header.
- QA quick pass (`taskReviewer`): focused review on the two new fields and the one new method within `CommandSystem.cs`.
- taskReviewer review request:
  - Review scope: additions to `src/CommandSystem.cs` — two new fields and `SetSuggestionMatcher`.
  - Primary checks: `_defaultMatcher` is `private static readonly PrefixSuggestionMatcher` via `ISuggestionMatcher`; `_suggestionMatcher` is instance-level nullable; `SetSuggestionMatcher` is unconditional (no `IsInitialized` guard); no LINQ; no accidental changes to existing members.
  - Required evidence: successful `dotnet build` output; diff showing only additive changes.
  - Blocking conditions: `_defaultMatcher` not static/readonly; `_suggestionMatcher` accidentally static; `SetSuggestionMatcher` guards on `IsInitialized`; any existing member signature changed; build error.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: check for any prior review comments on `CommandSystem.cs`; resolve before closing.

**Documentation Sync:**

- Docs to update in `docs/`: None at this step — defer to T-10.
- Update `.github/instructions/projectOverview.instructions.md` required: No (deferred to T-10).

**Completion Gate:**

- [ ] Implementation done
- [ ] `dotnet build` passes with zero errors
- [ ] `_defaultMatcher` is `private static readonly`; `_suggestionMatcher` is instance-level
- [ ] `SetSuggestionMatcher` has no `IsInitialized` guard
- [ ] No existing members in `CommandSystem.cs` changed
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-10)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — deferred to T-10)

**Commit Note:**

- Suggested commit scope: `src/CommandSystem.cs`
- Suggested commit message: `feat(autocomplete): add suggestion matcher fields and SetSuggestionMatcher to CommandSystem`

---

### T-5: Extend `CommandSystem` — `GetSuggestions` Overloads

- [ ] Not started

**Depends on:** T-1 (`CommandSuggestion`), T-2 (`ISuggestionMatcher`), T-3 (`PrefixSuggestionMatcher`), T-4 (matcher fields)

**Objective:**

Add the two public `GetSuggestions` overloads to `CommandSystem`. These are the primary API surface for live-registry suggestion queries.

**Inputs:**

- Requirements refs: FR-4, FR-5, FR-9, FR-10, FR-11, NFR-1, NFR-2, NFR-3, NFR-7
- Design refs: "Data Flow / Control Flow → `CommandSystem.GetSuggestions`", "Code Examples → `CommandSystem.GetSuggestions` implementation sketch", "Implementation Notes → Null Return Guard on Matcher", "Implementation Notes → Sort Preservation Invariant", "Implementation Notes → `Parameters` Null Guard"

**Implementation Steps:**

1. Open `src/CommandSystem.cs` (continued from T-4).
2. Add the single-argument overload (delegates to the two-argument overload):
   ```csharp
   public CommandSuggestion[] GetSuggestions(string prefix)
   {
       return GetSuggestions(prefix, null);
   }
   ```
3. Add the two-argument overload:

   ```csharp
   public CommandSuggestion[] GetSuggestions(string prefix, ISuggestionMatcher matcher)
   {
       if (!IsInitialized)
           return Array.Empty<CommandSuggestion>();

       ISuggestionMatcher effective = matcher ?? _suggestionMatcher ?? _defaultMatcher;
       string[] names = _registry.GetAllNames();
       IList<string> matched = effective.Match(prefix, names);

       if (matched == null || matched.Count == 0)
           return Array.Empty<CommandSuggestion>();

       CommandSuggestion[] results = new CommandSuggestion[matched.Count];
       for (int i = 0; i < matched.Count; i++)
       {
           string name = matched[i];
           CommandParameterInfo[] parameters = Array.Empty<CommandParameterInfo>();
           string description = string.Empty;

           if (_registry.TryGetCommand(name, out Core.CommandDefinition def))
           {
               parameters = def.Parameters;
               description = def.Description ?? string.Empty;
           }

           results[i] = new CommandSuggestion(name, parameters, description);
       }

       return results;
   }
   ```

   - Key invariants to preserve exactly as written:
     - `!IsInitialized` early return with `Array.Empty<CommandSuggestion>()` (FR-4, NFR-7).
     - Resolution chain: `matcher ?? _suggestionMatcher ?? _defaultMatcher` (FR-5).
     - Null guard on `matched` return from matcher (design note).
     - Result loop iterates `matched` in index order — **no sort after matcher returns** (FR-11).
     - `TryGetCommand` fallback ensures `parameters` and `description` are never null (FR-1, FR-9).

4. Do not add LINQ, `Array.Sort`, or any sorting after the loop.
5. Do not change any existing method signatures or remove any existing members.

**Validation:**

- Build check: `dotnet build` must succeed with zero errors.
- Code review: confirm `!IsInitialized` guard; confirm resolution chain order; confirm null guard on `matched`; confirm result loop iterates in index order with no post-sort; confirm `Array.Empty<CommandSuggestion>()` used for all empty-result paths; confirm no LINQ.
- QA quick pass (`taskReviewer`): review additions to `CommandSystem.cs`.
- taskReviewer review request:
  - Review scope: two new `GetSuggestions` methods added to `src/CommandSystem.cs`.
  - Primary checks: pre-init guard present; resolution chain `matcher ?? _suggestionMatcher ?? _defaultMatcher`; matcher null-return guard; no sort after loop; `TryGetCommand` fallback ensures neither `parameters` nor `description` is null in any result; `Array.Empty<CommandSuggestion>()` for all empty paths; no LINQ; no changes to existing members.
  - Required evidence: successful `dotnet build` output; diff showing only additive changes.
  - Blocking conditions: missing pre-init guard; wrong resolution chain order; missing null-return guard on matcher result; sort call after loop; null `parameters` or `description` possible in result; LINQ usage; any existing member altered; build error.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve any open notes from T-4 review on this file.

**Documentation Sync:**

- Docs to update in `docs/`: None at this step — defer to T-10.
- Update `.github/instructions/projectOverview.instructions.md` required: No (deferred to T-10).

**Completion Gate:**

- [ ] Implementation done
- [ ] `dotnet build` passes with zero errors
- [ ] Pre-init guard returns `Array.Empty<CommandSuggestion>()`
- [ ] Matcher resolution chain: `matcher ?? _suggestionMatcher ?? _defaultMatcher`
- [ ] Null/empty guard on matcher return value
- [ ] No sort after result loop
- [ ] `parameters` and `description` never null in any result struct
- [ ] No existing members in `CommandSystem.cs` changed
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-10)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — deferred to T-10)

**Commit Note:**

- Suggested commit scope: `src/CommandSystem.cs`
- Suggested commit message: `feat(autocomplete): add GetSuggestions overloads to CommandSystem`

---

### T-6: Extend `CommandMetadataSnapshot` — `GetSuggestions` Overloads

- [ ] Not started

**Depends on:** T-1 (`CommandSuggestion`), T-2 (`ISuggestionMatcher`), T-3 (`PrefixSuggestionMatcher`)

_(T-6 is independent of T-4 and T-5 and may be worked on in parallel if desired, but should be committed sequentially.)_

**Objective:**

Add the two public `GetSuggestions` overloads to `CommandMetadataSnapshot`, plus the private `_defaultMatcher` static used by the snapshot. This provides snapshot-local suggestion queries without any dependency on `CommandSystem` instance state.

**Inputs:**

- Requirements refs: FR-7, FR-8, FR-9, FR-10, FR-11, NFR-1, NFR-2, NFR-3, NFR-7
- Design refs: "Data Flow / Control Flow → `CommandMetadataSnapshot.GetSuggestions`", "Code Examples → `CommandMetadataSnapshot.GetSuggestions` implementation sketch", "Implementation Notes → Static Default Matcher", "Implementation Notes → Sort Preservation Invariant"

**Implementation Steps:**

1. Open `src/CommandMetadataSnapshot.cs`.
2. Add a `using System.Collections.Generic;` import if not already present (it is already present per existing file).
3. In the field declarations block, add the static default matcher:
   ```csharp
   private static readonly ISuggestionMatcher _defaultMatcher = new Core.PrefixSuggestionMatcher();
   ```

   - This is **separate from** `CommandSystem._defaultMatcher` — two distinct `PrefixSuggestionMatcher` instances, one per class. This keeps `CommandMetadataSnapshot` independent of `CommandSystem` state (design rationale: "Two separate `_defaultMatcher` statics").
4. Add the single-argument overload (delegates to the two-argument overload):
   ```csharp
   public CommandSuggestion[] GetSuggestions(string prefix)
   {
       return GetSuggestions(prefix, null);
   }
   ```
5. Add the two-argument overload:

   ```csharp
   public CommandSuggestion[] GetSuggestions(string prefix, ISuggestionMatcher matcher)
   {
       ISuggestionMatcher effective = matcher ?? _defaultMatcher;
       IList<string> matched = effective.Match(prefix, CommandNames);

       if (matched == null || matched.Count == 0)
           return Array.Empty<CommandSuggestion>();

       CommandSuggestion[] results = new CommandSuggestion[matched.Count];
       for (int i = 0; i < matched.Count; i++)
       {
           string name = matched[i];

           CommandParameterInfo[] parameters;
           if (!_entries.TryGetValue(name, out parameters) || parameters == null)
               parameters = Array.Empty<CommandParameterInfo>();

           string description;
           if (!_descriptions.TryGetValue(name, out description) || description == null)
               description = string.Empty;

           results[i] = new CommandSuggestion(name, parameters, description);
       }

       return results;
   }
   ```

   - Key invariants:
     - No `IsInitialized` guard needed — snapshot is self-contained; the `Empty` singleton naturally contains empty `CommandNames`, so the matcher returns empty and the method returns `Array.Empty<CommandSuggestion>()` (FR-7).
     - Resolution chain: `matcher ?? _defaultMatcher` only (no global matcher field on snapshot — FR-7, FR-8).
     - Null guard on `matched`.
     - Result loop iterates `matched` in index order — **no sort after matcher returns** (FR-11).
     - Double-guard `|| parameters == null` on `_entries.TryGetValue` handles stale/invalid names from custom matchers (design "Parameters Null Guard").

6. Do not add LINQ, `Array.Sort`, or any sorting after the loop.
7. Do not change any existing method signatures or members.

**Validation:**

- Build check: `dotnet build` must succeed with zero errors.
- Code review: confirm `_defaultMatcher` is `private static readonly`; confirm no `IsInitialized` guard (not applicable here); confirm resolution chain `matcher ?? _defaultMatcher`; confirm null guard on `matched`; confirm result loop in index order with no post-sort; confirm double-null guard on parameters; confirm no LINQ; confirm no changes to existing members.
- Confirm `CommandMetadataSnapshot.Empty` behavior: `Empty` is constructed with `Array.Empty<string>()` for `CommandNames` — the matcher will receive an empty array and return empty, so `GetSuggestions` will return `Array.Empty<CommandSuggestion>()` without entering the result-building loop.
- QA quick pass (`taskReviewer`): review additions to `CommandMetadataSnapshot.cs`.
- taskReviewer review request:
  - Review scope: additions to `src/CommandMetadataSnapshot.cs` — static field and two new methods.
  - Primary checks: `_defaultMatcher` is separate static from `CommandSystem`'s; no `IsInitialized` guard; resolution chain `matcher ?? _defaultMatcher` (no `_suggestionMatcher`); null guard on matcher return; no sort after loop; double-null guard on parameters; `Empty` singleton correctly returns empty array; no LINQ; no changes to existing members.
  - Required evidence: successful `dotnet build` output; diff showing only additive changes.
  - Blocking conditions: `_defaultMatcher` referencing `CommandSystem` state; wrong resolution chain; LINQ; sort after loop; missing null guard; any existing member changed; build error.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve any open notes on this file.

**Documentation Sync:**

- Docs to update in `docs/`: None at this step — defer to T-10.
- Update `.github/instructions/projectOverview.instructions.md` required: No (deferred to T-10).

**Completion Gate:**

- [ ] Implementation done
- [ ] `dotnet build` passes with zero errors
- [ ] `_defaultMatcher` is independent static (not referencing `CommandSystem`)
- [ ] Resolution chain is `matcher ?? _defaultMatcher` (no global matcher field)
- [ ] Null/empty guard on matcher return value
- [ ] No sort after result loop
- [ ] Double-null guard on `_entries.TryGetValue` output
- [ ] `Empty` singleton behavior confirmed (returns empty array)
- [ ] No existing members in `CommandMetadataSnapshot.cs` changed
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-10)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — deferred to T-10)

**Commit Note:**

- Suggested commit scope: `src/CommandMetadataSnapshot.cs`
- Suggested commit message: `feat(autocomplete): add GetSuggestions overloads to CommandMetadataSnapshot`

---

### T-7: Wire `Shutdown()` Reset for `_suggestionMatcher`

- [ ] Not started

**Depends on:** T-4 (`_suggestionMatcher` field must exist)

**Objective:**

Reset `_suggestionMatcher` to `null` inside `CommandSystem.Shutdown()` so that after shutdown the global matcher is cleared and any subsequent `GetSuggestions` calls use the built-in default.

**Inputs:**

- Requirements refs: FR-6 ("`Shutdown()` resets the global matcher to null"), NFR-6 (no existing signature changes), NFR-7
- Design refs: "Implementation Notes → `Shutdown` Reset"

**Implementation Steps:**

1. Open `src/CommandSystem.cs`.
2. Locate the `Shutdown()` method body.
3. Find the block where existing fields are reset/nulled (e.g., `_registry`, `_instanceRegistry`, `_pendingConverters`, etc.).
4. Add the following line alongside those resets:
   ```csharp
   _suggestionMatcher = null;
   ```
5. Do not add any other logic, conditions, or guard clauses.
6. Do not change the method's signature, access modifier, or any other existing reset/clear operation in the method body.

**Validation:**

- Build check: `dotnet build` must succeed with zero errors.
- Code review: confirm `_suggestionMatcher = null;` is present inside `Shutdown()`; confirm no other changes to the method.
- QA quick pass (`taskReviewer`): narrow review of the `Shutdown()` method only in `CommandSystem.cs`.
- taskReviewer review request:
  - Review scope: `Shutdown()` method body inside `src/CommandSystem.cs` — one new assignment only.
  - Primary checks: `_suggestionMatcher = null;` present in `Shutdown()`; no other `Shutdown()` logic altered; no changes to any other method.
  - Required evidence: successful `dotnet build` output; diff showing the single added line.
  - Blocking conditions: null assignment missing; any other existing `Shutdown()` logic removed or altered; build error.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve any open notes from T-4/T-5 review threads on this file.

**Documentation Sync:**

- Docs to update in `docs/`: None at this step — defer to T-10.
- Update `.github/instructions/projectOverview.instructions.md` required: No (deferred to T-10).

**Completion Gate:**

- [ ] Implementation done
- [ ] `dotnet build` passes with zero errors
- [ ] `_suggestionMatcher = null;` present inside `Shutdown()`
- [ ] No existing `Shutdown()` logic changed
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-10)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — deferred to T-10)

**Commit Note:**

- Suggested commit scope: `src/CommandSystem.cs`
- Suggested commit message: `feat(autocomplete): reset _suggestionMatcher in Shutdown`

---

### T-8: Write `SuggestionTests.cs` — Full Unit Test Coverage

- [ ] Not started

**Depends on:** T-1, T-2, T-3, T-4, T-5, T-6, T-7 (all production code must be in place)

**Objective:**

Write `tests/kmCommands.Tests/SuggestionTests.cs` covering all 15+ coverage areas specified in the requirements testing expectations and design's testing strategy. Tests use NUnit and target `net8.0`.

**Inputs:**

- Requirements refs: FR-1 through FR-11, NFR-1 through NFR-7; "Testing Expectations" section (15 coverage areas)
- Design refs: "Testing Strategy" table (18 test rows), "Data Flow / Control Flow", "Final Review Contract"

**Implementation Steps:**

Each numbered item below corresponds to one or more `[Test]` methods in the test class. Group tests into logical `[TestFixture]` regions or use `[Category]` if the existing test suite uses that convention. Mirror the `[SetUp]`/`[TearDown]` pattern used in other test files (call `CommandSystem.Shutdown()` in `[TearDown]` and/or `[SetUp]` to ensure isolation).

Write tests for the following coverage areas:

1. **Prefix match — correct subset** (FR-3, FR-4): Register commands `"health"`, `"help"`, `"jump"`. Call `GetSuggestions("he")`. Assert result contains exactly `"health"` and `"help"` (and not `"jump"`). Assert result length is 2.

2. **Prefix match — case-insensitive** (FR-3): Same setup. Call `GetSuggestions("HE")`. Assert same result set as `GetSuggestions("he")`.

3. **Null prefix returns all commands** (FR-10): Register two commands. Call `GetSuggestions(null)`. Assert all registered commands are returned (length == 2). Assert result is not null.

4. **Empty string prefix returns all commands** (FR-10): Same setup. Call `GetSuggestions(string.Empty)`. Assert all registered commands returned, sorted alphabetically.

5. **No-match prefix returns empty array (not null)** (FR-4): Register a command. Call `GetSuggestions("zzz")`. Assert result is not null and length is 0.

6. **Pre-`Initialize()` returns empty array without throwing** (FR-4, NFR-7): Without calling `Initialize()`, call `CommandSystem.GetSuggestions("x")`. Assert returns non-null empty array; no exception thrown.

7. **Post-`Shutdown()` returns empty array without throwing** (FR-4, NFR-7): `Initialize()`, register a command, `Shutdown()`, then call `GetSuggestions("x")`. Assert returns non-null empty array; no exception thrown.

8. **Two-arg overload uses supplied matcher, not global default** (FR-5): Create a custom `ISuggestionMatcher` stub that always returns a fixed list regardless of input (e.g., returns `["custom_result"]`). Register different commands. Call `GetSuggestions("x", customMatcher)`. Assert result contains `CommandSuggestion` for `"custom_result"` (or empty array if the stub name is not in registry — verify the guard behavior described in "Parameters Null Guard"; result length should equal stub list length, but results for unknown names use empty parameters/description).

   — Alternate variant for this test: create a stub that returns command names in reverse alphabetical order for any prefix. Register commands `"alpha"`, `"beta"`, `"gamma"`. Call `GetSuggestions("", customMatcher)`. Assert result order matches the stub's reverse order (FR-11 — order preserved from matcher).

9. **Two-arg null matcher falls back to global/built-in default** (FR-5): Call `GetSuggestions("he", null)` — must behave identically to `GetSuggestions("he")`. Assert results are the same as with the built-in default.

10. **`SetSuggestionMatcher` affects subsequent default calls** (FR-6): Create a custom matcher stub that always returns an empty list. Call `SetSuggestionMatcher(stub)`. Call `GetSuggestions("health")` (register relevant commands). Assert result is empty (stub returned nothing).

11. **`SetSuggestionMatcher(null)` reverts to built-in default** (FR-6): `SetSuggestionMatcher(stub)`, then `SetSuggestionMatcher(null)`. Call `GetSuggestions("he")`. Assert built-in prefix behavior is restored (returns correct prefix matches).

12. **`Shutdown()` resets global matcher** (FR-6): `Initialize()`, `SetSuggestionMatcher(stub)`, `Shutdown()`, re-`Initialize()`, register commands, call `GetSuggestions("he")`. Assert uses built-in prefix behavior (stub was cleared by `Shutdown()`).

13. **`CommandMetadataSnapshot.GetSuggestions` mirrors `CommandSystem`** (FR-7): `Initialize()`, register commands. Call `CommandSystem.GetSnapshot()`. Verify `snapshot.GetSuggestions("he")` returns the same command names as `CommandSystem.GetSuggestions("he")`.

14. **`CommandMetadataSnapshot.Empty.GetSuggestions` returns empty array** (FR-7): Call `CommandMetadataSnapshot.Empty.GetSuggestions("anything")`. Assert non-null empty array. Assert no exception.

15. **`CommandSuggestion.Parameters` never null for zero-parameter command** (FR-1): Register a command with no parameters. Call `GetSuggestions`. Assert `suggestion.Parameters` is not null; assert `suggestion.Parameters.Length == 0`.

16. **`CommandSuggestion.Description` never null for no-description command** (FR-1): Register a command without a description. Call `GetSuggestions`. Assert `suggestion.Description` is not null; assert `suggestion.Description == string.Empty`.

17. **Description correctly populated when registered** (FR-9): Register a command with description `"Heals the player"`. Call `GetSuggestions`. Assert `suggestion.Description == "Heals the player"`.

18. **Snapshot `GetSuggestions` with custom matcher preserves order** (FR-11): Build a snapshot. Call `snapshot.GetSuggestions("", reverseMatcher)`. Assert result order matches the matcher's returned order.

Follow the existing NUnit patterns in the test project (use `[SetUp]`, `[TearDown]`, `Assert.That` style, no LINQ in test helpers if avoidable).

**Validation:**

- All 18+ tests must pass: `dotnet test --filter "FullyQualifiedName~SuggestionTests"`.
- No existing tests broken: `dotnet test` must show 0 failures and 0 errors across all test files.
- Code review: confirm all 15 required coverage areas from requirements are addressed; confirm NUnit conventions used; confirm no LINQ in test helpers.
- QA quick pass (`taskReviewer`): review test file for completeness and correctness alignment.
- taskReviewer review request:
  - Review scope: `tests/kmCommands.Tests/SuggestionTests.cs` (entire new file).
  - Primary checks: all 15 requirement coverage areas present; custom matcher order-preservation test present; `CommandMetadataSnapshot.Empty` test present; null-prefix and empty-prefix both tested; pre-init and post-shutdown safety tests present; all tests are isolated (proper setup/teardown); no test leaks state to the next test; NUnit conventions followed.
  - Required evidence: `dotnet test` output showing all `SuggestionTests` passing; full test suite passing with zero regressions.
  - Blocking conditions: any of the 15 required coverage areas missing; any `SuggestionTests` test failing; any pre-existing test newly failing; test isolation broken (missing `Shutdown()` in teardown); build error.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: N/A (new file — resolve any review notes on implementation files first).

**Documentation Sync:**

- Docs to update in `docs/`: None at this step — defer to T-10.
- Update `.github/instructions/projectOverview.instructions.md` required: No (deferred to T-10).

**Completion Gate:**

- [ ] Implementation done — all 18 tests written
- [ ] `dotnet test --filter "FullyQualifiedName~SuggestionTests"` passes (all green)
- [ ] `dotnet test` full suite passes (zero regressions)
- [ ] All 15 requirement coverage areas accounted for
- [ ] Custom matcher order-preservation test present
- [ ] `CommandMetadataSnapshot.Empty` test present
- [ ] Null-prefix and empty-prefix both tested separately
- [ ] Pre-init and post-shutdown safety tests present
- [ ] QA quick pass done or exception documented
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (deferred to T-10)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (N/A — deferred to T-10)

**Commit Note:**

- Suggested commit scope: `tests/kmCommands.Tests/SuggestionTests.cs`
- Suggested commit message: `test(autocomplete): add SuggestionTests covering all FR/NFR areas`

---

### T-9: Validation Gate — Full Test Suite and Regression Check

- [ ] Not started

**Depends on:** T-8 (all tests written and locally passing)

**Objective:**

Run the full test suite from a clean build to confirm all 18 new `SuggestionTests` pass, no regressions are introduced, and the test count is as expected (existing count + new tests).

**Inputs:**

- Requirements refs: All FR/NFR (acceptance criteria must be satisfied)
- Design refs: "Testing Strategy", "Final Review Contract"

**Implementation Steps:**

1. Run a full clean build: `dotnet build --no-incremental`.
2. Run the full test suite: `dotnet test` from the solution root.
3. Verify the reported test count has increased by exactly the number of new `SuggestionTests` tests written in T-8.
4. Verify 0 failed tests and 0 error tests.
5. Review any warnings emitted by the build and confirm they are pre-existing (not introduced by this feature).
6. If any regression exists, return to the relevant task and fix before marking T-9 complete.

**Validation:**

- `dotnet build --no-incremental` succeeds with zero errors.
- `dotnet test` reports 0 failures, 0 errors; all `SuggestionTests` are green.
- Test count delta matches T-8 test count.
- No new build warnings introduced by this feature's files.
- QA quick pass (`taskReviewer`): review `dotnet test` output in full.
- taskReviewer review request:
  - Review scope: `dotnet test` and `dotnet build` output (no code changes expected in this task).
  - Primary checks: all `SuggestionTests` pass; total test count ≥ (prior count + 18); no previously-passing test now fails; build clean.
  - Required evidence: full `dotnet test` console output; build output.
  - Blocking conditions: any test failure; any build error; test count delta inconsistency.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve any outstanding review notes from T-1 through T-8.

**Documentation Sync:**

- Docs to update in `docs/`: None at this step — handled in T-10.
- Update `.github/instructions/projectOverview.instructions.md` required: No (handled in T-10).

**Completion Gate:**

- [ ] `dotnet build --no-incremental` passes with zero errors
- [ ] `dotnet test` passes: 0 failures, 0 errors
- [ ] All `SuggestionTests` green; test count delta consistent with T-8
- [ ] No new build warnings from feature files
- [ ] QA quick pass done
- [ ] taskReviewer output captured and any notes tracked
- [ ] Comments/check comments addressed
- [ ] Relevant docs in `docs/` updated or exception documented (handled in T-10)
- [ ] `.github/instructions/projectOverview.instructions.md` synced or N/A documented (handled in T-10)

**Commit Note:**

- Suggested commit scope: none (validation only; no new code)
- Suggested commit message: N/A — no commit needed unless minor fixes were made; if fixes are made, use `fix(autocomplete): address test suite regressions`

---

### T-10: Documentation Sync — `docs/commands.md`, `docs/architecture.md`, `projectOverview`

- [ ] Not started

**Depends on:** T-9 (feature complete and validated)

**Objective:**

Update `docs/commands.md` and `docs/architecture.md` to document the new suggestion API. Update `.github/instructions/projectOverview.instructions.md` to reflect the new types, files, and API additions.

**Inputs:**

- Requirements refs: FR-1 through FR-11 (public API surface now documented)
- Design refs: Entire design document (new components, data flow, API contract, lifecycle notes)

**Implementation Steps:**

1. **Update `docs/commands.md`:**
   - Add a new section (e.g., "## Command Suggestions") covering:
     - `CommandSystem.GetSuggestions(string prefix)` — describe behavior, null/empty prefix behavior, return type.
     - `CommandSystem.GetSuggestions(string prefix, ISuggestionMatcher matcher)` — describe the per-call override.
     - `CommandSystem.SetSuggestionMatcher(ISuggestionMatcher matcher)` — describe global override and null revert.
     - `CommandMetadataSnapshot.GetSuggestions` overloads — note snapshot-isolation.
     - `CommandSuggestion` struct — describe fields and their never-null guarantees.
     - `ISuggestionMatcher` — describe the interface contract for consumer-implemented matchers.
     - Lifecycle behavior: pre-init/post-shutdown returns empty array; `Shutdown()` resets global matcher.
     - Brief note that `PrefixSuggestionMatcher` is the built-in default (internal; not directly accessible to consumers).

2. **Update `docs/architecture.md`:**
   - Add `CommandSuggestion` and `ISuggestionMatcher` to the public layer description.
   - Add `PrefixSuggestionMatcher` to the internal layer description under `src/Core/`.
   - Note the matcher resolution chain and the sort-preservation invariant in the design decisions or data flow section.
   - Note the two independent `_defaultMatcher` statics and their rationale.

3. **Update `.github/instructions/projectOverview.instructions.md`:**
   - Under "Key Paths", add entries for the three new files:
     - `src/CommandSuggestion.cs`
     - `src/ISuggestionMatcher.cs`
     - `src/Core/PrefixSuggestionMatcher.cs`
   - Under "API Layer Summary", add entries for the new public API:
     - Discovery/Suggestion API: `GetSuggestions(prefix)`, `GetSuggestions(prefix, matcher)`, `SetSuggestionMatcher(matcher)` on `CommandSystem`
     - `CommandMetadataSnapshot.GetSuggestions` overloads
     - `CommandSuggestion` readonly struct
     - `ISuggestionMatcher` interface
   - Under "Implementation Direction", add:
     - `src/CommandSuggestion.cs` — public `CommandSuggestion` readonly struct
     - `src/ISuggestionMatcher.cs` — public `ISuggestionMatcher` interface
     - `src/Core/PrefixSuggestionMatcher.cs` — internal sealed `PrefixSuggestionMatcher`
     - Note the `_defaultMatcher` and `_suggestionMatcher` fields on `CommandSystem` and snapshot

**Validation:**

- Docs review: confirm all three new public types are described in `commands.md`; confirm `architecture.md` names the three new files and describes their layer; confirm `projectOverview.instructions.md` key paths and API summary are updated.
- No code changes; `dotnet build` and `dotnet test` are not re-run (no code changed).
- QA quick pass (`taskReviewer`): review all three updated docs files.
- taskReviewer review request:
  - Review scope: `docs/commands.md`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`.
  - Primary checks: `CommandSuggestion`, `ISuggestionMatcher`, `PrefixSuggestionMatcher` all documented; `GetSuggestions` overloads documented on both `CommandSystem` and snapshot; lifecycle (pre-init/post-shutdown) documented; `projectOverview` key paths and API summary updated; no inaccurate claims introduced.
  - Required evidence: updated file diffs showing new sections; no broken formatting.
  - Blocking conditions: any public API omitted from docs; inaccurate behavior description; `projectOverview` key paths not updated.
- Expected taskReviewer output: Decision, Findings, Gate Check Summary, Coverage Notes, Recommended Next Step.
- Comments/check comments reviewed: resolve any outstanding review notes from all prior tasks.

**Documentation Sync:**

- Docs to update in `docs/`: `docs/commands.md`, `docs/architecture.md` — YES, updated in this task.
- Update `.github/instructions/projectOverview.instructions.md` required: Yes.
- Sections to update: Key Paths, API Layer Summary, Implementation Direction.

**Completion Gate:**

- [ ] `docs/commands.md` updated with full suggestion API section
- [ ] `docs/architecture.md` updated with new types and design notes
- [ ] `.github/instructions/projectOverview.instructions.md` updated (Key Paths, API Layer Summary, Implementation Direction)
- [ ] QA quick pass done
- [ ] taskReviewer output captured and any notes tracked
- [ ] All outstanding comments/check comments addressed
- [ ] No code changes made in this task

**Commit Note:**

- Suggested commit scope: `docs/commands.md`, `docs/architecture.md`, `.github/instructions/projectOverview.instructions.md`
- Suggested commit message: `docs(autocomplete): document GetSuggestions API, ISuggestionMatcher, CommandSuggestion`

---

## Coverage Check

- **Requirements coverage:**
  - [ ] Every requirement is mapped to at least one task
  - [ ] No requirement is left unplanned

| Requirement                                                      | Covered by                                                           |
| ---------------------------------------------------------------- | -------------------------------------------------------------------- |
| FR-1 — `CommandSuggestion` struct                                | T-1 (struct), T-5/T-6 (never-null enforcement), T-8 tests 15–16      |
| FR-2 — `ISuggestionMatcher` interface                            | T-2                                                                  |
| FR-3 — Built-in `PrefixSuggestionMatcher`                        | T-3, T-8 tests 1–5                                                   |
| FR-4 — `CommandSystem.GetSuggestions(prefix)`                    | T-5, T-8 tests 1–7                                                   |
| FR-5 — `CommandSystem.GetSuggestions(prefix, matcher)`           | T-5, T-8 tests 8–9                                                   |
| FR-6 — `CommandSystem.SetSuggestionMatcher`                      | T-4, T-7, T-8 tests 10–12                                            |
| FR-7 — `CommandMetadataSnapshot.GetSuggestions(prefix)`          | T-6, T-8 tests 13–14                                                 |
| FR-8 — `CommandMetadataSnapshot.GetSuggestions(prefix, matcher)` | T-6, T-8 test 18                                                     |
| FR-9 — Description inclusion                                     | T-5, T-6, T-8 tests 17                                               |
| FR-10 — Null/empty prefix behavior                               | T-3, T-5, T-6, T-8 tests 3–4                                         |
| FR-11 — Matching is name-only; order preserved                   | T-5, T-6, T-3, T-8 tests 8, 18                                       |
| NFR-1 — No LINQ                                                  | T-3 (matcher), T-5, T-6; enforced in all task review gates           |
| NFR-2 — Allocation discipline                                    | T-3, T-5, T-6 design implementation; T-8 (implicit)                  |
| NFR-3 — IL2CPP/AOT safety                                        | T-1 (readonly struct), T-2 (non-generic method), T-3 (no reflection) |
| NFR-4 — No UnityEngine dependency                                | All tasks; enforced in review gates                                  |
| NFR-5 — `netstandard2.0` target                                  | Build gate in T-1 through T-7                                        |
| NFR-6 — Public API stability                                     | T-4, T-5, T-6, T-7 review gates (no existing signatures changed)     |
| NFR-7 — Lifecycle safety                                         | T-5 (pre-init guard), T-7 (shutdown reset), T-8 tests 6–7, 12        |

- **Design coverage:**
  - [ ] Key design components are mapped to tasks
  - [ ] Critical design constraints are represented in validation gates

| Design Component                                       | Covered by                                  |
| ------------------------------------------------------ | ------------------------------------------- |
| `CommandSuggestion` readonly struct                    | T-1                                         |
| `ISuggestionMatcher` interface                         | T-2                                         |
| `PrefixSuggestionMatcher` internal sealed class        | T-3                                         |
| `CommandSystem` fields + `SetSuggestionMatcher`        | T-4                                         |
| `CommandSystem.GetSuggestions` overloads               | T-5                                         |
| `CommandMetadataSnapshot.GetSuggestions` overloads     | T-6                                         |
| `Shutdown()` reset                                     | T-7                                         |
| Sort preservation invariant (no re-sort after matcher) | T-3 gate, T-5 gate, T-6 gate, T-8 test 8/18 |
| Null return guard on matcher                           | T-5 gate, T-6 gate                          |
| Parameters/Description never-null guard                | T-5 gate, T-6 gate, T-8 tests 15–16         |
| Separate `_defaultMatcher` statics on each class       | T-4 gate, T-6 gate                          |
| Allocation discipline (design table)                   | T-3, T-5, T-6 implementation notes          |
| IL2CPP safety notes                                    | T-1, T-2, T-3 gates                         |
| Thread safety (same-thread same as existing API)       | T-5 implementation (no new locking)         |
| Full test coverage from design testing strategy table  | T-8                                         |
| `docs/commands.md` and `docs/architecture.md`          | T-10                                        |
| `projectOverview.instructions.md` sync                 | T-10                                        |

- **Gaps or follow-ups:**
  - None identified. All FR/NFR items and design components are mapped. If during implementation a custom matcher stub structure is needed in test helpers, a private nested class or anonymous implementation pattern should be used within `SuggestionTests.cs` without introducing new production files.
