// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Markdig.Syntax;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public abstract class VersionTests(ITestOutputHelper output, string directive) : DirectiveTest<VersionBlock>(output,
$$"""
```{{{directive}}} 1.0.1-beta1 more information
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

	[Fact]
	public void SetsVersion() => Block!.Version.Should().Be(new SemVersion(1, 0, 1, "beta1"));
}

public class VersionAddedTests(ITestOutputHelper output) : VersionTests(output, "versionadded")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Version Added (1.0.1-beta1): more information");
}

public class VersionChangedTests(ITestOutputHelper output) : VersionTests(output, "versionchanged")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Version Changed (1.0.1-beta1): more information");
}
public class VersionRemovedTests(ITestOutputHelper output) : VersionTests(output, "versionremoved")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Version Removed (1.0.1-beta1): more information");
}
public class VersionDeprectatedTests(ITestOutputHelper output) : VersionTests(output, "deprecated")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Deprecated (1.0.1-beta1): more information");
}
