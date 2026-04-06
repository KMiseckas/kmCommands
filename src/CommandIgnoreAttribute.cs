// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;

namespace kmCommands
{
    /// <summary>
    /// Prevents a public method or property from being registered as a command
    /// during instance scanning. Takes precedence over <see cref="CommandAttribute"/> — if both
    /// are present on the same member, the member is skipped entirely.
    /// Has no effect on non-public members (they are already excluded from auto-scan).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property,
                     Inherited = false, AllowMultiple = false)]
    public sealed class CommandIgnoreAttribute : Attribute { }
}
