// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands
{
    /// <summary>
    /// Represents the result of an <c>UnregisterInstance</c> operation.
    /// Check <see cref="Success"/> before reading <see cref="ErrorMessage"/>.
    /// </summary>
    public readonly struct UnregisterResult
    {
        /// <summary>
        /// <c>true</c> if the unregister operation succeeded; <c>false</c> otherwise.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The number of commands removed from the registry.
        /// Zero when <see cref="Success"/> is <c>false</c>.
        /// </summary>
        public int RemovedCount { get; }

        /// <summary>
        /// A human-readable description of the failure, or <c>null</c> on success.
        /// </summary>
        public string ErrorMessage { get; }

        private UnregisterResult(bool success, int removedCount, string errorMessage)
        {
            Success = success;
            RemovedCount = removedCount;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a successful unregister result with the given removed command count.
        /// </summary>
        internal static UnregisterResult Ok(int removedCount)
        {
            return new UnregisterResult(true, removedCount, null);
        }

        /// <summary>
        /// Creates a failed unregister result with a descriptive message.
        /// </summary>
        internal static UnregisterResult Fail(string message)
        {
            return new UnregisterResult(false, 0, message);
        }
    }
}
