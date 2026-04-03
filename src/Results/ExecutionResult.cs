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
        CallbackThrewException
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

        private ExecutionResult(bool success, ExecutionError error, string errorMessage, Exception exception)
        {
            Success = success;
            Error = error;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        /// <summary>
        /// Creates a successful execution result.
        /// </summary>
        internal static ExecutionResult Ok()
        {
            return new ExecutionResult(true, ExecutionError.None, null, null);
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
