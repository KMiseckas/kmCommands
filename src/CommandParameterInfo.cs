// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Describes a single parameter in a command's signature.
    /// Passed by the consumer at registration time to declare the expected argument name and type.
    /// </summary>
    public sealed class CommandParameterInfo
    {
        /// <summary>
        /// The name of the parameter. Used in error messages and future metadata/autocomplete.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The expected .NET type for this parameter.
        /// Must be a type supported by the argument converter (e.g., <see cref="int"/>,
        /// <see cref="float"/>, <see cref="bool"/>, <see cref="string"/>).
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="CommandParameterInfo"/>.
        /// </summary>
        /// <param name="name">The parameter name. Must not be null.</param>
        /// <param name="type">The parameter type. Must not be null.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="name"/> or <paramref name="type"/> is null.
        /// This is a programming-error guard; null is never a valid argument at registration time.
        /// </exception>
        public CommandParameterInfo(string name, Type type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}
