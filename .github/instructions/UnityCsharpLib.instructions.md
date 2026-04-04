---
description: "Guidelines for building C# libraries for Unity, ensuring compatibility with IL2CPP and best practices for performance and maintainability."
applyTo: "**/*.cs"
---

# Unity C# SDK Guidelines

## Environment

- Target Unity 2021+
- Use C# 8 compatible features only
- Must support IL2CPP (AOT safe across all platforms)

## Hard Constraints

- Do NOT use LINQ in runtime code
- Do NOT use reflection-heavy logic
- Do NOT use dynamic or runtime code generation
- Do NOT use System.Reflection.Emit
- Avoid Activator.CreateInstance unless type is explicitly known
- Avoid generic virtual methods where possible (AOT issues)
- Avoid boxing/unboxing
- Avoid allocations in hot paths

## AOT / IL2CPP Safety

- All generic types used must be explicitly referenced somewhere (avoid stripping issues)
- Avoid code paths that rely on runtime type discovery
- Avoid interface-based generic dispatch in performance-critical paths
- Ensure code works without JIT assumptions
- Be aware of Unity code stripping (linker may remove unused code)

## Unity Rules

- Unity APIs must only be called on the main thread
- Do not rely on constructors for Unity objects
- Use explicit initialization methods instead
- UnityEngine.Object must be checked using `== null`
- Do not rely on `?.` or standard null semantics with Unity objects

## API Design

- Keep public API minimal and stable (DLL = breaking changes are costly)
- Provide explicit lifecycle:
  - Initialize(...)
  - Shutdown()
- No hidden static initialization
- Avoid global static state
- If static state is used, it must be resettable (domain reload safe)
- Avoid exposing Unity-specific types in public API where possible

## Architecture

- Separate:
  - Core (engine-agnostic, pure C#)
  - Unity integration layer (MonoBehaviour, bindings)
- Core must not depend on UnityEngine
- Unity layer adapts core → Unity

## Performance

- Cache frequently used references
- Avoid allocations in Update / loops
- Avoid closures in hot paths
- Prefer simple loops over abstractions
- Avoid virtual/interface calls in tight loops when possible

## Error Handling

- Validate inputs at boundaries
- Avoid exceptions in hot paths
- Use exceptions only for truly exceptional cases
- Do not log inside core logic
- Return structured error results (bool, enum, result types)

## Serialization

- Use [Serializable] for data containers
- Use [SerializeField] for private fields
- Do not serialize interfaces
- Avoid dictionaries unless wrapped
- Prefer simple DTO-style classes

## Formatting

- Follow .editorconfig
- Use nameof instead of string literals
- Keep files small and focused
- XML docs required for public APIs (keep concise)

## Dependencies

- Minimize dependencies (DLL size + compatibility)
- All dependencies must be Unity-compatible
- Document purpose of each dependency

## Testing

- Core logic must be testable outside Unity
- Avoid MonoBehaviour in core logic
- Separate logic from Unity-specific behaviour
