// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Marks an instance method as a registerable command.
    /// Apply to instance methods only; static methods decorated with this attribute are skipped
    /// during instance scanning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In <see cref="InstanceScanMode.Auto"/> mode, a public method decorated with this attribute
    /// takes its command name and <see cref="IsDevOnly"/> flag from the attribute rather than
    /// auto-scan defaults. Private methods decorated with this attribute are also discovered and
    /// registered.
    /// </para>
    /// <para>
    /// In <see cref="InstanceScanMode.AttributeOnly"/> mode, only methods decorated with this
    /// attribute (or <see cref="CommandAttribute"/>) are registered; all other public methods
    /// are ignored.
    /// </para>
    /// <para>
    /// Use <see cref="IsDevOnly"/> to restrict a command to development builds only:
    /// when <c>IsDevOnly = true</c> and <see cref="ScanOptions.DevMode"/> is <c>false</c>,
    /// the method is silently skipped during registration.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class InstanceCommandAttribute : Attribute
    {
        /// <summary>
        /// The command name used to invoke this method via <see cref="CommandSystem.Execute"/>.
        /// When <c>null</c>, the declaring method's name is used as the command name.
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
        /// Initializes a new <see cref="InstanceCommandAttribute"/> that uses the decorated
        /// method's own name as the command name.
        /// </summary>
        public InstanceCommandAttribute()
        {
            Name = null;
        }

        /// <summary>
        /// Initializes a new <see cref="InstanceCommandAttribute"/> with an explicit command name.
        /// </summary>
        /// <param name="name">
        /// The unique command name used to invoke this method.
        /// Must not be <c>null</c> or empty.
        /// </param>
        public InstanceCommandAttribute(string name)
        {
            Name = name;
        }
    }
}
