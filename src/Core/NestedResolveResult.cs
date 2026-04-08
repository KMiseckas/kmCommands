// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands.Core
{
    /// <summary>
    /// Result of a <see cref="NestedCommandResolver.ResolveArgs"/> call.
    /// On success, carries the fully resolved argument array.
    /// On failure, carries the structured <see cref="ExecutionResult"/> error.
    /// </summary>
    internal readonly struct NestedResolveResult
    {
        private readonly bool _success;
        private readonly ResolvedArg[] _resolvedArgs;
        private readonly ExecutionResult _error;

        /// <summary><c>true</c> when all nested tokens were resolved successfully.</summary>
        internal bool Success { get { return _success; } }

        /// <summary>The resolved argument array. Non-null only when <see cref="Success"/> is <c>true</c>.</summary>
        internal ResolvedArg[] ResolvedArgs { get { return _resolvedArgs; } }

        /// <summary>The structured error result. Meaningful only when <see cref="Success"/> is <c>false</c>.</summary>
        internal ExecutionResult Error { get { return _error; } }

        private NestedResolveResult(bool success, ResolvedArg[] resolvedArgs, ExecutionResult error)
        {
            _success = success;
            _resolvedArgs = resolvedArgs;
            _error = error;
        }

        internal static NestedResolveResult Ok(ResolvedArg[] args)
        {
            return new NestedResolveResult(true, args, default);
        }

        internal static NestedResolveResult Fail(ExecutionResult error)
        {
            return new NestedResolveResult(false, null, error);
        }
    }
}
