// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Marks a class as an instance command source and configures the default scanning behaviour
    /// when the class is registered via
    /// <see cref="CommandSystem.RegisterInstance(object, string)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the simple <see cref="CommandSystem.RegisterInstance(object, string)"/> overload is
    /// called, the system checks whether the target's type carries this attribute. If found, the
    /// attribute's <see cref="DefaultScanMode"/> is used instead of the library default
    /// (<see cref="InstanceScanMode.Auto"/>).
    /// </para>
    /// <para>
    /// The full overload
    /// <see cref="CommandSystem.RegisterInstance(object, string, ScanOptions, InstanceScanMode)"/>
    /// always uses the explicitly supplied <c>mode</c> argument and ignores this attribute.
    /// </para>
    /// <para>
    /// This attribute is not inherited; apply it directly to every class that requires
    /// non-default scanning behaviour.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class InstanceCommandSourceAttribute : Attribute
    {
        /// <summary>
        /// The scan mode applied when this type is registered via the simple
        /// <see cref="CommandSystem.RegisterInstance(object, string)"/> overload.
        /// Defaults to <see cref="InstanceScanMode.Auto"/>.
        /// </summary>
        public InstanceScanMode DefaultScanMode { get; set; }

        /// <summary>
        /// Initializes a new <see cref="InstanceCommandSourceAttribute"/> with
        /// <see cref="DefaultScanMode"/> set to <see cref="InstanceScanMode.Auto"/>.
        /// </summary>
        public InstanceCommandSourceAttribute()
        {
        }
    }
}
