// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace kmCommands.Core
{
    /// <summary>
    /// Minimal hand-rolled JSON object parser.
    /// Handles flat objects with string keys and primitive values (string, int, bool, null).
    /// Does NOT support nested objects, arrays, floating-point, or unicode escape sequences.
    /// </summary>
    internal static class JsonConfigParser
    {
        /// <summary>
        /// Represents a single successfully parsed key-value pair from a JSON object.
        /// </summary>
        internal readonly struct ParsedValue
        {
            /// <summary>The key string (unquoted).</summary>
            internal readonly string Key;

            /// <summary>
            /// The typed value: <c>int</c>, <c>bool</c>, <c>string</c>, or <c>null</c> for JSON null.
            /// </summary>
            internal readonly object Value;

            /// <summary>
            /// The C# type of <see cref="Value"/>: <c>typeof(int)</c>, <c>typeof(bool)</c>,
            /// <c>typeof(string)</c>, or <c>null</c> when the JSON value is <c>null</c>.
            /// </summary>
            internal readonly Type ValueType;

            internal ParsedValue(string key, object value, Type valueType)
            {
                Key = key;
                Value = value;
                ValueType = valueType;
            }
        }

        /// <summary>
        /// The output produced by <see cref="Parse"/>.
        /// On success <see cref="HasError"/> is <c>false</c> and <see cref="Values"/> contains all
        /// parsed key-value pairs (may be empty for <c>{}</c>).
        /// On failure <see cref="HasError"/> is <c>true</c> and <see cref="Error"/> is non-null.
        /// </summary>
        internal readonly struct ParseOutput
        {
            /// <summary>Parsed key-value pairs. Never null; empty when the JSON object is empty.</summary>
            internal readonly ParsedValue[] Values;

            /// <summary>Human-readable error message. Non-null only when <see cref="HasError"/> is <c>true</c>.</summary>
            internal readonly string Error;

            /// <summary><c>true</c> if parsing failed.</summary>
            internal readonly bool HasError;

            private ParseOutput(ParsedValue[] values, string error, bool hasError)
            {
                Values = values;
                Error = error;
                HasError = hasError;
            }

            internal static ParseOutput Success(ParsedValue[] values)
            {
                return new ParseOutput(values, null, false);
            }

            internal static ParseOutput Failure(string error)
            {
                return new ParseOutput(Array.Empty<ParsedValue>(), error, true);
            }
        }

        /// <summary>
        /// Parses a flat JSON object string and returns a <see cref="ParseOutput"/> with the
        /// extracted key-value pairs or an error description.
        /// </summary>
        /// <param name="json">The JSON string to parse. Must not be null.</param>
        internal static ParseOutput Parse(string json)
        {
            int pos = 0;

            SkipWhitespace(json, ref pos);

            if (!Consume(json, ref pos, '{'))
            {
                return ParseOutput.Failure("Expected '{' at the start of JSON object.");
            }

            var results = new List<ParsedValue>();

            SkipWhitespace(json, ref pos);

            // Empty object
            if (pos < json.Length && json[pos] == '}')
            {
                pos++;
                SkipWhitespace(json, ref pos);
                if (pos < json.Length)
                {
                    return ParseOutput.Failure("Unexpected content after closing '}'.");
                }
                return ParseOutput.Success(Array.Empty<ParsedValue>());
            }

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);

                // Parse key
                string key;
                if (!TryParseString(json, ref pos, out key))
                {
                    return ParseOutput.Failure(
                        string.Format("Expected quoted string key at position {0}.", pos));
                }

                SkipWhitespace(json, ref pos);

                if (!Consume(json, ref pos, ':'))
                {
                    return ParseOutput.Failure(
                        string.Format("Expected ':' after key '{0}'.", key));
                }

                SkipWhitespace(json, ref pos);

                // Parse value
                object value;
                Type valueType;
                string valueError;
                if (!TryParseValue(json, ref pos, out value, out valueType, out valueError))
                {
                    return ParseOutput.Failure(
                        string.Format("Error parsing value for key '{0}': {1}", key, valueError));
                }

                // Last-write-wins: remove any prior entry with the same key
                for (int i = results.Count - 1; i >= 0; i--)
                {
                    if (results[i].Key == key)
                    {
                        results.RemoveAt(i);
                        break;
                    }
                }
                results.Add(new ParsedValue(key, value, valueType));

                SkipWhitespace(json, ref pos);

                if (pos >= json.Length)
                {
                    return ParseOutput.Failure("Unexpected end of input; expected ',' or '}'.");
                }

                char separator = json[pos];
                if (separator == '}')
                {
                    pos++;
                    SkipWhitespace(json, ref pos);
                    if (pos < json.Length)
                    {
                        return ParseOutput.Failure("Unexpected content after closing '}'.");
                    }
                    return ParseOutput.Success(results.ToArray());
                }
                else if (separator == ',')
                {
                    pos++;
                    SkipWhitespace(json, ref pos);

                    // Trailing comma check: if next non-whitespace is '}' that is invalid JSON
                    if (pos < json.Length && json[pos] == '}')
                    {
                        return ParseOutput.Failure("Trailing comma before closing '}'.");
                    }
                }
                else
                {
                    return ParseOutput.Failure(
                        string.Format("Expected ',' or '}}' at position {0}.", pos));
                }
            }

            return ParseOutput.Failure("Unexpected end of input; object was not closed.");
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && IsWhitespace(json[pos]))
            {
                pos++;
            }
        }

        private static bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        private static bool Consume(string json, ref int pos, char expected)
        {
            if (pos < json.Length && json[pos] == expected)
            {
                pos++;
                return true;
            }
            return false;
        }

        private static bool TryParseString(string json, ref int pos, out string result)
        {
            result = null;

            if (pos >= json.Length || json[pos] != '"')
            {
                return false;
            }

            pos++; // skip opening quote
            int start = pos;

            while (pos < json.Length)
            {
                char c = json[pos];
                if (c == '\\')
                {
                    // Skip escaped character (basic escape support)
                    pos += 2;
                    continue;
                }
                if (c == '"')
                {
                    result = json.Substring(start, pos - start);
                    pos++; // skip closing quote
                    return true;
                }
                pos++;
            }

            return false; // unterminated string
        }

        private static bool TryParseValue(
            string json,
            ref int pos,
            out object value,
            out Type valueType,
            out string error)
        {
            value = null;
            valueType = null;
            error = null;

            if (pos >= json.Length)
            {
                error = "Unexpected end of input while reading value.";
                return false;
            }

            char c = json[pos];

            // String value
            if (c == '"')
            {
                string strVal;
                if (!TryParseString(json, ref pos, out strVal))
                {
                    error = "Unterminated string value.";
                    return false;
                }
                value = strVal;
                valueType = typeof(string);
                return true;
            }

            // Boolean: true
            if (json.Length - pos >= 4 &&
                json[pos] == 't' && json[pos + 1] == 'r' && json[pos + 2] == 'u' && json[pos + 3] == 'e')
            {
                // Ensure it's not followed by alphanumeric (e.g. "trueish")
                int endPos = pos + 4;
                if (endPos < json.Length && IsAlphanumericOrUnderscore(json[endPos]))
                {
                    error = string.Format("Invalid token at position {0}.", pos);
                    return false;
                }
                value = true;
                valueType = typeof(bool);
                pos += 4;
                return true;
            }

            // Boolean: false
            if (json.Length - pos >= 5 &&
                json[pos] == 'f' && json[pos + 1] == 'a' && json[pos + 2] == 'l' &&
                json[pos + 3] == 's' && json[pos + 4] == 'e')
            {
                int endPos = pos + 5;
                if (endPos < json.Length && IsAlphanumericOrUnderscore(json[endPos]))
                {
                    error = string.Format("Invalid token at position {0}.", pos);
                    return false;
                }
                value = false;
                valueType = typeof(bool);
                pos += 5;
                return true;
            }

            // Null
            if (json.Length - pos >= 4 &&
                json[pos] == 'n' && json[pos + 1] == 'u' && json[pos + 2] == 'l' && json[pos + 3] == 'l')
            {
                int endPos = pos + 4;
                if (endPos < json.Length && IsAlphanumericOrUnderscore(json[endPos]))
                {
                    error = string.Format("Invalid token at position {0}.", pos);
                    return false;
                }
                value = null;
                valueType = null; // JSON null
                pos += 4;
                return true;
            }

            // Integer (including negative)
            if (c == '-' || (c >= '0' && c <= '9'))
            {
                int start = pos;
                if (c == '-')
                {
                    pos++;
                    if (pos >= json.Length || !(json[pos] >= '0' && json[pos] <= '9'))
                    {
                        error = "Invalid negative number: expected digit after '-'.";
                        pos = start;
                        return false;
                    }
                }
                while (pos < json.Length && json[pos] >= '0' && json[pos] <= '9')
                {
                    pos++;
                }
                // Reject floats
                if (pos < json.Length && (json[pos] == '.' || json[pos] == 'e' || json[pos] == 'E'))
                {
                    error = "Floating-point numbers are not supported.";
                    pos = start;
                    return false;
                }

                string numStr = json.Substring(start, pos - start);
                int intVal;
                if (!int.TryParse(numStr, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out intVal))
                {
                    error = string.Format(
                        "Integer value '{0}' is out of the range of System.Int32.", numStr);
                    pos = start;
                    return false;
                }
                value = intVal;
                valueType = typeof(int);
                return true;
            }

            error = string.Format("Unsupported value starting with '{0}' at position {1}.", c, pos);
            return false;
        }

        private static bool IsAlphanumericOrUnderscore(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                   (c >= '0' && c <= '9') || c == '_';
        }
    }
}
