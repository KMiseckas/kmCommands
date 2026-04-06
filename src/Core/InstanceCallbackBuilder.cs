// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace kmCommands.Core
{
    /// <summary>
    /// Builds AOT-safe <see cref="CommandCallback"/> delegates that close over a specific
    /// target instance. Uses <see cref="Delegate.CreateDelegate"/> to bind strongly-typed
    /// delegates at registration time — safe under IL2CPP on Unity 2021+.
    /// </summary>
    internal static class InstanceCallbackBuilder
    {
        /// <summary>
        /// Builds a callback for an instance method.
        /// Void methods return <c>null</c>; non-void methods return the boxed return value.
        /// </summary>
        internal static CommandCallback BuildMethodCallback(
            object target,
            MethodInfo method,
            ParameterInfo[] parameters)
        {
            bool isVoid = method.ReturnType == typeof(void);

            if (parameters.Length == 0)
            {
                if (isVoid)
                {
                    // Zero-param void fast path: Action delegate bound to instance.
                    Action action = (Action)Delegate.CreateDelegate(typeof(Action), target, method);
                    return _ => { action(); return null; };
                }
                else
                {
                    // Zero-param non-void: Func<TReturn> bound to instance.
                    Type funcType = typeof(Func<>).MakeGenericType(method.ReturnType);
                    Delegate del = Delegate.CreateDelegate(funcType, target, method);
                    return _ => del.DynamicInvoke(null);
                }
            }

            Type[] paramTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                paramTypes[i] = parameters[i].ParameterType;
            }

            if (isVoid)
            {
                Type actionType = GetActionType(paramTypes);
                Delegate del = Delegate.CreateDelegate(actionType, target, method);
                return args => { del.DynamicInvoke(args); return null; };
            }
            else
            {
                Type funcType = GetFuncType(paramTypes, method.ReturnType);
                Delegate del = Delegate.CreateDelegate(funcType, target, method);
                return args => del.DynamicInvoke(args);
            }
        }

        /// <summary>
        /// Builds a callback for a property getter. Returns the property value.
        /// </summary>
        internal static CommandCallback BuildGetterCallback(object target, PropertyInfo property)
        {
            MethodInfo getter = property.GetGetMethod();
            Type funcType = typeof(Func<>).MakeGenericType(property.PropertyType);
            Delegate del = Delegate.CreateDelegate(funcType, target, getter);
            return _ => del.DynamicInvoke(null);
        }

        /// <summary>
        /// Builds a callback for a property setter. Always returns <c>null</c>.
        /// </summary>
        internal static CommandCallback BuildSetterCallback(object target, PropertyInfo property)
        {
            MethodInfo setter = property.GetSetMethod();
            Type actionType = typeof(Action<>).MakeGenericType(property.PropertyType);
            Delegate del = Delegate.CreateDelegate(actionType, target, setter);
            return args => { del.DynamicInvoke(args); return null; };
        }

        // ── delegate type helpers ────────────────────────────────────────────────

        private static Type GetActionType(Type[] paramTypes)
        {
            switch (paramTypes.Length)
            {
                case 1: return typeof(Action<>).MakeGenericType(paramTypes);
                case 2: return typeof(Action<,>).MakeGenericType(paramTypes);
                case 3: return typeof(Action<,,>).MakeGenericType(paramTypes);
                case 4: return typeof(Action<,,,>).MakeGenericType(paramTypes);
                default:
                    throw new NotSupportedException(
                        string.Format("Commands with {0} parameters are not supported. Maximum is 4.",
                            paramTypes.Length));
            }
        }

        private static Type GetFuncType(Type[] paramTypes, Type returnType)
        {
            switch (paramTypes.Length)
            {
                case 1:
                    return typeof(Func<,>).MakeGenericType(paramTypes[0], returnType);
                case 2:
                    return typeof(Func<,,>).MakeGenericType(paramTypes[0], paramTypes[1], returnType);
                case 3:
                    return typeof(Func<,,,>).MakeGenericType(
                        paramTypes[0], paramTypes[1], paramTypes[2], returnType);
                case 4:
                    return typeof(Func<,,,,>).MakeGenericType(
                        paramTypes[0], paramTypes[1], paramTypes[2], paramTypes[3], returnType);
                default:
                    throw new NotSupportedException(
                        string.Format("Commands with {0} parameters are not supported. Maximum is 4.",
                            paramTypes.Length));
            }
        }
    }
}
