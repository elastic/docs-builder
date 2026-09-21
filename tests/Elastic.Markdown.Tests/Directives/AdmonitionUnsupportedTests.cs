// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives;

namespace Elastic.Markdown.Tests.Directives;

public abstract class AdmonitionUnsupportedTests(string directive) : DirectiveTest<UnsupportedDirectiveBlock>(
	$$"""
:::{{{directive}}}
This is an attention block
:::
A regular paragraph.
"""
)
{
	[Test]
	public void ParsesAsUnknown() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirective() => Block!.Directive.Should().Be(directive);
}

// ReSharper disable UnusedType.Global
public class DangerTests() : AdmonitionUnsupportedTests("danger");

public class ErrorTests() : AdmonitionUnsupportedTests("error");

public class HintTests() : AdmonitionUnsupportedTests("hint");

public class AttentionTests() : AdmonitionUnsupportedTests("attention");

public class CautionTests() : AdmonitionUnsupportedTests("caution");

public class SeeAlsoTests() : AdmonitionUnsupportedTests("seealso");
// ReSharper restore UnusedType.Global
