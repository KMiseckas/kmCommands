// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

namespace kmCommands
{
    /// <summary>
    /// An immutable record of a single successful command execution.
    /// </summary>
    public readonly struct CommandHistoryEntry
    {
        /// <summary>
        /// The command name as passed to <see cref="CommandSystem.Execute"/>.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// A snapshot copy of the argument tokens passed to <see cref="CommandSystem.Execute"/>.
        /// Mutating this array does not affect the stored entry or other snapshots.
        /// Never <c>null</c>; a command with no arguments uses <see cref="System.Array.Empty{T}()"/>.
        /// </summary>
        public string[] Args { get; }

        /// <summary>
        /// The return value from the command execution, or <c>null</c> for void commands.
        /// </summary>
        public object ReturnValue { get; }

        /// <summary>
        /// Initializes a new <see cref="CommandHistoryEntry"/> with the given command name,
        /// argument snapshot, and optional return value. The caller is responsible for passing an
        /// already-copied args array.
        /// </summary>
        /// <param name="commandName">The command name as passed to <see cref="CommandSystem.Execute"/>.</param>
        /// <param name="args">A pre-copied, non-null snapshot of the argument tokens.</param>
        /// <param name="returnValue">The return value from the callback, or <c>null</c> for void commands.</param>
        internal CommandHistoryEntry(string commandName, string[] args, object returnValue)
        {
            CommandName = commandName;
            Args = args;
            ReturnValue = returnValue;
        }
    }
}
