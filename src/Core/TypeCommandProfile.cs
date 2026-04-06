// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System.Reflection;

namespace kmCommands.Core
{
    /// <summary>
    /// Immutable per-type profile of validated command-hosting members.
    /// Built once during <see cref="CommandSystem.ScanCommandHosts"/> and reused on every
    /// subsequent <see cref="CommandSystem.RegisterInstance"/> call for that type, skipping
    /// all reflection and parameter-validation work.
    /// </summary>
    internal sealed class TypeCommandProfile
    {
        /// <summary>
        /// Methods decorated with <see cref="CommandAttribute"/> on this type.
        /// </summary>
        internal MethodEntry[] AttributeMethods { get; }

        /// <summary>
        /// Public instance methods eligible for auto-scan (no <see cref="CommandAttribute"/>
        /// and no <see cref="CommandIgnoreAttribute"/>).
        /// </summary>
        internal MethodEntry[] AutoScanMethods { get; }

        /// <summary>
        /// Public instance properties eligible for auto-scan.
        /// </summary>
        internal PropertyEntry[] AutoScanProperties { get; }

        internal TypeCommandProfile(
            MethodEntry[] attributeMethods,
            MethodEntry[] autoScanMethods,
            PropertyEntry[] autoScanProperties)
        {
            AttributeMethods = attributeMethods;
            AutoScanMethods = autoScanMethods;
            AutoScanProperties = autoScanProperties;
        }

        /// <summary>
        /// A <see cref="CommandAttribute"/>-decorated instance method with pre-validated metadata.
        /// </summary>
        internal readonly struct MethodEntry
        {
            internal MethodInfo Method { get; }
            internal ParameterInfo[] ReflectedParams { get; }
            internal CommandParameterInfo[] Parameters { get; }
            internal string CommandName { get; }
            internal string Description { get; }
            internal bool IsDevOnly { get; }

            internal MethodEntry(
                MethodInfo method,
                ParameterInfo[] reflectedParams,
                CommandParameterInfo[] parameters,
                string commandName,
                string description,
                bool isDevOnly)
            {
                Method = method;
                ReflectedParams = reflectedParams;
                Parameters = parameters;
                CommandName = commandName;
                Description = description;
                IsDevOnly = isDevOnly;
            }
        }

        /// <summary>
        /// An auto-scannable public property with pre-validated metadata.
        /// </summary>
        internal readonly struct PropertyEntry
        {
            internal PropertyInfo Property { get; }
            internal bool CanRead { get; }
            internal bool CanWrite { get; }
            internal bool SetterTypeSupported { get; }

            internal PropertyEntry(
                PropertyInfo property,
                bool canRead,
                bool canWrite,
                bool setterTypeSupported)
            {
                Property = property;
                CanRead = canRead;
                CanWrite = canWrite;
                SetterTypeSupported = setterTypeSupported;
            }
        }
    }
}
