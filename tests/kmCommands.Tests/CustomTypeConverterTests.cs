using System;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class CustomTypeConverterTests
    {
        private CommandSystem _system;

        // A simple custom value type used across tests
        private struct Vector2Custom
        {
            public float X;
            public float Y;
        }

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

        // â”€â”€ helper â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>Parses "x,y" into a Vector2Custom.</summary>
        private static bool TryParseVector2(string input, out object result)
        {
            string[] parts = input.Split(',');
            if (parts.Length == 2
                && float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float x)
                && float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float y))
            {
                result = new Vector2Custom { X = x, Y = y };
                return true;
            }

            result = null;
            return false;
        }

        // â”€â”€ happy path â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void RegisterConverter_CustomType_AllowsCommandWithThatType()
        {
            _system.RegisterConverter(typeof(Vector2Custom), TryParseVector2);
            _system.Initialize();

            Vector2Custom captured = default;
            RegistrationResult reg = _system.Register(
                "move",
                new[] { new CommandParameterInfo("pos", typeof(Vector2Custom)) },
                args => { captured = (Vector2Custom)args[0]; return null; });

            Assert.That(reg.Success, Is.True, reg.ErrorMessage);

            ExecutionResult exec = _system.Execute("move", new[] { "3.0,4.5" });
            Assert.That(exec.Success, Is.True, exec.ErrorMessage);
            Assert.That(captured.X, Is.EqualTo(3.0f).Within(0.001f));
            Assert.That(captured.Y, Is.EqualTo(4.5f).Within(0.001f));
        }

        // â”€â”€ null-input guards â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void RegisterConverter_NullType_ReturnsFailure()
        {
            RegistrationResult result = _system.RegisterConverter(null, TryParseVector2);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.NullParameters));
        }

        [Test]
        public void RegisterConverter_NullDelegate_ReturnsFailure()
        {
            RegistrationResult result = _system.RegisterConverter(typeof(Vector2Custom), null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(RegistrationError.NullConverter));
        }

        // â”€â”€ override built-in â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void RegisterConverter_OverridesBuiltIn_UsesNewConverter()
        {
            // Register a replacement int converter that always returns 99
            _system.RegisterConverter(typeof(int), (string input, out object result) =>
            {
                result = 99;
                return true;
            });
            _system.Initialize();

            int captured = 0;
            _system.Register(
                "val",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => { captured = (int)args[0]; return null; });

            ExecutionResult exec = _system.Execute("val", new[] { "1" });
            Assert.That(exec.Success, Is.True, exec.ErrorMessage);
            Assert.That(captured, Is.EqualTo(99));
        }

        // â”€â”€ lifecycle: pre-Initialize â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void RegisterConverter_BeforeInitialize_SurvivesInitialize()
        {
            _system.RegisterConverter(typeof(Vector2Custom), TryParseVector2);
            _system.Initialize();

            RegistrationResult reg = _system.Register(
                "move",
                new[] { new CommandParameterInfo("pos", typeof(Vector2Custom)) },
                args => null);

            Assert.That(reg.Success, Is.True, reg.ErrorMessage);

            ExecutionResult exec = _system.Execute("move", new[] { "1.0,2.0" });
            Assert.That(exec.Success, Is.True, exec.ErrorMessage);
        }

        // â”€â”€ lifecycle: Shutdown clears â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Shutdown_ClearsCustomConverters()
        {
            _system.RegisterConverter(typeof(Vector2Custom), TryParseVector2);
            _system.Initialize();
            _system.Shutdown();

            // Re-initialize without re-registering the converter
            _system.Initialize();

            RegistrationResult reg = _system.Register(
                "move",
                new[] { new CommandParameterInfo("pos", typeof(Vector2Custom)) },
                args => null);

            Assert.That(reg.Success, Is.False);
            Assert.That(reg.Error, Is.EqualTo(RegistrationError.UnsupportedParameterType));
        }

        // â”€â”€ unsupported type â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Register_WithNoConverter_RejectsCommand()
        {
            _system.Initialize();

            RegistrationResult reg = _system.Register(
                "move",
                new[] { new CommandParameterInfo("pos", typeof(Vector2Custom)) },
                args => null);

            Assert.That(reg.Success, Is.False);
            Assert.That(reg.Error, Is.EqualTo(RegistrationError.UnsupportedParameterType));
        }

        // â”€â”€ failing converter â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_FailingCustomConverter_ReturnsConversionFailed()
        {
            _system.RegisterConverter(typeof(Vector2Custom), (string input, out object result) =>
            {
                result = null;
                return false; // always fail
            });
            _system.Initialize();

            _system.Register(
                "move",
                new[] { new CommandParameterInfo("pos", typeof(Vector2Custom)) },
                args => null);

            ExecutionResult exec = _system.Execute("move", new[] { "bad" });
            Assert.That(exec.Success, Is.False);
            Assert.That(exec.Error, Is.EqualTo(ExecutionError.ArgumentConversionFailed));
        }

        // â”€â”€ pre-init multiple converters â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void RegisterConverter_PreInit_MultipleConverters_AllFlushed()
        {
            _system.RegisterConverter(typeof(Vector2Custom), TryParseVector2);
            _system.RegisterConverter(typeof(Guid), (string input, out object result) =>
            {
                if (Guid.TryParse(input, out Guid g))
                {
                    result = g;
                    return true;
                }
                result = null;
                return false;
            });
            _system.Initialize();

            RegistrationResult regV2 = _system.Register(
                "move",
                new[] { new CommandParameterInfo("pos", typeof(Vector2Custom)) },
                args => null);

            RegistrationResult regGuid = _system.Register(
                "setId",
                new[] { new CommandParameterInfo("id", typeof(Guid)) },
                args => null);

            Assert.That(regV2.Success, Is.True, regV2.ErrorMessage);
            Assert.That(regGuid.Success, Is.True, regGuid.ErrorMessage);
        }

        // â”€â”€ pre-init last-write-wins â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void RegisterConverter_PreInit_Override_LastWriteWins()
        {
            // First registration always returns 1
            _system.RegisterConverter(typeof(int), (string input, out object result) =>
            {
                result = 1;
                return true;
            });

            // Second registration always returns 2 â€” must win
            _system.RegisterConverter(typeof(int), (string input, out object result) =>
            {
                result = 2;
                return true;
            });

            _system.Initialize();

            int captured = 0;
            _system.Register(
                "val",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => { captured = (int)args[0]; return null; });

            ExecutionResult exec = _system.Execute("val", new[] { "anything" });
            Assert.That(exec.Success, Is.True, exec.ErrorMessage);
            Assert.That(captured, Is.EqualTo(2));
        }
    }
}
