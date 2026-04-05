// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace kmCommands.Core
{
    /// <summary>
    /// Maps instance keys to the command names registered under each key, and to the target
    /// object reference. Supports atomic bulk removal of all commands for a given key.
    /// </summary>
    internal sealed class InstanceRegistry
    {
        // Key → list of full command names registered under that key (OrdinalIgnoreCase)
        private readonly Dictionary<string, List<string>> _keyToNames =
            new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

        // Key → target object reference (strong reference; lifetime managed by consumer)
        private readonly Dictionary<string, object> _keyToTarget =
            new Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Attempts to reserve the given key. Fails if the key is already registered.
        /// Also stores the target reference for lifecycle tracking.
        /// </summary>
        /// <returns><c>true</c> if the key was successfully reserved; <c>false</c> if already taken.</returns>
        internal bool TryReserveKey(string key, object target)
        {
            if (_keyToNames.ContainsKey(key))
            {
                return false;
            }

            _keyToNames[key] = new List<string>();
            _keyToTarget[key] = target;
            return true;
        }

        /// <summary>
        /// Records a command name as belonging to the given key.
        /// The key must have been reserved via <see cref="TryReserveKey"/> before calling this.
        /// </summary>
        internal void TrackCommand(string key, string fullCommandName)
        {
            _keyToNames[key].Add(fullCommandName);
        }

        /// <summary>
        /// Returns the list of command names registered under the given key.
        /// </summary>
        /// <returns><c>true</c> and the name list if found; <c>false</c> otherwise.</returns>
        internal bool TryGetCommandNames(string key, out List<string> names)
        {
            return _keyToNames.TryGetValue(key, out names);
        }

        /// <summary>
        /// Removes all data associated with the given key.
        /// </summary>
        internal void RemoveKey(string key)
        {
            _keyToNames.Remove(key);
            _keyToTarget.Remove(key);
        }

        /// <summary>
        /// Clears all registered keys and their associated data.
        /// </summary>
        internal void Clear()
        {
            _keyToNames.Clear();
            _keyToTarget.Clear();
        }
    }
}
