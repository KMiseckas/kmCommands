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
- Attribute-based registration (`[Command]`, `ScanOptions`, `AttributeScanner`) is fully implemented.
- Discovery API (`GetCommandNames`, `TryGetCommandParameters`, `GetSnapshot`) is fully implemented.
- `tests/` contains `kmCommands.Tests` (`net8.0`) with 139 passing unit tests.
- `docs/` contains architecture, Unity integration, and command authoring guides.
- Main project targets `netstandard2.0` for broad Unity compatibility.

## Key Paths

- `.github/instructions/`: reusable workflow and project guidance.
- `.github/agents/`: custom planner/developer/reviewer agents.
- `.github/tasks/<feature-slug>/`: `requirements.md`, `design.md`, `tasks.md`.
- `src/`: core library source.
- `src/CommandAttribute.cs`: public `[Command]` attribute for attribute-based registration.
- `src/ScanOptions.cs`: public `ScanOptions` struct controlling dev-mode filtering.
- `src/TypeConverterDelegate.cs`: public `TypeConverterDelegate` delegate for custom converter registration.
- `src/CommandMetadataSnapshot.cs`: public `CommandMetadataSnapshot` sealed class — immutable point-in-time registry snapshot.
- `src/Core/`: runtime internals (`CommandRegistry`, `ArgumentConverter`, `ExecutionHandler`, `AttributeScanner`, `CommandDefinition`).
- `src/Results/`: public result structs and error enums (`ScanResult`, `ScanEntry`, `RegistrationResult`, `ExecutionResult`).
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

- Command Registry: stores and resolves command definitions by name; provides `GetAllNames()` and `BuildSnapshot()` for discovery.
- Argument System: converts string tokens to typed arguments.
- Execution Engine: invokes callbacks and returns structured `ExecutionResult`.
- Attribute Scanner: discovers `[Command]`-decorated static methods at initialization time, validates them, builds AOT-safe delegates, and registers them into the Command Registry.
- Discovery Layer: `CommandSystem` exposes `GetCommandNames()`, `TryGetCommandParameters()`, and `GetSnapshot()` for read-only registry inspection; `CommandMetadataSnapshot` carries isolated snapshots.

## API Layer Summary

- Registration API: manual `Register(name, parameters, callback)` and `Register(name, parameters, callback, description)` — description is optional; pass `null` or use the 3-arg overload to omit.
- Converter API: `RegisterConverter(Type, TypeConverterDelegate)` returning `RegistrationResult` — registers or overrides a converter for a given `System.Type`; safe before or after `Initialize()`; cleared by `Shutdown()`.
- Scan API: `Scan(Type, ScanOptions)` and `Scan(Assembly, ScanOptions)` — attribute-based registration; returns `ScanResult` with per-command outcomes.
- Execution API: `Execute(name, string[] args)` with structured `ExecutionResult` output.
- Discovery API: `GetCommandNames()`, `TryGetCommandParameters(name, out parameters)`, `GetSnapshot()` — read-only registry inspection; safe before `Initialize()` and after `Shutdown()`. `CommandMetadataSnapshot` also exposes `TryGetDescription(name, out description)` for per-command help text.

## Typical Unity Client Usage

1. Unity layer calls `CommandSystem.Initialize()` at startup.
2. Unity layer registers commands manually (`Register(name, parameters, callback)`) and/or via scan (`Scan(typeof(MyCommands), options)` or `Scan(assembly, options)`).
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

- `src/CommandSystem.cs` — public entry point, lifecycle, registration, execution, scan API, and `RegisterConverter` with `_pendingConverters` pre-init buffer
- `src/CommandAttribute.cs` — public `[Command]` attribute (name + `IsDevOnly` flag)
- `src/ScanOptions.cs` — public `ScanOptions` struct (`DevMode` bool)
- `src/CommandCallback.cs` — public `CommandCallback` delegate
- `src/TypeConverterDelegate.cs` — public `TypeConverterDelegate` delegate; signature `bool(string input, out object result)`
- `src/CommandParameterInfo.cs` — public parameter descriptor
- `src/Results/RegistrationResult.cs` — `RegistrationResult` struct and `RegistrationError` enum (includes `InvalidMethod`, `NullConverter`)
- `src/Results/ExecutionResult.cs` — `ExecutionResult` struct and `ExecutionError` enum
- `src/Results/ScanResult.cs` — `ScanResult` class and `ScanEntry` struct
- `src/Core/CommandDefinition.cs` — internal command storage model
- `src/Core/CommandRegistry.cs` — internal dictionary-backed command store
- `src/Core/ArgumentConverter.cs` — internal string-to-type converter (int, float, bool, string); extensible via `AddConverter(Type, TryConvertFunc)` internal method
- `src/Core/ExecutionHandler.cs` — internal execution orchestrator
- `src/Core/AttributeScanner.cs` — internal attribute-based command discovery; uses `Delegate.CreateDelegate` for AOT-safe callbacks; 4-parameter max
- `src/CommandMetadataSnapshot.cs`: public `CommandMetadataSnapshot` sealed class; internal constructor; `Empty` singleton; `TryGetParameters()` for O(1) case-insensitive lookup; `TryGetDescription()` for O(1) case-insensitive description lookup.

Adjust only with explicit design updates.
