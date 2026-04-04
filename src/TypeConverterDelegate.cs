// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands
{
    /// <summary>
    /// Represents a method that attempts to convert a string token to an object of a specific type.
    /// </summary>
    /// <param name="input">The raw string token to convert.</param>
    /// <param name="result">
    /// When this method returns <c>true</c>, contains the converted value; otherwise <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if conversion succeeded; <c>false</c> otherwise.</returns>
    public delegate bool TypeConverterDelegate(string input, out object result);
}
