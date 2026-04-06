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
- Instance command registration (`RegisterInstance`, `UnregisterInstance`, `InstanceScanner`, `InstanceCallbackBuilder`) is fully implemented.
- Discovery API (`GetCommandNames`, `TryGetCommandParameters`, `GetSnapshot`) is fully implemented.
- `tests/` contains `kmCommands.Tests` (`net8.0`) with 272 passing unit tests.
- `docs/` contains architecture, Unity integration, and command authoring guides.
- Main project targets `netstandard2.0` for broad Unity compatibility.

## Key Paths

- `.github/instructions/`: reusable workflow and project guidance.
- `.github/agents/`: custom planner/developer/reviewer agents.
- `.github/tasks/<feature-slug>/`: `requirements.md`, `design.md`, `tasks.md`.
- `src/`: core library source.
- `src/CommandAttribute.cs`: public `[Command]` attribute for attribute-based registration.
- `src/CommandIgnoreAttribute.cs`: public `[CommandIgnore]` attribute — prevents a method or property from being registered in any scan mode; overrides `[Command]` when both are present.
- `src/ScanOptions.cs`: public `ScanOptions` struct controlling dev-mode filtering.
- `src/TypeConverterDelegate.cs`: public `TypeConverterDelegate` delegate for custom converter registration.
- `src/InstanceScanMode.cs`: public `InstanceScanMode` enum — `Auto` (default) / `AttributeOnly`.
- `src/CommandMetadataSnapshot.cs`: public `CommandMetadataSnapshot` sealed class — immutable point-in-time registry snapshot.
- `src/Results/UnregisterResult.cs`: public `UnregisterResult` readonly struct — result of `UnregisterInstance` with `Success`, `RemovedCount`, `ErrorMessage`.
- `src/Core/`: runtime internals (`CommandRegistry`, `ArgumentConverter`, `ExecutionHandler`, `AttributeScanner`, `InstanceScanner`, `InstanceCallbackBuilder`, `InstanceRegistry`, `CommandDefinition`).
- `src/Results/`: public result structs and error enums (`ScanResult`, `ScanEntry`, `RegistrationResult`, `ExecutionResult`); `ScanResult` exposes `IsAlreadyInitialized` (bool) and internal `AlreadyInitialized()` factory.
- `src/CommandHistoryEntry.cs`: public `CommandHistoryEntry` readonly struct — immutable record of one successful execution (name + args + returnValue snapshot).
- `src/Core/CommandHistoryBuffer.cs`: internal `CommandHistoryBuffer` sealed class — fixed-capacity ring buffer storing `CommandHistoryEntry` values.
- `src/Core/InstanceRegistry.cs`: internal `InstanceRegistry` sealed class — maps instanceKey → command names + target object.
- `src/Core/InstanceScanner.cs`: internal `InstanceScanner` sealed class — discovers and registers instance members.
- `src/Core/InstanceCallbackBuilder.cs`: internal static class — builds AOT-safe instance-bound delegates.
- `tests/kmCommands.Tests/`: NUnit test project (272 passing tests).
- `tests/kmCommands.Tests/AutoScanAtInitializeTests.cs`: 25 tests covering all scanning-at-initialize behavior.
- `tests/kmCommands.Tests/InstanceScannerTests.cs`: 27 tests covering InstanceScanner/InstanceCallbackBuilder internal behavior.
- `tests/kmCommands.Tests/InstanceCommandRegistrationTests.cs`: integration tests for RegisterInstance/UnregisterInstance public API.
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
- Scan-at-Init API: `Initialize(Type[], ScanOptions, int)`, `Initialize(Assembly[], ScanOptions, int)`, `Initialize(Type[], Assembly[], ScanOptions, int)` — three overloads that initialize the system and scan targets in one call; return an aggregated `ScanResult`; idempotent (already-initialized path returns `ScanResult.IsAlreadyInitialized == true`). History capacity parameter defaults to `DefaultHistoryCapacity`.
- Instance Registration API: `RegisterInstance(target, key)` and `RegisterInstance(target, key, ScanOptions, InstanceScanMode)` — discovers and registers instance-bound commands under `key.commandName`; returns `ScanResult`; guards: `NotInitialized`, `NullTarget`, `InvalidInstanceKey`, `DuplicateInstanceKey`. `UnregisterInstance(key)` removes all commands for that key; returns `UnregisterResult` with `Success`, `RemovedCount`, `ErrorMessage`.
- Execution API: `Execute(name, string[] args)` with structured `ExecutionResult` output. Successful executions are recorded to the history buffer. `ExecutionError.InstanceNull` is returned when an instance command's bound target is null/destroyed.
- Discovery API: `GetCommandNames()`, `TryGetCommandParameters(name, out parameters)`, `GetSnapshot()` — read-only registry inspection; safe before `Initialize()` and after `Shutdown()`. `CommandMetadataSnapshot` also exposes `TryGetDescription(name, out description)` for per-command help text.
- History API: `DefaultHistoryCapacity` constant (64); `Initialize(int historyCapacity)` overload (capacity clamped to ≥ 1); `GetHistory()` returns `CommandHistoryEntry[]` snapshot (oldest→newest); `HistoryCount` property (non-allocating); `ClearHistory()` clears the buffer. All history members are safe before `Initialize()` and after `Shutdown()`.

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

- `src/CommandSystem.cs` — public entry point, lifecycle, registration, execution, scan API, `RegisterConverter`, `RegisterInstance`, `UnregisterInstance` with `_pendingConverters`, `_instanceRegistry`, `_instanceScanner`
- `src/CommandAttribute.cs` — public `[Command]` attribute (name + `IsDevOnly` flag)
- `src/ScanOptions.cs` — public `ScanOptions` struct (`DevMode` bool)
- `src/InstanceScanMode.cs` — public `InstanceScanMode` enum; `Auto=0`, `AttributeOnly=1`
- `src/CommandCallback.cs` — public `CommandCallback` delegate; signature `object(object[] args)`; return `null` for void commands
- `src/TypeConverterDelegate.cs` — public `TypeConverterDelegate` delegate; signature `bool(string input, out object result)`
- `src/CommandParameterInfo.cs` — public parameter descriptor
- `src/Results/RegistrationResult.cs` — `RegistrationResult` struct and `RegistrationError` enum (includes `InvalidMethod`, `NullConverter`, `NullTarget`, `DuplicateInstanceKey`, `InvalidInstanceKey`)
- `src/Results/ExecutionResult.cs` — `ExecutionResult` struct and `ExecutionError` enum (includes `InstanceNull`); `ReturnValue` (object) and `HasReturnValue` (bool) properties on success; `Ok(object returnValue = null)` factory
- `src/Results/ScanResult.cs` — `ScanResult` class and `ScanEntry` struct; `ScanResult` exposes `IsAlreadyInitialized` (bool) public property, `AlreadyInitialized()` internal factory, and `SystemFailure(error, message)` internal factory.
- `src/Results/UnregisterResult.cs` — `UnregisterResult` readonly struct; `Success`, `RemovedCount`, `ErrorMessage`; `internal static Ok(int)` / `Fail(string)` factories.
- `src/Core/CommandDefinition.cs` — internal command storage model; `IsInstanceCommand` bool property (default `false`)
- `src/Core/CommandRegistry.cs` — internal dictionary-backed command store; `TryRemove(name)` removes by name
- `src/Core/ArgumentConverter.cs` — internal string-to-type converter (int, float, bool, string); extensible via `AddConverter(Type, TryConvertFunc)` internal method
- `src/Core/ExecutionHandler.cs` — internal execution orchestrator; four-catch pattern: `TargetInvocationException`+NRE+IsInstanceCommand → InstanceNull; direct NRE+IsInstanceCommand → InstanceNull; other TargetInvocationException → CallbackThrewException; other Exception → CallbackThrewException
- `src/Core/AttributeScanner.cs` — internal attribute-based command discovery; uses `Delegate.CreateDelegate` for AOT-safe callbacks; 4-parameter max
- `src/Core/InstanceRegistry.cs` — internal `InstanceRegistry` sealed class; maps key → List<commandName> and key → target; `TryReserveKey`, `TrackCommand`, `TryGetCommandNames`, `RemoveKey`, `Clear`
- `src/Core/InstanceScanner.cs` — internal `InstanceScanner` sealed class; `Scan(target, key, options, mode)` → `ScanResult`; applies `[CommandIgnore]` exclusion check; auto-scanned public members (no `[Command]`) are implicitly dev-only and only registered when `ScanOptions.DevMode = true`; marks `IsInstanceCommand=true`
- `src/Core/InstanceCallbackBuilder.cs` — internal static class; `BuildMethodCallback`, `BuildGetterCallback`, `BuildSetterCallback`; AOT-safe via `Delegate.CreateDelegate`
- `src/CommandMetadataSnapshot.cs`: public `CommandMetadataSnapshot` sealed class; internal constructor; `Empty` singleton; `TryGetParameters()` for O(1) case-insensitive lookup; `TryGetDescription()` for O(1) case-insensitive description lookup.
- `src/CommandHistoryEntry.cs`: public `CommandHistoryEntry` readonly struct; internal constructor; `CommandName`, `Args`, and `ReturnValue` get-only properties; args snapshot never null.
- `src/Core/CommandHistoryBuffer.cs`: internal `CommandHistoryBuffer` sealed class; fixed-size ring buffer; `Record(name, args, returnValue)`, `GetSnapshot()` (oldest→newest), `Clear()`, `Count`.

Adjust only with explicit design updates.
