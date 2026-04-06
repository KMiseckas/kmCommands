using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class SuggestionTests
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

        // ── helpers ──────────────────────────────────────────────────────────

        private void RegisterNoArgs(string name)
        {
            RegistrationResult r = _system.Register(name, Array.Empty<CommandParameterInfo>(), _ => null);
            Assert.That(r.Success, Is.True, string.Format("Setup: Failed to register '{0}': {1}", name, r.ErrorMessage));
        }

        private void RegisterWithDescription(string name, string description)
        {
            RegistrationResult r = _system.Register(name, Array.Empty<CommandParameterInfo>(), _ => null, description);
            Assert.That(r.Success, Is.True, string.Format("Setup: Failed to register '{0}': {1}", name, r.ErrorMessage));
        }

        private void RegisterWithParams(string name, CommandParameterInfo[] parameters)
        {
            RegistrationResult r = _system.Register(name, parameters, _ => null);
            Assert.That(r.Success, Is.True, string.Format("Setup: Failed to register '{0}': {1}", name, r.ErrorMessage));
        }

        // ── test 1: prefix match — correct subset ─────────────────────────────

        [Test]
        public void GetSuggestions_PrefixMatch_ReturnsCorrectSubset()
        {
            RegisterNoArgs("health");
            RegisterNoArgs("help");
            RegisterNoArgs("jump");

            CommandSuggestion[] result = _system.GetSuggestions("he");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result[0].CommandName, Is.EqualTo("health").Or.EqualTo("help"));
            Assert.That(result[1].CommandName, Is.EqualTo("health").Or.EqualTo("help"));
            bool hasHealth = result[0].CommandName == "health" || result[1].CommandName == "health";
            bool hasHelp = result[0].CommandName == "help" || result[1].CommandName == "help";
            Assert.That(hasHealth, Is.True);
            Assert.That(hasHelp, Is.True);
        }

        // ── test 2: prefix match — case-insensitive ──────────────────────────

        [Test]
        public void GetSuggestions_PrefixMatch_CaseInsensitive()
        {
            RegisterNoArgs("health");
            RegisterNoArgs("help");
            RegisterNoArgs("jump");

            CommandSuggestion[] lower = _system.GetSuggestions("he");
            CommandSuggestion[] upper = _system.GetSuggestions("HE");

            Assert.That(upper.Length, Is.EqualTo(lower.Length));
            for (int i = 0; i < lower.Length; i++)
            {
                Assert.That(upper[i].CommandName, Is.EqualTo(lower[i].CommandName).IgnoreCase);
            }
        }

        // ── test 3: null prefix returns all commands ─────────────────────────

        [Test]
        public void GetSuggestions_NullPrefix_ReturnsAllCommands()
        {
            RegisterNoArgs("alpha");
            RegisterNoArgs("beta");

            CommandSuggestion[] result = _system.GetSuggestions(null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(2));
        }

        // ── test 4: empty string prefix returns all commands (sorted) ─────────

        [Test]
        public void GetSuggestions_EmptyPrefix_ReturnsAllCommandsSorted()
        {
            RegisterNoArgs("beta");
            RegisterNoArgs("alpha");

            CommandSuggestion[] result = _system.GetSuggestions(string.Empty);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result[0].CommandName, Is.EqualTo("alpha").IgnoreCase);
            Assert.That(result[1].CommandName, Is.EqualTo("beta").IgnoreCase);
        }

        // ── test 5: no-match prefix returns empty array (not null) ────────────

        [Test]
        public void GetSuggestions_NoMatch_ReturnsEmptyArrayNotNull()
        {
            RegisterNoArgs("health");

            CommandSuggestion[] result = _system.GetSuggestions("zzz");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        // ── test 6: pre-Initialize returns empty array without throwing ───────

        [Test]
        public void GetSuggestions_BeforeInitialize_ReturnsEmptyWithoutThrowing()
        {
            CommandSystem uninit = new CommandSystem();

            CommandSuggestion[] result = null;
            Assert.That(() => result = uninit.GetSuggestions("x"), Throws.Nothing);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        // ── test 7: post-Shutdown returns empty array without throwing ─────────

        [Test]
        public void GetSuggestions_AfterShutdown_ReturnsEmptyWithoutThrowing()
        {
            RegisterNoArgs("health");
            _system.Shutdown();

            CommandSuggestion[] result = null;
            Assert.That(() => result = _system.GetSuggestions("he"), Throws.Nothing);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));

            // Re-initialize so TearDown doesn't fail
            _system.Initialize();
        }

        // ── test 8: two-arg overload preserves matcher-determined order ────────

        [Test]
        public void GetSuggestions_TwoArg_PreservesMatcherDeterminedOrder()
        {
            RegisterNoArgs("alpha");
            RegisterNoArgs("beta");
            RegisterNoArgs("gamma");

            ISuggestionMatcher reverseMatcher = new ReverseOrderMatcher();
            CommandSuggestion[] result = _system.GetSuggestions(string.Empty, reverseMatcher);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(3));
            // Reverse matcher returns gamma, beta, alpha
            Assert.That(result[0].CommandName, Is.EqualTo("gamma").IgnoreCase);
            Assert.That(result[1].CommandName, Is.EqualTo("beta").IgnoreCase);
            Assert.That(result[2].CommandName, Is.EqualTo("alpha").IgnoreCase);
        }

        // ── test 9: two-arg null matcher falls back to built-in ──────────────

        [Test]
        public void GetSuggestions_TwoArgNullMatcher_FallsBackToBuiltIn()
        {
            RegisterNoArgs("health");
            RegisterNoArgs("help");
            RegisterNoArgs("jump");

            CommandSuggestion[] defaultResult = _system.GetSuggestions("he");
            CommandSuggestion[] nullMatcherResult = _system.GetSuggestions("he", null);

            Assert.That(nullMatcherResult.Length, Is.EqualTo(defaultResult.Length));
            for (int i = 0; i < defaultResult.Length; i++)
            {
                Assert.That(nullMatcherResult[i].CommandName, Is.EqualTo(defaultResult[i].CommandName));
            }
        }

        // ── test 10: SetSuggestionMatcher affects subsequent default calls ────

        [Test]
        public void SetSuggestionMatcher_AffectsSubsequentDefaultCalls()
        {
            RegisterNoArgs("health");
            RegisterNoArgs("help");

            ISuggestionMatcher emptyMatcher = new EmptyMatcher();
            _system.SetSuggestionMatcher(emptyMatcher);

            CommandSuggestion[] result = _system.GetSuggestions("he");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        // ── test 11: SetSuggestionMatcher(null) reverts to built-in ──────────

        [Test]
        public void SetSuggestionMatcher_Null_RevertsToBuiltIn()
        {
            RegisterNoArgs("health");
            RegisterNoArgs("help");
            RegisterNoArgs("jump");

            _system.SetSuggestionMatcher(new EmptyMatcher());
            _system.SetSuggestionMatcher(null);

            CommandSuggestion[] result = _system.GetSuggestions("he");
            Assert.That(result.Length, Is.EqualTo(2));
        }

        // ── test 12: Shutdown resets global matcher ───────────────────────────

        [Test]
        public void Shutdown_ResetsGlobalMatcher()
        {
            RegisterNoArgs("health");
            RegisterNoArgs("help");

            _system.SetSuggestionMatcher(new EmptyMatcher());
            _system.Shutdown();
            _system.Initialize();

            RegisterNoArgs("health");
            RegisterNoArgs("help");

            CommandSuggestion[] result = _system.GetSuggestions("he");
            Assert.That(result.Length, Is.EqualTo(2));
        }

        // ── test 13: CommandMetadataSnapshot mirrors CommandSystem ────────────

        [Test]
        public void CommandMetadataSnapshot_GetSuggestions_MirrorsCommandSystem()
        {
            RegisterNoArgs("health");
            RegisterNoArgs("help");
            RegisterNoArgs("jump");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            CommandSuggestion[] systemResult = _system.GetSuggestions("he");
            CommandSuggestion[] snapshotResult = snapshot.GetSuggestions("he");

            Assert.That(snapshotResult.Length, Is.EqualTo(systemResult.Length));
            for (int i = 0; i < systemResult.Length; i++)
            {
                Assert.That(snapshotResult[i].CommandName, Is.EqualTo(systemResult[i].CommandName));
            }
        }

        // ── test 14: CommandMetadataSnapshot.Empty returns empty array ────────

        [Test]
        public void CommandMetadataSnapshot_Empty_GetSuggestions_ReturnsEmptyArrayWithoutThrowing()
        {
            CommandSuggestion[] result = null;
            Assert.That(() => result = CommandMetadataSnapshot.Empty.GetSuggestions("anything"), Throws.Nothing);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        // ── test 15: Parameters never null for zero-parameter command ─────────

        [Test]
        public void GetSuggestions_ZeroParamCommand_ParametersNotNull()
        {
            RegisterNoArgs("health");

            CommandSuggestion[] result = _system.GetSuggestions("health");

            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0].Parameters, Is.Not.Null);
            Assert.That(result[0].Parameters.Length, Is.EqualTo(0));
        }

        // ── test 16: Description never null for no-description command ────────

        [Test]
        public void GetSuggestions_NoDescription_DescriptionNotNull()
        {
            RegisterNoArgs("health");

            CommandSuggestion[] result = _system.GetSuggestions("health");

            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0].Description, Is.Not.Null);
            Assert.That(result[0].Description, Is.EqualTo(string.Empty));
        }

        // ── test 17: Description correctly populated when registered ──────────

        [Test]
        public void GetSuggestions_WithDescription_DescriptionCorrectlyPopulated()
        {
            RegisterWithDescription("heal", "Heals the player");

            CommandSuggestion[] result = _system.GetSuggestions("heal");

            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0].Description, Is.EqualTo("Heals the player"));
        }

        // ── test 18: Snapshot GetSuggestions with custom matcher preserves order

        [Test]
        public void CommandMetadataSnapshot_GetSuggestions_CustomMatcher_PreservesOrder()
        {
            RegisterNoArgs("alpha");
            RegisterNoArgs("beta");
            RegisterNoArgs("gamma");

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            ISuggestionMatcher reverseMatcher = new ReverseOrderMatcher();
            CommandSuggestion[] result = snapshot.GetSuggestions(string.Empty, reverseMatcher);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(3));
            Assert.That(result[0].CommandName, Is.EqualTo("gamma").IgnoreCase);
            Assert.That(result[1].CommandName, Is.EqualTo("beta").IgnoreCase);
            Assert.That(result[2].CommandName, Is.EqualTo("alpha").IgnoreCase);
        }

        // ── stub matchers ─────────────────────────────────────────────────────

        private sealed class EmptyMatcher : ISuggestionMatcher
        {
            public IList<string> Match(string prefix, string[] commandNames)
            {
                return new List<string>();
            }
        }

        private sealed class ReverseOrderMatcher : ISuggestionMatcher
        {
            public IList<string> Match(string prefix, string[] commandNames)
            {
                if (commandNames == null || commandNames.Length == 0)
                    return new List<string>();

                List<string> results = new List<string>();
                for (int i = commandNames.Length - 1; i >= 0; i--)
                {
                    results.Add(commandNames[i]);
                }
                return results;
            }
        }
    }
}
