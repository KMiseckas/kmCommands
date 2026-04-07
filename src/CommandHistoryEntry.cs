// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using kmCommands.Core;

namespace kmCommands
{
    /// <summary>
    /// An immutable record of a single command execution event. Entries are created for all
    /// executions that pass the <c>IsInitialized</c> guard, including both successful and failed
    /// outcomes. The <see cref="Status"/> property distinguishes success from failure.
    /// </summary>
    public readonly struct CommandHistoryEntry
    {
        private readonly string _commandName;
        private readonly string[] _args;
        private readonly object _returnValue;
        private readonly DateTime _timestamp;
        private readonly string[] _rawInput;
        private readonly ExecutionError _status;
        private readonly string _errorDetail;

        /// <summary>
        /// The command name as passed to <see cref="CommandSystem.Execute"/>.
        /// </summary>
        public string CommandName => _commandName;

        /// <summary>
        /// A snapshot copy of the argument tokens passed to <see cref="CommandSystem.Execute"/>.
        /// Mutating this array does not affect the stored entry or other snapshots.
        /// Never <c>null</c>; a command with no arguments uses <see cref="Array.Empty{T}()"/>.
        /// </summary>
        public string[] Args => _args;

        /// <summary>
        /// The return value from the command execution, or <c>null</c> for void commands or
        /// failed executions.
        /// </summary>
        public object ReturnValue => _returnValue;

        /// <summary>
        /// UTC time at which this entry was recorded.
        /// </summary>
        public DateTime Timestamp => _timestamp;

        /// <summary>
        /// Snapshot of the raw input tokens as passed to <see cref="CommandSystem.Execute"/>,
        /// before any processing. Index 0 is always the command name. Indices 1..n are the
        /// argument tokens. Never <c>null</c>; length is always at least 1.
        /// </summary>
        public string[] RawInput => _rawInput;

        /// <summary>
        /// The execution outcome. <see cref="ExecutionError.None"/> for successful executions;
        /// the specific error value for failures.
        /// </summary>
        public ExecutionError Status => _status;

        /// <summary>
        /// Human-readable error detail, or <c>null</c> for successful executions.
        /// Matches <see cref="ExecutionResult.ErrorMessage"/> for failure entries.
        /// </summary>
        public string ErrorDetail => _errorDetail;

        /// <summary>
        /// Initializes a new <see cref="CommandHistoryEntry"/> with the given execution context.
        /// The caller is responsible for passing already-copied array snapshots.
        /// </summary>
        /// <param name="commandName">The command name as passed to <see cref="CommandSystem.Execute"/>.</param>
        /// <param name="args">A pre-copied, non-null snapshot of the argument tokens.</param>
        /// <param name="returnValue">The return value from the callback, or <c>null</c> for void commands.</param>
        /// <param name="timestamp">The UTC time at which the entry was recorded.</param>
        /// <param name="rawInput">An isolated snapshot of the raw input tokens (command name at index 0, args at 1..n).</param>
        /// <param name="status">The execution outcome status.</param>
        /// <param name="errorDetail">The error message from the result, or <c>null</c> for successful executions.</param>
        internal CommandHistoryEntry(
            string commandName,
            string[] args,
            object returnValue,
            DateTime timestamp,
            string[] rawInput,
            ExecutionError status,
            string errorDetail)
        {
            _commandName = commandName;
            _args        = args;
            _returnValue = returnValue;
            _timestamp   = timestamp;
            _rawInput    = rawInput;
            _status      = status;
            _errorDetail = errorDetail;
        }
    }
}
