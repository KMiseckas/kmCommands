using System;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class OptionalParameterTests
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

        // ── helpers ────────────────────────────────────────────────────────

        private RegistrationResult Register(string name, CommandParameterInfo[] parameters, CommandCallback cb)
        {
            return _system.Register(name, parameters, cb);
        }

        // ── AC-1: Required param — IsOptional=false, DefaultValue=null ─────

        [Test]
        public void RequiredParam_HasIsOptionalFalse_AndNullDefaultValue()
        {
            CommandParameterInfo param = new CommandParameterInfo("x", typeof(int));
            Assert.That(param.IsOptional, Is.False);
            Assert.That(param.DefaultValue, Is.Null);
        }

        // ── AC-2: Optional param — IsOptional=true, correct DefaultValue ───

        [Test]
        public void OptionalParam_HasIsOptionalTrue_AndExpectedDefaultValue()
        {
            CommandParameterInfo param = new CommandParameterInfo("x", typeof(int), 42);
            Assert.That(param.IsOptional, Is.True);
            Assert.That(param.DefaultValue, Is.EqualTo(42));
        }

        // ── AC-3: Mismatched default type → ArgumentException ──────────────

        [Test]
        public void OptionalParam_TypeMismatch_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CommandParameterInfo("x", typeof(int), "wrong"));
        }

        // ── AC-4: Null default → ArgumentNullException ─────────────────────

        [Test]
        public void OptionalParam_NullDefault_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CommandParameterInfo("x", typeof(int), (object)null));
        }

        // ── AC-5: All-required registration succeeds (regression) ──────────

        [Test]
        public void Register_AllRequired_Succeeds()
        {
            RegistrationResult result = Register(
                "cmd",
                new[] { new CommandParameterInfo("a", typeof(int)), new CommandParameterInfo("b", typeof(string)) },
                _ => { });
            Assert.That(result.Success, Is.True);
        }

        // ── AC-6: Trailing optional params — registration succeeds ─────────

        [Test]
        public void Register_TrailingOptional_Succeeds()
        {
            RegistrationResult result = Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(int)),
                    new CommandParameterInfo("b", typeof(string), "default")
                },
                _ => { });
            Assert.That(result.Success, Is.True);
        }

        // ── AC-7: Optional before required → OptionalParameterBeforeRequired

        [Test]
        public void Register_OptionalBeforeRequired_ReturnsError()
        {
            RegistrationResult result = Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(string), "default"),
                    new CommandParameterInfo("b", typeof(int))
                },
                _ => { });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.OptionalParameterBeforeRequired));
        }

        // ── AC-8: All-optional registration succeeds ───────────────────────

        [Test]
        public void Register_AllOptional_Succeeds()
        {
            RegistrationResult result = Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(int), 1),
                    new CommandParameterInfo("b", typeof(string), "hi")
                },
                _ => { });
            Assert.That(result.Success, Is.True);
        }

        // ── AC-9: Execute with all args (required + optional) succeeds ──────

        [Test]
        public void Execute_AllArguments_Succeeds()
        {
            Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(int)),
                    new CommandParameterInfo("b", typeof(string), "default")
                },
                _ => { });

            ExecutionResult result = _system.Execute("cmd", new[] { "5", "hello" });
            Assert.That(result.Success, Is.True);
        }

        // ── AC-10: Execute with only required args succeeds ────────────────

        [Test]
        public void Execute_OnlyRequiredArgs_Succeeds()
        {
            Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(int)),
                    new CommandParameterInfo("b", typeof(string), "default")
                },
                _ => { });

            ExecutionResult result = _system.Execute("cmd", new[] { "5" });
            Assert.That(result.Success, Is.True);
        }

        // ── AC-11: Execute omitting subset of trailing optional args succeeds

        [Test]
        public void Execute_SubsetOfOptionalArgs_Succeeds()
        {
            Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(int)),
                    new CommandParameterInfo("b", typeof(string), "default"),
                    new CommandParameterInfo("c", typeof(bool), true)
                },
                _ => { });

            // Supply required + first optional, omit second optional
            ExecutionResult result = _system.Execute("cmd", new[] { "5", "hello" });
            Assert.That(result.Success, Is.True);
        }

        // ── AC-12: Too few args (below required) → ArgumentCountMismatch ───

        [Test]
        public void Execute_TooFewArgs_ReturnsArgumentCountMismatch()
        {
            Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(int)),
                    new CommandParameterInfo("b", typeof(string)),
                    new CommandParameterInfo("c", typeof(bool), true)
                },
                _ => { });

            ExecutionResult result = _system.Execute("cmd", new[] { "5" });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentCountMismatch));
        }

        // ── AC-13: Too many args (above total) → ArgumentCountMismatch ─────

        [Test]
        public void Execute_TooManyArgs_ReturnsArgumentCountMismatch()
        {
            Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(int)),
                    new CommandParameterInfo("b", typeof(string), "default")
                },
                _ => { });

            ExecutionResult result = _system.Execute("cmd", new[] { "5", "hello", "extra" });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentCountMismatch));
        }

        // ── AC-14: Omitted optional — default injected without string conversion

        [Test]
        public void Execute_OmittedOptional_InjectsDefaultDirectly()
        {
            object[] received = null;

            Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(int)),
                    new CommandParameterInfo("b", typeof(int), 42)
                },
                args => { received = args; });

            ExecutionResult result = _system.Execute("cmd", new[] { "1" });

            Assert.That(result.Success, Is.True);
            Assert.That(received, Is.Not.Null);
            Assert.That(received[1], Is.EqualTo(42));
            Assert.That(received[1], Is.InstanceOf<int>());  // not the string "42"
        }

        // ── AC-15: Correct mix of caller values and defaults in callback order

        [Test]
        public void Execute_MixedArgs_CallbackReceivesCorrectValues()
        {
            object[] received = null;

            Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("num", typeof(int)),
                    new CommandParameterInfo("msg", typeof(string), "hello"),
                    new CommandParameterInfo("flag", typeof(bool), true)
                },
                args => { received = args; });

            ExecutionResult result = _system.Execute("cmd", new[] { "7" });

            Assert.That(result.Success, Is.True);
            Assert.That(received, Is.Not.Null);
            Assert.That(received[0], Is.EqualTo(7));
            Assert.That(received[1], Is.EqualTo("hello"));
            Assert.That(received[2], Is.EqualTo(true));
        }

        // ── Error message: range format when optional params present ────────

        [Test]
        public void Execute_TooFewArgs_ErrorMessageShowsRange()
        {
            Register(
                "cmd",
                new[]
                {
                    new CommandParameterInfo("a", typeof(int)),
                    new CommandParameterInfo("b", typeof(string), "default")
                },
                _ => { });

            ExecutionResult result = _system.Execute("cmd", Array.Empty<string>());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentCountMismatch));
            Assert.That(result.ErrorMessage, Does.Contain("between 1 and 2"));
        }

        // ── Error message: unchanged format when all-required ───────────────

        [Test]
        public void Execute_TooFewArgs_AllRequired_ErrorMessageUnchanged()
        {
            Register(
                "cmd",
                new[] { new CommandParameterInfo("a", typeof(int)), new CommandParameterInfo("b", typeof(string)) },
                _ => { });

            ExecutionResult result = _system.Execute("cmd", new[] { "5" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentCountMismatch));
            Assert.That(result.ErrorMessage, Does.Contain("expects 2 argument(s) but received 1"));
        }
    }
}
