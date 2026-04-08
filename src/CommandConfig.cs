// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using kmCommands.Core;

namespace kmCommands
{
    /// <summary>
    /// Holds the initialisation settings for <see cref="CommandSystem"/>, loaded from a JSON
    /// configuration file or string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Construct with <c>new CommandConfig()</c> to get all default values, or use
    /// <see cref="FromJson"/> / <see cref="FromFile"/> to populate from JSON.
    /// </para>
    /// <para>
    /// Configuration files must never contain secrets or credentials.
    /// </para>
    /// </remarks>
    public sealed class CommandConfig
    {
        /// <summary>
        /// The maximum number of history entries to retain.
        /// Defaults to <see cref="CommandSystem.DefaultHistoryCapacity"/>.
        /// Values less than 1 are clamped to 1 by <see cref="CommandSystem.Initialize(CommandConfig)"/>.
        /// </summary>
        public int HistoryCapacity { get; set; } = CommandSystem.DefaultHistoryCapacity;

        /// <summary>
        /// When <c>true</c>, the system initialises in dev mode — dev-only commands are
        /// registered and scan operations behave as if <see cref="ScanOptions.DevMode"/> is
        /// <c>true</c> unless explicitly overridden.
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool DevMode { get; set; }

        /// <summary>
        /// The maximum nesting depth for <c>$(…)</c> command arguments.
        /// Defaults to <see cref="CommandSystem.DefaultNestedCommandDepth"/>.
        /// Values less than 1 are clamped to 1 by <see cref="CommandSystem.Initialize(CommandConfig)"/>.
        /// </summary>
        public int NestedCommandDepth { get; set; } = CommandSystem.DefaultNestedCommandDepth;

        /// <summary>
        /// Parses a JSON string and returns a <see cref="ConfigResult"/> carrying either a
        /// populated <see cref="CommandConfig"/> (with optional warnings) or a failure.
        /// </summary>
        /// <param name="json">
        /// The JSON object string to parse. Must be a flat <c>{ "key": value }</c> object.
        /// </param>
        /// <returns>
        /// A <see cref="ConfigResult"/> with <see cref="ConfigResult.Success"/> set to
        /// <c>true</c> on success, or <c>false</c> if the JSON was malformed or a known key
        /// had the wrong type.
        /// </returns>
        public static ConfigResult FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return ConfigResult.Fail(ConfigError.InvalidJson,
                    "JSON string must not be null or empty.");
            }

            JsonConfigParser.ParseOutput output = JsonConfigParser.Parse(json);
            if (output.HasError)
            {
                return ConfigResult.Fail(ConfigError.InvalidJson, output.Error);
            }

            var config = new CommandConfig();
            List<string> warnings = null;

            for (int i = 0; i < output.Values.Length; i++)
            {
                JsonConfigParser.ParsedValue entry = output.Values[i];

                if (StringEquals(entry.Key, "historyCapacity"))
                {
                    if (entry.ValueType != typeof(int))
                    {
                        return ConfigResult.Fail(ConfigError.TypeMismatch,
                            string.Format("Expected integer for 'historyCapacity', got {0}.",
                                entry.ValueType != null ? entry.ValueType.Name : "null"));
                    }
                    config.HistoryCapacity = (int)entry.Value;
                }
                else if (StringEquals(entry.Key, "devMode"))
                {
                    if (entry.ValueType != typeof(bool))
                    {
                        return ConfigResult.Fail(ConfigError.TypeMismatch,
                            string.Format("Expected boolean for 'devMode', got {0}.",
                                entry.ValueType != null ? entry.ValueType.Name : "null"));
                    }
                    config.DevMode = (bool)entry.Value;
                }
                else if (StringEquals(entry.Key, "nestedCommandDepth"))
                {
                    if (entry.ValueType != typeof(int))
                    {
                        return ConfigResult.Fail(ConfigError.TypeMismatch,
                            string.Format("Expected integer for 'nestedCommandDepth', got {0}.",
                                entry.ValueType != null ? entry.ValueType.Name : "null"));
                    }
                    config.NestedCommandDepth = (int)entry.Value;
                }
                else
                {
                    if (warnings == null)
                    {
                        warnings = new List<string>();
                    }
                    warnings.Add(string.Format("Unknown config key: '{0}'.", entry.Key));
                }
            }

            string[] warningArray = warnings != null
                ? warnings.ToArray()
                : Array.Empty<string>();

            return ConfigResult.Ok(config, warningArray);
        }

        /// <summary>
        /// Reads a JSON file at <paramref name="filePath"/> and returns a
        /// <see cref="ConfigResult"/> in the same way as <see cref="FromJson"/>.
        /// </summary>
        /// <param name="filePath">Path to the JSON configuration file.</param>
        /// <returns>
        /// A <see cref="ConfigResult"/> with <see cref="ConfigResult.Success"/> set to
        /// <c>true</c> on success, or <c>false</c> if the file could not be read or its
        /// contents were invalid.
        /// </returns>
        public static ConfigResult FromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return ConfigResult.Fail(ConfigError.FileReadError,
                    "File path must not be null or empty.");
            }

            string json;
            try
            {
                json = System.IO.File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                return ConfigResult.Fail(ConfigError.FileReadError, ex.Message);
            }

            return FromJson(json);
        }

        private static bool StringEquals(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
