// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Configuration options for a <see cref="CommandSystem.Scan(System.Type, ScanOptions)"/> or
    /// <see cref="CommandSystem.Scan(System.Reflection.Assembly, ScanOptions)"/> operation.
    /// </summary>
    public struct ScanOptions
    {
        /// <summary>
        /// When <c>true</c>, commands decorated with <c>IsDevOnly = true</c> are included in the scan,
        /// and auto-scanned public members are also included.
        /// When <c>false</c> (default), <c>IsDevOnly</c> commands and auto-scanned members are skipped.
        /// </summary>
        public bool DevMode { get; set; }

        /// <summary>
        /// When non-null, <see cref="CommandSystem.RegisterInstance"/> walks the inheritance chain
        /// from the concrete type up to (but not including) this boundary type, accumulating
        /// discoverable members from each level.
        /// When <c>null</c> (default), only members declared directly on the target type are
        /// discovered (<c>BindingFlags.DeclaredOnly</c> behaviour).
        /// </summary>
        /// <remarks>
        /// Typical Unity usage: set to <c>typeof(MonoBehaviour)</c> so intermediate user-defined
        /// base classes are scanned while the MonoBehaviour API surface is excluded.
        /// </remarks>
        public Type ScanUpTo { get; set; }
    }
}
