# Recommendations

> Action points identified by Feature Investigator. Remove items as they are implemented or explicitly discarded.

---

## AI Infrastructure (Dev-Only)

### Missing or Incomplete

- [ ] `ILlmProvider.CallAsync(string prompt)` has no `CancellationToken` parameter — the Agent Loop spec requires cancellation support, but without a CT on the interface the consumer's HTTP layer cannot abort in-flight requests; only inter-iteration cancellation is possible, which is insufficient for long-running LLM calls.
- [ ] `Shutdown()` is synchronous and does immediate null assignments — introducing an async AI queue requires a `CancellationTokenSource` to be stored and cancelled before state is nulled; the current cleanup model cannot safely drain or cancel pending async work.
- [ ] The `KMCOMMANDS_AI` conditional compilation symbol has no documented activation path for Unity — Unity uses project-level Scripting Define Symbols (Player Settings); docs must explain exactly how consumers enable and disable this symbol; absence of guidance makes the compile-time gate unusable in practice.

### Improvements

- [ ] `AiSettings.ContextTokenBudget` trimming strategy is underspecified — the library has no model-specific tokenizer and cannot count tokens accurately; the spec should define the concrete trimming unit (e.g., entry count or total character count) rather than using "token budget" language that implies knowledge the library does not have.
- [ ] The async AI queue's role relative to `ExecuteNaturalLanguageAsync` (which already returns a `Task`) is ambiguous — clarify whether the queue is an internal concurrency guard, a consumer-facing fire-and-forget surface, or both; conflating the two will complicate the API design.
- [ ] Unity's `SynchronizationContext` means `Execute()` (documented as main-thread-only, not thread-safe) may be on the wrong thread when an async continuation resumes — the spec should state explicitly that the consumer is responsible for marshalling AI call results back to the main thread before the resolved command is dispatched.

### Questions / Clarifications Needed

- [ ] Should `ILlmProvider.CallAsync` accept a `CancellationToken`? Adding it now keeps the interface forward-compatible with the Agent Loop's CT requirements; adding it later is a breaking change.
- [ ] What is the concrete threading contract for the AI queue? Is the queue itself main-thread-bound, or does it dispatch work onto a background thread and marshal results back?

---

## Natural Language Command Dispatch (Dev-Only)

### Missing or Incomplete

- [ ] The response envelope `{ "command": "...", "args": [...] }` requires JSON array parsing — the existing `JsonConfigParser` explicitly does not support arrays; a new parser or parser extension is needed before NL dispatch can be implemented.
- [ ] The Agent Loop response `{ "done": bool, "commands": [{ "name": "...", "args": [...] }] }` requires nested object array parsing — equally unsupported by the current parser; both NL dispatch and agent loop share this dependency.

### Improvements

- [ ] LLMs commonly wrap JSON responses in markdown code fences (` ```json ... ``` `) — the response envelope parser should strip these before attempting to parse; treating raw LLM output as guaranteed bare JSON will cause frequent `ParseFailure` results in practice.
- [ ] When `ExecuteNaturalLanguageAsync` calls through `Execute()`, the history entry records the resolved command and args but loses the original natural-language string — consider whether `CommandHistoryEntry` should carry an optional original-input field, or whether a separate NL-specific history entry type is warranted, to preserve auditability of NL interactions.
- [ ] `IPromptFormatter` interface contract is underspecified — the vision describes "replace or wrap the default system prompt and/or user message" but does not define the method signature, what context data is passed (registry snapshot? command name candidates?), or what the return type should be; this needs a concrete design before implementation.

---

## AI Agent Loop (Dev-Only)

### Missing or Incomplete

- [ ] No consecutive-failure threshold defined — the loop continues executing until `done: true` or `MaxIterations` is reached, even if every command fails; a hallucinating LLM that never signals done can exhaust the iteration cap while producing only errors; a consecutive-failure cap should be an explicit design decision.

### Improvements

- [ ] `AgentLoopResult` carries per-iteration summaries as an unbounded list — with large `MaxIterations` values this grows linearly; the design should state a maximum or note the allocation implication so consumers can set `MaxIterations` conservatively.
- [ ] The multi-command-per-iteration failure policy is underspecified — when executing the `"commands"` array sequentially within one iteration and a command fails, it is unclear whether the remaining commands in that same iteration are skipped or still executed; the spec should state the behaviour explicitly.

### Questions / Clarifications Needed

- [ ] Who is responsible for web requests? The `ILlmProvider` abstraction correctly places all HTTP, auth, retry, and rate-limiting responsibility on the consumer — but this should be stated explicitly in the AI Infrastructure docs section (not just in the Agent Loop section) so consumers understand the full ownership model before implementing their provider.

---
