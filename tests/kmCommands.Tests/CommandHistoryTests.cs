using System;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    public class CommandHistoryTests
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

        private void RegisterNoArgs(string name)
        {
            Register(name, Array.Empty<CommandParameterInfo>(), _ => null);
        }

        // â”€â”€ pre-init / lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void HistoryCount_BeforeInitialize_ReturnsZero()
        {
            CommandSystem uninit = new CommandSystem();
            Assert.That(uninit.HistoryCount, Is.EqualTo(0));
        }

        [Test]
        public void GetHistory_BeforeInitialize_ReturnsEmptyArray()
        {
            CommandSystem uninit = new CommandSystem();
            CommandHistoryEntry[] result = uninit.GetHistory();
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void ClearHistory_BeforeInitialize_DoesNotThrow()
        {
            CommandSystem uninit = new CommandSystem();
            Assert.That(() => uninit.ClearHistory(), Throws.Nothing);
        }

        [Test]
        public void HistoryCount_AfterInitialize_IsZero()
        {
            Assert.That(_system.HistoryCount, Is.EqualTo(0));
        }

        [Test]
        public void HistoryCount_AfterShutdownAndReinitialize_IsZero()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", Array.Empty<string>());
            Assert.That(_system.HistoryCount, Is.EqualTo(1));

            _system.Shutdown();
            _system.Initialize();

            Assert.That(_system.HistoryCount, Is.EqualTo(0));
        }

        // â”€â”€ recording behavior â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_SuccessfulCommand_IncrementsHistoryCount()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", Array.Empty<string>());
            Assert.That(_system.HistoryCount, Is.EqualTo(1));
        }

        [Test]
        public void Execute_SuccessfulCommand_RecordsCorrectName()
        {
            RegisterNoArgs("mycmd");
            _system.Execute("mycmd", Array.Empty<string>());
            Assert.That(_system.GetHistory()[0].CommandName, Is.EqualTo("mycmd"));
        }

        [Test]
        public void Execute_SuccessfulCommand_RecordsCorrectArgs()
        {
            Register("greet", new[] { new CommandParameterInfo("msg", typeof(string)) }, _ => null);
            _system.Execute("greet", new[] { "hello" });
            Assert.That(_system.GetHistory()[0].Args[0], Is.EqualTo("hello"));
        }

        [Test]
        public void Execute_FailedCommand_RecordsFailureEntryInHistory()
        {
            _system.Execute("doesnotexist", Array.Empty<string>());
            Assert.That(_system.HistoryCount, Is.EqualTo(1));
            Assert.That(_system.GetHistory()[0].Status, Is.EqualTo(ExecutionError.CommandNotFound));
        }

        [Test]
        public void Execute_ArgumentConversionFailed_RecordsFailureEntryInHistory()
        {
            Register("add", new[] { new CommandParameterInfo("n", typeof(int)) }, _ => null);
            _system.Execute("add", new[] { "notanumber" });
            Assert.That(_system.HistoryCount, Is.EqualTo(1));
            Assert.That(_system.GetHistory()[0].Status, Is.EqualTo(ExecutionError.ArgumentConversionFailed));
        }

        // â”€â”€ argument snapshot independence â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Execute_MutatingArgsAfterExecute_DoesNotAffectStoredEntry()
        {
            Register("greet", new[] { new CommandParameterInfo("msg", typeof(string)) }, _ => null);
            string[] args = new[] { "original" };
            _system.Execute("greet", args);
            args[0] = "mutated";
            Assert.That(_system.GetHistory()[0].Args[0], Is.EqualTo("original"));
        }

        // â”€â”€ entry ordering â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void GetHistory_MultipleEntries_ReturnsOldestToNewest()
        {
            RegisterNoArgs("alpha");
            RegisterNoArgs("beta");
            RegisterNoArgs("gamma");
            _system.Execute("alpha", Array.Empty<string>());
            _system.Execute("beta", Array.Empty<string>());
            _system.Execute("gamma", Array.Empty<string>());

            CommandHistoryEntry[] history = _system.GetHistory();
            Assert.That(history[0].CommandName, Is.EqualTo("alpha"));
            Assert.That(history[1].CommandName, Is.EqualTo("beta"));
            Assert.That(history[2].CommandName, Is.EqualTo("gamma"));
        }

        // â”€â”€ capacity and eviction â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void Initialize_CustomCapacity_LimitsBufferSize()
        {
            _system.Shutdown();
            _system.Initialize(3);

            RegisterNoArgs("cmd1");
            RegisterNoArgs("cmd2");
            RegisterNoArgs("cmd3");

            _system.Execute("cmd1", Array.Empty<string>());
            _system.Execute("cmd2", Array.Empty<string>());
            _system.Execute("cmd3", Array.Empty<string>());

            Assert.That(_system.HistoryCount, Is.EqualTo(3));
        }

        [Test]
        public void Execute_BeyondCapacity_EvictsOldestEntry()
        {
            _system.Shutdown();
            _system.Initialize(3);

            RegisterNoArgs("cmd1");
            RegisterNoArgs("cmd2");
            RegisterNoArgs("cmd3");
            RegisterNoArgs("cmd4");

            _system.Execute("cmd1", Array.Empty<string>());
            _system.Execute("cmd2", Array.Empty<string>());
            _system.Execute("cmd3", Array.Empty<string>());
            _system.Execute("cmd4", Array.Empty<string>());

            CommandHistoryEntry[] history = _system.GetHistory();
            Assert.That(history[0].CommandName, Is.EqualTo("cmd2"));
        }

        [Test]
        public void Execute_BeyondCapacity_CountStaysAtCapacity()
        {
            _system.Shutdown();
            _system.Initialize(3);

            RegisterNoArgs("cmd1");
            RegisterNoArgs("cmd2");
            RegisterNoArgs("cmd3");
            RegisterNoArgs("cmd4");

            _system.Execute("cmd1", Array.Empty<string>());
            _system.Execute("cmd2", Array.Empty<string>());
            _system.Execute("cmd3", Array.Empty<string>());
            _system.Execute("cmd4", Array.Empty<string>());

            Assert.That(_system.HistoryCount, Is.EqualTo(3));
        }

        [Test]
        public void Initialize_CapacityLessThanOne_ClampsToOne()
        {
            _system.Shutdown();
            _system.Initialize(0);

            RegisterNoArgs("first");
            RegisterNoArgs("second");

            _system.Execute("first", Array.Empty<string>());
            Assert.That(_system.HistoryCount, Is.EqualTo(1));

            _system.Execute("second", Array.Empty<string>());
            Assert.That(_system.HistoryCount, Is.EqualTo(1));
            Assert.That(_system.GetHistory()[0].CommandName, Is.EqualTo("second"));
        }

        [Test]
        public void DefaultHistoryCapacity_IsPositiveInteger()
        {
            Assert.That(CommandSystem.DefaultHistoryCapacity, Is.GreaterThan(0));
        }

        [Test]
        public void Initialize_DefaultCapacity_IsUsedWhenNoArgOverload()
        {
            int cap = CommandSystem.DefaultHistoryCapacity;

            for (int i = 0; i < cap; i++)
            {
                string name = string.Format("cmd{0}", i);
                RegisterNoArgs(name);
                _system.Execute(name, Array.Empty<string>());
            }

            Assert.That(_system.HistoryCount, Is.EqualTo(cap));

            RegisterNoArgs("overflow");
            _system.Execute("overflow", Array.Empty<string>());
            Assert.That(_system.HistoryCount, Is.EqualTo(cap));
        }

        // â”€â”€ clear â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void ClearHistory_ResetsCountToZero()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", Array.Empty<string>());
            _system.ClearHistory();
            Assert.That(_system.HistoryCount, Is.EqualTo(0));
        }

        [Test]
        public void ClearHistory_GetHistoryReturnsEmpty()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", Array.Empty<string>());
            _system.ClearHistory();
            Assert.That(_system.GetHistory().Length, Is.EqualTo(0));
        }

        [Test]
        public void ClearHistory_AfterClear_NewEntryIsRecorded()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", Array.Empty<string>());
            _system.ClearHistory();
            _system.Execute("cmd", Array.Empty<string>());
            Assert.That(_system.HistoryCount, Is.EqualTo(1));
        }

        // â”€â”€ snapshot independence from live buffer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Test]
        public void GetHistory_Snapshot_IsNotAffectedBySubsequentExecute()
        {
            RegisterNoArgs("cmd1");
            RegisterNoArgs("cmd2");

            _system.Execute("cmd1", Array.Empty<string>());
            CommandHistoryEntry[] snapshot = _system.GetHistory();
            int snapshotLength = snapshot.Length;

            _system.Execute("cmd2", Array.Empty<string>());
            Assert.That(snapshot.Length, Is.EqualTo(snapshotLength));
        }

        // ── Timestamp ─────────────────────────────────────────────────────────────

        [Test]
        public void Execute_SuccessfulCommand_Timestamp_KindIsUtc()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", Array.Empty<string>());
            Assert.That(_system.GetHistory()[0].Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void Execute_SuccessfulCommand_Timestamp_IsWithinOneSecondOfUtcNow()
        {
            RegisterNoArgs("cmd");
            DateTime before = DateTime.UtcNow;
            _system.Execute("cmd", Array.Empty<string>());
            DateTime after = DateTime.UtcNow;

            DateTime ts = _system.GetHistory()[0].Timestamp;
            Assert.That(ts, Is.GreaterThanOrEqualTo(before));
            Assert.That(ts, Is.LessThanOrEqualTo(after.AddSeconds(1)));
        }

        [Test]
        public void Execute_FailedCommand_Timestamp_KindIsUtc()
        {
            _system.Execute("doesnotexist", Array.Empty<string>());
            Assert.That(_system.GetHistory()[0].Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        // ── Status ────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_SuccessfulCommand_Status_IsNone()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", Array.Empty<string>());
            Assert.That(_system.GetHistory()[0].Status, Is.EqualTo(ExecutionError.None));
        }

        [Test]
        public void Execute_CommandNotFound_Status_IsCommandNotFound()
        {
            _system.Execute("doesnotexist", Array.Empty<string>());
            Assert.That(_system.GetHistory()[0].Status, Is.EqualTo(ExecutionError.CommandNotFound));
        }

        [Test]
        public void Execute_ArgumentConversionFailed_Status_IsArgumentConversionFailed()
        {
            Register("add", new[] { new CommandParameterInfo("n", typeof(int)) }, _ => null);
            _system.Execute("add", new[] { "notanumber" });
            Assert.That(_system.GetHistory()[0].Status, Is.EqualTo(ExecutionError.ArgumentConversionFailed));
        }

        [Test]
        public void Execute_ArgumentCountMismatch_Status_IsArgumentCountMismatch()
        {
            Register("add", new[] { new CommandParameterInfo("n", typeof(int)) }, _ => null);
            _system.Execute("add", Array.Empty<string>());
            Assert.That(_system.GetHistory()[0].Status, Is.EqualTo(ExecutionError.ArgumentCountMismatch));
        }

        // ── ErrorDetail ───────────────────────────────────────────────────────────

        [Test]
        public void Execute_SuccessfulCommand_ErrorDetail_IsNull()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", Array.Empty<string>());
            Assert.That(_system.GetHistory()[0].ErrorDetail, Is.Null);
        }

        [Test]
        public void Execute_FailedCommand_ErrorDetail_MatchesExecutionResultErrorMessage()
        {
            ExecutionResult result = _system.Execute("doesnotexist", Array.Empty<string>());
            Assert.That(_system.GetHistory()[0].ErrorDetail, Is.EqualTo(result.ErrorMessage));
        }

        // ── RawInput ──────────────────────────────────────────────────────────────

        [Test]
        public void Execute_ZeroArgs_RawInput_LengthIsOneAndContainsCommandName()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", Array.Empty<string>());
            CommandHistoryEntry entry = _system.GetHistory()[0];
            Assert.That(entry.RawInput.Length, Is.EqualTo(1));
            Assert.That(entry.RawInput[0], Is.EqualTo("cmd"));
        }

        [Test]
        public void Execute_MultipleArgs_RawInput_ContainsCommandNameAtIndexZeroAndAllArgs()
        {
            Register("greet", new[] { new CommandParameterInfo("a", typeof(string)), new CommandParameterInfo("b", typeof(string)) }, _ => null);
            _system.Execute("greet", new[] { "a", "b" });
            CommandHistoryEntry entry = _system.GetHistory()[0];
            Assert.That(entry.RawInput.Length, Is.EqualTo(3));
            Assert.That(entry.RawInput[0], Is.EqualTo("greet"));
            Assert.That(entry.RawInput[1], Is.EqualTo("a"));
            Assert.That(entry.RawInput[2], Is.EqualTo("b"));
        }

        [Test]
        public void Execute_NullArgs_RawInput_LengthIsOne()
        {
            RegisterNoArgs("cmd");
            _system.Execute("cmd", null);
            CommandHistoryEntry entry = _system.GetHistory()[0];
            Assert.That(entry.RawInput.Length, Is.EqualTo(1));
            Assert.That(entry.RawInput[0], Is.EqualTo("cmd"));
        }

        [Test]
        public void Execute_MutatingArgsAfterExecute_DoesNotAffectRawInput()
        {
            Register("greet", new[] { new CommandParameterInfo("msg", typeof(string)) }, _ => null);
            string[] args = new[] { "original" };
            _system.Execute("greet", args);
            string rawBefore = _system.GetHistory()[0].RawInput[1];
            args[0] = "mutated";
            Assert.That(_system.GetHistory()[0].RawInput[1], Is.EqualTo(rawBefore));
        }

        // ── NotInitialized guard ──────────────────────────────────────────────────

        [Test]
        public void Execute_BeforeInitialize_DoesNotRecord()
        {
            CommandSystem uninit = new CommandSystem();
            uninit.Execute("cmd", Array.Empty<string>());
            Assert.That(uninit.HistoryCount, Is.EqualTo(0));
        }

        // ── ReturnValue on failure ────────────────────────────────────────────────

        [Test]
        public void Execute_FailedCommand_ReturnValue_IsNull()
        {
            _system.Execute("doesnotexist", Array.Empty<string>());
            Assert.That(_system.GetHistory()[0].ReturnValue, Is.Null);
        }
    }
}
