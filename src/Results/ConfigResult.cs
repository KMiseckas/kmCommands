// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Enumerates the possible failure reasons for a <see cref="CommandConfig"/> parse operation.
    /// </summary>
    public enum ConfigError
    {
        /// <summary>No error. Used when <see cref="ConfigResult.Success"/> is <c>true</c>.</summary>
        None = 0,

        /// <summary>
        /// The JSON input could not be parsed — it was null, empty, or structurally malformed.
        /// </summary>
        InvalidJson,

        /// <summary>
        /// A known configuration key was present but its value had the wrong JSON type
        /// (e.g. <c>"devMode": 42</c> instead of a boolean).
        /// </summary>
        TypeMismatch,

        /// <summary>
        /// The configuration file could not be read — the path was invalid, the file did not
        /// exist, or an I/O error occurred.
        /// </summary>
        FileReadError
    }

    /// <summary>
    /// Carries the outcome of a <see cref="CommandConfig.FromJson"/> or
    /// <see cref="CommandConfig.FromFile"/> operation.
    /// </summary>
    /// <remarks>
    /// On success, <see cref="Success"/> is <c>true</c>, <see cref="Config"/> is non-null, and
    /// <see cref="Warnings"/> contains zero or more warning strings for unknown JSON keys.
    /// On failure, <see cref="Success"/> is <c>false</c>, <see cref="Config"/> is <c>null</c>, and
    /// <see cref="ErrorMessage"/> describes what went wrong.
    /// </remarks>
    public readonly struct ConfigResult
    {
        /// <summary><c>true</c> if the parse operation succeeded.</summary>
        public bool Success { get; }

        /// <summary>
        /// The populated configuration object on success; <c>null</c> when
        /// <see cref="Success"/> is <c>false</c>.
        /// </summary>
        public CommandConfig Config { get; }

        /// <summary>
        /// The failure code when <see cref="Success"/> is <c>false</c>;
        /// <see cref="ConfigError.None"/> on success.
        /// </summary>
        public ConfigError Error { get; }

        /// <summary>
        /// A human-readable description of the failure, or <c>null</c> on success.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Zero or more warning strings (e.g. for unrecognised JSON keys).
        /// Never <c>null</c> when <see cref="Success"/> is <c>true</c>.
        /// </summary>
        public string[] Warnings { get; }

        private ConfigResult(
            bool success,
            CommandConfig config,
            ConfigError error,
            string errorMessage,
            string[] warnings)
        {
            Success = success;
            Config = config;
            Error = error;
            ErrorMessage = errorMessage;
            Warnings = warnings;
        }

        /// <summary>
        /// Creates a successful result with the populated config and optional warnings.
        /// </summary>
        /// <param name="config">The populated configuration. Must not be <c>null</c>.</param>
        /// <param name="warnings">
        /// Warning strings for unknown keys. Must not be <c>null</c>; use
        /// <c>Array.Empty&lt;string&gt;()</c> when there are none.
        /// </param>
        internal static ConfigResult Ok(CommandConfig config, string[] warnings)
        {
            return new ConfigResult(true, config, ConfigError.None, null, warnings);
        }

        /// <summary>
        /// Creates a failed result with the given error code and message.
        /// </summary>
        internal static ConfigResult Fail(ConfigError error, string message)
        {
            return new ConfigResult(false, null, error, message, Array.Empty<string>());
        }
    }
}
