// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Marks a class as a command host. Types decorated with this attribute can be
    /// pre-scanned at startup via <see cref="CommandSystem.ScanCommandHosts(Type[])"/>
    /// to cache their member metadata, avoiding repeated reflection at
    /// <see cref="CommandSystem.RegisterInstance(object, string)"/> time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CommandHostAttribute : Attribute { }
}
