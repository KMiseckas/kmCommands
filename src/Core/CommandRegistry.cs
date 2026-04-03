// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace kmCommands.Core
{
    /// <summary>
    /// Internal dictionary-backed store for <see cref="CommandDefinition"/> instances.
    /// Provides case-insensitive command name lookup via <see cref="System.StringComparer.OrdinalIgnoreCase"/>.
    /// </summary>
    internal sealed class CommandRegistry
    {
        private readonly Dictionary<string, CommandDefinition> _commands =
            new Dictionary<string, CommandDefinition>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The number of registered commands.
        /// </summary>
        internal int Count => _commands.Count;

        /// <summary>
        /// Attempts to register a command definition.
        /// </summary>
        /// <param name="definition">The command definition to register.</param>
        /// <returns>
        /// <c>true</c> if registered successfully; <c>false</c> if a command with the same name
        /// already exists.
        /// </returns>
        internal bool TryRegister(CommandDefinition definition)
        {
            if (_commands.ContainsKey(definition.Name))
            {
                return false;
            }

            _commands.Add(definition.Name, definition);
            return true;
        }

        /// <summary>
        /// Attempts to look up a command by name (case-insensitive).
        /// </summary>
        /// <param name="name">The command name to look up.</param>
        /// <param name="definition">
        /// When this method returns <c>true</c>, the matched command definition; otherwise <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if a command with the given name was found; otherwise <c>false</c>.</returns>
        internal bool TryGetCommand(string name, out CommandDefinition definition)
        {
            return _commands.TryGetValue(name, out definition);
        }

        /// <summary>
        /// Removes all registered commands.
        /// </summary>
        internal void Clear()
        {
            _commands.Clear();
        }
    }
}
