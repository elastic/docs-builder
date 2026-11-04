// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Tests.Directives;

namespace Elastic.Markdown.Tests.FileInclusion;

[InheritsTests]
public class LiteralIncludeUsingPropertyTests() : DirectiveTest<IncludeBlock>("""
:::{include} _snippets/test.txt
:literal: true
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello world*";
		fileSystem.AddFile(@"docs/_snippets/test.txt", inclusion);
	}

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void IncludesInclusionHtml() => Html.Should().Be("*Hello world*");
}

[InheritsTests]
public class LiteralIncludeTests() : DirectiveTest<IncludeBlock>("""
:::{literalinclude} _snippets/test.md
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello world*";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void IncludesInclusionHtml() => Html.Should().Be("*Hello world*");
}

[InheritsTests]
public class LiteralIncludeRelativeTraversalBlocked() : DirectiveTest<IncludeBlock>("""
:::{literalinclude} ../../../outside.txt
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) => fileSystem.AddFile(@"outside.txt", "some content");

	[Test]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("must resolve within the documentation source directory"));
	}
}

[InheritsTests]
public class LiteralIncludeAbsoluteTraversalBlocked() : DirectiveTest<IncludeBlock>("""
:::{literalinclude} /../../../outside.txt
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) => fileSystem.AddFile(@"outside.txt", "some content");

	[Test]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("must resolve within the documentation source directory"));
	}
}

[InheritsTests]
public class LiteralIncludeHiddenDirectoryBlocked() : DirectiveTest<IncludeBlock>("""
:::{literalinclude} .config/data.txt
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) => fileSystem.AddFile(@"docs/.config/data.txt", "some content");

	[Test]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("must not traverse hidden directories"));
	}
}
