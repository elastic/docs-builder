// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.FileInclusion;


public class IncludeTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
```{include} _snippets/test.md
```
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
			.Contain("Hello world")
			.And.Be("<p><em>Hello world</em></p>\n")
		;
}


public class IncludeSubstitutionTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
---
title: My Document
sub:
  foo: "bar"
---
```{include} _snippets/test.md
```
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello {{foo}}*";
		fileSystem.AddFile(@"docs/source/_snippets/test.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void InclusionInheritsYamlContext() =>
		Html.Should()
			.Contain("Hello bar")
			.And.Be("<p><em>Hello bar</em></p>\n")
		;
}


public class IncludeNotFoundTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
```{include} _snippets/notfound.md
```
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void IncludesNothing() => Html.Should().Be("");

	[Fact]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.Should()
			.OnlyContain(d => d.Message.Contains("notfound.md` does not exist"));
	}
}

public class IncludeRequiresArgument(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
```{include}
```
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void IncludesNothing() => Html.Should().Be("");

	[Fact]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.Should()
			.OnlyContain(d => d.Message.Contains("include requires an argument."));
	}
}
