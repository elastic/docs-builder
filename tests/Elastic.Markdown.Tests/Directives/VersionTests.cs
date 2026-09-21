// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Versions;
using Elastic.Markdown.Myst.Directives.Version;

namespace Elastic.Markdown.Tests.Directives;

public abstract class VersionTests(string directive) : DirectiveTest<VersionBlock>(
	$$"""
:::{{{directive}}} 1.0.1-beta1 more information
Version brief summary
:::
A regular paragraph.
"""
)
{
	[Test]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be(directive);

	[Test]
	public void SetsVersion() => Block!.Version.Should().Be(new SemVersion(1, 0, 1, "beta1"));
}

public class VersionAddedTests() : VersionTests("versionadded")
{
	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Version Added (1.0.1-beta1): more information");
}

public class VersionChangedTests() : VersionTests("versionchanged")
{
	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Version Changed (1.0.1-beta1): more information");
}

public class VersionRemovedTests() : VersionTests("versionremoved")
{
	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Version Removed (1.0.1-beta1): more information");
}

public class VersionDeprectatedTests() : VersionTests("deprecated")
{
	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Deprecated (1.0.1-beta1): more information");
}

public abstract class VersionValidationTests(string version) : DirectiveTest<VersionBlock>(
	$$"""
:::{versionchanged} {{version}} more information
Version brief summary
:::
A regular paragraph.
"""
);

public class SimpleVersion() : VersionValidationTests("7.17")
{
	[Test]
	public void SetsVersion() => Block!.Version.Should().Be(new SemVersion(7, 17, 0));

	[Test]
	public void HasNoError() => Collector.Diagnostics.Should().BeEmpty();
}

public class MajorVersionOnly() : VersionValidationTests("8")
{
	[Test]
	public void HasError() =>
		Collector.Diagnostics.Should().HaveCount(1).And.Contain(d => d.Message.Contains("'8' is not a valid version"));
}

public class BranchVersion() : VersionValidationTests("8.x")
{
	[Test]
	public void HasError() =>
		Collector.Diagnostics.Should().HaveCount(1).And.Contain(d => d.Message.Contains("'8.x' is not a valid version"));
}
