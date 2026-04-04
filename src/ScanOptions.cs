// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands
{
    /// <summary>
    /// Configuration options for a <see cref="CommandSystem.Scan(System.Type, ScanOptions)"/> or
    /// <see cref="CommandSystem.Scan(System.Reflection.Assembly, ScanOptions)"/> operation.
    /// </summary>
    public struct ScanOptions
    {
        /// <summary>
        /// When <c>true</c>, commands decorated with <c>IsDevOnly = true</c> are included in the scan.
        /// When <c>false</c> (default), <c>IsDevOnly</c> commands are silently skipped during scanning.
        /// </summary>
        public bool DevMode { get; set; }
    }
}
