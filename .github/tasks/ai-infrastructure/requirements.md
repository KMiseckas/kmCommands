# AI Infrastructure (Dev-Only)

## Status

Ready for Design

## Branch

- Name: `feat_ai-infrastructure`
- Base: `feat_llm_feature`
- Rationale: `feat_` — new capability; foundation layer for all LLM-backed features (NL dispatch, Agent Loop)

## Summary

Introduce the compile-time-gated infrastructure that all LLM-backed features will build on: a provider interface, AI-specific configuration, a shared result type hierarchy, a cancellation-aware async invocation path, and Shutdown cleanup that safely drains in-flight work. No consumer-facing AI command surface is included here; this PR delivers only the scaffolding that later features slot into.

All AI types exist exclusively behind the `KMCOMMANDS_AI` conditional compilation symbol and are fully absent from builds that do not define it.

## Goals

- Define a minimal, AOT-safe provider interface that any consumer backend can implement.
- Define AI-specific configuration that integrates with the existing `CommandConfig` path.
- Define a shared result type hierarchy covering all AI call outcomes.
- Establish a cancellation-aware async queue as an internal concurrency guard.
- Extend `Shutdown()` to cancel and drain in-flight AI work before clearing state.
- Document the Unity activation path for the `KMCOMMANDS_AI` symbol.

## In Scope

- `ILlmProvider` interface with `Task<string> CallAsync(string prompt, CancellationToken cancellationToken)`.
- `AiSettings` value type — AI-specific configuration; injectable at `Initialize()` time or via `CommandConfig`.
- `AiResult` (or equivalent) shared result type with all required outcome variants.
- `KMCOMMANDS_AI` compile-time gate applied to all new AI types.
- Runtime guard at every AI call site (`NotConfigured` when provider is null or symbol absent).
- Internal async AI queue — concurrency guard and cancellation surface; not a consumer-facing API.
- Cancellation-aware `Shutdown()` extension — cancels the queue's `CancellationTokenSource`, drains/cancels pending AI work, then clears provider reference.
- `SetLlmProvider(ILlmProvider)` / provider injection API on `CommandSystem` (or equivalent) — safe before and after `Initialize()`; passing null clears the provider.
- Documentation for the `KMCOMMANDS_AI` Unity activation path (Player Settings → Scripting Define Symbols).
- Documentation warnings: do not enable in release builds; do not hard-code tokens in source; consumer owns rate limiting, cost management, and web request responsibility.

## Out of Scope

- `ExecuteNaturalLanguageAsync` and NL command dispatch.
- AI Agent Loop.
- `IPromptFormatter` interface.
- Registry-to-JSON context builder.
- Any model-specific types, bundled HTTP client, or bundled LLM provider.
- Auth tokens — never accepted, stored, or logged by the library.
- Serialisation of AI config to any file that could ship in a release build.
- Expression evaluation.
- Command aliases.
- Command chaining.

## Requirements

### Provider Interface

1. `ILlmProvider` is a public interface, defined only when `KMCOMMANDS_AI` is defined.
2. `ILlmProvider` has exactly one method: `Task<string> CallAsync(string prompt, CancellationToken cancellationToken)`.
3. The interface has no generic type parameters (AOT-safe).
4. The library does not ship any implementation of `ILlmProvider`.

### AI Settings

5. `AiSettings` is a public value type (struct), defined only when `KMCOMMANDS_AI` is defined.
6. `AiSettings` exposes `MaxIterations` (int) — upper bound for Agent Loop iterations (used by a later feature, declared here for completeness).
7. `AiSettings` exposes `MaxContextEntries` (int) — the maximum number of recent `CommandHistoryEntry` records included as context in a prompt; this is the concrete context-window control, replacing any "token budget" concept that would imply token-counting capability the library does not have.
8. `AiSettings` provides sensible defaults for both fields.
9. `AiSettings` is injectable via an `Initialize()` overload and/or via an `AiSettings` property on `CommandConfig` (only when `KMCOMMANDS_AI` is defined).

### Result Types

10. A shared `AiResult` type (or equivalent name) is defined only when `KMCOMMANDS_AI` is defined.
11. `AiResult` covers all of the following outcome variants: `Success`, `NotConfigured`, `ProviderError`, `ParseFailure`, `CapReached`, `Cancelled`.
12. `AiResult` carries enough information (error type + optional message) for the consumer to distinguish and handle each variant.

### Provider Injection

13. `CommandSystem` exposes a method (e.g., `SetLlmProvider(ILlmProvider provider)`) that is defined only when `KMCOMMANDS_AI` is defined.
14. Passing `null` to `SetLlmProvider` clears the current provider.
15. `SetLlmProvider` is safe to call before `Initialize()`, after `Initialize()`, and after `Shutdown()`.
16. A null or unset provider is never a hard error at injection time — failure is reported at call time via `NotConfigured`.

### Compile-Time Gate

17. Every AI type (interface, struct, class, enum, method) is wrapped in `#if KMCOMMANDS_AI … #endif` or equivalent conditional compilation.
18. A build without `KMCOMMANDS_AI` defined produces no AI-related symbols and no dead code that references undefined types.

### Runtime Guard

19. Every AI call site checks that the provider is non-null before proceeding; if null, it returns an `AiResult` with variant `NotConfigured` immediately.
20. The runtime guard must not throw or log — the `NotConfigured` result is the sole signal.

### Async Queue

21. An internal async AI queue serialises concurrent AI requests so a single provider reference is not called simultaneously from multiple threads of dispatch.
22. The queue is `internal` (package-private) — not part of the public API, but accessible to other internal features (e.g., Agent Loop) within the library.
23. The queue holds a `CancellationTokenSource` that is cancelled by `Shutdown()`.
24. The queue passes its `CancellationToken` (combined with any caller-supplied token) into `ILlmProvider.CallAsync`.

### Shutdown

25. `Shutdown()` cancels the AI queue's `CancellationTokenSource` before nulling the provider reference or other AI state.
26. `Shutdown()` does not await or drain pending AI tasks — it cancels and discards. In-flight requests are abandoned; `Shutdown()` remains synchronous and does not become `async`.
27. `Shutdown()` sets the provider reference to null after cancellation.
28. `Shutdown()` resets `AiSettings` to defaults (consistent with existing reset behaviour in `Shutdown()`).

### Documentation

29. A developer-facing documentation section explains how to enable the `KMCOMMANDS_AI` symbol in Unity (Player Settings → Other Settings → Scripting Define Symbols).
30. Documentation explicitly warns: do not enable `KMCOMMANDS_AI` in release builds.
31. Documentation explicitly warns: do not hard-code LLM API tokens in source; consumer is responsible for secure token management.
32. Documentation states that the consumer's `ILlmProvider` implementation owns all HTTP transport, authentication, retry logic, and rate limiting.
33. Documentation states that `Execute()` is main-thread-only and not thread-safe; the consumer is responsible for marshalling AI call results back to the main thread before dispatching a resolved command through `Execute()`.

## Acceptance Overview

- A build that defines `KMCOMMANDS_AI` compiles without error and exposes all AI infrastructure types.
- A build that does **not** define `KMCOMMANDS_AI` compiles without error and has zero AI-related symbols in the output.
- `SetLlmProvider(null)` followed by an AI call returns `AiResult.NotConfigured` (no exception).
- `SetLlmProvider(provider)` followed by `Shutdown()` followed by an AI call returns `AiResult.NotConfigured`.
- `Shutdown()` with an in-flight AI request does not deadlock or throw.
- `AiSettings.MaxContextEntries` is an integer count; no floating-point token estimation is present.

## Testing Expectations

- Unit tests: Partially required
- Notes:
  - The `NotConfigured` guard, provider injection/clearing, and `Shutdown()` state-reset behaviour are all deterministically testable in the NUnit project.
  - The async queue's cancellation behaviour can be tested with a fake `ILlmProvider` (controllable delay).
  - Compile-time gate correctness (`#if KMCOMMANDS_AI`) cannot be verified by NUnit; it must be verified by a deliberate build without the symbol defined.
  - Tests are only compiled/run when `KMCOMMANDS_AI` is defined in the test project.

## Open Questions

None — all questions resolved:

1. **Shutdown drain strategy** → cancel and discard; `Shutdown()` remains synchronous.
2. **`CommandConfig` extension** → `AiSettings` property directly on `CommandConfig`, gated by `#if KMCOMMANDS_AI`.
3. **Queue exposure** → `internal` so Agent Loop and other future internal features can enqueue work directly.

## PR Scope

- This work is intended to ship in one pull request with multiple commits.
- It is the foundation layer prerequisite for NL Command Dispatch and AI Agent Loop, which are separate PRs.
