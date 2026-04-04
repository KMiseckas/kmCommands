// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace kmCommands.Core
{
    /// <summary>
    /// Internal string-to-object type converter supporting built-in primitive types.
    /// Uses a dictionary of converter functions for extensibility without runtime code generation.
    /// </summary>
    internal sealed class ArgumentConverter
    {
        /// <summary>
        /// A converter function that attempts to convert a string input to an object of a target type.
        /// Returns <c>true</c> on success and writes the converted value to <paramref name="result"/>.
        /// </summary>
        internal delegate bool TryConvertFunc(string input, out object result);

        private readonly Dictionary<Type, TryConvertFunc> _converters;

        internal ArgumentConverter()
        {
            _converters = new Dictionary<Type, TryConvertFunc>(4)
            {
                { typeof(int),    TryConvertInt    },
                { typeof(float),  TryConvertFloat  },
                { typeof(bool),   TryConvertBool   },
                { typeof(string), TryConvertString }
            };
        }

        /// <summary>
        /// Attempts to convert a string token to the given target type.
        /// </summary>
        /// <param name="targetType">The target .NET type.</param>
        /// <param name="input">The string token to convert.</param>
        /// <param name="result">The converted object value, or <c>null</c> on failure.</param>
        /// <returns><c>true</c> if conversion succeeded; otherwise <c>false</c>.</returns>
        internal bool TryConvert(Type targetType, string input, out object result)
        {
            result = null;

            if (!_converters.TryGetValue(targetType, out TryConvertFunc converter))
            {
                return false;
            }

            return converter(input, out result);
        }

        /// <summary>
        /// Returns <c>true</c> if the given type has a registered converter.
        /// </summary>
        internal bool IsTypeSupported(Type type)
        {
            return _converters.ContainsKey(type);
        }

        /// <summary>
        /// Adds or replaces the converter for the given type.
        /// </summary>
        internal void AddConverter(Type type, TryConvertFunc converter)
        {
            _converters[type] = converter;
        }

        private static bool TryConvertInt(string input, out object result)
        {
            if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                result = value;
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryConvertFloat(string input, out object result)
        {
            if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                result = value;
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryConvertBool(string input, out object result)
        {
            // bool.TryParse handles "True"/"False" case-insensitively.
            if (bool.TryParse(input, out bool value))
            {
                result = value;
                return true;
            }

            result = null;
            return false;
        }

        private static bool TryConvertString(string input, out object result)
        {
            result = input;
            return true;
        }
    }
}
