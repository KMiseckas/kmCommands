// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Describes the reason a command execution operation failed.
    /// </summary>
    public enum ExecutionError
    {
        /// <summary>No error. Execution succeeded.</summary>
        None = 0,

        /// <summary>The command system has not been initialized via <c>Initialize()</c>.</summary>
        NotInitialized,

        /// <summary>The provided command name was null or empty.</summary>
        NullOrEmptyCommandName,

        /// <summary>No command with the given name is registered.</summary>
        CommandNotFound,

        /// <summary>The number of provided arguments does not match the command's parameter count.</summary>
        ArgumentCountMismatch,

        /// <summary>One or more string arguments could not be converted to the declared parameter types.</summary>
        ArgumentConversionFailed,

        /// <summary>The command's callback delegate threw an unhandled exception during execution.</summary>
        CallbackThrewException,

        /// <summary>
        /// The instance bound to this command is null or has been garbage collected.
        /// Call <c>UnregisterInstance</c> to clean up stale commands.
        /// </summary>
        InstanceNull,

        /// <summary>The nesting depth limit was reached before all nested commands could be resolved.</summary>
        NestedCommandDepthExceeded,

        /// <summary>An inner command in a nested expression failed during execution.</summary>
        NestedCommandFailed,

        /// <summary>An inner command returns void and cannot be used as an argument value.</summary>
        NestedCommandVoidReturn,

        /// <summary>A nested command token could not be parsed (e.g., empty expression <c>$()</c>).</summary>
        NestedCommandParseFailed,

        /// <summary>The return type of an inner command is incompatible with the outer parameter type.</summary>
        NestedCommandTypeMismatch
    }

    /// <summary>
    /// Represents the result of a command execution operation.
    /// Check <see cref="Success"/> before using <see cref="Error"/>, <see cref="ErrorMessage"/>,
    /// or <see cref="Exception"/>.
    /// </summary>
    public readonly struct ExecutionResult
    {
        /// <summary>
        /// <c>true</c> if execution succeeded and the callback was invoked; <c>false</c> otherwise.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The specific error that caused execution to fail.
        /// <see cref="ExecutionError.None"/> when <see cref="Success"/> is <c>true</c>.
        /// </summary>
        public ExecutionError Error { get; }

        /// <summary>
        /// A human-readable description of the error, or <c>null</c> on success.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// The exception thrown by the command's callback, if any.
        /// Non-null only when <see cref="Error"/> is <see cref="ExecutionError.CallbackThrewException"/>.
        /// <c>null</c> for all other error conditions and on success.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// The return value produced by the command callback, or <c>null</c> for void commands.
        /// Only meaningful when <see cref="Success"/> is <c>true</c>.
        /// </summary>
        public object ReturnValue { get; }

        /// <summary>
        /// <c>true</c> when the callback produced a non-null return value.
        /// </summary>
        public bool HasReturnValue { get; }

        private ExecutionResult(bool success, ExecutionError error, string errorMessage, Exception exception,
            object returnValue = null, bool hasReturnValue = false)
        {
            Success = success;
            Error = error;
            ErrorMessage = errorMessage;
            Exception = exception;
            ReturnValue = returnValue;
            HasReturnValue = hasReturnValue;
        }

        /// <summary>
        /// Creates a successful execution result.
        /// </summary>
        /// <param name="returnValue">
        /// The return value from the callback, or <c>null</c> for void commands.
        /// </param>
        internal static ExecutionResult Ok(object returnValue = null)
        {
            return new ExecutionResult(true, ExecutionError.None, null, null,
                returnValue, returnValue != null);
        }

        /// <summary>
        /// Creates a failed execution result with the given error, message, and optional exception.
        /// </summary>
        internal static ExecutionResult Fail(ExecutionError error, string message, Exception exception = null)
        {
            return new ExecutionResult(false, error, message, exception);
        }
    }
}
