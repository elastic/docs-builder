// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
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
	public override Task InitializeAsync()
	{
		// language=markdown
		var inclusion = "*Hello world*";
		FileSystem.AddFile(@"docs/source/_snippets/test.md", inclusion);
		return base.InitializeAsync();
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
	public override Task InitializeAsync()
	{
		// language=markdown
		var inclusion = "*Hello {{foo}}*";
		FileSystem.AddFile(@"docs/source/_snippets/test.md", inclusion);
		return base.InitializeAsync();
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
