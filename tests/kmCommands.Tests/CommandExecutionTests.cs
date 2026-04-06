using System;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class CommandExecutionTests
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

        // â”€â”€ helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private void Register(string name, CommandParameterInfo[] parameters, CommandCallback cb)
        {
            RegistrationResult r = _system.Register(name, parameters, cb);
            Assert.That(r.Success, Is.True, string.Format("Setup: Failed to register '{0}': {1}", name, r.ErrorMessage));
        }

        private void RegisterNoArgs(string name, CommandCallback cb)
        {
            Register(name, Array.Empty<CommandParameterInfo>(), cb);
        }

        // â”€â”€ command not found â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_CommandNotFound_ReturnsCommandNotFound()
        {
            ExecutionResult result = _system.Execute("nobody", null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.CommandNotFound));
        }

        // â”€â”€ argument count mismatch â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_TooFewArgs_ReturnsArgumentCountMismatch()
        {
            Register("cmd", new[] { new CommandParameterInfo("x", typeof(int)) }, _ => null);
            ExecutionResult result = _system.Execute("cmd", Array.Empty<string>());
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentCountMismatch));
        }

        [Test]
        public void Execute_TooManyArgs_ReturnsArgumentCountMismatch()
        {
            RegisterNoArgs("cmd", _ => null);
            ExecutionResult result = _system.Execute("cmd", new[] { "extra" });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentCountMismatch));
        }

        // â”€â”€ type conversion failure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_BadIntArg_ReturnsArgumentConversionFailed()
        {
            Register("cmd", new[] { new CommandParameterInfo("count", typeof(int)) }, _ => null);
            ExecutionResult result = _system.Execute("cmd", new[] { "notanumber" });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentConversionFailed));
            Assert.That(result.ErrorMessage, Does.Contain("count").And.Contain("0"));
        }

        // â”€â”€ callback throws â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_CallbackThrows_ReturnsCallbackThrewException()
        {
            RegisterNoArgs("boom", _ => throw new InvalidOperationException("kaboom"));
            ExecutionResult result = _system.Execute("boom", null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.CallbackThrewException));
            Assert.That(result.Exception, Is.InstanceOf<InvalidOperationException>());
            Assert.That(result.Exception.Message, Is.EqualTo("kaboom"));
        }

        // â”€â”€ success paths â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_ZeroArgCommand_NullArgs_Succeeds()
        {
            bool invoked = false;
            RegisterNoArgs("quit", _ => { invoked = true; return null; });
            ExecutionResult result = _system.Execute("quit", null);
            Assert.That(result.Success, Is.True);
            Assert.That(invoked, Is.True);
        }

        [Test]
        public void Execute_ZeroArgCommand_EmptyArray_Succeeds()
        {
            bool invoked = false;
            RegisterNoArgs("quit", _ => { invoked = true; return null; });
            ExecutionResult result = _system.Execute("quit", Array.Empty<string>());
            Assert.That(result.Success, Is.True);
            Assert.That(invoked, Is.True);
        }

        [Test]
        public void Execute_StringArg_CallbackReceivesCorrectValue()
        {
            string captured = null;
            Register("greet", new[] { new CommandParameterInfo("name", typeof(string)) },
                args => { captured = (string)args[0]; return null; });

            ExecutionResult result = _system.Execute("greet", new[] { "world" });
            Assert.That(result.Success, Is.True);
            Assert.That(captured, Is.EqualTo("world"));
        }

        [Test]
        public void Execute_IntArg_CallbackReceivesCorrectValue()
        {
            int captured = 0;
            Register("setlevel", new[] { new CommandParameterInfo("level", typeof(int)) },
                args => { captured = (int)args[0]; return null; });

            ExecutionResult result = _system.Execute("setlevel", new[] { "99" });
            Assert.That(result.Success, Is.True);
            Assert.That(captured, Is.EqualTo(99));
        }

        [Test]
        public void Execute_FloatArg_CallbackReceivesCorrectValue()
        {
            float captured = 0f;
            Register("setspeed", new[] { new CommandParameterInfo("speed", typeof(float)) },
                args => { captured = (float)args[0]; return null; });

            ExecutionResult result = _system.Execute("setspeed", new[] { "2.5" });
            Assert.That(result.Success, Is.True);
            Assert.That(captured, Is.EqualTo(2.5f));
        }

        [Test]
        public void Execute_BoolArg_CallbackReceivesCorrectValue()
        {
            bool captured = false;
            Register("setgod", new[] { new CommandParameterInfo("enabled", typeof(bool)) },
                args => { captured = (bool)args[0]; return null; });

            ExecutionResult result = _system.Execute("setgod", new[] { "true" });
            Assert.That(result.Success, Is.True);
            Assert.That(captured, Is.EqualTo(true));
        }

        [Test]
        public void Execute_AllFourBuiltInTypes_CallbackReceivesAllCorrectly()
        {
            string capturedStr = null;
            int capturedInt = 0;
            float capturedFloat = 0f;
            bool capturedBool = false;

            Register("cmd", new[]
            {
                new CommandParameterInfo("name",  typeof(string)),
                new CommandParameterInfo("count", typeof(int)),
                new CommandParameterInfo("ratio", typeof(float)),
                new CommandParameterInfo("flag",  typeof(bool))
            }, args =>
            {
                capturedStr = (string)args[0];
                capturedInt = (int)args[1];
                capturedFloat = (float)args[2];
                capturedBool = (bool)args[3];
                return null;
            });

            ExecutionResult result = _system.Execute("cmd", new[] { "hero", "10", "1.5", "false" });
            Assert.That(result.Success, Is.True);
            Assert.That(capturedStr, Is.EqualTo("hero"));
            Assert.That(capturedInt, Is.EqualTo(10));
            Assert.That(capturedFloat, Is.EqualTo(1.5f));
            Assert.That(capturedBool, Is.EqualTo(false));
        }

        // â”€â”€ case-insensitive name lookup â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_CaseInsensitiveName_AllVariantsSucceed()
        {
            bool invoked = false;
            RegisterNoArgs("SetHP", _ => { invoked = true; return null; });

            ExecutionResult lower = _system.Execute("sethp", null);
            Assert.That(lower.Success, Is.True);

            invoked = false;
            ExecutionResult upper = _system.Execute("SETHP", null);
            Assert.That(upper.Success, Is.True);
            Assert.That(invoked, Is.True);
        }

        // â”€â”€ reinit clears commands â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_AfterShutdownAndReinit_PreviousCommandsAreGone()
        {
            RegisterNoArgs("persist", _ => null);

            _system.Shutdown();
            _system.Initialize();

            ExecutionResult result = _system.Execute("persist", null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.CommandNotFound));
        }

        // â”€â”€ CommandParameterInfo null guard â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void CommandParameterInfo_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CommandParameterInfo(null, typeof(int)));
        }

        [Test]
        public void CommandParameterInfo_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CommandParameterInfo("x", null));
        }
    }
}
