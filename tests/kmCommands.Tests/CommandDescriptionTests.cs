// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class CommandDescriptionTests
    {
        private CommandSystem _system;

        [SetUp]
        public void SetUp()
        {
            _system = new CommandSystem();
            _system.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_system.IsInitialized)
            {
                _system.Shutdown();
            }
        }

        // ── support: inner scan targets ──────────────────────────────────

        private static class ScanTargets
        {
            [Command("described", Description = "A described command")]
            public static void DescribedCommand() { }

            [Command("nodesc")]
            public static void NoDescCommand() { }
        }

        // ── AC #1: manual register with non-null description ─────────────

        [Test]
        public void Register_WithNonNullDescription_SnapshotContainsDescription()
        {
            _system.Register("cmd", Array.Empty<CommandParameterInfo>(), _ => { }, "Describes cmd");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("cmd", out string desc);

            Assert.That(found, Is.True);
            Assert.That(desc, Is.EqualTo("Describes cmd"));
        }

        // ── AC #2: manual register without description ───────────────────

        [Test]
        public void Register_WithoutDescription_SnapshotDescriptionIsNull()
        {
            _system.Register("cmd", Array.Empty<CommandParameterInfo>(), _ => { });

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("cmd", out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        // ── AC #3: manual register with empty-string description ─────────

        [Test]
        public void Register_WithEmptyStringDescription_SnapshotDescriptionIsEmptyString()
        {
            _system.Register("cmd", Array.Empty<CommandParameterInfo>(), _ => { }, "");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("cmd", out string desc);

            Assert.That(found, Is.True);
            Assert.That(desc, Is.EqualTo(""));
        }

        // ── AC #4: attribute registration with description ───────────────

        [Test]
        public void Scan_AttributeWithDescription_SnapshotContainsDescription()
        {
            _system.Scan(typeof(ScanTargets));

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("described", out string desc);

            Assert.That(found, Is.True);
            Assert.That(desc, Is.EqualTo("A described command"));
        }

        // ── AC #5: attribute registration without description ────────────

        [Test]
        public void Scan_AttributeWithoutDescription_SnapshotDescriptionIsNull()
        {
            _system.Scan(typeof(ScanTargets));

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("nodesc", out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        // ── AC #6: case-insensitive snapshot lookup ───────────────────────

        [Test]
        public void TryGetDescription_ExistingCommandWithDescription_CaseInsensitiveLookup()
        {
            _system.Register("MyCmd", Array.Empty<CommandParameterInfo>(), _ => { }, "Help text");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            Assert.That(snapshot.TryGetDescription("mycmd", out string lower), Is.True);
            Assert.That(lower, Is.EqualTo("Help text"));

            Assert.That(snapshot.TryGetDescription("MYCMD", out string upper), Is.True);
            Assert.That(upper, Is.EqualTo("Help text"));
        }

        // ── AC #7: null description returns false ─────────────────────────

        [Test]
        public void TryGetDescription_CommandWithNullDescription_ReturnsFalse()
        {
            _system.Register("cmd", Array.Empty<CommandParameterInfo>(), _ => { }, null);

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("cmd", out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        // ── AC #8: Empty singleton returns false ──────────────────────────

        [Test]
        public void Empty_TryGetDescription_ReturnsFalseWithNullDescription()
        {
            CommandMetadataSnapshot empty = _system.GetSnapshot(); // system is initialized but empty

            bool found = empty.TryGetDescription("anything", out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        // ── AC #9: snapshot isolation ─────────────────────────────────────

        [Test]
        public void SnapshotIsolation_DescriptionNotIncludedForLaterRegisteredCommand()
        {
            _system.Register("first", Array.Empty<CommandParameterInfo>(), _ => { }, "First desc");
            CommandMetadataSnapshot snapshotBefore = _system.GetSnapshot();

            _system.Register("second", Array.Empty<CommandParameterInfo>(), _ => { }, "Second desc");

            // Snapshot taken before 'second' was registered must not contain 'second'.
            bool found = snapshotBefore.TryGetDescription("second", out string desc);
            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);

            // A new snapshot must contain 'second'.
            CommandMetadataSnapshot snapshotAfter = _system.GetSnapshot();
            Assert.That(snapshotAfter.TryGetDescription("second", out string desc2), Is.True);
            Assert.That(desc2, Is.EqualTo("Second desc"));
        }
    }
}
