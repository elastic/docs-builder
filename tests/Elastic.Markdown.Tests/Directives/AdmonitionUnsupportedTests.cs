// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public abstract class AdmonitionUnsupportedTests(ITestOutputHelper output, string directive)
	: DirectiveTest<UnsupportedDirectiveBlock>(output,
		$$"""
		  ```{{{directive}}}
		  This is an attention block
		  ```
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
public class AttentionTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "attention");
public class DangerTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "danger");
public class ErrorTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "error");
public class HintTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "hint");
public class ImportantTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "important");
public class SeeAlsoTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "seealso");
public class AdmonitionTitleTests(ITestOutputHelper output) : AdmonitionUnsupportedTests(output, "admonition");
// ReSharper restore UnusedType.Global
