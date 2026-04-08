// kmCommands (https://github.com/KMiseckas/kmCommands)
// Copyright (c) 2026 Klaudijus Miseckas
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.

using NUnit.Framework;
using kmCommands.Core;

namespace kmCommands.Tests
{
    [TestFixture]
    internal class NestedCommandTokenizerTests
    {
        [Test]
        public void Tokenize_BasicArgs_ReturnsSplitTokens()
        {
            string[] result = NestedCommandTokenizer.Tokenize("cmd arg1 arg2");
            Assert.That(result, Is.EqualTo(new[] { "cmd", "arg1", "arg2" }));
        }

        [Test]
        public void Tokenize_SingleToken_ReturnsOneElement()
        {
            string[] result = NestedCommandTokenizer.Tokenize("cmd");
            Assert.That(result, Is.EqualTo(new[] { "cmd" }));
        }

        [Test]
        public void Tokenize_NullInput_ReturnsEmpty()
        {
            string[] result = NestedCommandTokenizer.Tokenize(null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Tokenize_EmptyString_ReturnsEmpty()
        {
            string[] result = NestedCommandTokenizer.Tokenize(string.Empty);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Tokenize_LeadingAndTrailingSpaces_AreTrimmed()
        {
            string[] result = NestedCommandTokenizer.Tokenize("  cmd arg1  ");
            Assert.That(result, Is.EqualTo(new[] { "cmd", "arg1" }));
        }

        [Test]
        public void Tokenize_MultipleSpacesBetweenTokens_Collapsed()
        {
            string[] result = NestedCommandTokenizer.Tokenize("cmd  arg1   arg2");
            Assert.That(result, Is.EqualTo(new[] { "cmd", "arg1", "arg2" }));
        }

        [Test]
        public void Tokenize_NestedDelimiterToken_KeptAtomic()
        {
            string[] result = NestedCommandTokenizer.Tokenize("cmd $(inner 1) arg2");
            Assert.That(result, Is.EqualTo(new[] { "cmd", "$(inner 1)", "arg2" }));
        }

        [Test]
        public void Tokenize_DeepNestedDelimiterToken_KeptAtomic()
        {
            string[] result = NestedCommandTokenizer.Tokenize("cmd $(a $(b 1))");
            Assert.That(result, Is.EqualTo(new[] { "cmd", "$(a $(b 1))" }));
        }

        [Test]
        public void Tokenize_OnlyNestedToken()
        {
            string[] result = NestedCommandTokenizer.Tokenize("$(inner 1)");
            Assert.That(result, Is.EqualTo(new[] { "$(inner 1)" }));
        }

        [Test]
        public void Tokenize_UnbalancedParenTreatedAsLiteral_NoException()
        {
            // Should not throw; unbalanced paren is consumed to end-of-string.
            string[] result = null;
            Assert.DoesNotThrow(() => result = NestedCommandTokenizer.Tokenize("$(unclosed"));
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Tokenize_EmptyNestedExpression()
        {
            // "$()": parse-failed detection happens in the resolver, not the tokenizer.
            string[] result = NestedCommandTokenizer.Tokenize("$()");
            Assert.That(result, Is.EqualTo(new[] { "$()" }));
        }

        [Test]
        public void Tokenize_ThreeLevelNesting_KeptAtomic()
        {
            string[] result = NestedCommandTokenizer.Tokenize("$(a $(b $(c 1)))");
            Assert.That(result, Is.EqualTo(new[] { "$(a $(b $(c 1)))" }));
        }
    }
}
