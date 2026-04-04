// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace kmCommands
{
    /// <summary>
    /// An immutable, point-in-time snapshot of the command registry's metadata.
    /// Obtained via <see cref="CommandSystem.GetSnapshot()"/>.
    /// </summary>
    /// <remarks>
    /// The snapshot is isolated from subsequent registrations: calling <see cref="CommandSystem.Register"/>
    /// or <see cref="CommandSystem.Scan(Type, ScanOptions)"/> after taking a snapshot does not
    /// affect any already-taken snapshot instance.
    /// </remarks>
    public sealed class CommandMetadataSnapshot
    {
        private static readonly CommandMetadataSnapshot _empty =
            new CommandMetadataSnapshot(
                Array.Empty<string>(),
                new Dictionary<string, CommandParameterInfo[]>(StringComparer.OrdinalIgnoreCase));

        private readonly Dictionary<string, CommandParameterInfo[]> _entries;

        /// <summary>
        /// All command names captured at snapshot time, sorted by ordinal case-insensitive order.
        /// </summary>
        public string[] CommandNames { get; }

        /// <summary>
        /// A reusable empty snapshot. Returned when the system is not initialized or the registry is empty.
        /// </summary>
        internal static CommandMetadataSnapshot Empty => _empty;

        internal CommandMetadataSnapshot(string[] names, Dictionary<string, CommandParameterInfo[]> entries)
        {
            CommandNames = names;
            _entries = entries;
        }

        /// <summary>
        /// Attempts to retrieve the parameter descriptors for the named command.
        /// Lookup is case-insensitive.
        /// </summary>
        /// <param name="name">The command name to look up.</param>
        /// <param name="parameters">
        /// When this method returns <c>true</c>, the parameter descriptors captured at snapshot time.
        /// <c>null</c> when this method returns <c>false</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the command was found in this snapshot; <c>false</c> if
        /// <paramref name="name"/> is null or empty, or no command with that name was captured.
        /// </returns>
        public bool TryGetParameters(string name, out CommandParameterInfo[] parameters)
        {
            if (string.IsNullOrEmpty(name))
            {
                parameters = null;
                return false;
            }

            return _entries.TryGetValue(name, out parameters);
        }
    }
}
