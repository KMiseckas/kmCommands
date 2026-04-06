// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace kmCommands.Core
{
    /// <summary>
    /// Maps <see cref="Type"/> to <see cref="TypeCommandProfile"/> for types that have been
    /// pre-scanned via <see cref="CommandSystem.ScanCommandHosts"/>.
    /// </summary>
    internal sealed class TypeCommandProfileCache
    {
        private readonly Dictionary<Type, TypeCommandProfile> _cache
            = new Dictionary<Type, TypeCommandProfile>();

        internal bool TryGet(Type type, out TypeCommandProfile profile)
        {
            return _cache.TryGetValue(type, out profile);
        }

        internal void Add(Type type, TypeCommandProfile profile)
        {
            _cache[type] = profile;
        }

        internal void Clear()
        {
            _cache.Clear();
        }
    }
}
