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
        /// <c>true</c> if this parameter has a declared default value and may be omitted at call time.
        /// </summary>
        public bool IsOptional { get; }

        /// <summary>
        /// The declared default value for this parameter, or <c>null</c> if <see cref="IsOptional"/> is <c>false</c>.
        /// The runtime type is guaranteed to be assignable to <see cref="Type"/> (enforced at construction).
        /// </summary>
        public object DefaultValue { get; }

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

        /// <summary>
        /// Initializes a new optional <see cref="CommandParameterInfo"/> with a declared default value.
        /// </summary>
        /// <param name="name">The parameter name. Must not be null.</param>
        /// <param name="type">The parameter type. Must not be null.</param>
        /// <param name="defaultValue">
        /// The default value to inject when this argument is omitted at call time.
        /// Must not be null. Must be assignable to <paramref name="type"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="name"/>, <paramref name="type"/>, or <paramref name="defaultValue"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="defaultValue"/>'s runtime type is not assignable to <paramref name="type"/>.
        /// </exception>
        public CommandParameterInfo(string name, Type type, object defaultValue)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));

            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));

            if (!type.IsAssignableFrom(defaultValue.GetType()))
                throw new ArgumentException(
                    string.Format(
                        "Default value of type '{0}' is not assignable to parameter type '{1}'.",
                        defaultValue.GetType().Name, type.Name),
                    nameof(defaultValue));

            DefaultValue = defaultValue;
            IsOptional = true;
        }
    }
}
