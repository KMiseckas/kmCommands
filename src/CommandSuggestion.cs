// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands
{
    /// <summary>
    /// An immutable value carrying a matched command name, its parameter descriptors,
    /// and its description. Returned by <see cref="CommandSystem.GetSuggestions"/> overloads
    /// and <see cref="CommandMetadataSnapshot.GetSuggestions"/> overloads.
    /// </summary>
    public readonly struct CommandSuggestion
    {
        /// <summary>
        /// The registered command name.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// The parameter descriptors for the command. Never null.
        /// </summary>
        public CommandParameterInfo[] Parameters { get; }

        /// <summary>
        /// The description text for the command. Never null.
        /// </summary>
        public string Description { get; }

        internal CommandSuggestion(string commandName, CommandParameterInfo[] parameters, string description)
        {
            CommandName = commandName;
            Parameters = parameters;
            Description = description;
        }
    }
}
