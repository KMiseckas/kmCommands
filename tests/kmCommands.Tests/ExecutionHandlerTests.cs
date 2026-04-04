using System;
using kmCommands.Core;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class ExecutionHandlerTests
    {
        private CommandRegistry _registry;
        private ArgumentConverter _converter;
        private ExecutionHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _registry = new CommandRegistry();
            _converter = new ArgumentConverter();
            _handler = new ExecutionHandler(_registry, _converter);
        }

        // ── helpers ────────────────────────────────────────────────────────

        private void RegisterNoArgs(string name, CommandCallback callback)
        {
            _registry.TryRegister(new CommandDefinition(
                name,
                Array.Empty<CommandParameterInfo>(),
                callback,
                null));
        }

        private void RegisterWithParams(string name, CommandParameterInfo[] parameters, CommandCallback callback)
        {
            _registry.TryRegister(new CommandDefinition(name, parameters, callback, null));
        }

        // ── null / empty command name ───────────────────────────────────────

        [Test]
        public void Execute_NullCommandName_ReturnsNullOrEmptyCommandName()
        {
            ExecutionResult result = _handler.Execute(null, null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NullOrEmptyCommandName));
        }

        [Test]
        public void Execute_EmptyCommandName_ReturnsNullOrEmptyCommandName()
        {
            ExecutionResult result = _handler.Execute("", null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NullOrEmptyCommandName));
        }

        // ── command not found ──────────────────────────────────────────────

        [Test]
        public void Execute_UnknownCommand_ReturnsCommandNotFound()
        {
            ExecutionResult result = _handler.Execute("nonexistent", null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.CommandNotFound));
            Assert.That(result.ErrorMessage, Does.Contain("nonexistent"));
        }

        // ── argument count mismatch ────────────────────────────────────────

        [Test]
        public void Execute_TooFewArgs_ReturnsArgumentCountMismatch()
        {
            RegisterWithParams("cmd", new[]
            {
                new CommandParameterInfo("x", typeof(int)),
                new CommandParameterInfo("y", typeof(int))
            }, _ => { });

            ExecutionResult result = _handler.Execute("cmd", new[] { "1" });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentCountMismatch));
            Assert.That(result.ErrorMessage, Does.Contain("2").And.Contain("1"));
        }

        [Test]
        public void Execute_TooManyArgs_ReturnsArgumentCountMismatch()
        {
            RegisterNoArgs("cmd", _ => { });

            ExecutionResult result = _handler.Execute("cmd", new[] { "extra" });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentCountMismatch));
        }

        // ── argument conversion failure ────────────────────────────────────

        [Test]
        public void Execute_WrongArgType_ReturnsArgumentConversionFailed()
        {
            RegisterWithParams("cmd", new[]
            {
                new CommandParameterInfo("count", typeof(int))
            }, _ => { });

            ExecutionResult result = _handler.Execute("cmd", new[] { "notanumber" });
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.ArgumentConversionFailed));
            Assert.That(result.ErrorMessage, Does.Contain("count").And.Contain("0"));
        }

        // ── callback throws ────────────────────────────────────────────────

        [Test]
        public void Execute_CallbackThrows_ReturnsCallbackThrewException()
        {
            RegisterNoArgs("boom", _ => throw new InvalidOperationException("test error"));

            ExecutionResult result = _handler.Execute("boom", null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.CallbackThrewException));
            Assert.That(result.Exception, Is.InstanceOf<InvalidOperationException>());
            Assert.That(result.Exception.Message, Is.EqualTo("test error"));
        }

        // ── success paths ──────────────────────────────────────────────────

        [Test]
        public void Execute_ZeroArgCommand_NullArgs_Succeeds()
        {
            bool invoked = false;
            RegisterNoArgs("quit", _ => { invoked = true; });

            ExecutionResult result = _handler.Execute("quit", null);
            Assert.That(result.Success, Is.True);
            Assert.That(invoked, Is.True);
        }

        [Test]
        public void Execute_ZeroArgCommand_EmptyArray_Succeeds()
        {
            bool invoked = false;
            RegisterNoArgs("quit", _ => { invoked = true; });

            ExecutionResult result = _handler.Execute("quit", Array.Empty<string>());
            Assert.That(result.Success, Is.True);
            Assert.That(invoked, Is.True);
        }

        [Test]
        public void Execute_TypedArgs_CallbackReceivesCorrectTypes()
        {
            string capturedString = null;
            int capturedInt = 0;
            float capturedFloat = 0f;
            bool capturedBool = false;

            RegisterWithParams("cmd", new[]
            {
                new CommandParameterInfo("name", typeof(string)),
                new CommandParameterInfo("count", typeof(int)),
                new CommandParameterInfo("ratio", typeof(float)),
                new CommandParameterInfo("flag", typeof(bool))
            }, args =>
            {
                capturedString = (string)args[0];
                capturedInt    = (int)args[1];
                capturedFloat  = (float)args[2];
                capturedBool   = (bool)args[3];
            });

            ExecutionResult result = _handler.Execute("cmd", new[] { "hero", "42", "1.5", "true" });

            Assert.That(result.Success, Is.True);
            Assert.That(capturedString, Is.EqualTo("hero"));
            Assert.That(capturedInt,    Is.EqualTo(42));
            Assert.That(capturedFloat,  Is.EqualTo(1.5f));
            Assert.That(capturedBool,   Is.EqualTo(true));
        }

        [Test]
        public void Execute_CaseInsensitiveName_Succeeds()
        {
            bool invoked = false;
            RegisterNoArgs("SetDamage", _ => { invoked = true; });

            ExecutionResult lower = _handler.Execute("setdamage", null);
            Assert.That(lower.Success, Is.True);
            Assert.That(invoked, Is.True);

            invoked = false;
            ExecutionResult upper = _handler.Execute("SETDAMAGE", null);
            Assert.That(upper.Success, Is.True);
            Assert.That(invoked, Is.True);
        }
    }
}
