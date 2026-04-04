---
description: "Memories for agent to use across all interactions."
applyTo: ".github/**"
---

# Workspace Memory

Use this file as durable project memory for agent workflows.

## Preferences

- Keep planning artifacts (`requirements.md`, `design.md`, `tasks.md`) accurate and evidence-driven.

## Project Rules

- Do not mark tasks complete without matching completion-gate evidence.
- Prefer minimal token usage unless it reduces planning/review quality.

## Conventions

- Keep memory notes to one concise bullet per fact.
- Place each note under the closest matching heading.
- Avoid duplicating notes that already exist.

## Do Not

- Do not store transient run logs or large command outputs.
- Do not store speculative guidance as confirmed project rules.

## Retention

- Keep this file concise; when it grows beyond ~200 lines, move stale notes into an archive file under `.github/instructions/archive/`.
