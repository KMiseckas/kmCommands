// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace kmCommands
{
    /// <summary>
    /// Defines the matching contract for command name lookup used by
    /// <see cref="CommandSystem.GetSuggestions"/> and
    /// <see cref="CommandMetadataSnapshot.GetSuggestions"/>.
    /// </summary>
    /// <remarks>
    /// Implementations receive a prefix and a sorted snapshot of all registered command names,
    /// and return an ordered list of matched names. The library never re-sorts after the matcher
    /// returns; return order is preserved in the final <see cref="CommandSuggestion"/> array.
    /// </remarks>
    public interface ISuggestionMatcher
    {
        /// <summary>
        /// Returns an ordered list of command names from <paramref name="commandNames"/> that
        /// match <paramref name="prefix"/>.
        /// </summary>
        /// <param name="prefix">
        /// The partial input string to match against. Null or empty means "return all".
        /// </param>
        /// <param name="commandNames">
        /// A sorted snapshot of all registered command names at call time.
        /// </param>
        /// <returns>
        /// An ordered list of matched command names. May be empty; must not be null.
        /// </returns>
        IList<string> Match(string prefix, string[] commandNames);
    }
}
