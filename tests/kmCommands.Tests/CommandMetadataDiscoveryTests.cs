using System;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class CommandMetadataDiscoveryTests
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

        // â”€â”€ helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void Register(string name, CommandParameterInfo[] parameters)
        {
            RegistrationResult r = _system.Register(name, parameters, args => null);
            Assert.That(r.Success, Is.True,
                string.Format("Setup: Failed to register '{0}': {1}", name, r.ErrorMessage));
        }

        private void RegisterNoArgs(string name)
        {
            Register(name, Array.Empty<CommandParameterInfo>());
        }

        // â”€â”€ GetCommandNames â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void GetCommandNames_BeforeInit_ReturnsEmptyArray()
        {
            _system.Shutdown();
            string[] names = _system.GetCommandNames();
            Assert.That(names, Is.Empty);
        }

        [Test]
        public void GetCommandNames_InitNoCommands_ReturnsEmptyArray()
        {
            string[] names = _system.GetCommandNames();
            Assert.That(names, Is.Empty);
        }

        [Test]
        public void GetCommandNames_WithRegisteredCommands_ReturnsAllNames()
        {
            RegisterNoArgs("alpha");
            RegisterNoArgs("beta");
            RegisterNoArgs("gamma");

            string[] names = _system.GetCommandNames();

            Assert.That(names, Has.Length.EqualTo(3));
            Assert.That(names, Has.Member("alpha"));
            Assert.That(names, Has.Member("beta"));
            Assert.That(names, Has.Member("gamma"));
        }

        [Test]
        public void GetCommandNames_NamesAreSortedOrdinalIgnoreCase()
        {
            RegisterNoArgs("Zebra");
            RegisterNoArgs("apple");
            RegisterNoArgs("Mango");

            string[] names = _system.GetCommandNames();

            Assert.That(names, Has.Length.EqualTo(3));
            Assert.That(names[0], Is.EqualTo("apple").IgnoreCase);
            Assert.That(names[1], Is.EqualTo("Mango").IgnoreCase);
            Assert.That(names[2], Is.EqualTo("Zebra").IgnoreCase);
        }

        [Test]
        public void GetCommandNames_AfterShutdown_ReturnsEmptyArray()
        {
            RegisterNoArgs("cmd");
            _system.Shutdown();

            string[] names = _system.GetCommandNames();

            Assert.That(names, Is.Empty);
        }

        // â”€â”€ TryGetCommandParameters â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void TryGetCommandParameters_BeforeInit_ReturnsFalse()
        {
            _system.Shutdown();

            bool found = _system.TryGetCommandParameters("cmd", out CommandParameterInfo[] parameters);

            Assert.That(found, Is.False);
            Assert.That(parameters, Is.Null);
        }

        [Test]
        public void TryGetCommandParameters_NullName_ReturnsFalse()
        {
            bool found = _system.TryGetCommandParameters(null, out CommandParameterInfo[] parameters);

            Assert.That(found, Is.False);
            Assert.That(parameters, Is.Null);
        }

        [Test]
        public void TryGetCommandParameters_EmptyName_ReturnsFalse()
        {
            bool found = _system.TryGetCommandParameters(string.Empty, out CommandParameterInfo[] parameters);

            Assert.That(found, Is.False);
            Assert.That(parameters, Is.Null);
        }

        [Test]
        public void TryGetCommandParameters_UnknownCommand_ReturnsFalse()
        {
            bool found = _system.TryGetCommandParameters("nonexistent", out CommandParameterInfo[] parameters);

            Assert.That(found, Is.False);
            Assert.That(parameters, Is.Null);
        }

        [Test]
        public void TryGetCommandParameters_KnownCommand_ReturnsTrueAndParams()
        {
            CommandParameterInfo[] expected =
            {
                new CommandParameterInfo("x", typeof(int)),
                new CommandParameterInfo("label", typeof(string))
            };
            Register("move", expected);

            bool found = _system.TryGetCommandParameters("move", out CommandParameterInfo[] parameters);

            Assert.That(found, Is.True);
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[0].Name, Is.EqualTo("x"));
            Assert.That(parameters[1].Name, Is.EqualTo("label"));
        }

        [Test]
        public void TryGetCommandParameters_IsCaseInsensitive()
        {
            CommandParameterInfo[] expected = { new CommandParameterInfo("val", typeof(float)) };
            Register("SetSpeed", expected);

            bool foundLower = _system.TryGetCommandParameters("setspeed", out CommandParameterInfo[] p1);
            bool foundUpper = _system.TryGetCommandParameters("SETSPEED", out CommandParameterInfo[] p2);

            Assert.That(foundLower, Is.True);
            Assert.That(foundUpper, Is.True);
            Assert.That(p1, Has.Length.EqualTo(1));
            Assert.That(p2, Has.Length.EqualTo(1));
        }

        [Test]
        public void TryGetCommandParameters_EmptyParams_ReturnsEmptyArray()
        {
            RegisterNoArgs("noop");

            bool found = _system.TryGetCommandParameters("noop", out CommandParameterInfo[] parameters);

            Assert.That(found, Is.True);
            Assert.That(parameters, Is.Empty);
        }

        [Test]
        public void TryGetCommandParameters_AfterShutdown_ReturnsFalse()
        {
            RegisterNoArgs("cmd");
            _system.Shutdown();

            bool found = _system.TryGetCommandParameters("cmd", out CommandParameterInfo[] parameters);

            Assert.That(found, Is.False);
            Assert.That(parameters, Is.Null);
        }

        // â”€â”€ GetSnapshot â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void GetSnapshot_BeforeInit_ReturnsEmptySnapshot()
        {
            _system.Shutdown();

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.CommandNames, Is.Empty);
        }

        [Test]
        public void GetSnapshot_NoCommands_ReturnsEmptyCommandNames()
        {
            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            Assert.That(snapshot.CommandNames, Is.Empty);
        }

        [Test]
        public void GetSnapshot_CommandNames_ContainsAllRegisteredNames()
        {
            RegisterNoArgs("foo");
            RegisterNoArgs("bar");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            Assert.That(snapshot.CommandNames, Has.Length.EqualTo(2));
            Assert.That(snapshot.CommandNames, Has.Member("foo"));
            Assert.That(snapshot.CommandNames, Has.Member("bar"));
        }

        [Test]
        public void GetSnapshot_TryGetParameters_ReturnsCorrectParameters()
        {
            CommandParameterInfo[] parms = { new CommandParameterInfo("amount", typeof(int)) };
            Register("heal", parms);

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetParameters("heal", out CommandParameterInfo[] result);

            Assert.That(found, Is.True);
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("amount"));
            Assert.That(result[0].Type, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void GetSnapshot_TryGetParameters_IsCaseInsensitive()
        {
            RegisterNoArgs("Reload");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool foundLower = snapshot.TryGetParameters("reload", out _);
            bool foundUpper = snapshot.TryGetParameters("RELOAD", out _);

            Assert.That(foundLower, Is.True);
            Assert.That(foundUpper, Is.True);
        }

        [Test]
        public void GetSnapshot_TryGetParameters_UnknownCommand_ReturnsFalse()
        {
            RegisterNoArgs("exists");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            bool found = snapshot.TryGetParameters("does_not_exist", out CommandParameterInfo[] result);

            Assert.That(found, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetSnapshot_IsIsolatedFromSubsequentRegistrations()
        {
            RegisterNoArgs("commandA");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            RegisterNoArgs("commandB");

            Assert.That(snapshot.CommandNames, Has.Length.EqualTo(1));
            Assert.That(snapshot.CommandNames, Has.No.Member("commandB"));
        }

        [Test]
        public void GetSnapshot_ParameterArray_IsStructurallyCopied()
        {
            CommandParameterInfo[] parms = { new CommandParameterInfo("n", typeof(int)) };
            Register("target", parms);

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            _system.TryGetCommandParameters("target", out CommandParameterInfo[] liveArray);
            snapshot.TryGetParameters("target", out CommandParameterInfo[] snapshotArray);

            // Arrays should be structurally equal but different object references
            Assert.That(snapshotArray, Is.Not.SameAs(liveArray));
            Assert.That(snapshotArray, Has.Length.EqualTo(liveArray.Length));
            Assert.That(snapshotArray[0], Is.SameAs(liveArray[0])); // immutable refs shared
        }

        [Test]
        public void GetSnapshot_AfterShutdown_ReturnsEmptySnapshot()
        {
            RegisterNoArgs("cmd");
            _system.Shutdown();

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.CommandNames, Is.Empty);
        }
    }
}
