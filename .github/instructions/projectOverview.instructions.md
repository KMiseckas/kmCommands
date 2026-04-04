---
description: "Concise project overview for kmCommands so agents can understand scope, architecture, and constraints without scanning the repository."
applyTo: "**"
---

# kmCommands Project Overview

## What This Project Is

kmCommands is a lightweight, platform-agnostic C# command-system library for Unity 2021+.

It focuses on command definition, parsing, validation, execution, and metadata exposure.

It does not implement UI, input handling, rendering, MonoBehaviour lifecycle behavior, or scene logic.

## Current Repository State

- Core command system (registration + execution) is implemented in `src/`.
- `src/` contains `CommandSystem`, result types, and internal runtime components.
- `tests/` contains `kmCommands.Tests` (`net8.0`) with 71 passing unit tests.
- `docs/` contains architecture, Unity integration, and command authoring guides.
- Main project targets `netstandard2.0` for broad Unity compatibility.

## Key Paths

- `.github/instructions/`: reusable workflow and project guidance.
- `.github/agents/`: custom planner/developer/reviewer agents.
- `.github/tasks/<feature-slug>/`: `requirements.md`, `design.md`, `tasks.md`.
- `src/`: core library source.
- `src/Core/`: runtime internals (`CommandRegistry`, `ArgumentConverter`, `ExecutionHandler`, `CommandDefinition`).
- `src/Results/`: public result structs and error enums.
- `tests/kmCommands.Tests/`: NUnit test project.
- `docs/`: architecture and usage documentation.

## Dependencies And Target Versions

- .NET target: `netstandard2.0`
- Unity compatibility: Unity 2021+
- Runtime dependencies: none
- Test framework: NUnit (`net8.0` tests project)
- Core must remain engine-agnostic (`UnityEngine` avoided in `src/`)

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

## Systems In Action

- Command Registry: stores and resolves command definitions by name.
- Argument System: converts string tokens to typed arguments.
- Execution Engine: invokes callbacks and returns structured `ExecutionResult`.

## API Layer Summary

- Registration API: manual register(name, parameters, callback).
- Execution API: execute(name, string args) with structured result output.

## Typical Unity Client Usage

1. Unity layer calls `CommandSystem.Initialize()` at startup.
2. Unity layer registers commands manually (`Register(name, parameters, callback)`).
3. Unity UI/input layer splits raw command input and calls `CommandSystem.Execute(name, args)`.
4. kmCommands converts arguments to declared types and invokes the callback.
5. Unity layer inspects the returned `ExecutionResult` and renders feedback.

## Critical Constraints

- Keep core library independent from UnityEngine where possible.
- Follow IL2CPP/AOT-safe patterns.
- Avoid LINQ in runtime hot paths.
- Avoid reflection-heavy per-frame behavior; cache discovery results.
- Minimize allocations in parse/execute loops.
- Do not hide static global state; lifecycle should be explicit (`Initialize(...)`, `Shutdown()`).
- Public API stability matters (library consumed by external Unity clients).
- Unity-facing concerns (input/UI/rendering) stay outside core library.
- Planning artifacts (`requirements.md`, `design.md`, `tasks.md`) are source-of-truth for implementation.

## Required Source Header

Add this header at the top of new source files in `src/`:

```csharp
// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
```

## Docs

- `docs/architecture.md` — component overview, data flow, design decisions, IL2CPP notes.
- `docs/unity-integration.md` — quickstart for adding kmCommands to a Unity project.
- `docs/commands.md` — command authoring guide: registration, parameter types, callbacks, error handling.

## Implementation Direction

The `src/` structure currently implements:

- `src/CommandSystem.cs` — public entry point, lifecycle, registration, execution API
- `src/CommandCallback.cs` — public `CommandCallback` delegate
- `src/CommandParameterInfo.cs` — public parameter descriptor
- `src/Results/` — `RegistrationResult`, `ExecutionResult`, and error enums
- `src/Core/CommandDefinition.cs` — internal command storage model
- `src/Core/CommandRegistry.cs` — internal dictionary-backed command store
- `src/Core/ArgumentConverter.cs` — internal string-to-type converter (int, float, bool, string)
- `src/Core/ExecutionHandler.cs` — internal execution orchestrator

Adjust only with explicit design updates.
