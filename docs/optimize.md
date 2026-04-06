# kmCommands — Optimization Notes

## Overview

This document tracks known performance concerns, optimization goals, and implementation status for hot paths and allocation-sensitive areas of kmCommands. Each section describes the current state, what work is still needed, and the goal of any planned change.

---

## Project Notes

- Core must remain `netstandard2.0`-compatible and IL2CPP/AOT-safe.
- Avoid allocations in execute and parse hot paths.
- Reflection is acceptable at registration time; never acceptable per execution.
- Unity UI layers drive snapshot and history read patterns — optimization guidance must account for per-frame and event-driven consumers.

---

### ✅ Instance Command Registration — AOT Safety and Delegate Binding

Reflection at `RegisterInstance()` time is a one-time cost per registration call. Delegates are built once via `Delegate.CreateDelegate` and cached in the command registry for all future executions.

- [x] `InstanceScanner` uses `type.GetMethods()` and `GetCustomAttribute<CommandAttribute>()` at registration time only — no per-execution reflection
- [x] `InstanceCallbackBuilder` uses `Delegate.CreateDelegate` for all instance method and property bindings — AOT/IL2CPP safe on Unity 2021+
- [x] Dev-only command filtering available via `ScanOptions.DevMode` and `[Command(IsDevOnly = true)]`
- [x] `InstanceScanMode.AttributeOnly` limits scanning scope to `[Command]`-decorated members when full auto-scan is unwanted
- [x] `instanceKey.commandName` naming scheme provides the runtime instance-to-function mapping — no identity resolution required in the library

---

### 🔲 Instance Command Registration — Per-Type Scan Caching

When many instances of the same type register simultaneously (e.g., a large number of enemies spawning at scene load), `InstanceScanner` reflects the type's methods and properties once per `RegisterInstance()` call. No per-type cache exists today, so the reflection work is repeated for each instance.

- [ ] Profile the registration path with a high instance count (50+ of the same type) to quantify real cost
- [ ] Evaluate caching reflected `MethodInfo` / `PropertyInfo` arrays per `System.Type` across multiple `RegisterInstance()` calls
- [ ] Ensure cache is cleared on `Shutdown()` to remain domain-reload safe
- [ ] Any caching approach must not store references to bound instances — only type-level metadata

---

### ✅ Command History — Non-Allocating Count Check

`HistoryCount` returns the current entry count directly from the buffer without allocating. Consumers can use this as a lightweight change signal before deciding whether a full snapshot is needed.

- [x] `HistoryCount` property is a direct field read — no allocation, safe to poll per frame
- [x] `GetHistory()` returns an independent `CommandHistoryEntry[]` snapshot allocated on each call — correct for snapshot semantics but callers must not call it more often than needed

---

### 🔲 Command History — Snapshot Allocation Guidance and Change Notification

`GetHistory()` allocates a new array on every call via `GetSnapshot()`. A Unity UI layer that calls it on every frame or on every render cycle will cause continuous GC pressure. There is currently no event or dirty flag to tell a consumer that new entries have been added.

- [ ] Document the recommended snapshot pattern in `unity-integration.md`: cache the returned array, and re-fetch only when the count has changed (compare `HistoryCount` before and after execution)
- [ ] Evaluate adding a `HistoryVersion` counter (incremented on each `Record()` call) so consumers can skip the allocation when the buffer has not changed since the last read
- [ ] Evaluate an optional `IHistoryWriter` adapter (already tracked in `vision.md`) as an alternative model: push entries to the consumer immediately on record rather than polling via `GetHistory()`
- [ ] Ensure any new mechanism remains non-allocating in the common case (no new entries since last check)
