using System;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class CommandSystemLifecycleTests
    {
        private CommandSystem _system;

        [SetUp]
        public void SetUp()
        {
            _system = new CommandSystem();
        }

        [TearDown]
        public void TearDown()
        {
            if (_system.IsInitialized)
            {
                _system.Shutdown();
            }
        }

        [Test]
        public void Initialize_SetsIsInitializedTrue()
        {
            _system.Initialize();
            Assert.That(_system.IsInitialized, Is.True);
        }

        [Test]
        public void Shutdown_SetsIsInitializedFalse()
        {
            _system.Initialize();
            _system.Shutdown();
            Assert.That(_system.IsInitialized, Is.False);
        }

        [Test]
        public void Initialize_WhenAlreadyInitialized_IsNoOp()
        {
            _system.Initialize();
            Assert.DoesNotThrow(() => _system.Initialize());
            Assert.That(_system.IsInitialized, Is.True);
        }

        [Test]
        public void Shutdown_WhenNotInitialized_IsNoOp()
        {
            Assert.DoesNotThrow(() => _system.Shutdown());
            Assert.That(_system.IsInitialized, Is.False);
        }

        [Test]
        public void Shutdown_AfterDoubleShutdown_IsNoOp()
        {
            _system.Initialize();
            _system.Shutdown();
            Assert.DoesNotThrow(() => _system.Shutdown());
            Assert.That(_system.IsInitialized, Is.False);
        }

        [Test]
        public void Initialize_AfterShutdown_WorksCleanly()
        {
            _system.Initialize();
            _system.Shutdown();
            _system.Initialize();
            Assert.That(_system.IsInitialized, Is.True);
        }

        [Test]
        public void Register_BeforeInit_ReturnsNotInitialized()
        {
            RegistrationResult result = _system.Register(
                "foo",
                Array.Empty<CommandParameterInfo>(),
                _ => { });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.NotInitialized));
        }

        [Test]
        public void Execute_BeforeInit_ReturnsNotInitialized()
        {
            ExecutionResult result = _system.Execute("foo", null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NotInitialized));
        }
    }

    [TestFixture]
    public class CommandRegistrationTests
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
            _system.Shutdown();
        }

        private static CommandParameterInfo[] NoParams()
            => Array.Empty<CommandParameterInfo>();

        private static CommandCallback NoOp()
            => _ => { };

        [Test]
        public void Register_ValidCommand_Succeeds()
        {
            RegistrationResult result = _system.Register("foo", NoParams(), NoOp());
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.None));
        }

        [Test]
        public void Register_DuplicateName_ReturnsDuplicateCommandName()
        {
            _system.Register("foo", NoParams(), NoOp());
            RegistrationResult result = _system.Register("foo", NoParams(), NoOp());
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.DuplicateCommandName));
            Assert.That(result.ErrorMessage, Does.Contain("foo"));
        }

        [Test]
        public void Register_NullName_ReturnsNullOrEmptyName()
        {
            RegistrationResult result = _system.Register(null, NoParams(), NoOp());
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.NullOrEmptyName));
        }

        [Test]
        public void Register_EmptyName_ReturnsNullOrEmptyName()
        {
            RegistrationResult result = _system.Register("", NoParams(), NoOp());
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.NullOrEmptyName));
        }

        [Test]
        public void Register_NullParameters_ReturnsNullParameters()
        {
            RegistrationResult result = _system.Register("foo", null, NoOp());
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.NullParameters));
        }

        [Test]
        public void Register_NullCallback_ReturnsNullCallback()
        {
            RegistrationResult result = _system.Register("foo", NoParams(), null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.NullCallback));
        }

        [Test]
        public void Register_UnsupportedParameterType_ReturnsUnsupportedParameterType()
        {
            CommandParameterInfo[] parameters = new[]
            {
                new CommandParameterInfo("val", typeof(double))
            };

            RegistrationResult result = _system.Register("foo", parameters, NoOp());
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.UnsupportedParameterType));
            Assert.That(result.ErrorMessage, Does.Contain("Double"));
        }

        [Test]
        public void Register_MixedValidAndInvalidParams_ReturnsUnsupportedParameterType()
        {
            CommandParameterInfo[] parameters = new[]
            {
                new CommandParameterInfo("valid", typeof(int)),
                new CommandParameterInfo("bad", typeof(decimal))
            };

            RegistrationResult result = _system.Register("foo", parameters, NoOp());
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.UnsupportedParameterType));
        }

        [Test]
        public void Register_AfterShutdownAndReinit_CommandsAreGone()
        {
            _system.Register("foo", NoParams(), NoOp());
            _system.Shutdown();
            _system.Initialize();

            // "foo" should no longer be registered — registering again should succeed
            RegistrationResult result = _system.Register("foo", NoParams(), NoOp());
            Assert.That(result.Success, Is.True);
        }
    }
}
