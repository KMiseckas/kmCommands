---
description: "Shared rules for custom planning/development/review agents to reduce duplication and keep behavior consistent."
applyTo: ".github/agents/*.agent.md"
---

# Shared Agentic Workflow Rules

## Non-Negotiable

- Treat `requirements.md`, `design.md`, and `tasks.md` as source-of-truth artifacts.
- Do not skip evidence-based gates: completion claims must be backed by explicit validation output.
- Do not auto-start implementation unless the user explicitly approves that handoff.
- Ask only high-impact clarification questions that materially affect scope, correctness, or validation.

## Completion Integrity

- A task is complete only when implementation, validation, and review-gate evidence are present.
- If evidence is missing or contradictory, report `BLOCKED` (or equivalent blocked state) instead of assuming completion.
- If test execution is not possible, document why and require the best available alternative evidence.

## Output Discipline

- Keep outputs concise and findings-first for review workflows.
- Separate confirmed facts, assumptions, and open questions.
- Prefer minimal, scoped changes over broad rewrites.
