// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using kmCommands.Core;
using NUnit.Framework;

namespace kmCommands.Tests
{
    /// <summary>
    /// Tests for the internal <see cref="JsonConfigParser"/> (Task 1) and the full
    /// config feature (Tasks 2–5): <see cref="ConfigResult"/>, <see cref="CommandConfig"/>,
    /// and <see cref="CommandSystem.Initialize(CommandConfig)"/>.
    /// </summary>
    [TestFixture]
    public class ConfigTests
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

        // ── JsonConfigParser direct tests ─────────────────────────────────────────

        [Test]
        public void Parse_ValidFlatObject_ReturnsNoError()
        {
            var output = JsonConfigParser.Parse(
                "{ \"name\": \"hello\", \"count\": 42, \"flag\": true, \"nothing\": null }");

            Assert.That(output.HasError, Is.False, output.Error);
            Assert.That(output.Values.Length, Is.EqualTo(4));
        }

        [Test]
        public void Parse_ValidFlatObject_CorrectTypes()
        {
            var output = JsonConfigParser.Parse(
                "{ \"name\": \"hello\", \"count\": 42, \"flag\": true, \"nothing\": null }");

            Assert.That(output.HasError, Is.False);
            Assert.That(output.Values[0].ValueType, Is.EqualTo(typeof(string)));
            Assert.That(output.Values[0].Value, Is.EqualTo("hello"));
            Assert.That(output.Values[1].ValueType, Is.EqualTo(typeof(int)));
            Assert.That(output.Values[1].Value, Is.EqualTo(42));
            Assert.That(output.Values[2].ValueType, Is.EqualTo(typeof(bool)));
            Assert.That(output.Values[2].Value, Is.EqualTo(true));
            Assert.That(output.Values[3].ValueType, Is.Null);
            Assert.That(output.Values[3].Value, Is.Null);
        }

        [Test]
        public void Parse_EmptyObject_ReturnsNoErrorAndZeroValues()
        {
            var output = JsonConfigParser.Parse("{}");

            Assert.That(output.HasError, Is.False, output.Error);
            Assert.That(output.Values.Length, Is.EqualTo(0));
        }

        [Test]
        public void Parse_WhitespaceHeavy_ParsesCorrectly()
        {
            var output = JsonConfigParser.Parse(
                "  {  \n  \"historyCapacity\"  :  128  \n  }  ");

            Assert.That(output.HasError, Is.False, output.Error);
            Assert.That(output.Values.Length, Is.EqualTo(1));
            Assert.That(output.Values[0].Value, Is.EqualTo(128));
        }

        [Test]
        public void Parse_NegativeInteger_AcceptedAndParsedAsInt()
        {
            var output = JsonConfigParser.Parse("{ \"capacity\": -5 }");

            Assert.That(output.HasError, Is.False, output.Error);
            Assert.That(output.Values.Length, Is.EqualTo(1));
            Assert.That((int)output.Values[0].Value, Is.EqualTo(-5));
            Assert.That(output.Values[0].ValueType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void Parse_DuplicateKeys_LastValueWins()
        {
            var output = JsonConfigParser.Parse(
                "{ \"key\": 1, \"key\": 2, \"key\": 3 }");

            Assert.That(output.HasError, Is.False, output.Error);
            Assert.That(output.Values.Length, Is.EqualTo(1));
            Assert.That((int)output.Values[0].Value, Is.EqualTo(3));
        }

        [Test]
        public void Parse_FalseBoolean_ParsedCorrectly()
        {
            var output = JsonConfigParser.Parse("{ \"flag\": false }");

            Assert.That(output.HasError, Is.False, output.Error);
            Assert.That(output.Values[0].Value, Is.EqualTo(false));
            Assert.That(output.Values[0].ValueType, Is.EqualTo(typeof(bool)));
        }

        [Test]
        public void Parse_MissingOpenBrace_ReturnsError()
        {
            var output = JsonConfigParser.Parse("\"key\": 1 }");

            Assert.That(output.HasError, Is.True);
            Assert.That(output.Error, Is.Not.Null);
        }

        [Test]
        public void Parse_MissingColon_ReturnsError()
        {
            var output = JsonConfigParser.Parse("{ \"key\" 1 }");

            Assert.That(output.HasError, Is.True);
            Assert.That(output.Error, Is.Not.Null);
        }

        [Test]
        public void Parse_UnclosedObject_ReturnsError()
        {
            var output = JsonConfigParser.Parse("{ \"key\": 1");

            Assert.That(output.HasError, Is.True);
            Assert.That(output.Error, Is.Not.Null);
        }

        [Test]
        public void Parse_TrailingContentAfterClose_ReturnsError()
        {
            var output = JsonConfigParser.Parse("{ \"key\": 1 } extra");

            Assert.That(output.HasError, Is.True);
            Assert.That(output.Error, Is.Not.Null);
        }

        [Test]
        public void Parse_FloatValue_ReturnsError()
        {
            var output = JsonConfigParser.Parse("{ \"rate\": 1.5 }");

            Assert.That(output.HasError, Is.True);
            Assert.That(output.Error, Is.Not.Null);
        }
    }
}
