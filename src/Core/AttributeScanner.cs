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
    /// Internal component that discovers <see cref="CommandAttribute"/>-decorated static methods,
    /// validates them, builds AOT-safe <see cref="CommandCallback"/> delegates, and registers
    /// commands into the <see cref="CommandRegistry"/>.
    /// </summary>
    internal sealed class AttributeScanner
    {
        private readonly CommandRegistry _registry;
        private readonly ArgumentConverter _converter;

        internal AttributeScanner(CommandRegistry registry, ArgumentConverter converter)
        {
            _registry = registry;
            _converter = converter;
        }

        /// <summary>
        /// Scans all types in the given assembly for <see cref="CommandAttribute"/>-decorated
        /// static methods and registers each as a command. Handles partially-loaded assemblies
        /// gracefully via <see cref="ReflectionTypeLoadException"/>.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="options">Scan configuration (e.g., dev-mode filtering).</param>
        /// <returns>
        /// A <see cref="ScanResult"/> with per-command outcomes merged across all types.
        /// </returns>
        internal ScanResult ScanAssembly(Assembly assembly, ScanOptions options)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types ?? Array.Empty<Type>();
            }

            List<ScanEntry> entries = new List<ScanEntry>();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                {
                    continue;
                }

                ScanResult typeResult = ScanType(types[i], options);
                for (int j = 0; j < typeResult.Entries.Length; j++)
                {
                    entries.Add(typeResult.Entries[j]);
                }
            }

            return new ScanResult(entries.ToArray());
        }

        /// <summary>
        /// Scans a single type for <see cref="CommandAttribute"/>-decorated static methods and
        /// registers each as a command.
        /// </summary>
        /// <param name="type">The type to scan.</param>
        /// <param name="options">Scan configuration (e.g., dev-mode filtering).</param>
        /// <returns>A <see cref="ScanResult"/> with per-command outcomes.</returns>
        internal ScanResult ScanType(Type type, ScanOptions options)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            MethodInfo[] methods = type.GetMethods(flags);
            List<ScanEntry> entries = new List<ScanEntry>();

            for (int i = 0; i < methods.Length; i++)
            {
                CommandAttribute attr = methods[i].GetCustomAttribute<CommandAttribute>();
                if (attr == null)
                {
                    continue;
                }

                ScanEntry? entry = ProcessMethod(methods[i], attr, options);
                if (entry.HasValue)
                {
                    entries.Add(entry.Value);
                }
            }

            return new ScanResult(entries.ToArray());
        }

        /// <summary>
        /// Processes a single method through the full validation and registration pipeline.
        /// Returns <c>null</c> if the method is silently skipped (e.g., dev-only in non-dev mode).
        /// </summary>
        private ScanEntry? ProcessMethod(MethodInfo method, CommandAttribute attr, ScanOptions options)
        {
            // 1. Silent skip for IsDevOnly commands outside dev mode.
            if (attr.IsDevOnly && !options.DevMode)
            {
                return null;
            }

            string name = attr.Name;

            // 2. Non-static methods are a programmer error — report as failure.
            if (!method.IsStatic)
            {
                return new ScanEntry(name, RegistrationResult.Fail(
                    RegistrationError.InvalidMethod,
                    string.Format(
                        "Method '{0}.{1}' is not static. Only static methods can be registered via [Command].",
                        method.DeclaringType != null ? method.DeclaringType.Name : "?",
                        method.Name)));
            }

            // 3. Parameter mapping and type validation.
            ParameterInfo[] reflectedParams = method.GetParameters();
            CommandParameterInfo[] parameters = new CommandParameterInfo[reflectedParams.Length];

            for (int i = 0; i < reflectedParams.Length; i++)
            {
                Type paramType = reflectedParams[i].ParameterType;
                if (!_converter.IsTypeSupported(paramType))
                {
                    return new ScanEntry(name, RegistrationResult.Fail(
                        RegistrationError.UnsupportedParameterType,
                        string.Format(
                            "Parameter '{0}' at index {1} has unsupported type '{2}'.",
                            reflectedParams[i].Name, i, paramType.Name)));
                }

                parameters[i] = new CommandParameterInfo(reflectedParams[i].Name, paramType);
            }

            // 4. Build AOT-safe callback.
            CommandCallback callback = BuildCallback(method, reflectedParams);

            // 5. Register; fail on duplicate name.
            CommandDefinition definition = new CommandDefinition(name, parameters, callback, attr.Description);
            if (!_registry.TryRegister(definition))
            {
                return new ScanEntry(name, RegistrationResult.Fail(
                    RegistrationError.DuplicateCommandName,
                    string.Format("A command named '{0}' is already registered.", name)));
            }

            // 6. Success.
            return new ScanEntry(name, RegistrationResult.Ok());
        }

        /// <summary>
        /// Creates an AOT-safe <see cref="CommandCallback"/> delegate for the given method.
        /// Uses <see cref="Delegate.CreateDelegate"/> to bind a strongly-typed intermediate
        /// delegate at scan time; the zero-parameter path avoids <c>DynamicInvoke</c> entirely.
        /// </summary>
        private static CommandCallback BuildCallback(MethodInfo method, ParameterInfo[] reflectedParams)
        {
            if (reflectedParams.Length == 0)
            {
                // Zero-parameter fast path: direct delegate call, no boxing.
                Action del = (Action)Delegate.CreateDelegate(typeof(Action), method);
                return _ => del();
            }

            Type[] paramTypes = new Type[reflectedParams.Length];
            for (int i = 0; i < reflectedParams.Length; i++)
            {
                paramTypes[i] = reflectedParams[i].ParameterType;
            }

            Type actionType = GetActionDelegateType(paramTypes);

            // Delegate.CreateDelegate preserves the method reference under IL2CPP stripping.
            // DynamicInvoke on a pre-bound concrete delegate is AOT-safe on Unity 2021+ IL2CPP.
            Delegate typedDelegate = Delegate.CreateDelegate(actionType, method);
            return args => typedDelegate.DynamicInvoke(args);
        }

        /// <summary>
        /// Returns the closed generic <c>Action&lt;T...&gt;</c> type for the given parameter types.
        /// Supports 1–4 parameters; throws <see cref="NotSupportedException"/> for 5+.
        /// </summary>
        private static Type GetActionDelegateType(Type[] paramTypes)
        {
            switch (paramTypes.Length)
            {
                case 1: return typeof(Action<>).MakeGenericType(paramTypes);
                case 2: return typeof(Action<,>).MakeGenericType(paramTypes);
                case 3: return typeof(Action<,,>).MakeGenericType(paramTypes);
                case 4: return typeof(Action<,,,>).MakeGenericType(paramTypes);
                default:
                    throw new NotSupportedException(
                        string.Format(
                            "Commands with {0} parameters are not supported. Maximum is 4.",
                            paramTypes.Length));
            }
        }
    }
}
