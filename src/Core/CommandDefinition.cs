// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands.Core
{
    /// <summary>
    /// Internal storage model for a registered command.
    /// Holds the command name, parameter signature, and callback delegate.
    /// </summary>
    internal sealed class CommandDefinition
    {
        internal string Name { get; }
        internal CommandParameterInfo[] Parameters { get; }
        internal CommandCallback Callback { get; }

        internal CommandDefinition(string name, CommandParameterInfo[] parameters, CommandCallback callback)
        {
            Name = name;
            Parameters = parameters;
            Callback = callback;
        }
    }
}
