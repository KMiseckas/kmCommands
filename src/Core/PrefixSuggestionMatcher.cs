// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace kmCommands.Core
{
    /// <summary>
    /// Default built-in <see cref="ISuggestionMatcher"/> implementation.
    /// Returns all command names that begin with the given prefix (case-insensitive ordinal).
    /// Null or empty prefix returns all names. Stateless singleton.
    /// </summary>
    internal sealed class PrefixSuggestionMatcher : ISuggestionMatcher
    {
        /// <inheritdoc/>
        public IList<string> Match(string prefix, string[] commandNames)
        {
            if (commandNames == null || commandNames.Length == 0)
                return Array.Empty<string>();

            bool matchAll = string.IsNullOrEmpty(prefix);
            List<string> results = new List<string>();

            for (int i = 0; i < commandNames.Length; i++)
            {
                if (matchAll || commandNames[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    results.Add(commandNames[i]);
            }

            return results;
        }
    }
}
