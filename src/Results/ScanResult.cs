// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Represents the outcome of scanning a single command method during a
    /// <see cref="CommandSystem.Scan(System.Type, ScanOptions)"/> or
    /// <see cref="CommandSystem.Scan(System.Reflection.Assembly, ScanOptions)"/> operation.
    /// </summary>
    public readonly struct ScanEntry
    {
        /// <summary>
        /// The command name from the <see cref="CommandAttribute"/>.
        /// <c>string.Empty</c> for system-level failures not tied to a specific command.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// The registration outcome for this command.
        /// </summary>
        public RegistrationResult Result { get; }

        internal ScanEntry(string commandName, RegistrationResult result)
        {
            CommandName = commandName;
            Result = result;
        }
    }

    /// <summary>
    /// Represents the aggregate outcome of a <see cref="CommandSystem.Scan"/> operation.
    /// Contains per-command <see cref="ScanEntry"/> results and a quick <see cref="HasErrors"/> flag.
    /// </summary>
    public sealed class ScanResult
    {
        /// <summary>
        /// All per-command outcomes produced during the scan.
        /// </summary>
        public ScanEntry[] Entries { get; }

        /// <summary>
        /// <c>true</c> if any entry in <see cref="Entries"/> has <c>Result.Success == false</c>;
        /// otherwise <c>false</c>.
        /// </summary>
        public bool HasErrors { get; }

        /// <summary>
        /// <c>true</c> when this result was returned because <see cref="CommandSystem"/>
        /// was already initialized; the scan was not run. Distinct from a successful scan
        /// that found zero commands (<c>Entries.Length == 0</c>, <c>IsAlreadyInitialized == false</c>).
        /// </summary>
        public bool IsAlreadyInitialized { get; }

        internal ScanResult(ScanEntry[] entries, bool isAlreadyInitialized = false)
        {
            Entries = entries;
            IsAlreadyInitialized = isAlreadyInitialized;
            bool hasErrors = false;
            for (int i = 0; i < entries.Length; i++)
            {
                if (!entries[i].Result.Success)
                {
                    hasErrors = true;
                    break;
                }
            }
            HasErrors = hasErrors;
        }

        /// <summary>
        /// Creates a <see cref="ScanResult"/> representing a system-level failure not tied
        /// to a specific command (e.g., the system was not initialized).
        /// </summary>
        internal static ScanResult SystemFailure(RegistrationError error, string message)
        {
            return new ScanResult(new[]
            {
                new ScanEntry(string.Empty, RegistrationResult.Fail(error, message))
            });
        }

        /// <summary>
        /// Creates a <see cref="ScanResult"/> indicating that initialization was already complete
        /// and no scan was performed.
        /// </summary>
        internal static ScanResult AlreadyInitialized()
            => new ScanResult(Array.Empty<ScanEntry>(), isAlreadyInitialized: true);
    }
}
