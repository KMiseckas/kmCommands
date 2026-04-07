// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands.Core
{
    /// <summary>
    /// Resolves nested <c>$(…)</c> command tokens within a <c>string[]</c> argument array.
    /// Enforces depth limits, records inner command history, and propagates structured errors.
    /// </summary>
    internal sealed class NestedCommandResolver
    {
        private readonly CommandRegistry _registry;
        private readonly ExecutionHandler _executionHandler;
        private readonly CommandHistoryBuffer _historyBuffer;
        private readonly int _maxDepth;

        internal NestedCommandResolver(
            CommandRegistry registry,
            ExecutionHandler executionHandler,
            CommandHistoryBuffer historyBuffer,
            int maxDepth)
        {
            _registry = registry;
            _executionHandler = executionHandler;
            _historyBuffer = historyBuffer;
            _maxDepth = maxDepth;
        }

        /// <summary>
        /// Resolves all nested command tokens in <paramref name="args"/>.
        /// Returns a <see cref="ResolvedArg"/> array on success, or a structured error.
        /// Inner commands are executed and recorded in history during resolution.
        /// </summary>
        internal NestedResolveResult ResolveArgs(string[] args, int currentDepth)
        {
            if (args == null || args.Length == 0)
                return NestedResolveResult.Ok(Array.Empty<ResolvedArg>());

            ResolvedArg[] resolved = new ResolvedArg[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (!IsNestedToken(arg))
                {
                    resolved[i] = ResolvedArg.FromString(arg);
                    continue;
                }

                // Depth guard — check before going deeper.
                if (currentDepth >= _maxDepth)
                {
                    return NestedResolveResult.Fail(ExecutionResult.Fail(
                        ExecutionError.NestedCommandDepthExceeded,
                        string.Format(
                            "Nested command depth limit ({0}) exceeded at argument index {1}.",
                            _maxDepth, i),
                        null));
                }

                // Parse inner expression — strip "$(" prefix and ")" suffix.
                string content = arg.Substring(2, arg.Length - 3);
                string[] tokens = NestedCommandTokenizer.Tokenize(content);

                if (tokens.Length == 0)
                {
                    return NestedResolveResult.Fail(ExecutionResult.Fail(
                        ExecutionError.NestedCommandParseFailed,
                        string.Format(
                            "Empty nested command expression at argument index {0}.", i),
                        null));
                }

                string innerName = tokens[0];
                string[] innerArgs;
                if (tokens.Length > 1)
                {
                    innerArgs = new string[tokens.Length - 1];
                    for (int j = 1; j < tokens.Length; j++)
                        innerArgs[j - 1] = tokens[j];
                }
                else
                {
                    innerArgs = Array.Empty<string>();
                }

                // Pre-execution validation: command must exist and must not return void.
                if (!_registry.TryGetCommand(innerName, out CommandDefinition innerDef))
                {
                    return NestedResolveResult.Fail(ExecutionResult.Fail(
                        ExecutionError.NestedCommandFailed,
                        string.Format(
                            "Nested command '{0}' at argument index {1} not found.",
                            innerName, i),
                        null));
                }

                if (innerDef.ReturnType == typeof(void))
                {
                    return NestedResolveResult.Fail(ExecutionResult.Fail(
                        ExecutionError.NestedCommandVoidReturn,
                        string.Format(
                            "Nested command '{0}' at argument index {1} returns void and cannot be used as an argument.",
                            innerName, i),
                        null));
                }

                // Recursively resolve the inner command's own args.
                NestedResolveResult innerArgsResolved = ResolveArgs(innerArgs, currentDepth + 1);
                if (!innerArgsResolved.Success)
                    return innerArgsResolved;

                // Execute the inner command.
                DateTime innerTimestamp = DateTime.UtcNow;
                string[] innerRawInput = BuildRawInput(innerName, innerArgs);
                ExecutionResult innerResult = _executionHandler.ExecuteResolved(
                    innerName, innerArgsResolved.ResolvedArgs);

                // Always record the inner command in history.
                _historyBuffer.Record(
                    innerName,
                    innerArgs,
                    innerResult.ReturnValue,
                    innerTimestamp,
                    innerRawInput,
                    innerResult.Error,
                    innerResult.ErrorMessage);

                if (!innerResult.Success)
                {
                    return NestedResolveResult.Fail(ExecutionResult.Fail(
                        ExecutionError.NestedCommandFailed,
                        string.Format(
                            "Nested command '{0}' at argument index {1} failed: {2}",
                            innerName, i, innerResult.ErrorMessage),
                        innerResult.Exception));
                }

                resolved[i] = ResolvedArg.FromObject(innerResult.ReturnValue);
            }

            return NestedResolveResult.Ok(resolved);
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="arg"/> is a balanced nested token
        /// (starts with <c>$(</c> and ends with <c>)</c>, length ≥ 3).
        /// </summary>
        private static bool IsNestedToken(string arg)
        {
            return arg != null
                && arg.Length >= 3
                && arg[0] == '$'
                && arg[1] == '('
                && arg[arg.Length - 1] == ')';
        }

        /// <summary>
        /// Builds the raw input snapshot used for history recording (mirrors the one in <c>CommandSystem</c>).
        /// </summary>
        private static string[] BuildRawInput(string name, string[] args)
        {
            if (args == null || args.Length == 0)
                return new string[] { name };

            string[] raw = new string[1 + args.Length];
            raw[0] = name;
            for (int i = 0; i < args.Length; i++)
                raw[i + 1] = args[i];
            return raw;
        }
    }
}
