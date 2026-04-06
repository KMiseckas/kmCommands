// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace kmCommands.Core
{
    /// <summary>
    /// Discovers registerable members on a target type and registers them as instance commands
    /// under the <c>instanceKey.commandName</c> naming scheme.
    /// </summary>
    internal sealed class InstanceScanner
    {
        private readonly CommandRegistry _registry;
        private readonly ArgumentConverter _converter;
        private readonly InstanceRegistry _instanceRegistry;

        internal InstanceScanner(
            CommandRegistry registry,
            ArgumentConverter converter,
            InstanceRegistry instanceRegistry)
        {
            _registry = registry;
            _converter = converter;
            _instanceRegistry = instanceRegistry;
        }

        /// <summary>
        /// Scans the target's type for registerable members and registers them.
        /// </summary>
        internal ScanResult Scan(
            object target,
            string instanceKey,
            ScanOptions options,
            InstanceScanMode mode)
        {
            Type type = target.GetType();
            List<ScanEntry> entries = new List<ScanEntry>();

            // Step 1: [Command]-decorated instance methods (all access levels, DeclaredOnly).
            ScanAttributeDecoratedMethods(target, type, instanceKey, options, entries);

            // Step 2: Auto-scan public declared instance members (only in Auto mode).
            if (mode == InstanceScanMode.Auto)
            {
                ScanPublicMethods(target, type, instanceKey, entries);
                ScanPublicProperties(target, type, instanceKey, entries);
            }

            return new ScanResult(entries.ToArray());
        }

        // ── Step 1: [Command]-decorated instance methods ─────────────────────────

        private void ScanAttributeDecoratedMethods(
            object target,
            Type type,
            string instanceKey,
            ScanOptions options,
            List<ScanEntry> entries)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Instance | BindingFlags.DeclaredOnly;

            MethodInfo[] methods = type.GetMethods(flags);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                CommandAttribute attr = method.GetCustomAttribute<CommandAttribute>();
                if (attr == null) continue;

                // Skip static methods — they belong to static scan, not instance scan.
                if (method.IsStatic) continue;

                // Dev-only filter.
                if (attr.IsDevOnly && !options.DevMode) continue;

                string commandName = attr.Name;
                string fullName = string.Format("{0}.{1}", instanceKey, commandName);

                ScanEntry? entry = ValidateAndRegisterMethod(
                    target, method, fullName, attr.Description);

                if (entry.HasValue)
                {
                    entries.Add(entry.Value);
                }
            }
        }

        // ── Step 2a: Auto-scan public declared instance methods ──────────────────

        private void ScanPublicMethods(
            object target,
            Type type,
            string instanceKey,
            List<ScanEntry> entries)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            MethodInfo[] methods = type.GetMethods(flags);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];

                // Skip property accessors, operator overloads, and other special names.
                if (method.IsSpecialName) continue;
                if (method.IsAbstract) continue;

                // Skip [Command]-decorated methods — already handled in Step 1.
                if (method.GetCustomAttribute<CommandAttribute>() != null) continue;

                // Generic methods cannot be registered — produce a descriptive failed entry.
                if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                {
                    string genericName = string.Format("{0}.{1}", instanceKey, method.Name);
                    entries.Add(new ScanEntry(genericName, RegistrationResult.Fail(
                        RegistrationError.InvalidMethod,
                        string.Format(
                            "Method '{0}' is generic and cannot be registered as a command.",
                            method.Name))));
                    continue;
                }

                string fullName = string.Format("{0}.{1}", instanceKey, method.Name);

                ScanEntry? entry = ValidateAndRegisterMethod(
                    target, method, fullName, null);

                if (entry.HasValue)
                {
                    entries.Add(entry.Value);
                }
            }
        }

        // ── Step 2b: Auto-scan public declared instance properties ───────────────

        private void ScanPublicProperties(
            object target,
            Type type,
            string instanceKey,
            List<ScanEntry> entries)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            PropertyInfo[] properties = type.GetProperties(flags);

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];

                // Skip indexers.
                if (property.GetIndexParameters().Length > 0) continue;

                // Getter command
                if (property.CanRead && property.GetGetMethod() != null)
                {
                    string getterName = string.Format("{0}.get_{1}", instanceKey, property.Name);
                    ScanEntry getterEntry = RegisterPropertyGetter(target, property, getterName);
                    entries.Add(getterEntry);
                }

                // Setter command (only if property type is supported)
                if (property.CanWrite && property.GetSetMethod() != null)
                {
                    if (!_converter.IsTypeSupported(property.PropertyType))
                    {
                        // Skip setter silently — unsupported type cannot be set via string arg.
                        continue;
                    }

                    string setterName = string.Format("{0}.set_{1}", instanceKey, property.Name);
                    ScanEntry setterEntry = RegisterPropertySetter(target, property, setterName);
                    entries.Add(setterEntry);
                }
            }
        }

        // ── Registration helpers ─────────────────────────────────────────────────

        private ScanEntry? ValidateAndRegisterMethod(
            object target,
            MethodInfo method,
            string fullName,
            string description)
        {
            ParameterInfo[] reflectedParams = method.GetParameters();

            // Validate parameters: no ref/out/in, no generic type params, type support check.
            CommandParameterInfo[] parameters = new CommandParameterInfo[reflectedParams.Length];
            for (int i = 0; i < reflectedParams.Length; i++)
            {
                ParameterInfo p = reflectedParams[i];

                if (p.ParameterType.IsByRef)
                {
                    return new ScanEntry(fullName, RegistrationResult.Fail(
                        RegistrationError.InvalidMethod,
                        string.Format(
                            "Method '{0}': parameter '{1}' at index {2} is ref/out/in and cannot be used as a command parameter.",
                            method.Name, p.Name, i)));
                }

                if (p.ParameterType.IsGenericParameter || p.ParameterType.ContainsGenericParameters)
                {
                    return new ScanEntry(fullName, RegistrationResult.Fail(
                        RegistrationError.InvalidMethod,
                        string.Format(
                            "Method '{0}': parameter '{1}' at index {2} has a generic type and cannot be used as a command parameter.",
                            method.Name, p.Name, i)));
                }

                if (!_converter.IsTypeSupported(p.ParameterType))
                {
                    return new ScanEntry(fullName, RegistrationResult.Fail(
                        RegistrationError.UnsupportedParameterType,
                        string.Format(
                            "Method '{0}': parameter '{1}' at index {2} has unsupported type '{3}'.",
                            method.Name, p.Name, i, p.ParameterType.Name)));
                }

                parameters[i] = new CommandParameterInfo(p.Name, p.ParameterType);
            }

            CommandCallback callback = InstanceCallbackBuilder.BuildMethodCallback(
                target, method, reflectedParams);

            CommandDefinition definition = new CommandDefinition(
                fullName, parameters, callback, description, isInstanceCommand: true);

            if (!_registry.TryRegister(definition))
            {
                return new ScanEntry(fullName, RegistrationResult.Fail(
                    RegistrationError.DuplicateCommandName,
                    string.Format("A command named '{0}' is already registered.", fullName)));
            }

            _instanceRegistry.TrackCommand(
                fullName.Substring(0, fullName.IndexOf('.')),
                fullName);

            return new ScanEntry(fullName, RegistrationResult.Ok());
        }

        private ScanEntry RegisterPropertyGetter(object target, PropertyInfo property, string fullName)
        {
            CommandCallback callback = InstanceCallbackBuilder.BuildGetterCallback(target, property);
            CommandDefinition definition = new CommandDefinition(
                fullName, Array.Empty<CommandParameterInfo>(), callback, null, isInstanceCommand: true);

            if (!_registry.TryRegister(definition))
            {
                return new ScanEntry(fullName, RegistrationResult.Fail(
                    RegistrationError.DuplicateCommandName,
                    string.Format("A command named '{0}' is already registered.", fullName)));
            }

            _instanceRegistry.TrackCommand(
                fullName.Substring(0, fullName.IndexOf('.')),
                fullName);

            return new ScanEntry(fullName, RegistrationResult.Ok());
        }

        private ScanEntry RegisterPropertySetter(object target, PropertyInfo property, string fullName)
        {
            CommandParameterInfo[] parameters = new[]
            {
                new CommandParameterInfo("value", property.PropertyType)
            };

            CommandCallback callback = InstanceCallbackBuilder.BuildSetterCallback(target, property);
            CommandDefinition definition = new CommandDefinition(
                fullName, parameters, callback, null, isInstanceCommand: true);

            if (!_registry.TryRegister(definition))
            {
                return new ScanEntry(fullName, RegistrationResult.Fail(
                    RegistrationError.DuplicateCommandName,
                    string.Format("A command named '{0}' is already registered.", fullName)));
            }

            _instanceRegistry.TrackCommand(
                fullName.Substring(0, fullName.IndexOf('.')),
                fullName);

            return new ScanEntry(fullName, RegistrationResult.Ok());
        }
    }
}
