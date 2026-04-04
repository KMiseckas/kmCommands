// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
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

        /// <summary>
        /// Gets a value indicating whether the system has been initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Initializes the command system. Idempotent — calling when already initialized is a no-op.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            _registry = new CommandRegistry();
            _converter = new ArgumentConverter();
            _executionHandler = new ExecutionHandler(_registry, _converter);
            _attributeScanner = new AttributeScanner(_registry, _converter);
            IsInitialized = true;
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
            IsInitialized = false;
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

            CommandDefinition definition = new CommandDefinition(name, parameters, callback);

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

            return _executionHandler.Execute(commandName, args);
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
    }
}
