// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands.Core
{
    /// <summary>
    /// Discriminated value carrying either a raw string argument (for normal conversion)
    /// or a pre-resolved object value (from inner command execution).
    /// Consumed by <see cref="ExecutionHandler.ExecuteResolved"/>.
    /// </summary>
    internal readonly struct ResolvedArg
    {
        private readonly bool _isPreResolved;
        private readonly string _stringValue;
        private readonly object _objectValue;

        /// <summary><c>true</c> when this argument holds a pre-resolved object; <c>false</c> for raw strings.</summary>
        internal bool IsPreResolved { get { return _isPreResolved; } }

        /// <summary>The raw string value. Only meaningful when <see cref="IsPreResolved"/> is <c>false</c>.</summary>
        internal string StringValue { get { return _stringValue; } }

        /// <summary>The pre-resolved object value. Only meaningful when <see cref="IsPreResolved"/> is <c>true</c>.</summary>
        internal object ObjectValue { get { return _objectValue; } }

        private ResolvedArg(bool isPreResolved, string stringValue, object objectValue)
        {
            _isPreResolved = isPreResolved;
            _stringValue = stringValue;
            _objectValue = objectValue;
        }

        /// <summary>Creates a raw-string argument. <see cref="IsPreResolved"/> will be <c>false</c>.</summary>
        internal static ResolvedArg FromString(string value)
        {
            return new ResolvedArg(false, value, null);
        }

        /// <summary>Creates a pre-resolved object argument. <see cref="IsPreResolved"/> will be <c>true</c>.</summary>
        internal static ResolvedArg FromObject(object value)
        {
            return new ResolvedArg(true, null, value);
        }
    }
}
