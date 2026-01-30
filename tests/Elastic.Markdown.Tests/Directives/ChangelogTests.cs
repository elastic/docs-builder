// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogBasicTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogBasicTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") =>
		// Create the default bundles folder with a test bundle
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
  lifecycle: ga
entries:
- title: Add new feature
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  areas:
  - Search
  pr: "123456"
  description: This is a great new feature.
- title: Fix important bug
  type: bug-fix
  products:
  - product: elasticsearch
    target: 9.3.0
  areas:
  - Indexing
  pr: "123457"
"""));

	[Fact]
	public void ParsesChangelogBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("changelog");

	[Fact]
	public void FindsBundlesFolder() => Block!.Found.Should().BeTrue();

	[Fact]
	public void SetsCorrectBundlesFolderPath() => Block!.BundlesFolderPath.Should().EndWith("changelog/bundles");

	[Fact]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(1);

	[Fact]
	public void RendersMarkdownContent()
	{
		Html.Should().Contain("9.3.0");
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Add new feature");
		Html.Should().Contain("Fixes");
		Html.Should().Contain("Fix important bug");
	}
}

public class ChangelogMultipleBundlesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMultipleBundlesTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""")
	{
		// Create multiple bundles with different versions
		FileSystem.AddFile("docs/changelog/bundles/9.2.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.2.0
entries:
- title: Feature in 9.2.0
  type: feature
  products:
  - product: elasticsearch
    target: 9.2.0
  pr: "111111"
"""));

		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Feature in 9.3.0
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  pr: "222222"
"""));

		FileSystem.AddFile("docs/changelog/bundles/9.10.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.10.0
entries:
- title: Feature in 9.10.0
  type: feature
  products:
  - product: elasticsearch
    target: 9.10.0
  pr: "333333"
"""));
	}

	[Fact]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(3);

	[Fact]
	public void RendersInSemverOrder()
	{
		// Should be sorted by semver descending: 9.10.0 > 9.3.0 > 9.2.0
		var idx910 = Html.IndexOf("9.10.0", StringComparison.Ordinal);
		var idx93 = Html.IndexOf("9.3.0", StringComparison.Ordinal);
		var idx92 = Html.IndexOf("9.2.0", StringComparison.Ordinal);

		idx910.Should().BeLessThan(idx93, "9.10.0 should appear before 9.3.0");
		idx93.Should().BeLessThan(idx92, "9.3.0 should appear before 9.2.0");
	}

	[Fact]
	public void RendersAllVersions()
	{
		Html.Should().Contain("9.10.0");
		Html.Should().Contain("9.3.0");
		Html.Should().Contain("9.2.0");
	}
}

public class ChangelogCustomPathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogCustomPathTests(ITestOutputHelper output) : base(output,
"""
:::{changelog} release-notes/bundles
:::
""") => FileSystem.AddFile("docs/release-notes/bundles/1.0.0.yaml", new MockFileData(
"""
products:
- product: my-product
  target: 1.0.0
entries:
- title: First release
  type: feature
  products:
  - product: my-product
    target: 1.0.0
  pr: "1"
"""));

	[Fact]
	public void FindsBundlesFolder() => Block!.Found.Should().BeTrue();

	[Fact]
	public void SetsCorrectBundlesFolderPath() => Block!.BundlesFolderPath.Should().EndWith("release-notes/bundles");

	[Fact]
	public void RendersContent()
	{
		Html.Should().Contain("1.0.0");
		Html.Should().Contain("First release");
	}
}

public class ChangelogNotFoundTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
"""
:::{changelog} missing-bundles
:::
""")
{
	[Fact]
	public void ReportsFolderNotFound() => Block!.Found.Should().BeFalse();

	[Fact]
	public void EmitsErrorForMissingFolder()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("does not exist"));
	}
}

public class ChangelogDefaultPathMissingTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
"""
:::{changelog}
:::
""")
{
	[Fact]
	public void EmitsErrorForMissingDefaultFolder()
	{
		// No bundles folder created, so it should emit an error
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("does not exist"));
	}
}

public class ChangelogWithBreakingChangesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogWithBreakingChangesTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Breaking change in API
  type: breaking-change
  products:
  - product: elasticsearch
    target: 9.3.0
  description: The API has changed significantly.
  impact: Users must update their code.
  action: Follow the migration guide.
  pr: "222222"
"""));

	[Fact]
	public void RendersBreakingChangesSection()
	{
		Html.Should().Contain("Breaking changes");
		Html.Should().Contain("Breaking change in API");
	}

	[Fact]
	public void RendersImpactAndAction()
	{
		Html.Should().Contain("Impact");
		Html.Should().Contain("Users must update their code");
		Html.Should().Contain("Action");
		Html.Should().Contain("Follow the migration guide");
	}
}

public class ChangelogWithDeprecationsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogWithDeprecationsTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Deprecated old API
  type: deprecation
  products:
  - product: elasticsearch
    target: 9.3.0
  description: The old API is deprecated.
  impact: The API will be removed in a future version.
  action: Use the new API instead.
  pr: "333333"
"""));

	[Fact]
	public void RendersDeprecationsSection()
	{
		Html.Should().Contain("Deprecations");
		Html.Should().Contain("Deprecated old API");
	}
}

public class ChangelogEmptyBundleTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogEmptyBundleTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries: []
"""));

	[Fact]
	public void HandlesEmptyBundle()
	{
		Html.Should().Contain("No new features, enhancements, or fixes");
	}
}

public class ChangelogEmptyFolderTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogEmptyFolderTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") =>
		// Create the folder but don't add any YAML files
		FileSystem.AddDirectory("docs/changelog/bundles");

	[Fact]
	public void ReportsFolderEmpty() => Block!.Found.Should().BeFalse();

	[Fact]
	public void EmitsErrorForEmptyFolder()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("contains no YAML files"));
	}
}

public class ChangelogAbsolutePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogAbsolutePathTests(ITestOutputHelper output) : base(output,
"""
:::{changelog} /release-notes/bundles
:::
""") => FileSystem.AddFile("docs/release-notes/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Test feature
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  pr: "444444"
"""));

	[Fact]
	public void FindsBundlesFolderWithAbsolutePath() => Block!.Found.Should().BeTrue();

	[Fact]
	public void SetsCorrectBundlesFolderPath() => Block!.BundlesFolderPath.Should().Contain("release-notes");
}
