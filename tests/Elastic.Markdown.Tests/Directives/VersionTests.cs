// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Markdig.Syntax;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public abstract class VersionTests(ITestOutputHelper output, string directive) : DirectiveTest<VersionBlock>(output,
$$"""
```{{{directive}}}
Version brief summary
```
A regular paragraph.
"""
)
{
	[Fact]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be(directive);
}

public class VersionAddedTests(ITestOutputHelper output) : VersionTests(output, "versionadded")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Version Added");
}

public class VersionChangedTests(ITestOutputHelper output) : VersionTests(output, "versionchanged")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Version Changed");
}
public class VersionRemovedTests(ITestOutputHelper output) : VersionTests(output, "versionremoved")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Version Removed");
}
public class VersionDeprectatedTests(ITestOutputHelper output) : VersionTests(output, "deprecated")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Deprecated");
}
