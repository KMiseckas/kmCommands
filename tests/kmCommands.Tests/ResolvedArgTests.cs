// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using NUnit.Framework;
using kmCommands.Core;

namespace kmCommands.Tests
{
    [TestFixture]
    internal class ResolvedArgTests
    {
        [Test]
        public void FromString_SetsIsPreResolvedFalse()
        {
            ResolvedArg arg = ResolvedArg.FromString("hello");
            Assert.That(arg.IsPreResolved, Is.False);
        }

        [Test]
        public void FromString_StringValueIsPreserved()
        {
            ResolvedArg arg = ResolvedArg.FromString("hello");
            Assert.That(arg.StringValue, Is.EqualTo("hello"));
        }

        [Test]
        public void FromObject_SetsIsPreResolvedTrue()
        {
            ResolvedArg arg = ResolvedArg.FromObject(42);
            Assert.That(arg.IsPreResolved, Is.True);
        }

        [Test]
        public void FromObject_ObjectValueIsPreserved()
        {
            object obj = new object();
            ResolvedArg arg = ResolvedArg.FromObject(obj);
            Assert.That(arg.ObjectValue, Is.SameAs(obj));
        }

        [Test]
        public void FromObject_NullObjectValue_IsValid()
        {
            ResolvedArg arg = ResolvedArg.FromObject(null);
            Assert.That(arg.IsPreResolved, Is.True);
            Assert.That(arg.ObjectValue, Is.Null);
        }
    }
}
