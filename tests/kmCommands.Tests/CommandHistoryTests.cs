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
    }
}
