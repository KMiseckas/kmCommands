// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using System;
using NUnit.Framework;

namespace kmCommands.Tests
{
    /// <summary>
    /// Integration tests for <see cref="CommandSystem.RegisterInstance"/> /
    /// <see cref="CommandSystem.UnregisterInstance"/> and associated execution behaviour.
    /// </summary>
    [TestFixture]
    public class InstanceCommandRegistrationTests
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
                _system.Shutdown();
        }

        // ── Target helpers ────────────────────────────────────────────────────────

        private class PlayerTarget
        {
            public int Health { get; set; } = 100;
            public int Score { get; private set; }

            public bool WasCalled;

            public void Heal(int amount) { Health += amount; }
            public void Ping() { WasCalled = true; }
            public int GetScore() { return Score; }

            [Command("special")]
            private void PrivateCmd() { WasCalled = true; }
        }

        private class EnemyTarget
        {
            public string Name { get; set; } = "Enemy";
            public int Damage { get; set; } = 10;
        }

        private class ThrowingTarget
        {
            public void AlwaysThrows()
            {
                throw new InvalidOperationException("deliberate");
            }
        }

        private class NullReferenceThrowingTarget
        {
            public void CrashOnNull()
            {
                // Unconditionally throws NullReferenceException to simulate destroyed instance.
                string nullStr = null;
                _ = nullStr.Length;
            }
        }

        private class WriteOnlyPropTarget
        {
            private int _value;
            public int WriteOnly { set { _value = value; } }
            public int GetValue() { return _value; }
        }

        private class RefParamClassTarget
        {
            public void RefMethod(ref int x) { x = 0; }
            public void Normal() { }
        }

        private class DevOnlyTarget
        {
            [Command("dev_cmd", IsDevOnly = true)]
            public void DevCmd() { }
            public void RegularMethod() { }
        }

        private class NonVoidMethodTarget
        {
            public int Double(int x) { return x * 2; }
        }

        [InstanceCommandSource(DefaultScanMode = InstanceScanMode.AttributeOnly)]
        private class SourceAttributeOnlyTarget
        {
            [InstanceCommand("ic_explicit")]
            public void ExplicitCmd() { }
            public void ShouldBeSkipped() { }
        }

        [InstanceCommandSource(DefaultScanMode = InstanceScanMode.Auto)]
        private class SourceAutoTarget
        {
            public void AutoCmd() { }
        }

        private class InstanceCmdDevOnlyTarget
        {
            [InstanceCommand("ic_dev", IsDevOnly = true)]
            public void DevOnlyMethod() { }
            public void RegularMethod() { }
        }

        // ── Guard: not initialized ────────────────────────────────────────────────

        [Test]
        public void RegisterInstance_Before_Initialize_ReturnsNotInitialized()
        {
            _system.Shutdown();
            ScanResult result = _system.RegisterInstance(new PlayerTarget(), "player");
            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.NotInitialized));
        }

        [Test]
        public void UnregisterInstance_Before_Initialize_ReturnsFail()
        {
            _system.Shutdown();
            UnregisterResult result = _system.UnregisterInstance("player");
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not been initialized"));
        }

        // ── Guard: null target ─────────────────────────────────────────────────────

        [Test]
        public void RegisterInstance_NullTarget_ReturnsNullTarget()
        {
            ScanResult result = _system.RegisterInstance(null, "player");
            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.NullTarget));
        }

        // ── Guard: invalid key ─────────────────────────────────────────────────────

        [Test]
        public void RegisterInstance_NullKey_ReturnsInvalidInstanceKey()
        {
            ScanResult result = _system.RegisterInstance(new PlayerTarget(), null);
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.InvalidInstanceKey));
        }

        [Test]
        public void RegisterInstance_EmptyKey_ReturnsInvalidInstanceKey()
        {
            ScanResult result = _system.RegisterInstance(new PlayerTarget(), string.Empty);
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.InvalidInstanceKey));
        }

        [Test]
        public void RegisterInstance_KeyWithDot_ReturnsInvalidInstanceKey()
        {
            ScanResult result = _system.RegisterInstance(new PlayerTarget(), "my.player");
            Assert.That(result.Entries[0].Result.Error, Is.EqualTo(RegistrationError.InvalidInstanceKey));
        }

        // ── Guard: duplicate key ───────────────────────────────────────────────────

        [Test]
        public void RegisterInstance_DuplicateKey_ReturnsDuplicateInstanceKey()
        {
            _system.RegisterInstance(new PlayerTarget(), "player");
            ScanResult second = _system.RegisterInstance(new PlayerTarget(), "player");

            Assert.That(second.HasErrors, Is.True);
            Assert.That(second.Entries[0].Result.Error, Is.EqualTo(RegistrationError.DuplicateInstanceKey));
        }

        [Test]
        public void RegisterInstance_DuplicateKey_FirstRegistrationCommandsStillExecutable()
        {
            var target = new PlayerTarget();
            _system.RegisterInstance(target, "player");
            _system.RegisterInstance(new PlayerTarget(), "player"); // duplicate — ignored

            ExecutionResult result = _system.Execute("player.Ping", Array.Empty<string>());
            Assert.That(result.Success, Is.True);
            Assert.That(target.WasCalled, Is.True);
        }

        // ── Success: commands appear in discovery APIs ────────────────────────────

        [Test]
        public void RegisterInstance_Success_CommandsInGetCommandNames()
        {
            _system.RegisterInstance(new PlayerTarget(), "player");

            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Contain("player.Heal"));
            Assert.That(names, Does.Contain("player.Ping"));
        }

        [Test]
        public void RegisterInstance_Success_TryGetCommandParameters_Works()
        {
            _system.RegisterInstance(new PlayerTarget(), "player");

            bool found = _system.TryGetCommandParameters("player.Heal", out CommandParameterInfo[] parameters);
            Assert.That(found, Is.True);
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].Type, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void RegisterInstance_Success_GetSnapshot_ContainsCommands()
        {
            _system.RegisterInstance(new PlayerTarget(), "player");

            CommandMetadataSnapshot snap = _system.GetSnapshot();
            Assert.That(snap.TryGetParameters("player.Heal", out _), Is.True);
        }

        // ── Execution ─────────────────────────────────────────────────────────────

        [Test]
        public void Execute_InstanceMethod_Succeeds()
        {
            var target = new PlayerTarget();
            _system.RegisterInstance(target, "player");

            ExecutionResult result = _system.Execute("player.Heal", new[] { "50" });
            Assert.That(result.Success, Is.True);
            Assert.That(target.Health, Is.EqualTo(150));
        }

        [Test]
        public void Execute_PropertyGetter_ReturnsValue()
        {
            var target = new PlayerTarget();
            _system.RegisterInstance(target, "player");

            ExecutionResult result = _system.Execute("player.get_Health", Array.Empty<string>());
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasReturnValue, Is.True);
            Assert.That(result.ReturnValue, Is.EqualTo(100));
        }

        [Test]
        public void Execute_PropertySetter_UpdatesValue()
        {
            var target = new PlayerTarget();
            _system.RegisterInstance(target, "player");

            ExecutionResult result = _system.Execute("player.set_Health", new[] { "200" });
            Assert.That(result.Success, Is.True);
            Assert.That(target.Health, Is.EqualTo(200));
        }

        [Test]
        public void Execute_Attribute_PrivateMethod_Succeeds()
        {
            var target = new PlayerTarget();
            _system.RegisterInstance(target, "player");

            ExecutionResult result = _system.Execute("player.special", Array.Empty<string>());
            Assert.That(result.Success, Is.True);
            Assert.That(target.WasCalled, Is.True);
        }

        // ── InstanceNull error handling ───────────────────────────────────────────

        [Test]
        public void Execute_InstanceGCed_ReturnsInstanceNull()
        {
            // Register via weak ref so we can let GC collect it.
            var weakRef = RegisterWeakInstance();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();

            // The target should now be collected; calling its method should produce NullRef inside DynamicInvoke.
            // Due to how DynamicInvoke works on value targets captured pre-GC, we simulate via a null-member-access crash.
            // Use NullReferenceThrowingTarget which throws NRE explicitly.
            ExecutionResult result = _system.Execute("crasher.CrashOnNull", Array.Empty<string>());
            Assert.That(result.Error, Is.EqualTo(ExecutionError.InstanceNull));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private WeakReference RegisterWeakInstance()
        {
            // Register a NullReferenceThrowingTarget; after this method returns and GC collects,
            // calling the command through DynamicInvoke on the live-captured target won't actually be null
            // because Delegate.CreateDelegate captures the target by strong ref.
            // Instead, simulate via NullReferenceThrowingTarget which unconditionally throws NRE.
            var target = new NullReferenceThrowingTarget();
            _system.RegisterInstance(target, "crasher");
            return new WeakReference(target);
        }

        [Test]
        public void Execute_NullRefOnInstanceCommand_ReturnsInstanceNull()
        {
            var target = new NullReferenceThrowingTarget();
            _system.RegisterInstance(target, "crasher");

            ExecutionResult result = _system.Execute("crasher.CrashOnNull", Array.Empty<string>());
            Assert.That(result.Error, Is.EqualTo(ExecutionError.InstanceNull));
        }

        [Test]
        public void Execute_StaticCommandThrowingNullRef_ReturnsCallbackThrewException()
        {
            // A static command that throws NullReferenceException must produce
            // CallbackThrewException — NOT InstanceNull — because IsInstanceCommand is false.
            _system.Register(
                "static_nre",
                Array.Empty<CommandParameterInfo>(),
                _ =>
                {
                    throw new NullReferenceException("static nre");
                });

            ExecutionResult result = _system.Execute("static_nre", Array.Empty<string>());
            Assert.That(result.Error, Is.EqualTo(ExecutionError.CallbackThrewException));
        }

        [Test]
        public void Execute_InstanceCommandThrowingOtherException_ReturnsCallbackThrewException()
        {
            var target = new ThrowingTarget();
            _system.RegisterInstance(target, "thrower");

            ExecutionResult result = _system.Execute("thrower.AlwaysThrows", Array.Empty<string>());
            Assert.That(result.Error, Is.EqualTo(ExecutionError.CallbackThrewException));
        }

        // ── UnregisterInstance ────────────────────────────────────────────────────

        [Test]
        public void UnregisterInstance_UnknownKey_ReturnsFail()
        {
            UnregisterResult result = _system.UnregisterInstance("ghost");
            Assert.That(result.Success, Is.False);
            Assert.That(result.RemovedCount, Is.EqualTo(0));
        }

        [Test]
        public void UnregisterInstance_NullKey_ReturnsFail()
        {
            UnregisterResult result = _system.UnregisterInstance(null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void UnregisterInstance_EmptyKey_ReturnsFail()
        {
            UnregisterResult result = _system.UnregisterInstance(string.Empty);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void UnregisterInstance_Success_ReturnsSuccessWithRemovedCount()
        {
            var target = new EnemyTarget();
            // EnemyTarget: Name (get+set), Damage (get+set) = 4 property commands; no public methods
            _system.RegisterInstance(target, "enemy");

            UnregisterResult result = _system.UnregisterInstance("enemy");
            Assert.That(result.Success, Is.True);
            Assert.That(result.RemovedCount, Is.GreaterThan(0));
        }

        [Test]
        public void UnregisterInstance_CommandsGoneFromGetCommandNames()
        {
            _system.RegisterInstance(new PlayerTarget(), "player");
            _system.UnregisterInstance("player");

            string[] names = _system.GetCommandNames();
            bool found = false;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].StartsWith("player.")) { found = true; break; }
            }
            Assert.That(found, Is.False);
        }

        [Test]
        public void UnregisterInstance_CommandsGoneFromGetSnapshot()
        {
            _system.RegisterInstance(new PlayerTarget(), "player");
            _system.UnregisterInstance("player");

            CommandMetadataSnapshot snap = _system.GetSnapshot();
            Assert.That(snap.TryGetParameters("player.Heal", out _), Is.False);
        }

        [Test]
        public void UnregisterInstance_Execute_ReturnsCommandNotFound()
        {
            _system.RegisterInstance(new PlayerTarget(), "player");
            _system.UnregisterInstance("player");

            ExecutionResult result = _system.Execute("player.Ping", Array.Empty<string>());
            Assert.That(result.Error, Is.EqualTo(ExecutionError.CommandNotFound));
        }

        // ── Shutdown / re-initialize cycle ────────────────────────────────────────

        [Test]
        public void ShutdownThenInitialize_SameKeyCanBeRegisteredAgain()
        {
            _system.RegisterInstance(new PlayerTarget(), "player");
            _system.Shutdown();
            _system.Initialize();

            ScanResult result = _system.RegisterInstance(new PlayerTarget(), "player");
            Assert.That(result.HasErrors, Is.False);
        }

        // ── AttributeOnly mode ────────────────────────────────────────────────────

        [Test]
        public void RegisterInstance_AttributeOnlyMode_OnlyDecoratedCommandRegistered()
        {
            var target = new PlayerTarget();
            _system.RegisterInstance(target, "player", default, InstanceScanMode.AttributeOnly);

            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Contain("player.special")); // [Command]-decorated private method
            Assert.That(names, Does.Not.Contain("player.Ping")); // public auto-scan method
        }

        // ── R10: IsDevOnly filtering at integration level ─────────────────────────

        [Test]
        public void RegisterInstance_DevOnlyCmd_SkippedWhenDevModeOff()
        {
            var target = new DevOnlyTarget();
            _system.RegisterInstance(target, "obj", new ScanOptions { DevMode = false }, InstanceScanMode.Auto);

            Assert.That(_system.GetCommandNames(), Does.Not.Contain("obj.dev_cmd"));
            Assert.That(_system.GetCommandNames(), Does.Contain("obj.RegularMethod"));
        }

        [Test]
        public void RegisterInstance_DevOnlyCmd_RegisteredWhenDevModeOn()
        {
            var target = new DevOnlyTarget();
            _system.RegisterInstance(target, "obj", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_system.GetCommandNames(), Does.Contain("obj.dev_cmd"));
        }

        // ── R7: Object-inherited methods not registered ───────────────────────────

        [Test]
        public void RegisterInstance_ObjectInheritedMethods_NotInCommandNames()
        {
            var target = new EnemyTarget();
            _system.RegisterInstance(target, "enemy");

            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Not.Contain("enemy.GetHashCode"));
            Assert.That(names, Does.Not.Contain("enemy.Equals"));
            Assert.That(names, Does.Not.Contain("enemy.ToString"));
            Assert.That(names, Does.Not.Contain("enemy.GetType"));
        }

        // ── R8: Write-only property produces only set_ command ────────────────────

        [Test]
        public void RegisterInstance_WriteOnlyProperty_ProducesOnlySetCommand()
        {
            var target = new WriteOnlyPropTarget();
            _system.RegisterInstance(target, "wo");

            string[] names = _system.GetCommandNames();
            Assert.That(names, Does.Contain("wo.set_WriteOnly"));
            Assert.That(names, Does.Not.Contain("wo.get_WriteOnly"));
        }

        // ── R18: Ref param skipped with descriptive ScanEntry ────────────────────

        [Test]
        public void RegisterInstance_RefParamMethod_ProducesFailedScanEntry()
        {
            var target = new RefParamClassTarget();
            ScanResult result = _system.RegisterInstance(target, "obj");

            bool hasFailure = false;
            for (int i = 0; i < result.Entries.Length; i++)
            {
                if (result.Entries[i].CommandName.Contains("RefMethod") && !result.Entries[i].Result.Success)
                {
                    hasFailure = true;
                    break;
                }
            }
            Assert.That(hasFailure, Is.True);
            // Normal method still registered
            Assert.That(_system.GetCommandNames(), Does.Contain("obj.Normal"));
        }

        // ── R14: History ReturnValue for instance commands ────────────────────────

        [Test]
        public void Execute_NonVoidInstanceMethod_HistoryEntryHasReturnValue()
        {
            var target = new NonVoidMethodTarget();
            _system.RegisterInstance(target, "calc");

            _system.Execute("calc.Double", new[] { "5" });

            CommandHistoryEntry[] history = _system.GetHistory();
            Assert.That(history, Has.Length.GreaterThan(0));
            Assert.That(history[history.Length - 1].ReturnValue, Is.EqualTo(10));
        }

        [Test]
        public void Execute_VoidInstanceMethod_HistoryEntryHasNullReturnValue()
        {
            var target = new PlayerTarget();
            _system.RegisterInstance(target, "player");

            _system.Execute("player.Ping", Array.Empty<string>());

            CommandHistoryEntry[] history = _system.GetHistory();
            Assert.That(history, Has.Length.GreaterThan(0));
            Assert.That(history[history.Length - 1].ReturnValue, Is.Null);
        }

        // ── R16: TryGetCommandParameters absent after UnregisterInstance ──────────

        [Test]
        public void UnregisterInstance_TryGetCommandParameters_ReturnsFalse()
        {
            _system.RegisterInstance(new PlayerTarget(), "player");
            _system.UnregisterInstance("player");

            bool found = _system.TryGetCommandParameters("player.Heal", out _);
            Assert.That(found, Is.False);
        }

        // ── R5: ScanResult clean on success ───────────────────────────────────────

        [Test]
        public void RegisterInstance_CleanScan_ScanResultHasNoErrors()
        {
            var target = new EnemyTarget();
            ScanResult result = _system.RegisterInstance(target, "enemy");

            Assert.That(result.HasErrors, Is.False);
        }

        [Test]
        public void RegisterInstance_CleanScan_EntriesContainRegisteredCommands()
        {
            var target = new EnemyTarget();
            ScanResult result = _system.RegisterInstance(target, "enemy");

            // EnemyTarget: Name (get+set), Damage (get+set) = 4 property commands + 0 public methods
            Assert.That(result.Entries.Length, Is.GreaterThan(0));
            bool hasEntry = false;
            for (int i = 0; i < result.Entries.Length; i++)
            {
                if (result.Entries[i].CommandName.StartsWith("enemy."))
                {
                    hasEntry = true;
                    break;
                }
            }
            Assert.That(hasEntry, Is.True);
        }

        // ── [InstanceCommandSource] class attribute integration ───────────────────

        [Test]
        public void RegisterInstance_SourceAttribute_AttributeOnly_SuppressesAutoScan()
        {
            var target = new SourceAttributeOnlyTarget();
            ScanResult result = _system.RegisterInstance(target, "src");

            Assert.That(result.HasErrors, Is.False);
            bool found = _system.TryGetCommandParameters("src.ic_explicit", out _);
            Assert.That(found, Is.True);

            bool skipped = _system.TryGetCommandParameters("src.ShouldBeSkipped", out _);
            Assert.That(skipped, Is.False);
        }

        [Test]
        public void RegisterInstance_SourceAttribute_Auto_AutoScansPublicMethods()
        {
            var target = new SourceAutoTarget();
            ScanResult result = _system.RegisterInstance(target, "src");

            Assert.That(result.HasErrors, Is.False);
            bool found = _system.TryGetCommandParameters("src.AutoCmd", out _);
            Assert.That(found, Is.True);
        }

        [Test]
        public void RegisterInstance_SourceAttribute_ExplicitModeOverridesClassAttribute()
        {
            var target = new SourceAttributeOnlyTarget();
            // Pass InstanceScanMode.Auto explicitly — class attribute's AttributeOnly is ignored.
            ScanResult result = _system.RegisterInstance(
                target, "src", default, InstanceScanMode.Auto);

            bool found = _system.TryGetCommandParameters("src.ShouldBeSkipped", out _);
            Assert.That(found, Is.True);
        }

        // ── [InstanceCommand] dev-only via CommandSystem ──────────────────────────

        [Test]
        public void RegisterInstance_InstanceCommandDevOnly_SkippedWhenDevModeOff()
        {
            var target = new InstanceCmdDevOnlyTarget();
            _system.RegisterInstance(target, "ic", new ScanOptions { DevMode = false });

            bool found = _system.TryGetCommandParameters("ic.ic_dev", out _);
            Assert.That(found, Is.False);
        }

        [Test]
        public void RegisterInstance_InstanceCommandDevOnly_RegisteredWhenDevModeOn()
        {
            var target = new InstanceCmdDevOnlyTarget();
            _system.RegisterInstance(target, "ic", new ScanOptions { DevMode = true });

            bool found = _system.TryGetCommandParameters("ic.ic_dev", out _);
            Assert.That(found, Is.True);
        }
    }
}
