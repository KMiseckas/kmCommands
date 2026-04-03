---
description: "Concise project overview for kmCommands so agents can understand scope, architecture, and constraints without scanning the repository."
applyTo: "**"
---

# kmCommands Project Overview

## What This Project Is

kmCommands is a lightweight, platform-agnostic C# command-system library for Unity 2021+ projects.

It focuses on command definition, discovery, parsing, validation, execution, and metadata exposure.

It does not implement UI, input handling, rendering, MonoBehaviour lifecycle behavior, or scene logic.

## Current Repository State

- Project is scaffolded.
- `src/` is currently empty.
- `tests/` is currently empty.
- `docs/` is currently empty.
- Main project targets `netstandard2.0` for broad Unity compatibility.

## Folder Hierarchy

| Path                    | Purpose                                                              |
| ----------------------- | -------------------------------------------------------------------- |
| `.github/instructions/` | Agent instruction files and project guidance.                        |
| `.vscode/`              | Editor configuration.                                                |
| `src/`                  | Core kmCommands library source code (to be implemented).             |
| `tests/`                | Unit/integration tests for core logic (to be implemented).           |
| `docs/`                 | Documentation, examples, and integration guides (to be implemented). |
| `kmCommands.csproj`     | Library project definition (`netstandard2.0`).                       |
| `kmCommands.sln`        | Solution file for development workflow.                              |
| `bin/`, `obj/`          | Build artifacts (generated).                                         |

## Dependencies And Target Versions

| Item                           | Value                            |
| ------------------------------ | -------------------------------- |
| .NET target                    | `netstandard2.0`                 |
| Unity compatibility target     | Unity 2021+                      |
| C# language expectation        | C# 8-compatible patterns         |
| Runtime package dependencies   | None currently declared          |
| UnityEngine dependency in core | Avoid; keep core engine-agnostic |

## Library Goals

- Provide robust, extensible runtime command execution.
- Support attribute-based command registration.
- Support manual registration API.
- Parse and validate arguments from string input.
- Support command chaining in a single input line.
- Expose command metadata for autocompletion/tooling.
- Keep core logic independent from Unity UI and rendering.

## Non-Goals

- No command console UI.
- No keyboard/controller input processing.
- No text/graphics rendering.
- No direct dependency on Unity scene update lifecycle.

## Systems In Action (Planned)

| System             | Responsibility                                                      | Inputs                                    | Outputs                                    |
| ------------------ | ------------------------------------------------------------------- | ----------------------------------------- | ------------------------------------------ |
| Command Registry   | Store and resolve command definitions and aliases.                  | Registration API / attribute scan results | Resolved command metadata + callback       |
| Command Parser     | Parse raw command text and chain segments.                          | Raw string input                          | Parsed invocation model                    |
| Argument System    | Convert tokens to typed arguments and validate signatures/defaults. | Parsed tokens + command signature         | Typed arguments or structured errors       |
| Execution Engine   | Execute callbacks and orchestrate command chains.                   | Invocation model + typed args             | Execution result(s), failures, diagnostics |
| Metadata Provider  | Expose command and argument info for autocomplete/help.             | Registry state                            | Queryable metadata model                   |
| Reflection Scanner | Discover attribute-based commands from assemblies; cache results.   | Assemblies/types                          | Command descriptors for registration       |

## API Layer Summary (Planned)

| API Layer              | Scope                                                                      |
| ---------------------- | -------------------------------------------------------------------------- |
| Registration API       | Register command definitions manually and via discovered descriptors.      |
| Execution API          | Execute single command or chain from text input.                           |
| Parsing/Validation API | Parse command input and validate arguments before callback invocation.     |
| Metadata API           | Enumerate commands, aliases, descriptions, signatures, and usage hints.    |
| Extensibility API      | Plug custom argument parsers, validators, middleware hooks, and providers. |

## Typical Unity Client Usage (Target)

1. Unity layer registers commands (attribute scan and/or manual API).
2. Unity UI/input layer collects command text from users.
3. Unity layer calls kmCommands execution API.
4. kmCommands returns success/error/results and metadata.
5. Unity layer renders feedback and suggestions.

## Critical Constraints

- Keep core library independent from UnityEngine where possible.
- Follow IL2CPP/AOT-safe patterns.
- Avoid LINQ in runtime hot paths.
- Avoid reflection-heavy per-frame behavior; cache discovery results.
- Minimize allocations in parse/execute loops.
- Do not hide static global state; lifecycle should be explicit (`Initialize(...)`, `Shutdown()`).
- Public API stability matters (library consumed by external Unity clients).
- Unity-facing concerns (input/UI/rendering) stay outside core library.

## Required Source Header

Add this header at the top of new source files in `src/`:

```csharp
// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
```

## Docs Expectations

- Add architecture summary and terminology in `docs/architecture.md`.
- Add Unity integration quickstart in `docs/unity-integration.md`.
- Add command authoring guide in `docs/commands.md`.
- Add examples showing manual and attribute-based registration.

## Implementation Direction

When source implementation starts in `src/`, organize around:

- `Core/Registry`
- `Core/Parsing`
- `Core/Arguments`
- `Core/Execution`
- `Core/Metadata`
- `Core/Reflection`
- `Abstractions` for interfaces/contracts

Keep these as guidance for future structure; adjust only with explicit design updates.
