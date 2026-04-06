// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands
{
    /// <summary>
    /// Controls how <c>RegisterInstance</c> discovers commands on a target type.
    /// </summary>
    public enum InstanceScanMode
    {
        /// <summary>
        /// Auto-scan all declared public instance methods and properties, plus any
        /// <see cref="CommandAttribute"/>-decorated non-public instance methods.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Only discover instance methods explicitly decorated with <see cref="CommandAttribute"/>.
        /// No auto-scan of public methods or properties is performed.
        /// </summary>
        AttributeOnly = 1
    }
}
