// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands
{
    /// <summary>
    /// Describes the reason a command registration operation failed.
    /// </summary>
    public enum RegistrationError
    {
        /// <summary>No error. Registration succeeded.</summary>
        None = 0,

        /// <summary>The command system has not been initialized via <c>Initialize()</c>.</summary>
        NotInitialized,

        /// <summary>The provided command name was null or empty.</summary>
        NullOrEmptyName,

        /// <summary>The provided parameters array was null.</summary>
        NullParameters,

        /// <summary>The provided callback delegate was null.</summary>
        NullCallback,

        /// <summary>A command with the same name is already registered.</summary>
        DuplicateCommandName,

        /// <summary>One or more parameter types are not supported by the argument converter.</summary>
        UnsupportedParameterType,

        /// <summary>The target method is not static. Only static methods can be registered via [Command].</summary>
        InvalidMethod,

        /// <summary>
        /// An optional parameter (one with a default value) appears before a required parameter
        /// in the command's parameter list. All optional parameters must trail all required parameters.
        /// </summary>
        OptionalParameterBeforeRequired
    }

    /// <summary>
    /// Represents the result of a command registration operation.
    /// Check <see cref="Success"/> before using <see cref="Error"/> or <see cref="ErrorMessage"/>.
    /// </summary>
    public readonly struct RegistrationResult
    {
        /// <summary>
        /// <c>true</c> if registration succeeded; <c>false</c> otherwise.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The specific error that caused registration to fail.
        /// <see cref="RegistrationError.None"/> when <see cref="Success"/> is <c>true</c>.
        /// </summary>
        public RegistrationError Error { get; }

        /// <summary>
        /// A human-readable description of the error, or <c>null</c> on success.
        /// </summary>
        public string ErrorMessage { get; }

        private RegistrationResult(bool success, RegistrationError error, string errorMessage)
        {
            Success = success;
            Error = error;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a successful registration result.
        /// </summary>
        internal static RegistrationResult Ok()
        {
            return new RegistrationResult(true, RegistrationError.None, null);
        }

        /// <summary>
        /// Creates a failed registration result with the given error and message.
        /// </summary>
        internal static RegistrationResult Fail(RegistrationError error, string message)
        {
            return new RegistrationResult(false, error, message);
        }
    }
}
