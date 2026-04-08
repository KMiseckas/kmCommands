// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

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

            int totalCount = definition.Parameters.Length;
            int requiredCount = definition.RequiredParameterCount;
            int actualCount = args != null ? args.Length : 0;

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
            catch (TargetInvocationException ex)
                when (definition.IsInstanceCommand && ex.InnerException is NullReferenceException)
            {
                return ExecutionResult.Fail(
                    ExecutionError.InstanceNull,
                    string.Format(
                        "Command '{0}' failed: the bound instance is null or destroyed.",
                        commandName),
                    ex.InnerException);
            }
            catch (NullReferenceException ex)
                when (definition.IsInstanceCommand)
            {
                // Direct invocations (non-DynamicInvoke fast paths) throw NullReferenceException
                // without the TargetInvocationException wrapper. Treat as InstanceNull.
                return ExecutionResult.Fail(
                    ExecutionError.InstanceNull,
                    string.Format(
                        "Command '{0}' failed: the bound instance is null or destroyed.",
                        commandName),
                    ex);
            }
            catch (TargetInvocationException ex)
            {
                return ExecutionResult.Fail(
                    ExecutionError.CallbackThrewException,
                    string.Format(
                        "Command '{0}' callback threw an exception: {1}",
                        commandName,
                        ex.InnerException != null ? ex.InnerException.Message : ex.Message),
                    ex.InnerException ?? ex);
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

        /// <summary>
        /// Executes a command with a mix of pre-resolved object values and raw string arguments.
        /// Pre-resolved arguments bypass <see cref="ArgumentConverter"/> and are type-checked directly.
        /// String arguments follow the standard conversion path.
        /// </summary>
        /// <param name="commandName">The command name to execute.</param>
        /// <param name="args">
        /// Mixed argument array. <c>null</c> is treated as an empty array.
        /// </param>
        /// <returns>A structured <see cref="ExecutionResult"/> for every outcome.</returns>
        internal ExecutionResult ExecuteResolved(string commandName, ResolvedArg[] args)
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

            int totalCount = definition.Parameters.Length;
            int requiredCount = definition.RequiredParameterCount;
            int actualCount = args != null ? args.Length : 0;

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
                    convertedArgs[i] = param.DefaultValue;
                    continue;
                }

                ResolvedArg ra = args[i];

                if (ra.IsPreResolved)
                {
                    object val = ra.ObjectValue;

                    if (val == null)
                    {
                        if (param.Type.IsValueType)
                        {
                            return ExecutionResult.Fail(
                                ExecutionError.NestedCommandTypeMismatch,
                                string.Format(
                                    "Nested command result at index {0} is null but parameter '{1}' expects value type {2}.",
                                    i, param.Name, param.Type.Name),
                                null);
                        }
                        convertedArgs[i] = null;
                    }
                    else if (param.Type.IsAssignableFrom(val.GetType()))
                    {
                        convertedArgs[i] = val;
                    }
                    else
                    {
                        // Fallback: try string conversion via ToString()
                        string asString = val.ToString();
                        if (_converter.TryConvert(param.Type, asString, out object converted))
                        {
                            convertedArgs[i] = converted;
                        }
                        else
                        {
                            return ExecutionResult.Fail(
                                ExecutionError.NestedCommandTypeMismatch,
                                string.Format(
                                    "Nested command result of type '{0}' at index {1} is not compatible with parameter '{2}' of type '{3}'.",
                                    val.GetType().Name, i, param.Name, param.Type.Name),
                                null);
                        }
                    }
                }
                else
                {
                    if (!_converter.TryConvert(param.Type, ra.StringValue, out object converted))
                    {
                        return ExecutionResult.Fail(
                            ExecutionError.ArgumentConversionFailed,
                            string.Format(
                                "Failed to convert argument '{0}' at index {1}: cannot convert '{2}' to {3}.",
                                param.Name, i, ra.StringValue, param.Type.Name),
                            null);
                    }
                    convertedArgs[i] = converted;
                }
            }

            try
            {
                object returnValue = definition.Callback(convertedArgs);
                return ExecutionResult.Ok(returnValue);
            }
            catch (TargetInvocationException ex)
                when (definition.IsInstanceCommand && ex.InnerException is NullReferenceException)
            {
                return ExecutionResult.Fail(
                    ExecutionError.InstanceNull,
                    string.Format(
                        "Command '{0}' failed: the bound instance is null or destroyed.",
                        commandName),
                    ex.InnerException);
            }
            catch (NullReferenceException ex)
                when (definition.IsInstanceCommand)
            {
                return ExecutionResult.Fail(
                    ExecutionError.InstanceNull,
                    string.Format(
                        "Command '{0}' failed: the bound instance is null or destroyed.",
                        commandName),
                    ex);
            }
            catch (TargetInvocationException ex)
            {
                return ExecutionResult.Fail(
                    ExecutionError.CallbackThrewException,
                    string.Format(
                        "Command '{0}' callback threw an exception: {1}",
                        commandName,
                        ex.InnerException != null ? ex.InnerException.Message : ex.Message),
                    ex.InnerException ?? ex);
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
