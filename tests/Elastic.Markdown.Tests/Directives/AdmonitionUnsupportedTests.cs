// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public abstract class AdmonitionUnsupportedTests(ITestOutputHelper output, string directive)
	: DirectiveTest<UnsupportedDirectiveBlock>(output,
$$"""
:::{{{directive}}}
This is an attention block
:::
A regular paragraph.
"""
	)
{
	[Fact]
	public void ParsesAsUnknown() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectDirective() => Block!.Directive.Should().Be(directive);
}

// ReSharper disable UnusedType.Global
public class DangerTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "danger");
public class ErrorTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "error");
public class HintTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "hint");
public class AttentionTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "attention");
public class CautionTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "caution");
public class SeeAlsoTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "seealso");
// ReSharper restore UnusedType.Global
