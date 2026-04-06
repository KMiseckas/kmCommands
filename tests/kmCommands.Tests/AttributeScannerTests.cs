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
    public class AttributeScannerTests
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
            if (_system.IsInitialized)
            {
                _system.Shutdown();
            }
        }

        // ---------------------------------------------------------------------------
        // Inner test command containers
        // ---------------------------------------------------------------------------

        private static class SingleCommandTarget
        {
            public static int LastAmount;

            [Command("scan_heal")]
            public static void Heal(int amount)
            {
                LastAmount = amount;
            }
        }

        private static class MultiCommandTarget
        {
            public static int LastGold;
            public static string LastMessage;

            [Command("scan_addgold")]
            public static void AddGold(int amount)
            {
                LastGold = amount;
            }

            [Command("scan_broadcast")]
            public static void Broadcast(string message)
            {
                LastMessage = message;
            }
        }

        private static class UnsupportedParamTarget
        {
            [Command("scan_bad")]
            public static void BadMethod(object unsupported)
            {
            }
        }

        private static class NoParamTarget
        {
            public static bool WasCalled;

            [Command("scan_ping")]
            public static void Ping()
            {
                WasCalled = true;
            }
        }

        private static class DevOnlyTarget
        {
            public static bool WasCalled;

            [Command("scan_debuginfo", IsDevOnly = true)]
            public static void DebugInfo()
            {
                WasCalled = true;
            }
        }

        private static class DuplicateNameTarget
        {
            [Command("scan_heal")]
            public static void HealToo(int amount)
            {
            }
        }

        // Named with prefix to be unique during assembly-wide scan
        private static class AssemblyTypeA
        {
            public static bool WasCalled;

            [Command("scan_asm_a")]
            public static void CommandA()
            {
                WasCalled = true;
            }
        }

        private static class AssemblyTypeB
        {
            public static bool WasCalled;

            [Command("scan_asm_b")]
            public static void CommandB()
            {
                WasCalled = true;
            }
        }

        // Non-static method with [Command] â€” requires BindingFlags.Instance so it is discovered
        private class NonStaticTarget
        {
            [Command("scan_instance")]
            public void InstanceMethod()
            {
            }
        }

        // ---------------------------------------------------------------------------
        // Tests
        // ---------------------------------------------------------------------------

        [Test]
        public void SingleAttributedStaticMethod_RegistersAndExecutes()
        {
            SingleCommandTarget.LastAmount = 0;

            ScanResult result = _system.Scan(typeof(SingleCommandTarget));

            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.Entries.Length, Is.EqualTo(1));
            Assert.That(result.Entries[0].CommandName, Is.EqualTo("scan_heal"));
            Assert.That(result.Entries[0].Result.Success, Is.True);

            ExecutionResult execResult = _system.Execute("scan_heal", new[] { "10" });
            Assert.That(execResult.Success, Is.True);
            Assert.That(SingleCommandTarget.LastAmount, Is.EqualTo(10));
        }

        [Test]
        public void MultipleAttributedMethods_AllRegistered()
        {
            MultiCommandTarget.LastGold = 0;
            MultiCommandTarget.LastMessage = null;

            ScanResult result = _system.Scan(typeof(MultiCommandTarget));

            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.Entries.Length, Is.EqualTo(2));

            ExecutionResult addGold = _system.Execute("scan_addgold", new[] { "500" });
            Assert.That(addGold.Success, Is.True);
            Assert.That(MultiCommandTarget.LastGold, Is.EqualTo(500));

            ExecutionResult broadcast = _system.Execute("scan_broadcast", new[] { "hello" });
            Assert.That(broadcast.Success, Is.True);
            Assert.That(MultiCommandTarget.LastMessage, Is.EqualTo("hello"));
        }

        [Test]
        public void UnsupportedParameterType_SkippedWithFailure()
        {
            ScanResult result = _system.Scan(typeof(UnsupportedParamTarget));

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Entries.Length, Is.EqualTo(1));
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.UnsupportedParameterType));

            ExecutionResult execResult = _system.Execute("scan_bad", Array.Empty<string>());
            Assert.That(execResult.Success, Is.False);
            Assert.That(execResult.Error, Is.EqualTo(ExecutionError.CommandNotFound));
        }

        [Test]
        public void NoParameterMethod_RegistersAndExecutes()
        {
            NoParamTarget.WasCalled = false;

            ScanResult result = _system.Scan(typeof(NoParamTarget));

            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.Entries.Length, Is.EqualTo(1));
            Assert.That(result.Entries[0].Result.Success, Is.True);

            ExecutionResult execResult = _system.Execute("scan_ping", Array.Empty<string>());
            Assert.That(execResult.Success, Is.True);
            Assert.That(NoParamTarget.WasCalled, Is.True);
        }

        [Test]
        public void IsDevOnlyTrue_DevModeFalse_CommandExcluded()
        {
            ScanOptions opts = new ScanOptions { DevMode = false };

            ScanResult result = _system.Scan(typeof(DevOnlyTarget), opts);

            // IsDevOnly commands must be silently skipped â€” no entry added at all
            Assert.That(result.Entries.Length, Is.EqualTo(0));
            Assert.That(result.HasErrors, Is.False);

            ExecutionResult execResult = _system.Execute("scan_debuginfo", Array.Empty<string>());
            Assert.That(execResult.Success, Is.False);
            Assert.That(execResult.Error, Is.EqualTo(ExecutionError.CommandNotFound));
        }

        [Test]
        public void IsDevOnlyTrue_DevModeTrue_CommandIncluded()
        {
            DevOnlyTarget.WasCalled = false;
            ScanOptions opts = new ScanOptions { DevMode = true };

            ScanResult result = _system.Scan(typeof(DevOnlyTarget), opts);

            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.Entries.Length, Is.EqualTo(1));
            Assert.That(result.Entries[0].Result.Success, Is.True);

            ExecutionResult execResult = _system.Execute("scan_debuginfo", Array.Empty<string>());
            Assert.That(execResult.Success, Is.True);
            Assert.That(DevOnlyTarget.WasCalled, Is.True);
        }

        [Test]
        public void DuplicateNameCollision_ReportedAsFailure()
        {
            // Register "scan_heal" manually first so it exists before the scan
            RegistrationResult manualReg = _system.Register(
                "scan_heal",
                new[] { new CommandParameterInfo("amount", typeof(int)) },
                _ => null);
            Assert.That(manualReg.Success, Is.True);

            ScanResult result = _system.Scan(typeof(SingleCommandTarget));

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Entries.Length, Is.EqualTo(1));
            Assert.That(result.Entries[0].CommandName, Is.EqualTo("scan_heal"));
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.DuplicateCommandName));
        }

        [Test]
        public void NonStaticMethod_ReportedAsInvalidMethod()
        {
            ScanResult result = _system.Scan(typeof(NonStaticTarget));

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Entries.Length, Is.EqualTo(1));
            Assert.That(result.Entries[0].CommandName, Is.EqualTo("scan_instance"));
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.InvalidMethod));
        }

        [Test]
        public void AssemblyWideScan_DiscoversAcrossTypes()
        {
            AssemblyTypeA.WasCalled = false;
            AssemblyTypeB.WasCalled = false;

            ScanResult result = _system.Scan(Assembly.GetExecutingAssembly());

            // Verify both assembly-type commands are present as successes
            bool foundA = false;
            bool foundB = false;
            for (int i = 0; i < result.Entries.Length; i++)
            {
                if (result.Entries[i].CommandName == "scan_asm_a" && result.Entries[i].Result.Success)
                    foundA = true;
                if (result.Entries[i].CommandName == "scan_asm_b" && result.Entries[i].Result.Success)
                    foundB = true;
            }

            Assert.That(foundA, Is.True, "scan_asm_a command not found in assembly scan results");
            Assert.That(foundB, Is.True, "scan_asm_b command not found in assembly scan results");

            ExecutionResult execA = _system.Execute("scan_asm_a", Array.Empty<string>());
            Assert.That(execA.Success, Is.True);
            Assert.That(AssemblyTypeA.WasCalled, Is.True);

            ExecutionResult execB = _system.Execute("scan_asm_b", Array.Empty<string>());
            Assert.That(execB.Success, Is.True);
            Assert.That(AssemblyTypeB.WasCalled, Is.True);
        }

        [Test]
        public void ScanBeforeInitialize_ReturnsSystemFailure()
        {
            _system.Shutdown();

            ScanResult result = _system.Scan(typeof(SingleCommandTarget));

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Entries.Length, Is.EqualTo(1));
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.NotInitialized));
        }

        [Test]
        public void ScanNullType_ReturnsSystemFailure()
        {
            ScanResult result = _system.Scan(null as Type);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Entries.Length, Is.EqualTo(1));
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.NullParameters));
        }
    }
}
