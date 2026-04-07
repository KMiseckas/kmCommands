// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands.Core
{
    /// <summary>
    /// Fixed-capacity ring buffer that stores <see cref="CommandHistoryEntry"/> values.
    /// When the buffer is full, the oldest entry is evicted to make room for the newest.
    /// Entries are ordered oldest to newest.
    /// </summary>
    internal sealed class CommandHistoryBuffer
    {
        private readonly CommandHistoryEntry[] _buffer;
        private readonly int _capacity;
        private int _head;
        private int _count;

        /// <summary>
        /// Initializes a new <see cref="CommandHistoryBuffer"/> with the specified capacity.
        /// The internal array is allocated once at construction time.
        /// </summary>
        /// <param name="capacity">The maximum number of entries to retain. Must be at least 1.</param>
        internal CommandHistoryBuffer(int capacity)
        {
            _capacity = capacity;
            _buffer = new CommandHistoryEntry[capacity];
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Gets the current number of recorded entries in the buffer.
        /// </summary>
        internal int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Records a new history entry. Copies the argument tokens before storing.
        /// If the buffer is at capacity, the oldest entry is evicted.
        /// </summary>
        /// <param name="commandName">The command name as passed to <see cref="CommandSystem.Execute"/>.</param>
        /// <param name="args">The argument tokens passed to the command. May be <c>null</c>.</param>
        /// <param name="returnValue">The return value from the callback, or <c>null</c> for void commands.</param>
        /// <param name="timestamp">The UTC time at which the entry was recorded.</param>
        /// <param name="rawInput">Pre-built isolated snapshot of raw input tokens; stored directly without re-copying.</param>
        /// <param name="status">The execution outcome status.</param>
        /// <param name="errorDetail">The error message from the result, or <c>null</c> for successful executions.</param>
        internal void Record(
            string commandName,
            string[] args,
            object returnValue,
            DateTime timestamp,
            string[] rawInput,
            ExecutionError status,
            string errorDetail)
        {
            string[] argsCopy = CopyArgs(args);
            CommandHistoryEntry entry = new CommandHistoryEntry(
                commandName, argsCopy, returnValue,
                timestamp, rawInput, status, errorDetail);

            if (_count < _capacity)
            {
                _buffer[(_head + _count) % _capacity] = entry;
                _count++;
            }
            else
            {
                // Buffer is full: overwrite the oldest slot and advance the head.
                _buffer[_head] = entry;
                _head = (_head + 1) % _capacity;
            }
        }

        /// <summary>
        /// Returns a snapshot of all current entries ordered oldest to newest.
        /// The returned array is independent of the live buffer.
        /// </summary>
        /// <returns>
        /// A new <see cref="CommandHistoryEntry"/> array, or <see cref="Array.Empty{T}()"/>
        /// when the buffer is empty.
        /// </returns>
        internal CommandHistoryEntry[] GetSnapshot()
        {
            if (_count == 0)
            {
                return Array.Empty<CommandHistoryEntry>();
            }

            CommandHistoryEntry[] snapshot = new CommandHistoryEntry[_count];

            for (int i = 0; i < _count; i++)
            {
                snapshot[i] = _buffer[(_head + i) % _capacity];
            }

            return snapshot;
        }

        /// <summary>
        /// Clears all entries from the buffer. Does not zero-fill the internal array;
        /// <see cref="Count"/> controls entry validity.
        /// </summary>
        internal void Clear()
        {
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Returns an independent copy of <paramref name="args"/>.
        /// Returns <see cref="Array.Empty{T}()"/> when the input is <c>null</c> or empty.
        /// </summary>
        private static string[] CopyArgs(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return Array.Empty<string>();
            }

            string[] copy = new string[args.Length];
            Array.Copy(args, copy, args.Length);
            return copy;
        }
    }
}
