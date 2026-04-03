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

- Core command system (registration + execution) is implemented in `src/`.
- `src/` contains the entry point (`CommandSystem`), result types, and internal runtime components.
- `tests/` contains `kmCommands.Tests` (NUnit, `net8.0`) with 71 passing unit tests.
- `docs/` contains architecture, Unity integration, and command authoring guides.
- Main project targets `netstandard2.0` for broad Unity compatibility.

## Folder Hierarchy

| Path                    | Purpose                                                              |
| ----------------------- | -------------------------------------------------------------------- |
| `.github/instructions/`          | Agent instruction files and project guidance.                                              |
| `.vscode/`                       | Editor configuration.                                                                      |
| `src/`                           | Core library source. Contains `CommandSystem`, result types, and `Core/` internal runtime. |
| `src/Core/`                      | Internal components: `CommandRegistry`, `ArgumentConverter`, `ExecutionHandler`, `CommandDefinition`. |
| `src/Results/`                   | Public result structs: `RegistrationResult`, `ExecutionResult` and their error enums.      |
| `src/Properties/`                | `AssemblyInfo.cs` — `InternalsVisibleTo` declaration.                                      |
| `tests/kmCommands.Tests/`        | NUnit test project (`net8.0`). 71 unit tests covering all runtime paths.                   |
| `docs/`                          | Architecture overview, Unity integration quickstart, and command authoring guide.           |
| `kmCommands.csproj`              | Library project definition (`netstandard2.0`).                                             |
| `kmCommands.sln`                 | Solution file — includes main project and test project.                                    |
| `bin/`, `obj/`                   | Build artifacts (generated).                                                               |

## Dependencies And Target Versions

| Item                           | Value                            |
| ------------------------------ | -------------------------------- |
| .NET target                    | `netstandard2.0`                 |
| Unity compatibility target     | Unity 2021+                      |
| C# language expectation        | C# 8-compatible patterns         |
| Runtime package dependencies   | None                             |
| Test framework                 | NUnit 4.x (`net8.0`, test project only) |
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

## Systems In Action

| System           | Responsibility                                                        | Inputs                            | Outputs                                           |
| ---------------- | --------------------------------------------------------------------- | --------------------------------- | ------------------------------------------------- |
| Command Registry | Store and resolve command definitions by name.                        | Registration API                  | Resolved command metadata + callback              |
| Argument System  | Convert tokens to typed arguments and validate signatures.            | Parsed tokens + command signature | Typed arguments or structured errors              |
| Execution Engine | Execute callbacks with converted arguments; return structured result. | Command name + string args        | `ExecutionResult` (success, failure, diagnostics) |

## API Layer Summary

| API Layer        | Scope                                                                                        |
| ---------------- | -------------------------------------------------------------------------------------------- |
| Registration API | Register command definitions manually: name, typed parameter signature, callback delegate.   |
| Execution API    | Execute a registered command by name with string argument tokens; returns structured result. |

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
