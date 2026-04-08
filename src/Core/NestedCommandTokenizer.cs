// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace kmCommands.Core
{
    /// <summary>
    /// Balanced-delimiter-aware whitespace tokenizer.
    /// Splits content by whitespace while keeping nested <c>$(…)</c> groups as atomic tokens.
    /// Pure static — no instance state, no side effects.
    /// </summary>
    internal static class NestedCommandTokenizer
    {
        private const string OpenDelimiter = "$(";
        private const char CloseDelimiter = ')';

        /// <summary>
        /// Tokenizes <paramref name="content"/> by whitespace, treating balanced
        /// <c>$(…)</c> groups as single atomic tokens.
        /// </summary>
        /// <param name="content">The content string to tokenize. <c>null</c> or empty returns an empty array.</param>
        /// <returns>Array of tokens. Never null.</returns>
        internal static string[] Tokenize(string content)
        {
            if (string.IsNullOrEmpty(content))
                return Array.Empty<string>();

            List<string> tokens = new List<string>();
            int i = 0;
            int len = content.Length;

            while (i < len)
            {
                // Skip whitespace
                while (i < len && content[i] == ' ') i++;
                if (i >= len) break;

                int start = i;

                if (i + 1 < len && content[i] == '$' && content[i + 1] == '(')
                {
                    // Nested delimiter token — find matching close paren via depth tracking.
                    int depth = 0;
                    while (i < len)
                    {
                        if (i + 1 < len && content[i] == '$' && content[i + 1] == '(')
                        {
                            depth++;
                            i += 2;
                        }
                        else if (content[i] == CloseDelimiter)
                        {
                            depth--;
                            i++;
                            if (depth == 0) break;
                        }
                        else
                        {
                            i++;
                        }
                    }
                    // If depth never reached 0 (unbalanced), we took everything up to end.
                }
                else
                {
                    // Normal token — read until whitespace.
                    while (i < len && content[i] != ' ') i++;
                }

                if (i > start)
                    tokens.Add(content.Substring(start, i - start));
            }

            return tokens.ToArray();
        }
    }
}
