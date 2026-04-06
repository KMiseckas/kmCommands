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
        private InstanceRegistry _instanceRegistry;
        private InstanceScanner _instanceScanner;
        private TypeCommandProfileCache _profileCache;
        private bool _devMode;
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
        /// <param name="devMode">
        /// When <c>true</c>, sets the system-wide dev mode flag. All subsequent scan and
        /// <see cref="RegisterInstance"/> operations will behave as if
        /// <see cref="ScanOptions.DevMode"/> is <c>true</c> unless explicitly overridden.
        /// </param>
        public void Initialize(bool devMode = false)
        {
            if (IsInitialized)
            {
                return;
            }

            _devMode = devMode;
            InitializeCore(DefaultHistoryCapacity);
        }

        /// <summary>
        /// Initializes the command system with an explicit history buffer capacity.
        /// Idempotent — calling when already initialized is a no-op.
        /// </summary>
        /// <param name="historyCapacity">
        /// The maximum number of history entries to retain. Values less than 1 are clamped to 1.
        /// </param>
        /// <param name="devMode">
        /// When <c>true</c>, sets the system-wide dev mode flag.
        /// </param>
        public void Initialize(int historyCapacity, bool devMode = false)
        {
            if (IsInitialized)
            {
                return;
            }

            _devMode = devMode;
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
            int historyCapacity = DefaultHistoryCapacity,
            bool devMode = false)
        {
            if (IsInitialized)
            {
                return ScanResult.AlreadyInitialized();
            }

            _devMode = devMode;
            InitializeCore(historyCapacity);
            return RunInitTimeScans(types, null, ResolveEffectiveOptions(options));
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
            int historyCapacity = DefaultHistoryCapacity,
            bool devMode = false)
        {
            if (IsInitialized)
            {
                return ScanResult.AlreadyInitialized();
            }

            _devMode = devMode;
            InitializeCore(historyCapacity);
            return RunInitTimeScans(null, assemblies, ResolveEffectiveOptions(options));
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
            int historyCapacity = DefaultHistoryCapacity,
            bool devMode = false)
        {
            if (IsInitialized)
            {
                return ScanResult.AlreadyInitialized();
            }

            _devMode = devMode;
            InitializeCore(historyCapacity);
            return RunInitTimeScans(types, assemblies, ResolveEffectiveOptions(options));
        }

        /// <summary>
        /// Resolves effective <see cref="ScanOptions"/> by OR-merging the system-wide
        /// <c>_devMode</c> flag with the caller-supplied options.
        /// If either the system flag or the caller's <c>DevMode</c> is <c>true</c>, the
        /// effective <c>DevMode</c> is <c>true</c>.
        /// </summary>
        private ScanOptions ResolveEffectiveOptions(ScanOptions callerOptions)
        {
            if (_devMode && !callerOptions.DevMode)
            {
                callerOptions.DevMode = true;
            }
            return callerOptions;
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
            _instanceRegistry = new InstanceRegistry();
            _instanceScanner = new InstanceScanner(_registry, _converter, _instanceRegistry);
            _profileCache = new TypeCommandProfileCache();

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
            _instanceRegistry?.Clear();
            _instanceRegistry = null;
            _instanceScanner = null;
            _profileCache?.Clear();
            _profileCache = null;
            _historyBuffer = null;
            _devMode = false;
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

            return _attributeScanner.ScanType(type, ResolveEffectiveOptions(options));
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

            return _attributeScanner.ScanAssembly(assembly, ResolveEffectiveOptions(options));
        }

        /// <summary>
        /// Scans an instance target for commands and registers them under the
        /// <c>instanceKey.commandName</c> naming scheme.
        /// Uses <see cref="InstanceScanMode.Auto"/> — registers all public methods and properties
        /// plus any members decorated with <see cref="CommandAttribute"/>.
        /// </summary>
        /// <param name="target">The instance to scan. Must not be <c>null</c>.</param>
        /// <param name="instanceKey">
        /// A unique key that namespaces commands for this instance (e.g., <c>"player"</c>).
        /// Must not be null, empty, or contain a <c>'.'</c> character.
        /// </param>
        /// <returns>A <see cref="ScanResult"/> describing the per-command outcomes.</returns>
        public ScanResult RegisterInstance(object target, string instanceKey)
        {
            return RegisterInstance(target, instanceKey, default, InstanceScanMode.Auto);
        }

        /// <summary>
        /// Scans an instance target for commands and registers them under the
        /// <c>instanceKey.commandName</c> naming scheme.
        /// </summary>
        /// <param name="target">The instance to scan. Must not be <c>null</c>.</param>
        /// <param name="instanceKey">
        /// A unique key that namespaces commands for this instance (e.g., <c>"player"</c>).
        /// Must not be null, empty, or contain a <c>'.'</c> character.
        /// </param>
        /// <param name="options">
        /// Scan configuration. When <see cref="ScanOptions.DevMode"/> is <c>false</c> (default),
        /// <see cref="CommandAttribute"/> members decorated with <c>IsDevOnly = true</c> are skipped.
        /// </param>
        /// <param name="mode">
        /// Controls which members are auto-discovered.
        /// <see cref="InstanceScanMode.Auto"/> registers public methods and properties.
        /// <see cref="InstanceScanMode.AttributeOnly"/> registers only <see cref="CommandAttribute"/>-decorated members.
        /// </param>
        /// <returns>A <see cref="ScanResult"/> describing the per-command outcomes.</returns>
        public ScanResult RegisterInstance(
            object target,
            string instanceKey,
            ScanOptions options,
            InstanceScanMode mode = InstanceScanMode.Auto)
        {
            if (!IsInitialized)
            {
                return ScanResult.SystemFailure(
                    RegistrationError.NotInitialized,
                    "CommandSystem has not been initialized. Call Initialize() first.");
            }

            if (target == null)
            {
                return ScanResult.SystemFailure(
                    RegistrationError.NullTarget,
                    "Target instance must not be null.");
            }

            if (string.IsNullOrEmpty(instanceKey))
            {
                return ScanResult.SystemFailure(
                    RegistrationError.InvalidInstanceKey,
                    "Instance key must not be null or empty.");
            }

            if (instanceKey.IndexOf('.') >= 0)
            {
                return ScanResult.SystemFailure(
                    RegistrationError.InvalidInstanceKey,
                    "Instance key must not contain a '.' character.");
            }

            if (!_instanceRegistry.TryReserveKey(instanceKey, target))
            {
                return ScanResult.SystemFailure(
                    RegistrationError.DuplicateInstanceKey,
                    string.Format("An instance with key '{0}' is already registered.", instanceKey));
            }

            ScanOptions effective = ResolveEffectiveOptions(options);

            if (_profileCache.TryGet(target.GetType(), out TypeCommandProfile profile))
            {
                return _instanceScanner.ScanFromProfile(
                    target, instanceKey, effective, mode, profile);
            }

            return _instanceScanner.Scan(target, instanceKey, effective, mode);
        }

        /// <summary>
        /// Pre-scans the given types and caches their member metadata so that subsequent
        /// <see cref="RegisterInstance"/> calls for instances of those types skip reflection
        /// and parameter-validation work.
        /// </summary>
        /// <param name="types">Types to pre-scan. Null array and null items are silently skipped.</param>
        /// <returns>
        /// A <see cref="ScanResult"/> recording which types were processed.
        /// An empty <see cref="ScanResult"/> is returned if <paramref name="types"/> is null.
        /// </returns>
        public ScanResult ScanCommandHosts(Type[] types)
        {
            return ScanCommandHosts(types, default);
        }

        /// <summary>
        /// Pre-scans the given types and caches their member metadata so that subsequent
        /// <see cref="RegisterInstance"/> calls for instances of those types skip reflection
        /// and parameter-validation work.
        /// </summary>
        /// <param name="types">Types to pre-scan. Null array and null items are silently skipped.</param>
        /// <param name="options">
        /// Scan options used to determine the hierarchy depth (<see cref="ScanOptions.ScanUpTo"/>).
        /// DevMode is resolved at <see cref="RegisterInstance"/> time, not at pre-scan time.
        /// </param>
        /// <returns>
        /// A <see cref="ScanResult"/> recording which types were processed.
        /// An empty <see cref="ScanResult"/> is returned if <paramref name="types"/> is null.
        /// </returns>
        public ScanResult ScanCommandHosts(Type[] types, ScanOptions options)
        {
            if (!IsInitialized)
            {
                return ScanResult.SystemFailure(
                    RegistrationError.NotInitialized,
                    "CommandSystem has not been initialized. Call Initialize() first.");
            }

            if (types == null)
            {
                return new ScanResult(Array.Empty<ScanEntry>());
            }

            List<ScanEntry> entries = new List<ScanEntry>();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null) continue;
                if (types[i].GetCustomAttribute<CommandHostAttribute>() == null) continue;
                TypeCommandProfile profile = _instanceScanner.BuildProfile(types[i], options);
                _profileCache.Add(types[i], profile);
                entries.Add(new ScanEntry(types[i].FullName, RegistrationResult.Ok()));
            }
            return new ScanResult(entries.ToArray());
        }

        /// <summary>
        /// Pre-scans all types in the given assemblies that are decorated with
        /// <see cref="CommandHostAttribute"/> and caches their member metadata.
        /// </summary>
        /// <param name="assemblies">Assemblies to scan. Null array and null items are silently skipped.</param>
        /// <returns>
        /// A <see cref="ScanResult"/> recording which types were pre-scanned.
        /// </returns>
        public ScanResult ScanCommandHosts(Assembly[] assemblies)
        {
            return ScanCommandHosts(assemblies, default);
        }

        /// <summary>
        /// Pre-scans all types in the given assemblies that are decorated with
        /// <see cref="CommandHostAttribute"/> and caches their member metadata.
        /// </summary>
        /// <param name="assemblies">Assemblies to scan. Null array and null items are silently skipped.</param>
        /// <param name="options">
        /// Scan options used to determine the hierarchy depth (<see cref="ScanOptions.ScanUpTo"/>).
        /// DevMode is resolved at <see cref="RegisterInstance"/> time, not at pre-scan time.
        /// </param>
        /// <returns>
        /// A <see cref="ScanResult"/> recording which types were pre-scanned.
        /// </returns>
        public ScanResult ScanCommandHosts(Assembly[] assemblies, ScanOptions options)
        {
            if (!IsInitialized)
            {
                return ScanResult.SystemFailure(
                    RegistrationError.NotInitialized,
                    "CommandSystem has not been initialized. Call Initialize() first.");
            }

            if (assemblies == null)
            {
                return new ScanResult(Array.Empty<ScanEntry>());
            }

            List<ScanEntry> entries = new List<ScanEntry>();
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i] == null) continue;
                Type[] allTypes = assemblies[i].GetTypes();
                for (int t = 0; t < allTypes.Length; t++)
                {
                    Type type = allTypes[t];
                    if (type.GetCustomAttribute<CommandHostAttribute>() == null) continue;
                    TypeCommandProfile profile = _instanceScanner.BuildProfile(type, options);
                    _profileCache.Add(type, profile);
                    entries.Add(new ScanEntry(type.FullName, RegistrationResult.Ok()));
                }
            }
            return new ScanResult(entries.ToArray());
        }

        /// <summary>
        /// Removes all commands registered under the given instance key and releases the
        /// associated instance reference.
        /// Typically called from <c>OnDestroy</c> (Unity) or equivalent cleanup code.
        /// </summary>
        /// <param name="instanceKey">
        /// The key used when the instance was registered via <see cref="RegisterInstance(object,string)"/>.
        /// </param>
        /// <returns>
        /// An <see cref="UnregisterResult"/> indicating success and the number of commands removed,
        /// or a failure with an error message.
        /// </returns>
        public UnregisterResult UnregisterInstance(string instanceKey)
        {
            if (!IsInitialized)
            {
                return UnregisterResult.Fail("CommandSystem has not been initialized.");
            }

            if (string.IsNullOrEmpty(instanceKey))
            {
                return UnregisterResult.Fail("Instance key must not be null or empty.");
            }

            if (!_instanceRegistry.TryGetCommandNames(instanceKey, out System.Collections.Generic.List<string> names))
            {
                return UnregisterResult.Fail(
                    string.Format("No instance registered with key '{0}'.", instanceKey));
            }

            for (int i = 0; i < names.Count; i++)
            {
                _registry.TryRemove(names[i]);
            }

            int removedCount = names.Count;
            _instanceRegistry.RemoveKey(instanceKey);

            return UnregisterResult.Ok(removedCount);
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
