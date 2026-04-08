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

        // â”€â”€ support: inner scan targets â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static class ScanTargets
        {
            [Command("described", Description = "A described command")]
            public static void DescribedCommand() { }

            [Command("nodesc")]
            public static void NoDescCommand() { }
        }

        // â”€â”€ AC #1: manual register with non-null description â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Register_WithNonNullDescription_SnapshotContainsDescription()
        {
            _system.Register("cmd", Array.Empty<CommandParameterInfo>(), _ => null, "Describes cmd");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("cmd", out string desc);

            Assert.That(found, Is.True);
            Assert.That(desc, Is.EqualTo("Describes cmd"));
        }

        // â”€â”€ AC #2: manual register without description â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Register_WithoutDescription_SnapshotDescriptionIsNull()
        {
            _system.Register("cmd", Array.Empty<CommandParameterInfo>(), _ => null);

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("cmd", out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        // â”€â”€ AC #3: manual register with empty-string description â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Register_WithEmptyStringDescription_SnapshotDescriptionIsEmptyString()
        {
            _system.Register("cmd", Array.Empty<CommandParameterInfo>(), _ => null, "");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("cmd", out string desc);

            Assert.That(found, Is.True);
            Assert.That(desc, Is.EqualTo(""));
        }

        // â”€â”€ AC #4: attribute registration with description â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Scan_AttributeWithDescription_SnapshotContainsDescription()
        {
            _system.Scan(typeof(ScanTargets));

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("described", out string desc);

            Assert.That(found, Is.True);
            Assert.That(desc, Is.EqualTo("A described command"));
        }

        // â”€â”€ AC #5: attribute registration without description â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Scan_AttributeWithoutDescription_SnapshotDescriptionIsNull()
        {
            _system.Scan(typeof(ScanTargets));

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("nodesc", out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        // â”€â”€ AC #6: case-insensitive snapshot lookup â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void TryGetDescription_ExistingCommandWithDescription_CaseInsensitiveLookup()
        {
            _system.Register("MyCmd", Array.Empty<CommandParameterInfo>(), _ => null, "Help text");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            Assert.That(snapshot.TryGetDescription("mycmd", out string lower), Is.True);
            Assert.That(lower, Is.EqualTo("Help text"));

            Assert.That(snapshot.TryGetDescription("MYCMD", out string upper), Is.True);
            Assert.That(upper, Is.EqualTo("Help text"));
        }

        // â”€â”€ AC #7: null description returns false â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void TryGetDescription_CommandWithNullDescription_ReturnsFalse()
        {
            _system.Register("cmd", Array.Empty<CommandParameterInfo>(), _ => null, null);

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetDescription("cmd", out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        [Test]
        public void TryGetDescription_NullName_ReturnsFalse()
        {
            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            bool found = snapshot.TryGetDescription(null, out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        [Test]
        public void TryGetDescription_EmptyName_ReturnsFalse()
        {
            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            bool found = snapshot.TryGetDescription(string.Empty, out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        // â”€â”€ AC #8: Empty singleton returns false â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Empty_TryGetDescription_ReturnsFalseWithNullDescription()
        {
            CommandMetadataSnapshot empty = _system.GetSnapshot(); // system is initialized but empty

            bool found = empty.TryGetDescription("anything", out string desc);

            Assert.That(found, Is.False);
            Assert.That(desc, Is.Null);
        }

        // â”€â”€ AC #9: snapshot isolation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void SnapshotIsolation_DescriptionNotIncludedForLaterRegisteredCommand()
        {
            _system.Register("first", Array.Empty<CommandParameterInfo>(), _ => null, "First desc");
            CommandMetadataSnapshot snapshotBefore = _system.GetSnapshot();

            _system.Register("second", Array.Empty<CommandParameterInfo>(), _ => null, "Second desc");

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
