using System;
using System.Globalization;
using System.Threading;
using kmCommands.Core;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class ArgumentConverterTests
    {
        private ArgumentConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = new ArgumentConverter();
        }

        // â”€â”€ int â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void TryConvert_Int_ValidPositive_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(int), "42", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void TryConvert_Int_Zero_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(int), "0", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void TryConvert_Int_Negative_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(int), "-10", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(-10));
        }

        [Test]
        public void TryConvert_Int_InvalidString_ReturnsFalse()
        {
            bool result = _converter.TryConvert(typeof(int), "abc", out object value);
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void TryConvert_Int_FloatString_ReturnsFalse()
        {
            bool result = _converter.TryConvert(typeof(int), "1.5", out object value);
            Assert.That(result, Is.False);
        }

        // â”€â”€ float â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void TryConvert_Float_ValidDecimal_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(float), "1.5", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(1.5f));
        }

        [Test]
        public void TryConvert_Float_NegativeDecimal_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(float), "-3.14", out object value);
            Assert.That(result, Is.True);
            Assert.That((float)value, Is.EqualTo(-3.14f).Within(0.0001f));
        }

        [Test]
        public void TryConvert_Float_InvalidString_ReturnsFalse()
        {
            bool result = _converter.TryConvert(typeof(float), "xyz", out object value);
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void TryConvert_Float_InvariantCulture_CommaDecimalCulture_StillParsesDot()
        {
            CultureInfo original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                bool result = _converter.TryConvert(typeof(float), "1.5", out object value);
                Assert.That(result, Is.True, "Float '1.5' should parse with InvariantCulture even on de-DE thread culture.");
                Assert.That(value, Is.EqualTo(1.5f));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        // â”€â”€ bool â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void TryConvert_Bool_TrueLowercase_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(bool), "true", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(true));
        }

        [Test]
        public void TryConvert_Bool_TrueMixedCase_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(bool), "True", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(true));
        }

        [Test]
        public void TryConvert_Bool_FalseLowercase_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(bool), "false", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(false));
        }

        [Test]
        public void TryConvert_Bool_FalseUppercase_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(bool), "FALSE", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(false));
        }

        [Test]
        public void TryConvert_Bool_InvalidString_ReturnsFalse()
        {
            bool result = _converter.TryConvert(typeof(bool), "yes", out object value);
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        }

        // â”€â”€ string â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void TryConvert_String_AnyInput_AlwaysReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(string), "hello world", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo("hello world"));
        }

        [Test]
        public void TryConvert_String_EmptyString_ReturnsTrue()
        {
            bool result = _converter.TryConvert(typeof(string), "", out object value);
            Assert.That(result, Is.True);
            Assert.That(value, Is.EqualTo(""));
        }

        // â”€â”€ unsupported type â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void TryConvert_UnsupportedType_Double_ReturnsFalse()
        {
            bool result = _converter.TryConvert(typeof(double), "1.0", out object value);
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void IsTypeSupported_Double_ReturnsFalse()
        {
            Assert.That(_converter.IsTypeSupported(typeof(double)), Is.False);
        }

        [Test]
        public void IsTypeSupported_BuiltInTypes_AllReturnTrue()
        {
            Assert.That(_converter.IsTypeSupported(typeof(int)), Is.True);
            Assert.That(_converter.IsTypeSupported(typeof(float)), Is.True);
            Assert.That(_converter.IsTypeSupported(typeof(bool)), Is.True);
            Assert.That(_converter.IsTypeSupported(typeof(string)), Is.True);
        }
    }

    [TestFixture]
    public class CommandRegistryTests
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
        public void TryRegister_NewCommand_ReturnsTrue()
        {
            bool result = _registry.TryRegister(MakeDefinition("foo"));
            Assert.That(result, Is.True);
        }

        [Test]
        public void TryRegister_DuplicateName_ReturnsFalse()
        {
            _registry.TryRegister(MakeDefinition("foo"));
            bool result = _registry.TryRegister(MakeDefinition("foo"));
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryRegister_DuplicateNameDifferentCase_ReturnsFalse()
        {
            _registry.TryRegister(MakeDefinition("foo"));
            bool result = _registry.TryRegister(MakeDefinition("FOO"));
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryGetCommand_ExistingCommand_ReturnsTrue()
        {
            _registry.TryRegister(MakeDefinition("bar"));
            bool found = _registry.TryGetCommand("bar", out CommandDefinition def);
            Assert.That(found, Is.True);
            Assert.That(def, Is.Not.Null);
            Assert.That(def.Name, Is.EqualTo("bar"));
        }

        [Test]
        public void TryGetCommand_CaseInsensitive_ReturnsTrue()
        {
            _registry.TryRegister(MakeDefinition("MyCmd"));
            bool found = _registry.TryGetCommand("mycmd", out CommandDefinition def);
            Assert.That(found, Is.True);
            Assert.That(def, Is.Not.Null);
        }

        [Test]
        public void TryGetCommand_NotFound_ReturnsFalse()
        {
            bool found = _registry.TryGetCommand("nonexistent", out CommandDefinition def);
            Assert.That(found, Is.False);
            Assert.That(def, Is.Null);
        }

        [Test]
        public void Count_AfterRegistrations_IsCorrect()
        {
            Assert.That(_registry.Count, Is.EqualTo(0));
            _registry.TryRegister(MakeDefinition("a"));
            Assert.That(_registry.Count, Is.EqualTo(1));
            _registry.TryRegister(MakeDefinition("b"));
            Assert.That(_registry.Count, Is.EqualTo(2));
        }

        [Test]
        public void Clear_RemovesAllCommands()
        {
            _registry.TryRegister(MakeDefinition("a"));
            _registry.TryRegister(MakeDefinition("b"));
            _registry.Clear();
            Assert.That(_registry.Count, Is.EqualTo(0));
            bool found = _registry.TryGetCommand("a", out _);
            Assert.That(found, Is.False);
        }
    }
}
