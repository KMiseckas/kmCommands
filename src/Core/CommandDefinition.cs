// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

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
        internal int RequiredParameterCount { get; }
        internal string Description { get; }

        /// <summary>
        /// <c>true</c> for commands registered via <c>RegisterInstance</c>; <c>false</c> for
        /// statically-registered and manually-registered commands.
        /// </summary>
        internal bool IsInstanceCommand { get; }

        /// <summary>
        /// The declared return type of the command's backing method or property.
        /// Defaults to <see cref="object"/> for manually-registered commands.
        /// <see cref="void"/> for commands that return no value.
        /// </summary>
        internal Type ReturnType { get; }

        internal CommandDefinition(string name, CommandParameterInfo[] parameters, CommandCallback callback,
            string description, bool isInstanceCommand = false, Type returnType = null)
        {
            Name = name;
            Parameters = parameters;
            Callback = callback;
            Description = description;
            IsInstanceCommand = isInstanceCommand;
            ReturnType = returnType ?? typeof(object);

            int required = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!parameters[i].IsOptional)
                    required++;
            }
            RequiredParameterCount = required;
        }
    }
}
