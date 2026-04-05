// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands.Core
{
    /// <summary>
    /// Orchestrates the command execution path:
    /// lookup → argument count validation → argument conversion → callback invocation → result.
    /// </summary>
    internal sealed class ExecutionHandler
    {
        private readonly CommandRegistry _registry;
        private readonly ArgumentConverter _converter;

        internal ExecutionHandler(CommandRegistry registry, ArgumentConverter converter)
        {
            _registry = registry;
            _converter = converter;
        }

        /// <summary>
        /// Executes the named command with the given string argument tokens.
        /// </summary>
        /// <param name="commandName">The command name to execute.</param>
        /// <param name="args">
        /// String argument tokens. <c>null</c> is treated as an empty array.
        /// </param>
        /// <returns>A structured <see cref="ExecutionResult"/> for every outcome.</returns>
        internal ExecutionResult Execute(string commandName, string[] args)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return ExecutionResult.Fail(
                    ExecutionError.NullOrEmptyCommandName,
                    "Command name is null or empty.",
                    null);
            }

            if (!_registry.TryGetCommand(commandName, out CommandDefinition definition))
            {
                return ExecutionResult.Fail(
                    ExecutionError.CommandNotFound,
                    string.Format("Command '{0}' not found.", commandName),
                    null);
            }

            int totalCount    = definition.Parameters.Length;
            int requiredCount = definition.RequiredParameterCount;
            int actualCount   = args != null ? args.Length : 0;

            if (actualCount < requiredCount || actualCount > totalCount)
            {
                string expectedDesc = requiredCount == totalCount
                    ? requiredCount.ToString()
                    : string.Format("between {0} and {1}", requiredCount, totalCount);

                return ExecutionResult.Fail(
                    ExecutionError.ArgumentCountMismatch,
                    string.Format(
                        "Command '{0}' expects {1} argument(s) but received {2}.",
                        commandName, expectedDesc, actualCount),
                    null);
            }

            object[] convertedArgs = totalCount > 0
                ? new object[totalCount]
                : Array.Empty<object>();

            for (int i = 0; i < totalCount; i++)
            {
                CommandParameterInfo param = definition.Parameters[i];

                if (i >= actualCount)
                {
                    // Argument omitted — inject declared default directly, no string conversion.
                    convertedArgs[i] = param.DefaultValue;
                    continue;
                }

                if (!_converter.TryConvert(param.Type, args[i], out object converted))
                {
                    return ExecutionResult.Fail(
                        ExecutionError.ArgumentConversionFailed,
                        string.Format(
                            "Failed to convert argument '{0}' at index {1}: cannot convert '{2}' to {3}.",
                            param.Name, i, args[i], param.Type.Name),
                        null);
                }

                convertedArgs[i] = converted;
            }

            try
            {
                object returnValue = definition.Callback(convertedArgs);
                return ExecutionResult.Ok(returnValue);
            }
            catch (Exception ex)
            {
                return ExecutionResult.Fail(
                    ExecutionError.CallbackThrewException,
                    string.Format(
                        "Command '{0}' callback threw an exception: {1}",
                        commandName, ex.Message),
                    ex);
            }
        }
    }
}
