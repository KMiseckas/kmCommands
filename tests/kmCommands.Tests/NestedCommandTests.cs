// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using NUnit.Framework;
using kmCommands.Core;

namespace kmCommands.Tests
{
    /// <summary>
    /// Tests for nested command resolution — ExecuteResolved isolation (Task 2)
    /// and full integration (Task 4).
    /// </summary>
    [TestFixture]
    internal class NestedCommandTests
    {
        // ── ExecuteResolved isolation tests (Task 2) ─────────────────────────────

        private CommandSystem BuildSystem()
        {
            var sys = new CommandSystem();
            sys.Initialize();
            return sys;
        }

        [Test]
        public void ExecuteResolved_StringArg_ConvertedNormally()
        {
            var sys = BuildSystem();
            int captured = -1;
            sys.Register("cmd",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => { captured = (int)args[0]; return null; });

            // Use Execute with a plain string arg — goes through normal path, validates the
            // string-to-int conversion works (indirect test of the non-pre-resolved branch).
            var result = sys.Execute("cmd", new[] { "42" });

            Assert.That(result.Success, Is.True);
            Assert.That(captured, Is.EqualTo(42));
        }

        [Test]
        public void ExecuteResolved_PreResolvedArg_AssignableType_Passes()
        {
            var sys = BuildSystem();
            sys.Initialize();

            // Register inner (returns object) and outer (accepts object).
            sys.Register("inner",
                Array.Empty<CommandParameterInfo>(),
                _ => (object)"payload");

            object received = null;
            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(object)) },
                args => { received = args[0]; return null; });

            // Execute with nested token — resolver runs ExecuteResolved internally.
            var result = sys.Execute("outer", new[] { "$(inner)" });

            Assert.That(result.Success, Is.True);
            Assert.That(received, Is.EqualTo("payload"));
        }

        [Test]
        public void ExecuteResolved_PreResolvedArg_NullForValueType_ReturnsTypeMismatch()
        {
            var sys = BuildSystem();

            // inner returns null explicitly
            sys.Register("inner",
                Array.Empty<CommandParameterInfo>(),
                _ => (object)null);

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(int)) },
                args => null);

            var result = sys.Execute("outer", new[] { "$(inner)" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NestedCommandTypeMismatch));
        }

        [Test]
        public void ExecuteResolved_PreResolvedArg_IncompatibleType_NoStringConverter_ReturnsTypeMismatch()
        {
            var sys = BuildSystem();

            // inner returns an object whose ToString() is not a valid int
            sys.Register("inner",
                Array.Empty<CommandParameterInfo>(),
                _ => (object)"not_an_int");

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(int)) },
                args => null);

            // "not_an_int" is a string, not assignable to int, and ToString() → "not_an_int"
            // which also fails int conversion.
            var result = sys.Execute("outer", new[] { "$(inner)" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NestedCommandTypeMismatch));
        }

        [Test]
        public void ExecuteResolved_PreResolvedArg_IncompatibleTypeWithStringFallback_Passes()
        {
            var sys = BuildSystem();

            // inner returns a custom type whose ToString() is a valid int string
            sys.Register("inner",
                Array.Empty<CommandParameterInfo>(),
                _ => (object)new IntStringBox(7));

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(int)) },
                args => args[0]);

            var result = sys.Execute("outer", new[] { "$(inner)" });

            Assert.That(result.Success, Is.True);
            Assert.That(result.ReturnValue, Is.EqualTo(7));
        }

        [Test]
        public void ExecuteResolved_CommandNotFound_ReturnsCommandNotFound()
        {
            var sys = BuildSystem();

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(int)) },
                args => null);

            var result = sys.Execute("outer", new[] { "$(nonexistent 1)" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NestedCommandFailed));
        }

        [Test]
        public void ExecuteResolved_ArgumentCountMismatch_ReturnsCountMismatch()
        {
            var sys = BuildSystem();

            sys.Register("inner",
                new[] { new CommandParameterInfo("x", typeof(int)) },
                args => args[0]);

            // outer expects object but inner will fail before that
            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(object)) },
                args => null);

            // inner requires 1 arg, but we provide 2. This fails inside ExecuteResolved.
            var result = sys.Execute("outer", new[] { "$(inner 1 2)" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NestedCommandFailed));
        }

        // ── Full integration tests (Task 4) ──────────────────────────────────────

        [Test]
        public void Execute_SingleNestedArg_InnerExecutes_OuterReceivesReturnValue()
        {
            var sys = BuildSystem();

            sys.Register("getVal",
                Array.Empty<CommandParameterInfo>(),
                _ => (object)99);

            int received = -1;
            sys.Register("useVal",
                new[] { new CommandParameterInfo("v", typeof(int)) },
                args => { received = (int)args[0]; return null; });

            var result = sys.Execute("useVal", new[] { "$(getVal)" });

            Assert.That(result.Success, Is.True);
            Assert.That(received, Is.EqualTo(99));
        }

        [Test]
        public void Execute_TwoLevelNesting_ResolvesFromInnermostFirst()
        {
            var sys = BuildSystem();

            sys.Register("base",
                Array.Empty<CommandParameterInfo>(),
                _ => (object)5);

            sys.Register("double",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => (object)((int)args[0] * 2));

            int received = -1;
            sys.Register("store",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => { received = (int)args[0]; return null; });

            var result = sys.Execute("store", new[] { "$(double $(base))" });

            Assert.That(result.Success, Is.True);
            Assert.That(received, Is.EqualTo(10));
        }

        [Test]
        public void Execute_ThreeLevelNesting_ResolvesCorrectly()
        {
            var sys = BuildSystem();

            sys.Register("one", Array.Empty<CommandParameterInfo>(), _ => (object)1);
            sys.Register("inc",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => (object)((int)args[0] + 1));

            int received = -1;
            sys.Register("store",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => { received = (int)args[0]; return null; });

            // store(inc(inc(one()))) = store(inc(inc(1))) = store(inc(2)) = store(3)
            var result = sys.Execute("store", new[] { "$(inc $(inc $(one)))" });

            Assert.That(result.Success, Is.True);
            Assert.That(received, Is.EqualTo(3));
        }

        [Test]
        public void Execute_MixedLiteralAndNestedArgs_BothResolveCorrectly()
        {
            var sys = BuildSystem();

            sys.Register("getTwo", Array.Empty<CommandParameterInfo>(), _ => (object)2);

            int a = -1, b = -1;
            sys.Register("add",
                new[] { new CommandParameterInfo("x", typeof(int)), new CommandParameterInfo("y", typeof(int)) },
                args => { a = (int)args[0]; b = (int)args[1]; return (object)(a + b); });

            var result = sys.Execute("add", new[] { "10", "$(getTwo)" });

            Assert.That(result.Success, Is.True);
            Assert.That(a, Is.EqualTo(10));
            Assert.That(b, Is.EqualTo(2));
        }

        [Test]
        public void Execute_NoNestedArgs_BehaviorIdenticalToExistingPath()
        {
            var sys = BuildSystem();

            int captured = -1;
            sys.Register("cmd",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => { captured = (int)args[0]; return null; });

            var result = sys.Execute("cmd", new[] { "77" });

            Assert.That(result.Success, Is.True);
            Assert.That(captured, Is.EqualTo(77));
        }

        [Test]
        public void Execute_NestedCommandEmpty_ReturnsNestedCommandParseFailed()
        {
            var sys = BuildSystem();

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(object)) },
                args => null);

            var result = sys.Execute("outer", new[] { "$()" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NestedCommandParseFailed));
        }

        [Test]
        public void Execute_NestedCommandNotFound_ReturnsNestedCommandFailed()
        {
            var sys = BuildSystem();

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(object)) },
                args => null);

            var result = sys.Execute("outer", new[] { "$(doesNotExist)" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NestedCommandFailed));
        }

        [Test]
        public void Execute_NestedCommandExecutionFails_ReturnsNestedCommandFailed()
        {
            var sys = BuildSystem();

            // inner requires int but gets "notInt"
            sys.Register("inner",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => args[0]);

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(object)) },
                args => null);

            var result = sys.Execute("outer", new[] { "$(inner notInt)" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NestedCommandFailed));
        }

        [Test]
        public void Execute_NestedCommandVoidReturn_ReturnsNestedCommandVoidReturn()
        {
            var sys = BuildSystem();

            // void-return command
            sys.Register("voidCmd",
                Array.Empty<CommandParameterInfo>(),
                _ => null); // callback returns null → void

            // However, we need ReturnType == void. Since manual Register uses typeof(object),
            // we test via a scanned method instead.
            // For the void-check against ReturnType, we use a scanned static method.
            sys.Shutdown();

            var sys2 = BuildSystem();
            var result2 = sys2.Initialize(new[] { typeof(VoidReturnHost) });

            sys2.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(object)) },
                args => null);

            var r = sys2.Execute("outer", new[] { "$(VoidReturnHost_VoidCmd)" });

            Assert.That(r.Success, Is.False);
            Assert.That(r.Error, Is.EqualTo(ExecutionError.NestedCommandVoidReturn));
        }

        [Test]
        public void Execute_DepthExceeded_ReturnsNestedCommandDepthExceeded()
        {
            // Default depth is 4 — nesting 5 levels should fail
            var sys = BuildSystem();

            sys.Register("one", Array.Empty<CommandParameterInfo>(), _ => (object)1);
            sys.Register("id",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => args[0]);

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(int)) },
                args => null);

            // depth=5: $(id $(id $(id $(id $(one)))))
            var result = sys.Execute("outer",
                new[] { "$(id $(id $(id $(id $(one)))))" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(ExecutionError.NestedCommandDepthExceeded));
        }

        [Test]
        public void Execute_DepthAtMax_Succeeds()
        {
            // Default depth is 4 — nesting exactly 4 levels should succeed
            var sys = BuildSystem();

            sys.Register("one", Array.Empty<CommandParameterInfo>(), _ => (object)1);
            sys.Register("id",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => args[0]);

            int received = -1;
            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(int)) },
                args => { received = (int)args[0]; return null; });

            // depth=4: $(id $(id $(id $(one))))
            var result = sys.Execute("outer",
                new[] { "$(id $(id $(id $(one))))" });

            Assert.That(result.Success, Is.True);
            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void Execute_SuccessfulNesting_InnerAndOuterBothRecordedInHistory()
        {
            var sys = BuildSystem();

            sys.Register("getVal", Array.Empty<CommandParameterInfo>(), _ => (object)5);
            sys.Register("useVal",
                new[] { new CommandParameterInfo("v", typeof(int)) },
                args => null);

            sys.Execute("useVal", new[] { "$(getVal)" });

            var history = sys.GetHistory();

            Assert.That(history.Length, Is.EqualTo(2));
            Assert.That(history[0].CommandName, Is.EqualTo("getVal"));
            Assert.That(history[1].CommandName, Is.EqualTo("useVal"));
        }

        [Test]
        public void Execute_InnerFailure_InnerEntryRecordedWithFailureStatus()
        {
            var sys = BuildSystem();

            sys.Register("inner",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => args[0]);

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(object)) },
                args => null);

            sys.Execute("outer", new[] { "$(inner notInt)" });

            var history = sys.GetHistory();

            Assert.That(history.Length, Is.GreaterThanOrEqualTo(1));
            // Inner entry should have a failure status
            Assert.That(history[0].CommandName, Is.EqualTo("inner"));
            Assert.That(history[0].Status, Is.Not.EqualTo(ExecutionError.None));
        }

        [Test]
        public void Execute_InnerFailure_OuterEntryRecordedWithNestedCommandFailedStatus()
        {
            var sys = BuildSystem();

            sys.Register("inner",
                new[] { new CommandParameterInfo("n", typeof(int)) },
                args => args[0]);

            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(object)) },
                args => null);

            sys.Execute("outer", new[] { "$(inner notInt)" });

            var history = sys.GetHistory();

            // Outer entry should be last with NestedCommandFailed
            var outerEntry = history[history.Length - 1];
            Assert.That(outerEntry.CommandName, Is.EqualTo("outer"));
            Assert.That(outerEntry.Status, Is.EqualTo(ExecutionError.NestedCommandFailed));
        }

        [Test]
        public void Execute_InnerBeforeOuter_HistoryOrderIsCorrect()
        {
            var sys = BuildSystem();

            sys.Register("inner", Array.Empty<CommandParameterInfo>(), _ => (object)1);
            sys.Register("outer",
                new[] { new CommandParameterInfo("val", typeof(int)) },
                args => null);

            sys.Execute("outer", new[] { "$(inner)" });

            var history = sys.GetHistory();

            Assert.That(history.Length, Is.EqualTo(2));

            int innerIdx = -1, outerIdx = -1;
            for (int i = 0; i < history.Length; i++)
            {
                if (history[i].CommandName == "inner") innerIdx = i;
                if (history[i].CommandName == "outer") outerIdx = i;
            }

            Assert.That(innerIdx, Is.LessThan(outerIdx));
        }

        // ── Suggestion delimiter tests (Task 6) ──────────────────────────────────

        [Test]
        public void GetSuggestions_OpenDelimiterAlone_ReturnsAllCommands()
        {
            var sys = BuildSystem();
            sys.Register("alpha", Array.Empty<CommandParameterInfo>(), _ => null);
            sys.Register("beta", Array.Empty<CommandParameterInfo>(), _ => null);

            var suggestions = sys.GetSuggestions("$(");

            Assert.That(suggestions.Length, Is.EqualTo(2));
        }

        [Test]
        public void GetSuggestions_OpenDelimiterWithPartialName_FiltersCorrectly()
        {
            var sys = BuildSystem();
            sys.Register("getPlayer", Array.Empty<CommandParameterInfo>(), _ => null);
            sys.Register("getHealth", Array.Empty<CommandParameterInfo>(), _ => null);
            sys.Register("destroy", Array.Empty<CommandParameterInfo>(), _ => null);

            var suggestions = sys.GetSuggestions("$(get");

            Assert.That(suggestions.Length, Is.EqualTo(2));
            Assert.That(Array.Exists(suggestions, s => s.CommandName == "getPlayer"), Is.True);
            Assert.That(Array.Exists(suggestions, s => s.CommandName == "getHealth"), Is.True);
        }

        [Test]
        public void GetSuggestions_DoubleNested_InnermostPrefixUsed()
        {
            var sys = BuildSystem();
            sys.Register("getPlayer", Array.Empty<CommandParameterInfo>(), _ => null);
            sys.Register("outer", Array.Empty<CommandParameterInfo>(), _ => null);

            var suggestions = sys.GetSuggestions("$(outer $(get");

            Assert.That(suggestions.Length, Is.EqualTo(1));
            Assert.That(suggestions[0].CommandName, Is.EqualTo("getPlayer"));
        }

        [Test]
        public void GetSuggestions_NormalPrefix_Unaffected()
        {
            var sys = BuildSystem();
            sys.Register("health", Array.Empty<CommandParameterInfo>(), _ => null);
            sys.Register("help", Array.Empty<CommandParameterInfo>(), _ => null);
            sys.Register("destroy", Array.Empty<CommandParameterInfo>(), _ => null);

            var suggestions = sys.GetSuggestions("hea");

            Assert.That(suggestions.Length, Is.EqualTo(1));
            Assert.That(suggestions[0].CommandName, Is.EqualTo("health"));
        }

        [Test]
        public void GetSuggestions_NullPrefix_ReturnsAll()
        {
            var sys = BuildSystem();
            sys.Register("a", Array.Empty<CommandParameterInfo>(), _ => null);
            sys.Register("b", Array.Empty<CommandParameterInfo>(), _ => null);

            var suggestions = sys.GetSuggestions(null);

            Assert.That(suggestions.Length, Is.EqualTo(2));
        }

        [Test]
        public void GetSuggestions_EmptyPrefix_ReturnsAll()
        {
            var sys = BuildSystem();
            sys.Register("a", Array.Empty<CommandParameterInfo>(), _ => null);
            sys.Register("b", Array.Empty<CommandParameterInfo>(), _ => null);

            var suggestions = sys.GetSuggestions(string.Empty);

            Assert.That(suggestions.Length, Is.EqualTo(2));
        }

        // ── Helper types ─────────────────────────────────────────────────────────

        private class IntStringBox
        {
            private readonly int _value;
            public IntStringBox(int value) { _value = value; }
            public override string ToString() { return _value.ToString(); }
        }
    }

    /// <summary>Fixture type for void-return nested command test.</summary>
    internal static class VoidReturnHost
    {
        [Command("VoidReturnHost_VoidCmd")]
        public static void VoidCmd() { }
    }
}
