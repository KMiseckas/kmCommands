// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Marks a static method as a registerable command.
    /// Apply to static methods only; instance methods are reported as failures during scanning.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CommandAttribute : Attribute
    {
        /// <summary>
        /// The command name used to invoke this method via <see cref="CommandSystem.Execute"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// When <c>true</c>, this command is only registered when the scan is run with
        /// <see cref="ScanOptions.DevMode"/> set to <c>true</c>.
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool IsDevOnly { get; set; }

        /// <summary>
        /// An optional human-readable description of what this command does.
        /// Defaults to <c>null</c> when not set.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Initializes a new <see cref="CommandAttribute"/> with the specified command name.
        /// </summary>
        /// <param name="name">The unique command name.</param>
        public CommandAttribute(string name)
        {
            Name = name;
        }
    }
}
