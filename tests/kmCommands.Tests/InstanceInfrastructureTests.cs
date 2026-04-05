// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using kmCommands.Core;
using NUnit.Framework;

namespace kmCommands.Tests
{
    // ── CommandRegistry.TryRemove ─────────────────────────────────────────────

    [TestFixture]
    public class CommandRegistryTryRemoveTests
    {
        private CommandRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new CommandRegistry();
        }

        private static CommandDefinition MakeDefinition(string name)
        {
            return new CommandDefinition(name, Array.Empty<CommandParameterInfo>(), _ => null, null);
        }

        [Test]
        public void TryRemove_ExistingCommand_ReturnsTrue()
        {
            _registry.TryRegister(MakeDefinition("foo"));
            bool result = _registry.TryRemove("foo");
            Assert.That(result, Is.True);
        }

        [Test]
        public void TryRemove_ExistingCommand_CommandNoLongerInRegistry()
        {
            _registry.TryRegister(MakeDefinition("foo"));
            _registry.TryRemove("foo");
            bool found = _registry.TryGetCommand("foo", out _);
            Assert.That(found, Is.False);
        }

        [Test]
        public void TryRemove_UnknownName_ReturnsFalse()
        {
            bool result = _registry.TryRemove("nonexistent");
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryRemove_CaseInsensitive_RemovesCorrectly()
        {
            _registry.TryRegister(MakeDefinition("MyCmd"));
            bool result = _registry.TryRemove("mycmd");
            Assert.That(result, Is.True);
            Assert.That(_registry.TryGetCommand("MyCmd", out _), Is.False);
        }

        [Test]
        public void TryRemove_DoesNotAffectOtherCommands()
        {
            _registry.TryRegister(MakeDefinition("foo"));
            _registry.TryRegister(MakeDefinition("bar"));
            _registry.TryRemove("foo");
            Assert.That(_registry.TryGetCommand("bar", out _), Is.True);
        }
    }

    // ── InstanceRegistry ──────────────────────────────────────────────────────

    [TestFixture]
    public class InstanceRegistryTests
    {
        private InstanceRegistry _registry;
        private object _target;

        [SetUp]
        public void SetUp()
        {
            _registry = new InstanceRegistry();
            _target = new object();
        }

        [Test]
        public void TryReserveKey_NewKey_ReturnsTrue()
        {
            bool result = _registry.TryReserveKey("player", _target);
            Assert.That(result, Is.True);
        }

        [Test]
        public void TryReserveKey_DuplicateKey_ReturnsFalse()
        {
            _registry.TryReserveKey("player", _target);
            bool result = _registry.TryReserveKey("player", new object());
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryReserveKey_DuplicateKey_OriginalRegistrationIntact()
        {
            object original = new object();
            _registry.TryReserveKey("player", original);
            _registry.TryReserveKey("player", new object()); // duplicate attempt fails silently
            // original is still tracked — we can still get names for this key
            bool found = _registry.TryGetCommandNames("player", out List<string> names);
            Assert.That(found, Is.True);
            Assert.That(names, Is.Not.Null);
        }

        [Test]
        public void TryReserveKey_CaseInsensitive_DuplicateDetected()
        {
            _registry.TryReserveKey("Player", _target);
            bool result = _registry.TryReserveKey("player", new object());
            Assert.That(result, Is.False);
        }

        [Test]
        public void TrackCommand_ThenGetCommandNames_ReturnsTrackedName()
        {
            _registry.TryReserveKey("player", _target);
            _registry.TrackCommand("player", "player.heal");
            _registry.TrackCommand("player", "player.get_hp");

            bool found = _registry.TryGetCommandNames("player", out List<string> names);
            Assert.That(found, Is.True);
            Assert.That(names, Contains.Item("player.heal"));
            Assert.That(names, Contains.Item("player.get_hp"));
            Assert.That(names.Count, Is.EqualTo(2));
        }

        [Test]
        public void TryGetCommandNames_UnknownKey_ReturnsFalse()
        {
            bool found = _registry.TryGetCommandNames("unknown", out List<string> names);
            Assert.That(found, Is.False);
            Assert.That(names, Is.Null);
        }

        [Test]
        public void RemoveKey_ExistingKey_KeyAndCommandsGone()
        {
            _registry.TryReserveKey("player", _target);
            _registry.TrackCommand("player", "player.heal");
            _registry.RemoveKey("player");

            bool found = _registry.TryGetCommandNames("player", out _);
            Assert.That(found, Is.False);
        }

        [Test]
        public void RemoveKey_AfterRemoval_NewReservationWithSameKeySucceeds()
        {
            _registry.TryReserveKey("player", _target);
            _registry.RemoveKey("player");
            bool result = _registry.TryReserveKey("player", new object());
            Assert.That(result, Is.True);
        }

        [Test]
        public void Clear_AllKeysGone()
        {
            _registry.TryReserveKey("player", _target);
            _registry.TryReserveKey("enemy", new object());
            _registry.Clear();

            Assert.That(_registry.TryGetCommandNames("player", out _), Is.False);
            Assert.That(_registry.TryGetCommandNames("enemy", out _), Is.False);
        }
    }

    // ── UnregisterResult ──────────────────────────────────────────────────────

    [TestFixture]
    public class UnregisterResultTests
    {
        [Test]
        public void Ok_ReturnsSuccessWithCorrectCount()
        {
            UnregisterResult result = UnregisterResult.Ok(3);
            Assert.That(result.Success, Is.True);
            Assert.That(result.RemovedCount, Is.EqualTo(3));
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void Ok_ZeroCount_IsValid()
        {
            UnregisterResult result = UnregisterResult.Ok(0);
            Assert.That(result.Success, Is.True);
            Assert.That(result.RemovedCount, Is.EqualTo(0));
        }

        [Test]
        public void Fail_ReturnsFailureWithMessage()
        {
            UnregisterResult result = UnregisterResult.Fail("not found");
            Assert.That(result.Success, Is.False);
            Assert.That(result.RemovedCount, Is.EqualTo(0));
            Assert.That(result.ErrorMessage, Is.EqualTo("not found"));
        }
    }

    // ── RegistrationError new values ──────────────────────────────────────────

    [TestFixture]
    public class RegistrationErrorNewValuesTests
    {
        [Test]
        public void RegistrationError_NullTarget_EnumValueExists()
        {
            RegistrationError error = RegistrationError.NullTarget;
            Assert.That((int)error, Is.GreaterThan(0));
        }

        [Test]
        public void RegistrationError_DuplicateInstanceKey_EnumValueExists()
        {
            RegistrationError error = RegistrationError.DuplicateInstanceKey;
            Assert.That((int)error, Is.GreaterThan(0));
        }

        [Test]
        public void RegistrationError_InvalidInstanceKey_EnumValueExists()
        {
            RegistrationError error = RegistrationError.InvalidInstanceKey;
            Assert.That((int)error, Is.GreaterThan(0));
        }

        [Test]
        public void RegistrationError_NewValues_AreDistinct()
        {
            Assert.That(RegistrationError.NullTarget, Is.Not.EqualTo(RegistrationError.DuplicateInstanceKey));
            Assert.That(RegistrationError.NullTarget, Is.Not.EqualTo(RegistrationError.InvalidInstanceKey));
            Assert.That(RegistrationError.DuplicateInstanceKey, Is.Not.EqualTo(RegistrationError.InvalidInstanceKey));
        }
    }
}
