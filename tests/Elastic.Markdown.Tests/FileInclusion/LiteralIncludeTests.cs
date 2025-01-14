// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.FileInclusion;


public class LiteralIncludeUsingPropertyTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::{include} _snippets/test.txt
:literal: true
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello world*";
		fileSystem.AddFile(@"docs/source/_snippets/test.txt", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void IncludesInclusionHtml() =>
		Html.Should()
			.Be("*Hello world*")
		;
}


public class LiteralIncludeTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::{literalinclude} _snippets/test.md
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello world*";
		fileSystem.AddFile(@"docs/source/_snippets/test.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void IncludesInclusionHtml() =>
		Html.Should()
			.Be("*Hello world*");
}
