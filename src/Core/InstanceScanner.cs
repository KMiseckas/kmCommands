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
            Type[] scanTypes = GetScanTypes(target.GetType(), options.ScanUpTo);
            List<ScanEntry> entries = new List<ScanEntry>();

            for (int t = 0; t < scanTypes.Length; t++)
            {
                Type type = scanTypes[t];

                // Step 1: [Command]-decorated instance methods (all access levels, DeclaredOnly).
                ScanAttributeDecoratedMethods(target, type, instanceKey, options, entries);

                // Step 2: Auto-scan public declared instance members (only in Auto mode).
                if (mode == InstanceScanMode.Auto)
                {
                    ScanPublicMethods(target, type, instanceKey, options, entries);
                    ScanPublicProperties(target, type, instanceKey, options, entries);
                }
            }

            return new ScanResult(entries.ToArray());
        }

        /// <summary>
        /// Returns the ordered list of types to scan for members.
        /// When <paramref name="scanUpTo"/> is null, returns only <paramref name="concreteType"/>.
        /// Otherwise walks from <paramref name="concreteType"/> up the inheritance chain, stopping
        /// before <paramref name="scanUpTo"/> (exclusive) and before <see cref="object"/>.
        /// </summary>
        private static Type[] GetScanTypes(Type concreteType, Type scanUpTo)
        {
            if (scanUpTo == null)
            {
                return new[] { concreteType };
            }

            List<Type> types = new List<Type>();
            Type current = concreteType;
            while (current != null && current != scanUpTo && current != typeof(object))
            {
                types.Add(current);
                current = current.BaseType;
            }

            return types.ToArray();
        }

        // ── Profile-based path (ScanCommandHosts pre-scan) ───────────────────────

        /// <summary>
        /// Builds a <see cref="TypeCommandProfile"/> for the given type by reflecting on its
        /// members and caching validated metadata. Does NOT create delegates or register commands.
        /// </summary>
        internal TypeCommandProfile BuildProfile(Type type, ScanOptions options)
        {
            Type[] scanTypes = GetScanTypes(type, options.ScanUpTo);

            List<TypeCommandProfile.MethodEntry> attrMethods =
                new List<TypeCommandProfile.MethodEntry>();
            List<TypeCommandProfile.MethodEntry> autoMethods =
                new List<TypeCommandProfile.MethodEntry>();
            List<TypeCommandProfile.PropertyEntry> autoProps =
                new List<TypeCommandProfile.PropertyEntry>();

            for (int t = 0; t < scanTypes.Length; t++)
            {
                BuildAttributeMethodEntries(scanTypes[t], attrMethods);
                BuildAutoScanMethodEntries(scanTypes[t], autoMethods);
                BuildAutoScanPropertyEntries(scanTypes[t], autoProps);
            }

            return new TypeCommandProfile(
                attrMethods.ToArray(),
                autoMethods.ToArray(),
                autoProps.ToArray());
        }

        private void BuildAttributeMethodEntries(
            Type type,
            List<TypeCommandProfile.MethodEntry> entries)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Instance | BindingFlags.DeclaredOnly;
            MethodInfo[] methods = type.GetMethods(flags);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                CommandAttribute attr = method.GetCustomAttribute<CommandAttribute>();
                if (attr == null) continue;
                if (method.IsStatic) continue;
                if (method.GetCustomAttribute<CommandIgnoreAttribute>() != null) continue;

                ParameterInfo[] reflectedParams = method.GetParameters();
                CommandParameterInfo[] parameters =
                    BuildParameterInfos(method, reflectedParams);
                if (parameters == null) continue; // validation failed — skip

                entries.Add(new TypeCommandProfile.MethodEntry(
                    method, reflectedParams, parameters,
                    attr.Name, attr.Description, attr.IsDevOnly));
            }
        }

        private void BuildAutoScanMethodEntries(
            Type type,
            List<TypeCommandProfile.MethodEntry> entries)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            MethodInfo[] methods = type.GetMethods(flags);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.IsSpecialName) continue;
                if (method.IsAbstract) continue;
                if (method.GetCustomAttribute<CommandAttribute>() != null) continue;
                if (method.GetCustomAttribute<CommandIgnoreAttribute>() != null) continue;
                if (method.IsGenericMethod || method.IsGenericMethodDefinition) continue;

                ParameterInfo[] reflectedParams = method.GetParameters();
                CommandParameterInfo[] parameters =
                    BuildParameterInfos(method, reflectedParams);
                if (parameters == null) continue; // unsupported type/params — skip

                entries.Add(new TypeCommandProfile.MethodEntry(
                    method, reflectedParams, parameters,
                    method.Name, null, false));
            }
        }

        private void BuildAutoScanPropertyEntries(
            Type type,
            List<TypeCommandProfile.PropertyEntry> entries)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            PropertyInfo[] properties = type.GetProperties(flags);

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (property.GetIndexParameters().Length > 0) continue;
                if (property.GetCustomAttribute<CommandIgnoreAttribute>() != null) continue;

                bool canRead = property.CanRead && property.GetGetMethod() != null;
                bool canWrite = property.CanWrite && property.GetSetMethod() != null;
                bool setterTypeSupported = canWrite &&
                    _converter.IsTypeSupported(property.PropertyType);

                entries.Add(new TypeCommandProfile.PropertyEntry(
                    property, canRead, canWrite, setterTypeSupported));
            }
        }

        /// <summary>
        /// Builds a <see cref="CommandParameterInfo"/> array from the method's parameters.
        /// Returns <c>null</c> if validation fails (ref/out, generic, unsupported type).
        /// </summary>
        private CommandParameterInfo[] BuildParameterInfos(
            MethodInfo method,
            ParameterInfo[] reflectedParams)
        {
            CommandParameterInfo[] parameters =
                new CommandParameterInfo[reflectedParams.Length];
            for (int i = 0; i < reflectedParams.Length; i++)
            {
                ParameterInfo p = reflectedParams[i];
                if (p.ParameterType.IsByRef) return null;
                if (p.ParameterType.IsGenericParameter ||
                    p.ParameterType.ContainsGenericParameters) return null;
                if (!_converter.IsTypeSupported(p.ParameterType)) return null;
                parameters[i] = new CommandParameterInfo(p.Name, p.ParameterType);
            }
            return parameters;
        }

        /// <summary>
        /// Registers commands for the target using a pre-built profile, skipping all member
        /// discovery reflection. Only <see cref="Delegate.CreateDelegate"/> occurs per instance.
        /// </summary>
        internal ScanResult ScanFromProfile(
            object target,
            string instanceKey,
            ScanOptions options,
            InstanceScanMode mode,
            TypeCommandProfile profile)
        {
            List<ScanEntry> entries = new List<ScanEntry>();

            // Step 1: Attribute-decorated methods from profile
            for (int i = 0; i < profile.AttributeMethods.Length; i++)
            {
                TypeCommandProfile.MethodEntry me = profile.AttributeMethods[i];
                if (me.IsDevOnly && !options.DevMode) continue;

                string fullName = string.Format("{0}.{1}", instanceKey, me.CommandName);
                CommandCallback callback = InstanceCallbackBuilder.BuildMethodCallback(
                    target, me.Method, me.ReflectedParams);
                CommandDefinition def = new CommandDefinition(
                    fullName, me.Parameters, callback, me.Description, isInstanceCommand: true);

                if (!_registry.TryRegister(def))
                {
                    entries.Add(new ScanEntry(fullName, RegistrationResult.Fail(
                        RegistrationError.DuplicateCommandName,
                        string.Format(
                            "A command named '{0}' is already registered.", fullName))));
                    continue;
                }
                _instanceRegistry.TrackCommand(instanceKey, fullName);
                entries.Add(new ScanEntry(fullName, RegistrationResult.Ok()));
            }

            // Step 2: Auto-scan from profile (only in Auto mode)
            if (mode == InstanceScanMode.Auto)
            {
                // Methods
                for (int i = 0; i < profile.AutoScanMethods.Length; i++)
                {
                    TypeCommandProfile.MethodEntry me = profile.AutoScanMethods[i];
                    if (!options.DevMode) continue; // implicitly dev-only

                    string fullName = string.Format("{0}.{1}", instanceKey, me.CommandName);
                    CommandCallback callback = InstanceCallbackBuilder.BuildMethodCallback(
                        target, me.Method, me.ReflectedParams);
                    CommandDefinition def = new CommandDefinition(
                        fullName, me.Parameters, callback, null, isInstanceCommand: true);

                    if (!_registry.TryRegister(def))
                    {
                        entries.Add(new ScanEntry(fullName, RegistrationResult.Fail(
                            RegistrationError.DuplicateCommandName,
                            string.Format(
                                "A command named '{0}' is already registered.", fullName))));
                        continue;
                    }
                    _instanceRegistry.TrackCommand(instanceKey, fullName);
                    entries.Add(new ScanEntry(fullName, RegistrationResult.Ok()));
                }

                // Properties
                for (int i = 0; i < profile.AutoScanProperties.Length; i++)
                {
                    TypeCommandProfile.PropertyEntry pe = profile.AutoScanProperties[i];
                    if (!options.DevMode) continue; // implicitly dev-only

                    if (pe.CanRead)
                    {
                        string getterName = string.Format(
                            "{0}.get_{1}", instanceKey, pe.Property.Name);
                        CommandCallback cb =
                            InstanceCallbackBuilder.BuildGetterCallback(target, pe.Property);
                        CommandDefinition def = new CommandDefinition(
                            getterName, Array.Empty<CommandParameterInfo>(),
                            cb, null, isInstanceCommand: true);
                        if (_registry.TryRegister(def))
                        {
                            _instanceRegistry.TrackCommand(instanceKey, getterName);
                            entries.Add(new ScanEntry(getterName, RegistrationResult.Ok()));
                        }
                        else
                        {
                            entries.Add(new ScanEntry(getterName, RegistrationResult.Fail(
                                RegistrationError.DuplicateCommandName,
                                string.Format(
                                    "A command named '{0}' is already registered.", getterName))));
                        }
                    }

                    if (pe.CanWrite && pe.SetterTypeSupported)
                    {
                        string setterName = string.Format(
                            "{0}.set_{1}", instanceKey, pe.Property.Name);
                        CommandParameterInfo[] setterParams = new[]
                        {
                            new CommandParameterInfo("value", pe.Property.PropertyType)
                        };
                        CommandCallback cb =
                            InstanceCallbackBuilder.BuildSetterCallback(target, pe.Property);
                        CommandDefinition def = new CommandDefinition(
                            setterName, setterParams, cb, null, isInstanceCommand: true);
                        if (_registry.TryRegister(def))
                        {
                            _instanceRegistry.TrackCommand(instanceKey, setterName);
                            entries.Add(new ScanEntry(setterName, RegistrationResult.Ok()));
                        }
                        else
                        {
                            entries.Add(new ScanEntry(setterName, RegistrationResult.Fail(
                                RegistrationError.DuplicateCommandName,
                                string.Format(
                                    "A command named '{0}' is already registered.", setterName))));
                        }
                    }
                }
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

                // [CommandIgnore] overrides [Command] — skip entirely.
                if (method.GetCustomAttribute<CommandIgnoreAttribute>() != null) continue;

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
            ScanOptions options,
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

                // [CommandIgnore] explicitly opts this member out.
                if (method.GetCustomAttribute<CommandIgnoreAttribute>() != null) continue;

                // Auto-scanned members are implicitly dev-only.
                if (!options.DevMode) continue;

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
            ScanOptions options,
            List<ScanEntry> entries)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            PropertyInfo[] properties = type.GetProperties(flags);

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];

                // Skip indexers.
                if (property.GetIndexParameters().Length > 0) continue;

                // [CommandIgnore] explicitly opts this member out.
                if (property.GetCustomAttribute<CommandIgnoreAttribute>() != null) continue;

                // Auto-scanned properties are implicitly dev-only.
                if (!options.DevMode) continue;

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
