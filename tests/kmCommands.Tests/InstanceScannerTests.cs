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
    /// Unit tests for InstanceScanner, InstanceCallbackBuilder, and InstanceScanMode.
    /// Uses internal components directly (InternalsVisibleTo).
    /// </summary>
    [TestFixture]
    public class InstanceScannerTests
    {
        private CommandRegistry _registry;
        private ArgumentConverter _converter;
        private InstanceRegistry _instanceRegistry;
        private InstanceScanner _scanner;

        [SetUp]
        public void SetUp()
        {
            _registry = new CommandRegistry();
            _converter = new ArgumentConverter();
            _instanceRegistry = new InstanceRegistry();
            _scanner = new InstanceScanner(_registry, _converter, _instanceRegistry);
        }

        private void ReserveKey(string key, object target)
        {
            _instanceRegistry.TryReserveKey(key, target);
        }

        // ── Target classes ────────────────────────────────────────────────────────

        private class PlayerTarget
        {
            public int Health { get; set; } = 100;
            public int ReadOnlyProp { get; } = 7;
            public float Speed { get; set; } = 5f;

            public int LastHealAmount;
            public bool WasCalled;

            public void Heal(int amount)
            {
                LastHealAmount = amount;
            }

            public void Ping()
            {
                WasCalled = true;
            }

            public int GetScore()
            {
                return 42;
            }

            [Command("player_special")]
            private void SpecialMethod()
            {
                WasCalled = true;
            }

            [Command("player_devonly", IsDevOnly = true)]
            public void DevOnlyMethod()
            {
                WasCalled = true;
            }

            private void PrivateMethod() { }
        }

        private class EnemyTarget
        {
            public string Name { get; set; } = "Enemy";
            public void Attack() { }
        }

        private class GenericMethodTarget
        {
            public void GenericMethod<T>(T value) { }
            public int NormalMethod() { return 1; }
        }

        private class RefParamTarget
        {
            public void RefMethod(ref int value) { value = 0; }
            public int NormalMethod() { return 1; }
        }

        private class UnsupportedParamTarget
        {
            public void BadMethod(System.Collections.Generic.List<int> list) { }
        }

        private class AttributeOnlyModeTarget
        {
            [Command("attr_cmd")]
            public void AttributeMethod() { }
            public void PublicAutoMethod() { }
        }

        private class InstanceNonVoidTarget
        {
            public int ComputeDouble(int value) { return value * 2; }
        }

        private class WriteOnlyPropertyTarget
        {
            private int _value;
            public int WriteOnly { set { _value = value; } }
            public int GetValue() { return _value; }
        }

        private class StaticCommandTarget
        {
            [Command("static_cmd")]
            public static void StaticMethod() { }
            public void NormalMethod() { }
        }

        private class IndexerTarget
        {
            private readonly int[] _data = new int[10];
            public int this[int index]
            {
                get { return _data[index]; }
                set { _data[index] = value; }
            }
            public int Normal { get; set; }
        }

        // ── [Command]-decorated instance methods ──────────────────────────────────

        [Test]
        public void Scan_AttributeDecorated_PrivateMethod_Registered()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", default, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.player_special", out _), Is.True);
        }

        [Test]
        public void Scan_AttributeDecorated_PrivateMethod_Callback_Executes()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", default, InstanceScanMode.Auto);

            _registry.TryGetCommand("player.player_special", out CommandDefinition def);
            def.Callback(Array.Empty<object>());
            Assert.That(target.WasCalled, Is.True);
        }

        [Test]
        public void Scan_AttributeDecorated_DevOnly_SkippedWhenDevModeOff()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = false }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.player_devonly", out _), Is.False);
        }

        [Test]
        public void Scan_AttributeDecorated_DevOnly_RegisteredWhenDevModeOn()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.player_devonly", out _), Is.True);
        }

        // ── Auto-scan public methods ──────────────────────────────────────────────

        [Test]
        public void AutoScan_PublicMethod_Registered()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.Heal", out _), Is.True);
        }

        [Test]
        public void AutoScan_PublicMethod_CallbackExecutes()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            _registry.TryGetCommand("player.Heal", out CommandDefinition def);
            def.Callback(new object[] { 30 });
            Assert.That(target.LastHealAmount, Is.EqualTo(30));
        }

        [Test]
        public void AutoScan_PublicVoidZeroParam_CallbackExecutes()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            _registry.TryGetCommand("player.Ping", out CommandDefinition def);
            def.Callback(Array.Empty<object>());
            Assert.That(target.WasCalled, Is.True);
        }

        [Test]
        public void AutoScan_VoidCallback_ReturnsNull()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            _registry.TryGetCommand("player.Ping", out CommandDefinition def);
            object returnValue = def.Callback(Array.Empty<object>());
            Assert.That(returnValue, Is.Null);
        }

        [Test]
        public void AutoScan_NonVoidCallback_ReturnsValue()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            _registry.TryGetCommand("player.GetScore", out CommandDefinition def);
            object returnValue = def.Callback(Array.Empty<object>());
            Assert.That(returnValue, Is.EqualTo(42));
        }

        [Test]
        public void AutoScan_PrivateMethod_NotRegistered()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", default, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.PrivateMethod", out _), Is.False);
        }

        [Test]
        public void AutoScan_InheritedObjectMethods_NotRegistered()
        {
            var target = new EnemyTarget();
            ReserveKey("enemy", target);
            _scanner.Scan(target, "enemy", default, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("enemy.GetHashCode", out _), Is.False);
            Assert.That(_registry.TryGetCommand("enemy.Equals", out _), Is.False);
            Assert.That(_registry.TryGetCommand("enemy.ToString", out _), Is.False);
            Assert.That(_registry.TryGetCommand("enemy.GetType", out _), Is.False);
        }

        // ── Property commands ─────────────────────────────────────────────────────

        [Test]
        public void AutoScan_ReadWriteProperty_ProducesBothGetAndSet()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.get_Health", out _), Is.True);
            Assert.That(_registry.TryGetCommand("player.set_Health", out _), Is.True);
        }

        [Test]
        public void AutoScan_ReadOnlyProperty_ProducesOnlyGet()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.get_ReadOnlyProp", out _), Is.True);
            Assert.That(_registry.TryGetCommand("player.set_ReadOnlyProp", out _), Is.False);
        }

        [Test]
        public void AutoScan_PropertyGetterCallback_ReturnsPropertyValue()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            _registry.TryGetCommand("player.get_Health", out CommandDefinition def);
            object value = def.Callback(Array.Empty<object>());
            Assert.That(value, Is.EqualTo(100));
        }

        [Test]
        public void AutoScan_PropertySetterCallback_SetsValue()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            _registry.TryGetCommand("player.set_Health", out CommandDefinition def);
            def.Callback(new object[] { 200 });
            Assert.That(target.Health, Is.EqualTo(200));
        }

        [Test]
        public void AutoScan_PropertySetterCallback_ReturnsNull()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            _registry.TryGetCommand("player.set_Health", out CommandDefinition def);
            object returnValue = def.Callback(new object[] { 99 });
            Assert.That(returnValue, Is.Null);
        }

        [Test]
        public void AutoScan_IndexerProperty_NotRegistered()
        {
            var target = new IndexerTarget();
            ReserveKey("idx", target);
            _scanner.Scan(target, "idx", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("idx.get_Item", out _), Is.False);
            Assert.That(_registry.TryGetCommand("idx.set_Item", out _), Is.False);
            Assert.That(_registry.TryGetCommand("idx.get_Normal", out _), Is.True);
        }

        // ── Invalid parameter rejection ───────────────────────────────────────────

        [Test]
        public void AutoScan_GenericMethod_ProducesFailedScanEntry()
        {
            var target = new GenericMethodTarget();
            ReserveKey("gen", target);
            ScanResult result = _scanner.Scan(target, "gen", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            bool hasFailure = false;
            for (int i = 0; i < result.Entries.Length; i++)
            {
                if (result.Entries[i].CommandName.Contains("GenericMethod") && !result.Entries[i].Result.Success)
                {
                    hasFailure = true;
                    break;
                }
            }
            Assert.That(hasFailure, Is.True);
            Assert.That(_registry.TryGetCommand("gen.GenericMethod", out _), Is.False);
        }

        [Test]
        public void AutoScan_GenericMethod_OtherMethodsStillRegistered()
        {
            var target = new GenericMethodTarget();
            ReserveKey("gen", target);
            _scanner.Scan(target, "gen", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("gen.NormalMethod", out _), Is.True);
        }

        [Test]
        public void AutoScan_RefParam_ProducesFailedScanEntry()
        {
            var target = new RefParamTarget();
            ReserveKey("ref_target", target);
            ScanResult result = _scanner.Scan(target, "ref_target", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

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
        }

        [Test]
        public void AutoScan_UnsupportedParamType_ProducesFailedScanEntry()
        {
            var target = new UnsupportedParamTarget();
            ReserveKey("bad", target);
            ScanResult result = _scanner.Scan(target, "bad", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(result.HasErrors, Is.True);
        }

        // ── [Command] not double-registered ──────────────────────────────────────

        [Test]
        public void Scan_AttributeDecoratedMethod_NotDoubleRegisteredByAutoScan()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            ScanResult result = _scanner.Scan(target, "player", default, InstanceScanMode.Auto);

            int count = 0;
            for (int i = 0; i < result.Entries.Length; i++)
            {
                if (result.Entries[i].CommandName == "player.player_special")
                    count++;
            }
            Assert.That(count, Is.EqualTo(1));
        }

        // ── InstanceScanMode.AttributeOnly ───────────────────────────────────────

        [Test]
        public void AttributeOnlyMode_SuppressesAutoScanOfPublicMethods()
        {
            var target = new AttributeOnlyModeTarget();
            ReserveKey("obj", target);
            _scanner.Scan(target, "obj", default, InstanceScanMode.AttributeOnly);

            Assert.That(_registry.TryGetCommand("obj.attr_cmd", out _), Is.True);
            Assert.That(_registry.TryGetCommand("obj.PublicAutoMethod", out _), Is.False);
        }

        // ── IsInstanceCommand flag ────────────────────────────────────────────────

        [Test]
        public void RegisteredCommands_HaveIsInstanceCommandTrue()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            _registry.TryGetCommand("player.Heal", out CommandDefinition def);
            Assert.That(def.IsInstanceCommand, Is.True);
        }

        // ── Non-void instance callback ────────────────────────────────────────────

        [Test]
        public void NonVoidInstanceMethod_CallbackReturnsBoxedValue()
        {
            var target = new InstanceNonVoidTarget();
            ReserveKey("calc", target);
            _scanner.Scan(target, "calc", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            _registry.TryGetCommand("calc.ComputeDouble", out CommandDefinition def);
            object result = def.Callback(new object[] { 7 });
            Assert.That(result, Is.EqualTo(14));
        }

        // ── Write-only property ───────────────────────────────────────────────────

        [Test]
        public void AutoScan_WriteOnlyProperty_ProducesOnlySet()
        {
            var target = new WriteOnlyPropertyTarget();
            ReserveKey("wo", target);
            _scanner.Scan(target, "wo", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("wo.set_WriteOnly", out _), Is.True);
            Assert.That(_registry.TryGetCommand("wo.get_WriteOnly", out _), Is.False);
        }

        // ── Static [Command] not registered by instance scan ─────────────────────

        [Test]
        public void Scan_StaticCommandDecorated_NotRegisteredByInstanceScanner()
        {
            var target = new StaticCommandTarget();
            ReserveKey("obj", target);
            _scanner.Scan(target, "obj", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("obj.static_cmd", out _), Is.False);
            Assert.That(_registry.TryGetCommand("obj.NormalMethod", out _), Is.True);
        }

        // ── New: Auto-scan DevMode filtering ────────────────────────────────────────────

        [Test]
        public void AutoScan_DevModeOff_SkipsPublicMethods()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = false }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.Heal", out _), Is.False);
            Assert.That(_registry.TryGetCommand("player.Ping", out _), Is.False);
        }

        [Test]
        public void AutoScan_DevModeOff_SkipsPublicProperties()
        {
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = false }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.get_Health", out _), Is.False);
            Assert.That(_registry.TryGetCommand("player.set_Health", out _), Is.False);
        }

        [Test]
        public void AutoScan_DevModeOff_AttributeDecoratedMethod_StillRegistered()
        {
            // [Command] without IsDevOnly is release-safe — always registers regardless of DevMode.
            var target = new PlayerTarget();
            ReserveKey("player", target);
            _scanner.Scan(target, "player", new ScanOptions { DevMode = false }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("player.player_special", out _), Is.True);
        }

        // ── New: [CommandIgnore] ────────────────────────────────────────────────────────────

        private class CommandIgnoreMethodTarget
        {
            [CommandIgnore]
            public void IgnoredMethod() { }
            public void VisibleMethod() { }

            [Command("attr_visible")]
            public void AttributeVisible() { }

            [Command("attr_ignored")]
            [CommandIgnore]
            public void AttributeIgnored() { }
        }

        private class CommandIgnorePropertyTarget
        {
            [CommandIgnore]
            public int IgnoredProp { get; set; }
            public int VisibleProp { get; set; }
        }

        [Test]
        public void CommandIgnore_OnMethod_SkipsAutoScan()
        {
            var target = new CommandIgnoreMethodTarget();
            ReserveKey("obj", target);
            _scanner.Scan(target, "obj", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("obj.IgnoredMethod", out _), Is.False);
            Assert.That(_registry.TryGetCommand("obj.VisibleMethod", out _), Is.True);
        }

        [Test]
        public void CommandIgnore_OnAttributeDecorated_SkipsRegistration()
        {
            // [CommandIgnore] wins over [Command].
            var target = new CommandIgnoreMethodTarget();
            ReserveKey("obj", target);
            _scanner.Scan(target, "obj", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("obj.attr_visible", out _), Is.True);
            Assert.That(_registry.TryGetCommand("obj.attr_ignored", out _), Is.False);
        }

        [Test]
        public void CommandIgnore_OnProperty_SkipsGetterAndSetter()
        {
            var target = new CommandIgnorePropertyTarget();
            ReserveKey("obj", target);
            _scanner.Scan(target, "obj", new ScanOptions { DevMode = true }, InstanceScanMode.Auto);

            Assert.That(_registry.TryGetCommand("obj.get_IgnoredProp", out _), Is.False);
            Assert.That(_registry.TryGetCommand("obj.set_IgnoredProp", out _), Is.False);
            Assert.That(_registry.TryGetCommand("obj.get_VisibleProp", out _), Is.True);
        }
    }
}
