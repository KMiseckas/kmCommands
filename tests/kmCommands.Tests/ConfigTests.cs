// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using kmCommands.Core;
using NUnit.Framework;

namespace kmCommands.Tests
{
    /// <summary>
    /// Tests for the internal <see cref="JsonConfigParser"/> (Task 1) and the full
    /// config feature (Tasks 2-5): <see cref="ConfigResult"/>, <see cref="CommandConfig"/>,
    /// and <see cref="CommandSystem.Initialize(CommandConfig)"/>.
    /// </summary>
    [TestFixture]
    public class ConfigTests
    {
        private CommandSystem _system;
        private string _tempFilePath;

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

            if (_tempFilePath != null && File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
                _tempFilePath = null;
            }
        }

        // -- JsonConfigParser direct tests ---------------------------------------

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

        // -- ConfigResult factory tests (Task 2) ---------------------------------

        [Test]
        public void ConfigResult_Ok_SetsSuccessTrue()
        {
            var config = new CommandConfig();
            var result = ConfigResult.Ok(config, Array.Empty<string>());

            Assert.That(result.Success, Is.True);
            Assert.That(result.Config, Is.SameAs(config));
            Assert.That(result.Error, Is.EqualTo(ConfigError.None));
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.Warnings, Is.Not.Null);
            Assert.That(result.Warnings.Length, Is.EqualTo(0));
        }

        [Test]
        public void ConfigResult_Ok_WarningsNotNull()
        {
            var config = new CommandConfig();
            var warnings = new[] { "warning one" };
            var result = ConfigResult.Ok(config, warnings);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Warnings, Is.Not.Null);
            Assert.That(result.Warnings.Length, Is.EqualTo(1));
            Assert.That(result.Warnings[0], Is.EqualTo("warning one"));
        }

        [Test]
        public void ConfigResult_Fail_SetsSuccessFalse()
        {
            var result = ConfigResult.Fail(ConfigError.InvalidJson, "bad json");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Config, Is.Null);
            Assert.That(result.Error, Is.EqualTo(ConfigError.InvalidJson));
            Assert.That(result.ErrorMessage, Is.EqualTo("bad json"));
            Assert.That(result.Warnings, Is.Not.Null);
            Assert.That(result.Warnings.Length, Is.EqualTo(0));
        }

        [Test]
        public void ConfigResult_Fail_FileReadError_HasCorrectEnum()
        {
            var result = ConfigResult.Fail(ConfigError.FileReadError, "file not found");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.FileReadError));
        }

        [Test]
        public void ConfigResult_Fail_TypeMismatch_HasCorrectEnum()
        {
            var result = ConfigResult.Fail(ConfigError.TypeMismatch, "type mismatch");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.TypeMismatch));
        }

        // -- CommandConfig defaults ----------------------------------------------

        [Test]
        public void CommandConfig_Defaults_MatchSystemDefaults()
        {
            var config = new CommandConfig();

            Assert.That(config.HistoryCapacity, Is.EqualTo(CommandSystem.DefaultHistoryCapacity));
            Assert.That(config.DevMode, Is.False);
        }

        // -- FromJson - valid inputs --------------------------------------------

        [Test]
        public void FromJson_FullConfig_SetsAllValues()
        {
            var result = CommandConfig.FromJson("{ \"historyCapacity\": 128, \"devMode\": true }");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(128));
            Assert.That(result.Config.DevMode, Is.True);
        }

        [Test]
        public void FromJson_PartialConfig_CapacityOnly_DefaultsDevMode()
        {
            var result = CommandConfig.FromJson("{ \"historyCapacity\": 256 }");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(256));
            Assert.That(result.Config.DevMode, Is.False);
        }

        [Test]
        public void FromJson_PartialConfig_DevModeOnly_DefaultsCapacity()
        {
            var result = CommandConfig.FromJson("{ \"devMode\": true }");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.DevMode, Is.True);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(CommandSystem.DefaultHistoryCapacity));
        }

        [Test]
        public void FromJson_EmptyObject_AllDefaults()
        {
            var result = CommandConfig.FromJson("{}");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(CommandSystem.DefaultHistoryCapacity));
            Assert.That(result.Config.DevMode, Is.False);
        }

        [Test]
        public void FromJson_WhitespaceHeavy_ParsesCorrectly()
        {
            var result = CommandConfig.FromJson("{  \"historyCapacity\" :  128  }");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(128));
        }

        [Test]
        public void FromJson_NegativeCapacity_SucceedsClampingDeferredToInit()
        {
            var result = CommandConfig.FromJson("{ \"historyCapacity\": -5 }");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(-5));
        }

        [Test]
        public void FromJson_ZeroCapacity_Succeeds()
        {
            var result = CommandConfig.FromJson("{ \"historyCapacity\": 0 }");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(0));
        }

        [Test]
        public void FromJson_CaseInsensitiveKeys_Parsed()
        {
            var result = CommandConfig.FromJson("{ \"HISTORYCAPACITY\": 100 }");

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(100));
        }

        // -- FromJson - unknown keys (warnings) ----------------------------------

        [Test]
        public void FromJson_UnknownKey_SuccessWithOneWarning()
        {
            var result = CommandConfig.FromJson("{ \"historyCapacity\": 64, \"unknownKey\": \"foo\" }");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Warnings.Length, Is.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("unknownKey"));
        }

        [Test]
        public void FromJson_TwoUnknownKeys_TwoWarnings()
        {
            var result = CommandConfig.FromJson("{ \"a\": 1, \"b\": true }");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Warnings.Length, Is.EqualTo(2));
        }

        [Test]
        public void FromJson_UnknownKeyStringValue_WarningNotError()
        {
            var result = CommandConfig.FromJson("{ \"myKey\": \"value\" }");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Warnings.Length, Is.EqualTo(1));
        }

        [Test]
        public void FromJson_UnknownKeyIntValue_WarningNotError()
        {
            var result = CommandConfig.FromJson("{ \"myKey\": 99 }");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Warnings.Length, Is.EqualTo(1));
        }

        [Test]
        public void FromJson_UnknownKeyBoolValue_WarningNotError()
        {
            var result = CommandConfig.FromJson("{ \"myKey\": false }");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Warnings.Length, Is.EqualTo(1));
        }

        [Test]
        public void FromJson_UnknownKeyNullValue_WarningNotError()
        {
            var result = CommandConfig.FromJson("{ \"myKey\": null }");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Warnings.Length, Is.EqualTo(1));
        }

        [Test]
        public void FromJson_SuccessResult_WarningsNeverNull()
        {
            var result = CommandConfig.FromJson("{}");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Warnings, Is.Not.Null);
        }

        // -- FromJson - errors --------------------------------------------------

        [Test]
        public void FromJson_Null_FailsWithInvalidJson()
        {
            var result = CommandConfig.FromJson(null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.InvalidJson));
            Assert.That(result.Config, Is.Null);
        }

        [Test]
        public void FromJson_EmptyString_FailsWithInvalidJson()
        {
            var result = CommandConfig.FromJson("");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.InvalidJson));
        }

        [Test]
        public void FromJson_MalformedJson_FailsWithInvalidJson()
        {
            var result = CommandConfig.FromJson("{ broken");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.InvalidJson));
        }

        [Test]
        public void FromJson_DevModeWrongType_Int_FailsWithTypeMismatch()
        {
            var result = CommandConfig.FromJson("{ \"devMode\": 42 }");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.TypeMismatch));
            Assert.That(result.Config, Is.Null);
        }

        [Test]
        public void FromJson_HistoryCapacityWrongType_Bool_FailsWithTypeMismatch()
        {
            var result = CommandConfig.FromJson("{ \"historyCapacity\": true }");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.TypeMismatch));
        }

        [Test]
        public void FromJson_HistoryCapacityWrongType_String_FailsWithTypeMismatch()
        {
            var result = CommandConfig.FromJson("{ \"historyCapacity\": \"128\" }");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.TypeMismatch));
        }

        [Test]
        public void FromJson_DevModeWrongType_String_FailsWithTypeMismatch()
        {
            var result = CommandConfig.FromJson("{ \"devMode\": \"true\" }");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.TypeMismatch));
        }

        [Test]
        public void FromJson_HistoryCapacityNull_FailsWithTypeMismatch()
        {
            var result = CommandConfig.FromJson("{ \"historyCapacity\": null }");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.TypeMismatch));
        }

        // -- FromFile - errors --------------------------------------------------

        [Test]
        public void FromFile_NonExistentPath_FailsWithFileReadError()
        {
            var result = CommandConfig.FromFile("nonexistent_file_that_does_not_exist.json");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.FileReadError));
            Assert.That(result.Config, Is.Null);
        }

        [Test]
        public void FromFile_NullPath_FailsWithFileReadError()
        {
            var result = CommandConfig.FromFile(null);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.FileReadError));
        }

        [Test]
        public void FromFile_EmptyPath_FailsWithFileReadError()
        {
            var result = CommandConfig.FromFile("");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.FileReadError));
        }

        // -- FromFile - valid temp file -----------------------------------------

        [Test]
        public void FromFile_ValidTempFile_SuccessWithCorrectValues()
        {
            _tempFilePath = Path.GetTempFileName();
            File.WriteAllText(_tempFilePath, "{ \"historyCapacity\": 200, \"devMode\": true }");

            var result = CommandConfig.FromFile(_tempFilePath);

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(200));
            Assert.That(result.Config.DevMode, Is.True);
        }

        [Test]
        public void FromFile_ValidTempFileEmptyObject_AllDefaults()
        {
            _tempFilePath = Path.GetTempFileName();
            File.WriteAllText(_tempFilePath, "{}");

            var result = CommandConfig.FromFile(_tempFilePath);

            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Config.HistoryCapacity, Is.EqualTo(CommandSystem.DefaultHistoryCapacity));
            Assert.That(result.Config.DevMode, Is.False);
        }

        // -- Initialize(CommandConfig) - integration ----------------------------

        [Test]
        public void Initialize_WithConfig_InitializesWithCorrectCapacityAndDevMode()
        {
            var config = new CommandConfig { HistoryCapacity = 128, DevMode = true };
            _system.Initialize(config);

            Assert.That(_system.IsInitialized, Is.True);

            // Verify DevMode is applied by registering a DevOnly command via scan
            ScanResult scan = _system.Scan(typeof(ConfigDevTarget));
            bool devOnlyRegistered = false;
            for (int i = 0; i < scan.Entries.Length; i++)
            {
                if (scan.Entries[i].CommandName == "config_devonly_cmd" &&
                    scan.Entries[i].Result.Success)
                {
                    devOnlyRegistered = true;
                    break;
                }
            }
            Assert.That(devOnlyRegistered, Is.True, "Dev-only command should be registered when DevMode = true");
        }

        [Test]
        public void Initialize_WithConfig_WhenAlreadyInitialized_IsNoOp()
        {
            _system.Initialize();
            Assert.That(_system.IsInitialized, Is.True);

            var config = new CommandConfig { HistoryCapacity = 200, DevMode = true };
            _system.Initialize(config); // should be no-op

            Assert.That(_system.IsInitialized, Is.True);
        }

        [Test]
        public void Initialize_WithNullConfig_IsNoOp()
        {
            _system.Initialize(null);

            Assert.That(_system.IsInitialized, Is.False);
        }

        [Test]
        public void Initialize_WithConfig_AfterShutdown_WorksCorrectly()
        {
            _system.Initialize();
            _system.Shutdown();

            var config = new CommandConfig { HistoryCapacity = 50, DevMode = false };
            _system.Initialize(config);

            Assert.That(_system.IsInitialized, Is.True);
        }

        [Test]
        public void Initialize_WithConfig_ZeroCapacity_ClampedToOne()
        {
            var config = new CommandConfig { HistoryCapacity = 0 };
            _system.Initialize(config);

            Assert.That(_system.IsInitialized, Is.True);

            // Executing a command should record to history (capacity is clamped to 1)
            _system.Register("test_zero_cap", Array.Empty<CommandParameterInfo>(), _ => null);
            _system.Execute("test_zero_cap", null);
            Assert.That(_system.HistoryCount, Is.EqualTo(1));
        }

        [Test]
        public void Initialize_WithConfig_HistoryCapacity_Applied()
        {
            var config = new CommandConfig { HistoryCapacity = 3 };
            _system.Initialize(config);

            _system.Register("cmd", Array.Empty<CommandParameterInfo>(), _ => null);

            // Execute more than capacity -- only last 3 should be retained
            for (int i = 0; i < 5; i++)
            {
                _system.Execute("cmd", null);
            }

            Assert.That(_system.HistoryCount, Is.EqualTo(3));
        }

        // -- Private target classes for scan tests ------------------------------

        private static class ConfigDevTarget
        {
            [Command("config_devonly_cmd", IsDevOnly = true)]
            public static void DevOnlyCommand() { }
        }

        // -- NestedCommandDepth config tests ------------------------------------

        [Test]
        public void FromJson_NestedCommandDepth_ParsedCorrectly()
        {
            var result = CommandConfig.FromJson("{ \"nestedCommandDepth\": 2 }");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Config.NestedCommandDepth, Is.EqualTo(2));
        }

        [Test]
        public void FromJson_NestedCommandDepthAbsent_DefaultApplied()
        {
            var result = CommandConfig.FromJson("{}");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Config.NestedCommandDepth,
                Is.EqualTo(CommandSystem.DefaultNestedCommandDepth));
        }

        [Test]
        public void FromJson_NestedCommandDepth_TypeMismatch_ReturnsFail()
        {
            var result = CommandConfig.FromJson("{ \"nestedCommandDepth\": \"bad\" }");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ConfigError.TypeMismatch));
        }

        [Test]
        public void Initialize_WithConfig_DepthApplied_DepthExceededAtConfiguredLimit()
        {
            // depth = 2: outer call starts at depth 0; each $(…) token increments depth.
            // 2 levels: $(mid $(inner)) — token at depth 0, inner's arg at depth 1 → success.
            // 3 levels: $(mid $(deep $(inner))) — depth 0, 1, then 2 >= maxDepth → exceeded.
            var config = new CommandConfig { NestedCommandDepth = 2 };
            _system.Initialize(config);

            _system.Register("inner",
                Array.Empty<CommandParameterInfo>(),
                _ => (object)"val");
            _system.Register("mid",
                new[] { new CommandParameterInfo("x", typeof(string)) },
                args => args[0]);
            _system.Register("deep",
                new[] { new CommandParameterInfo("x", typeof(string)) },
                args => args[0]);
            _system.Register("outer",
                new[] { new CommandParameterInfo("v", typeof(string)) },
                args => args[0]);

            // 2 levels deep — should succeed (depth 0 → token, depth 1 → inner token, depth 2 for empty args).
            var ok = _system.Execute("outer", new[] { "$(mid $(inner))" });
            Assert.That(ok.Success, Is.True,
                "Expected success at 2 nesting levels with limit 2.");

            // 3 levels deep — should fail: depth 2 hits `$(inner)` token which is >= maxDepth=2.
            var fail = _system.Execute("outer", new[] { "$(mid $(deep $(inner)))" });
            Assert.That(fail.Error, Is.EqualTo(ExecutionError.NestedCommandDepthExceeded),
                "Expected NestedCommandDepthExceeded at 3 nesting levels with limit 2.");
        }

        [Test]
        public void Initialize_WithConfig_DepthZero_ClampedToOne()
        {
            // depth = 0 in config → clamped to 1 in InitializeCore → one level of nesting allowed.
            var config = new CommandConfig { NestedCommandDepth = 0 };
            _system.Initialize(config);

            _system.Register("inner",
                Array.Empty<CommandParameterInfo>(),
                _ => (object)42);
            _system.Register("outer",
                new[] { new CommandParameterInfo("v", typeof(int)) },
                args => args[0]);

            // Exactly 1 level — should succeed.
            var ok = _system.Execute("outer", new[] { "$(inner)" });
            Assert.That(ok.Success, Is.True,
                "Expected 1-level nesting to succeed when depth clamped to 1.");
        }
    }
}
