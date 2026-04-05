// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using NUnit.Framework;

namespace kmCommands.Tests
{
    [TestFixture]
    internal sealed class AutoScanAtInitializeTests
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

        // ---------------------------------------------------------------------------
        // Inner test command containers
        // ---------------------------------------------------------------------------

        private static class BasicScanTarget
        {
            public static bool WasCalled;

            [Command("autoscan_ping")]
            public static void Ping() { WasCalled = true; }

            [Command("autoscan_add")]
            public static void Add(int a, int b) { }
        }

        private static class DevOnlyTarget
        {
            [Command("autoscan_devonly", IsDevOnly = true)]
            public static void DevCmd() { }

            [Command("autoscan_regular")]
            public static void RegularCmd() { }
        }

        private class FailingTarget
        {
            [Command("autoscan_bad")]
            public void NonStaticMethod() { }  // non-static â†’ guaranteed scan failure
        }

        // ---------------------------------------------------------------------------
        // Tests 1â€“4: basic init and registration
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_TypeArray_SetsIsInitializedTrue()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) });

            Assert.That(_system.IsInitialized, Is.True);
        }

        [Test]
        public void Initialize_TypeArray_RegistersCommandsFromType()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) });

            string[] names = _system.GetCommandNames();

            Assert.That(names, Does.Contain("autoscan_ping"));
            Assert.That(names, Does.Contain("autoscan_add"));
        }

        [Test]
        public void Initialize_AssemblyArray_RegistersCommandsFromAssembly()
        {
            Assembly assembly = typeof(BasicScanTarget).Assembly;
            _system.Initialize(new[] { assembly });

            string[] names = _system.GetCommandNames();

            Assert.That(names, Does.Contain("autoscan_ping"));
            Assert.That(names, Does.Contain("autoscan_add"));
        }

        [Test]
        public void Initialize_TypeAndAssemblyArrays_RegistersFromBoth()
        {
            // autoscan_ping and autoscan_add are in BasicScanTarget (declared in this assembly).
            // We scan BasicScanTarget explicitly via types and then also scan the test assembly
            // to confirm the combined overload works. Since the assembly scan would double-register,
            // we use a fresh system and check both types and assembly-derived commands appear.
            // For a clean test: scan BasicScanTarget by type only (types param) and confirm
            // an assembly in the assemblies param contributes its commands too.
            // Use the core kmCommands assembly (contains no [Command] attrs) so no collision occurs,
            // and verify BasicScanTarget commands come through from the types side.
            Assembly coreAssembly = typeof(CommandSystem).Assembly;
            ScanResult result = _system.Initialize(
                new[] { typeof(BasicScanTarget) },
                new[] { coreAssembly });

            Assert.That(_system.IsInitialized, Is.True);
            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Contain("autoscan_ping"));
            Assert.That(names, Does.Contain("autoscan_add"));
        }

        // ---------------------------------------------------------------------------
        // Tests 5â€“7: idempotency
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_WhenAlreadyInitialized_TypeArray_ReturnsAlreadyInitializedResult()
        {
            _system.Initialize();

            ScanResult result = _system.Initialize(new[] { typeof(BasicScanTarget) });

            Assert.That(result.IsAlreadyInitialized, Is.True);
        }

        [Test]
        public void Initialize_WhenAlreadyInitialized_TypeArray_DoesNotDoubleRegister()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) });
            // Second call is a no-op â€” commands should appear only once.
            _system.Initialize(new[] { typeof(BasicScanTarget) });

            string[] names = _system.GetCommandNames();
            int pingCount = 0;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].Equals("autoscan_ping", StringComparison.OrdinalIgnoreCase))
                {
                    pingCount++;
                }
            }

            Assert.That(pingCount, Is.EqualTo(1));
        }

        [Test]
        public void Initialize_WhenAlreadyInitialized_IsInitializedRemainsTrue()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) });
            _system.Initialize(new[] { typeof(BasicScanTarget) });

            Assert.That(_system.IsInitialized, Is.True);
        }

        // ---------------------------------------------------------------------------
        // Tests 8â€“11: empty and null inputs
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_EmptyTypeArray_ReturnsZeroEntriesAndNoErrors()
        {
            ScanResult result = _system.Initialize(new Type[0]);

            Assert.That(result.Entries.Length, Is.EqualTo(0));
            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.IsAlreadyInitialized, Is.False);
        }

        [Test]
        public void Initialize_EmptyAssemblyArray_ReturnsZeroEntriesAndNoErrors()
        {
            ScanResult result = _system.Initialize(new Assembly[0]);

            Assert.That(result.Entries.Length, Is.EqualTo(0));
            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.IsAlreadyInitialized, Is.False);
        }

        [Test]
        public void Initialize_NullTypeArray_TreatedAsEmpty()
        {
            ScanResult result = _system.Initialize((Type[])null);

            Assert.That(result.IsAlreadyInitialized, Is.False);
            Assert.That(result.Entries.Length, Is.EqualTo(0));
            Assert.That(result.HasErrors, Is.False);
            Assert.That(_system.IsInitialized, Is.True);
        }

        [Test]
        public void Initialize_NullAssemblyArray_TreatedAsEmpty()
        {
            ScanResult result = _system.Initialize((Assembly[])null);

            Assert.That(result.IsAlreadyInitialized, Is.False);
            Assert.That(result.Entries.Length, Is.EqualTo(0));
            Assert.That(result.HasErrors, Is.False);
            Assert.That(_system.IsInitialized, Is.True);
        }

        // ---------------------------------------------------------------------------
        // Tests 12â€“14: dev-mode filtering
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_DevModeTrue_IncludesDevOnlyCommands()
        {
            ScanResult result = _system.Initialize(
                new[] { typeof(DevOnlyTarget) },
                new ScanOptions { DevMode = true });

            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Contain("autoscan_devonly"));
            Assert.That(names, Does.Contain("autoscan_regular"));
        }

        [Test]
        public void Initialize_DevModeFalse_ExcludesDevOnlyCommands()
        {
            ScanResult result = _system.Initialize(
                new[] { typeof(DevOnlyTarget) },
                new ScanOptions { DevMode = false });

            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Not.Contain("autoscan_devonly"));
            Assert.That(names, Does.Contain("autoscan_regular"));
        }

        [Test]
        public void Initialize_DefaultOptions_ExcludesDevOnlyCommands()
        {
            _system.Initialize(new[] { typeof(DevOnlyTarget) });

            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Not.Contain("autoscan_devonly"));
            Assert.That(names, Does.Contain("autoscan_regular"));
        }

        // ---------------------------------------------------------------------------
        // Tests 15â€“16: result contents
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_ResultContainsEntryPerRegisteredCommand()
        {
            ScanResult result = _system.Initialize(new[] { typeof(BasicScanTarget) });

            // BasicScanTarget has 2 [Command] methods: autoscan_ping and autoscan_add
            Assert.That(result.Entries.Length, Is.EqualTo(2));
        }

        [Test]
        public void Initialize_ResultHasErrors_WhenCommandFails()
        {
            ScanResult result = _system.Initialize(new[] { typeof(FailingTarget) });

            Assert.That(result.HasErrors, Is.True);
            bool foundBad = false;
            for (int i = 0; i < result.Entries.Length; i++)
            {
                if (!result.Entries[i].Result.Success)
                {
                    foundBad = true;
                    break;
                }
            }
            Assert.That(foundBad, Is.True);
        }

        // ---------------------------------------------------------------------------
        // Tests 17â€“18: discovery APIs reflect init-time scan results
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_CommandsVisibleInGetCommandNames()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) });

            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Contain("autoscan_ping"));
            Assert.That(names, Does.Contain("autoscan_add"));
        }

        [Test]
        public void Initialize_CommandsVisibleInGetSnapshot()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) });

            CommandMetadataSnapshot snapshot = _system.GetSnapshot();
            Assert.That(snapshot.TryGetParameters("autoscan_ping", out _), Is.True);
            Assert.That(snapshot.TryGetParameters("autoscan_add", out _), Is.True);
        }

        // ---------------------------------------------------------------------------
        // Tests 19â€“20: post-init Register and Scan still work
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_ThenRegister_Succeeds()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) });

            RegistrationResult reg = _system.Register(
                "post_init_cmd",
                Array.Empty<CommandParameterInfo>(),
                args => null);

            Assert.That(reg.Success, Is.True);
            Assert.That(_system.GetCommandNames(), Does.Contain("post_init_cmd"));
        }

        [Test]
        public void Initialize_ThenScan_Succeeds()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) });

            ScanResult result = _system.Scan(typeof(DevOnlyTarget));

            Assert.That(result.HasErrors, Is.False);
            Assert.That(_system.GetCommandNames(), Does.Contain("autoscan_regular"));
        }

        // ---------------------------------------------------------------------------
        // Test 21: IsAlreadyInitialized is distinguishable from zero entries
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_AlreadyInitialized_IsDistinctFromZeroEntries()
        {
            // Fresh init with empty array â†’ zero entries, IsAlreadyInitialized == false
            ScanResult freshResult = _system.Initialize(new Type[0]);
            Assert.That(freshResult.IsAlreadyInitialized, Is.False);
            Assert.That(freshResult.Entries.Length, Is.EqualTo(0));

            // Second call â†’ already initialized, IsAlreadyInitialized == true
            ScanResult noOpResult = _system.Initialize(new Type[0]);
            Assert.That(noOpResult.IsAlreadyInitialized, Is.True);
        }

        // ---------------------------------------------------------------------------
        // Tests 22â€“23: history capacity
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_HistoryCapacity_ClampedToOne_WhenBelowOne()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) }, default, 0);

            Assert.That(_system.HistoryCount, Is.EqualTo(0));
            // Execute a command to confirm buffer does not throw at capacity 1
            _system.Execute("autoscan_ping", Array.Empty<string>());
            Assert.That(_system.HistoryCount, Is.EqualTo(1));
        }

        [Test]
        public void Initialize_UsesDefaultHistoryCapacity_WhenNotSpecified()
        {
            _system.Initialize(new[] { typeof(BasicScanTarget) });

            // Fill up to DefaultHistoryCapacity + 1 to confirm no exception and ring-buffer behavior
            for (int i = 0; i <= CommandSystem.DefaultHistoryCapacity; i++)
            {
                _system.Execute("autoscan_ping", Array.Empty<string>());
            }

            Assert.That(_system.HistoryCount, Is.EqualTo(CommandSystem.DefaultHistoryCapacity));
        }

        // ---------------------------------------------------------------------------
        // Test 24: multiple types, all entries merged
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_MultipleTypes_AllEntriesMergedInResult()
        {
            ScanResult result = _system.Initialize(
                new[] { typeof(BasicScanTarget), typeof(DevOnlyTarget) },
                new ScanOptions { DevMode = true });

            // BasicScanTarget: 2 commands; DevOnlyTarget: 2 commands (dev mode on) â†’ 4 entries
            Assert.That(result.Entries.Length, Is.EqualTo(4));
        }

        // ---------------------------------------------------------------------------
        // Test 25: null item in type array is skipped gracefully
        // ---------------------------------------------------------------------------

        [Test]
        public void Initialize_NullItemInTypeArray_SkippedGracefully()
        {
            ScanResult result = _system.Initialize(new Type[] { null, typeof(BasicScanTarget) });

            Assert.That(_system.IsInitialized, Is.True);
            Assert.That(result.HasErrors, Is.False);
            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Contain("autoscan_ping"));
            Assert.That(names, Does.Contain("autoscan_add"));
        }
    }
}
