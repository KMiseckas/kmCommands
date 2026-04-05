// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using kmCommands.Core;

namespace kmCommands
{
    /// <summary>
    /// Central entry point for the kmCommands system.
    /// Must be initialized via <see cref="Initialize"/> before any other operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Lifecycle:</strong> Call <see cref="Initialize"/> once at startup and
    /// <see cref="Shutdown"/> when the system is no longer needed. Both methods are idempotent.
    /// </para>
    /// <para>
    /// <strong>Thread safety:</strong> This class is not thread-safe. All calls must be made
    /// from the same thread (typically the main thread in a Unity application).
    /// </para>
    /// </remarks>
    public sealed class CommandSystem
    {
        private CommandRegistry _registry;
        private ArgumentConverter _converter;
        private ExecutionHandler _executionHandler;
        private AttributeScanner _attributeScanner;
        private CommandHistoryBuffer _historyBuffer;
        private readonly Dictionary<Type, TypeConverterDelegate> _pendingConverters
            = new Dictionary<Type, TypeConverterDelegate>();

        /// <summary>
        /// The default maximum number of entries stored in the command history buffer.
        /// Used when <see cref="Initialize()"/> is called without an explicit capacity argument.
        /// </summary>
        public const int DefaultHistoryCapacity = 64;

        /// <summary>
        /// Gets a value indicating whether the system has been initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Initializes the command system. Idempotent — calling when already initialized is a no-op.
        /// Uses <see cref="DefaultHistoryCapacity"/> as the history buffer size.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            InitializeCore(DefaultHistoryCapacity);
        }

        /// <summary>
        /// Initializes the command system with an explicit history buffer capacity.
        /// Idempotent — calling when already initialized is a no-op.
        /// </summary>
        /// <param name="historyCapacity">
        /// The maximum number of history entries to retain. Values less than 1 are clamped to 1.
        /// </param>
        public void Initialize(int historyCapacity)
        {
            if (IsInitialized)
            {
                return;
            }

            InitializeCore(historyCapacity);
        }

        /// <summary>
        /// Initializes the command system and scans the given types for
        /// <see cref="CommandAttribute"/>-decorated methods.
        /// Idempotent — if already initialized, returns a <see cref="ScanResult"/> with
        /// <see cref="ScanResult.IsAlreadyInitialized"/> set to <c>true</c> and no scan is run.
        /// </summary>
        /// <param name="types">
        /// Types to scan. Null array and null items are silently skipped.
        /// </param>
        /// <param name="options">
        /// Scan configuration. When <see cref="ScanOptions.DevMode"/> is <c>false</c> (default),
        /// commands decorated with <c>IsDevOnly = true</c> are skipped.
        /// </param>
        /// <param name="historyCapacity">
        /// The maximum number of history entries to retain. Values less than 1 are clamped to 1.
        /// Defaults to <see cref="DefaultHistoryCapacity"/>.
        /// </param>
        /// <returns>
        /// An aggregated <see cref="ScanResult"/> across all provided types, or a no-op result
        /// when the system was already initialized.
        /// </returns>
        public ScanResult Initialize(
            Type[] types,
            ScanOptions options = default,
            int historyCapacity = DefaultHistoryCapacity)
        {
            if (IsInitialized)
            {
                return ScanResult.AlreadyInitialized();
            }

            InitializeCore(historyCapacity);
            return RunInitTimeScans(types, null, options);
        }

        /// <summary>
        /// Initializes the command system and scans all types in the given assemblies for
        /// <see cref="CommandAttribute"/>-decorated methods.
        /// Idempotent — if already initialized, returns a <see cref="ScanResult"/> with
        /// <see cref="ScanResult.IsAlreadyInitialized"/> set to <c>true</c> and no scan is run.
        /// </summary>
        /// <param name="assemblies">
        /// Assemblies to scan. Null array and null items are silently skipped.
        /// </param>
        /// <param name="options">
        /// Scan configuration. When <see cref="ScanOptions.DevMode"/> is <c>false</c> (default),
        /// commands decorated with <c>IsDevOnly = true</c> are skipped.
        /// </param>
        /// <param name="historyCapacity">
        /// The maximum number of history entries to retain. Values less than 1 are clamped to 1.
        /// Defaults to <see cref="DefaultHistoryCapacity"/>.
        /// </param>
        /// <returns>
        /// An aggregated <see cref="ScanResult"/> across all provided assemblies, or a no-op result
        /// when the system was already initialized.
        /// </returns>
        public ScanResult Initialize(
            Assembly[] assemblies,
            ScanOptions options = default,
            int historyCapacity = DefaultHistoryCapacity)
        {
            if (IsInitialized)
            {
                return ScanResult.AlreadyInitialized();
            }

            InitializeCore(historyCapacity);
            return RunInitTimeScans(null, assemblies, options);
        }

        /// <summary>
        /// Initializes the command system and scans the given types and assemblies for
        /// <see cref="CommandAttribute"/>-decorated methods.
        /// Idempotent — if already initialized, returns a <see cref="ScanResult"/> with
        /// <see cref="ScanResult.IsAlreadyInitialized"/> set to <c>true</c> and no scan is run.
        /// </summary>
        /// <param name="types">
        /// Types to scan. Null array and null items are silently skipped.
        /// </param>
        /// <param name="assemblies">
        /// Assemblies to scan. Null array and null items are silently skipped.
        /// </param>
        /// <param name="options">
        /// Scan configuration. When <see cref="ScanOptions.DevMode"/> is <c>false</c> (default),
        /// commands decorated with <c>IsDevOnly = true</c> are skipped.
        /// </param>
        /// <param name="historyCapacity">
        /// The maximum number of history entries to retain. Values less than 1 are clamped to 1.
        /// Defaults to <see cref="DefaultHistoryCapacity"/>.
        /// </param>
        /// <returns>
        /// An aggregated <see cref="ScanResult"/> across all provided types and assemblies, or a
        /// no-op result when the system was already initialized.
        /// </returns>
        public ScanResult Initialize(
            Type[] types,
            Assembly[] assemblies,
            ScanOptions options = default,
            int historyCapacity = DefaultHistoryCapacity)
        {
            if (IsInitialized)
            {
                return ScanResult.AlreadyInitialized();
            }

            InitializeCore(historyCapacity);
            return RunInitTimeScans(types, assemblies, options);
        }

        /// <summary>
        /// Constructs the full object graph (registry, converter, execution handler, attribute scanner,
        /// history buffer) and flushes any pending converters registered before initialization.
        /// Must only be called after the idempotency guard confirms initialization is needed.
        /// </summary>
        private void InitializeCore(int historyCapacity)
        {
            int effectiveCapacity = historyCapacity < 1 ? 1 : historyCapacity;

            _registry = new CommandRegistry();
            _converter = new ArgumentConverter();
            _executionHandler = new ExecutionHandler(_registry, _converter);
            _attributeScanner = new AttributeScanner(_registry, _converter);

            foreach (KeyValuePair<Type, TypeConverterDelegate> entry in _pendingConverters)
            {
                _converter.AddConverter(entry.Key, AdaptConverter(entry.Value));
            }

            _pendingConverters.Clear();
            _historyBuffer = new CommandHistoryBuffer(effectiveCapacity);
            IsInitialized = true;
        }

        /// <summary>
        /// Scans the given types and assemblies and merges all per-command outcomes into
        /// a single aggregated <see cref="ScanResult"/>.
        /// Null arrays and null items within arrays are silently skipped.
        /// Must only be called after <see cref="InitializeCore"/> has run.
        /// </summary>
        private ScanResult RunInitTimeScans(Type[] types, Assembly[] assemblies, ScanOptions options)
        {
            List<ScanEntry> all = new List<ScanEntry>();

            if (types != null)
            {
                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i] == null) { continue; }
                    ScanResult r = _attributeScanner.ScanType(types[i], options);
                    for (int j = 0; j < r.Entries.Length; j++)
                    {
                        all.Add(r.Entries[j]);
                    }
                }
            }

            if (assemblies != null)
            {
                for (int i = 0; i < assemblies.Length; i++)
                {
                    if (assemblies[i] == null) { continue; }
                    ScanResult r = _attributeScanner.ScanAssembly(assemblies[i], options);
                    for (int j = 0; j < r.Entries.Length; j++)
                    {
                        all.Add(r.Entries[j]);
                    }
                }
            }

            return new ScanResult(all.ToArray());
        }

        /// <summary>
        /// Shuts down the command system, clearing all registered commands.
        /// Idempotent — calling when not initialized is a no-op.
        /// </summary>
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            _registry = null;
            _converter = null;
            _executionHandler = null;
            _attributeScanner = null;
            _historyBuffer = null;
            _pendingConverters.Clear();
            IsInitialized = false;
        }

        /// <summary>
        /// Registers a custom type converter for the specified type.
        /// If a converter for <paramref name="type"/> is already registered (built-in or custom),
        /// the new converter replaces it (last-write wins).
        /// </summary>
        /// <remarks>
        /// Converters registered before <see cref="Initialize"/> is called are buffered and flushed
        /// into the argument-conversion pipeline when <see cref="Initialize"/> runs.
        /// <see cref="Shutdown"/> clears all custom converters. Re-registering after a new
        /// <see cref="Initialize"/> cycle is supported.
        /// </remarks>
        /// <param name="type">
        /// The <see cref="System.Type"/> that this converter handles. Must not be <c>null</c>.
        /// </param>
        /// <param name="converter">
        /// The converter delegate. Must not be <c>null</c>.
        /// </param>
        /// <returns>
        /// A <see cref="RegistrationResult"/> indicating success or the specific failure reason.
        /// Returns failure with <see cref="RegistrationError.NullParameters"/> when
        /// <paramref name="type"/> is <c>null</c>, or <see cref="RegistrationError.NullConverter"/>
        /// when <paramref name="converter"/> is <c>null</c>.
        /// </returns>
        public RegistrationResult RegisterConverter(Type type, TypeConverterDelegate converter)
        {
            if (type == null)
            {
                return RegistrationResult.Fail(
                    RegistrationError.NullParameters,
                    "Type argument must not be null.");
            }

            if (converter == null)
            {
                return RegistrationResult.Fail(
                    RegistrationError.NullConverter,
                    "Converter delegate must not be null.");
            }

            if (!IsInitialized)
            {
                _pendingConverters[type] = converter;
                return RegistrationResult.Ok();
            }

            _converter.AddConverter(type, AdaptConverter(converter));
            return RegistrationResult.Ok();
        }

        /// <summary>
        /// Adapts a public <see cref="TypeConverterDelegate"/> to the internal
        /// <see cref="ArgumentConverter.TryConvertFunc"/> type via a thin lambda wrapper.
        /// The wrapper is allocated once at registration time, not on the execute hot path.
        /// </summary>
        private static ArgumentConverter.TryConvertFunc AdaptConverter(TypeConverterDelegate d)
        {
            return (string input, out object result) => d(input, out result);
        }

        /// <summary>
        /// Registers a command with the given name, parameter signature, and callback.
        /// </summary>
        /// <param name="name">
        /// The unique command name. Lookup is case-insensitive;
        /// the original casing is preserved for display/metadata.
        /// </param>
        /// <param name="parameters">
        /// Ordered parameter descriptors. Pass <see cref="System.Array.Empty{T}()"/> for zero-argument commands.
        /// Must not be <c>null</c>.
        /// </param>
        /// <param name="callback">
        /// The delegate to invoke when the command executes.
        /// Arguments will be pre-converted to the types declared in <paramref name="parameters"/>.
        /// </param>
        /// <returns>
        /// A <see cref="RegistrationResult"/> describing success or the specific failure reason.
        /// </returns>
        public RegistrationResult Register(
            string name,
            CommandParameterInfo[] parameters,
            CommandCallback callback)
        {
            return Register(name, parameters, callback, null);
        }

        /// <summary>
        /// Registers a command with the given name, parameter signature, callback, and optional description.
        /// </summary>
        /// <param name="name">
        /// The unique command name. Lookup is case-insensitive;
        /// the original casing is preserved for display/metadata.
        /// </param>
        /// <param name="parameters">
        /// Ordered parameter descriptors. Pass <see cref="System.Array.Empty{T}()"/> for zero-argument commands.
        /// Must not be <c>null</c>.
        /// </param>
        /// <param name="callback">
        /// The delegate to invoke when the command executes.
        /// Arguments will be pre-converted to the types declared in <paramref name="parameters"/>.
        /// </param>
        /// <param name="description">
        /// An optional human-readable description of what this command does.
        /// Pass <c>null</c> to register without a description.
        /// </param>
        /// <returns>
        /// A <see cref="RegistrationResult"/> describing success or the specific failure reason.
        /// </returns>
        public RegistrationResult Register(
            string name,
            CommandParameterInfo[] parameters,
            CommandCallback callback,
            string description)
        {
            if (!IsInitialized)
            {
                return RegistrationResult.Fail(
                    RegistrationError.NotInitialized,
                    "CommandSystem has not been initialized. Call Initialize() first.");
            }

            if (string.IsNullOrEmpty(name))
            {
                return RegistrationResult.Fail(
                    RegistrationError.NullOrEmptyName,
                    "Command name must not be null or empty.");
            }

            if (parameters == null)
            {
                return RegistrationResult.Fail(
                    RegistrationError.NullParameters,
                    "Parameters array must not be null. Use Array.Empty<CommandParameterInfo>() for zero-argument commands.");
            }

            if (callback == null)
            {
                return RegistrationResult.Fail(
                    RegistrationError.NullCallback,
                    "Callback must not be null.");
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (!_converter.IsTypeSupported(parameters[i].Type))
                {
                    return RegistrationResult.Fail(
                        RegistrationError.UnsupportedParameterType,
                        string.Format(
                            "Parameter '{0}' at index {1} has unsupported type '{2}'.",
                            parameters[i].Name, i, parameters[i].Type.Name));
                }
            }

            bool seenOptional = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].IsOptional)
                {
                    seenOptional = true;
                }
                else if (seenOptional)
                {
                    return RegistrationResult.Fail(
                        RegistrationError.OptionalParameterBeforeRequired,
                        string.Format(
                            "Required parameter '{0}' at index {1} appears after an optional parameter. " +
                            "All optional parameters must follow all required parameters.",
                            parameters[i].Name, i));
                }
            }

            CommandDefinition definition = new CommandDefinition(name, parameters, callback, description);

            if (!_registry.TryRegister(definition))
            {
                return RegistrationResult.Fail(
                    RegistrationError.DuplicateCommandName,
                    string.Format("A command named '{0}' is already registered.", name));
            }

            return RegistrationResult.Ok();
        }

        /// <summary>
        /// Executes a registered command by name with the given string argument tokens.
        /// Arguments are converted to the types declared in the command's parameter signature
        /// before the callback is invoked.
        /// </summary>
        /// <param name="commandName">
        /// The name of the command to execute. Lookup is case-insensitive.
        /// </param>
        /// <param name="args">
        /// String argument tokens. <c>null</c> is treated as an empty array.
        /// </param>
        /// <returns>
        /// An <see cref="ExecutionResult"/> describing success or the specific failure reason.
        /// </returns>
        public ExecutionResult Execute(string commandName, string[] args)
        {
            if (!IsInitialized)
            {
                return ExecutionResult.Fail(
                    ExecutionError.NotInitialized,
                    "CommandSystem has not been initialized. Call Initialize() first.",
                    null);
            }

            ExecutionResult result = _executionHandler.Execute(commandName, args);

            if (result.Success)
            {
                _historyBuffer.Record(commandName, args, result.ReturnValue);
            }

            return result;
        }

        /// <summary>
        /// The current number of recorded history entries. Returns 0 when not initialized.
        /// </summary>
        public int HistoryCount
        {
            get { return _historyBuffer != null ? _historyBuffer.Count : 0; }
        }

        /// <summary>
        /// Returns a snapshot of all currently recorded history entries, ordered oldest to newest.
        /// The returned array is independent of the live buffer; subsequent executions do not affect it.
        /// </summary>
        /// <returns>
        /// A new <see cref="CommandHistoryEntry"/> array, or <see cref="Array.Empty{T}()"/> when
        /// the system is not initialized or the history is empty.
        /// </returns>
        public CommandHistoryEntry[] GetHistory()
        {
            if (_historyBuffer == null)
            {
                return Array.Empty<CommandHistoryEntry>();
            }

            return _historyBuffer.GetSnapshot();
        }

        /// <summary>
        /// Clears all entries from the history buffer.
        /// No-op when the system is not initialized.
        /// </summary>
        public void ClearHistory()
        {
            if (_historyBuffer == null)
            {
                return;
            }

            _historyBuffer.Clear();
        }

        /// <summary>
        /// Scans a single type for <see cref="CommandAttribute"/>-decorated static methods and
        /// registers each as a command.
        /// </summary>
        /// <param name="type">The type to scan. Must not be <c>null</c>.</param>
        /// <param name="options">
        /// Scan configuration. When <see cref="ScanOptions.DevMode"/> is <c>false</c> (default),
        /// commands decorated with <c>IsDevOnly = true</c> are silently skipped.
        /// </param>
        /// <returns>
        /// A <see cref="ScanResult"/> containing per-command outcomes. Check
        /// <see cref="ScanResult.HasErrors"/> and iterate <see cref="ScanResult.Entries"/> to
        /// surface individual failures.
        /// </returns>
        public ScanResult Scan(Type type, ScanOptions options = default)
        {
            if (!IsInitialized)
            {
                return ScanResult.SystemFailure(
                    RegistrationError.NotInitialized,
                    "CommandSystem has not been initialized. Call Initialize() first.");
            }

            if (type == null)
            {
                return ScanResult.SystemFailure(
                    RegistrationError.NullParameters,
                    "Type argument must not be null.");
            }

            return _attributeScanner.ScanType(type, options);
        }

        /// <summary>
        /// Scans all types in the given assembly for <see cref="CommandAttribute"/>-decorated
        /// static methods and registers each as a command.
        /// </summary>
        /// <param name="assembly">The assembly to scan. Must not be <c>null</c>.</param>
        /// <param name="options">
        /// Scan configuration. When <see cref="ScanOptions.DevMode"/> is <c>false</c> (default),
        /// commands decorated with <c>IsDevOnly = true</c> are silently skipped.
        /// </param>
        /// <returns>
        /// A <see cref="ScanResult"/> containing per-command outcomes across all scanned types.
        /// </returns>
        public ScanResult Scan(Assembly assembly, ScanOptions options = default)
        {
            if (!IsInitialized)
            {
                return ScanResult.SystemFailure(
                    RegistrationError.NotInitialized,
                    "CommandSystem has not been initialized. Call Initialize() first.");
            }

            if (assembly == null)
            {
                return ScanResult.SystemFailure(
                    RegistrationError.NullParameters,
                    "Assembly argument must not be null.");
            }

            return _attributeScanner.ScanAssembly(assembly, options);
        }

        /// <summary>
        /// Returns the names of all currently registered commands.
        /// Names are returned sorted by ordinal case-insensitive comparison for deterministic output.
        /// </summary>
        /// <returns>
        /// A snapshot array of command names, or <see cref="Array.Empty{T}()"/> if the system is not
        /// initialized or no commands are registered.
        /// </returns>
        public string[] GetCommandNames()
        {
            if (!IsInitialized)
                return Array.Empty<string>();

            return _registry.GetAllNames();
        }

        /// <summary>
        /// Attempts to retrieve the parameter descriptors for the named command.
        /// Lookup is case-insensitive.
        /// </summary>
        /// <param name="name">The command name to look up.</param>
        /// <param name="parameters">
        /// When this method returns <c>true</c>, the parameter descriptors for the command.
        /// The returned array is the same instance stored in the registry — do not mutate it.
        /// <c>null</c> when this method returns <c>false</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the command was found; <c>false</c> if the system is not initialized,
        /// <paramref name="name"/> is null or empty, or no command with that name is registered.
        /// </returns>
        public bool TryGetCommandParameters(string name, out CommandParameterInfo[] parameters)
        {
            if (!IsInitialized || string.IsNullOrEmpty(name))
            {
                parameters = null;
                return false;
            }

            if (!_registry.TryGetCommand(name, out Core.CommandDefinition definition))
            {
                parameters = null;
                return false;
            }

            parameters = definition.Parameters;
            return true;
        }

        /// <summary>
        /// Returns a read-only snapshot of the full registry state at this moment.
        /// The snapshot is isolated: subsequent <see cref="Register"/> or
        /// <see cref="Scan(System.Type, ScanOptions)"/> calls do not affect an already-taken snapshot.
        /// </summary>
        /// <returns>
        /// A <see cref="CommandMetadataSnapshot"/> capturing the current registry contents,
        /// or <see cref="CommandMetadataSnapshot.Empty"/> if the system is not initialized.
        /// </returns>
        public CommandMetadataSnapshot GetSnapshot()
        {
            if (!IsInitialized)
                return CommandMetadataSnapshot.Empty;

            return _registry.BuildSnapshot();
        }
    }
}
