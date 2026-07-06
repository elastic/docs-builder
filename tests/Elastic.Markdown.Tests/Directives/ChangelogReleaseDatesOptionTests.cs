// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>Tests for the <c>:release-dates:</c> directive option.</summary>
public class ChangelogReleaseDatesOptionDefaultOffTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDatesOptionDefaultOffTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/1.34.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: apm-agent-dotnet
			  target: 1.34.0
			release-date: "2026-04-09"
			entries:
			- title: Add tracing improvements
			  type: feature
			  products:
			  - product: apm-agent-dotnet
			    target: 1.34.0
			  prs:
			  - "500"
			"""));

	[Fact]
	public void ReleaseDatesDisabledByDefault() =>
		Block!.ReleaseDatesEnabled.Should().BeFalse();

	[Fact]
	public void OmitsReleasedLineWhenFlagOmitted() =>
		Html.Should().NotContain("Released:");

	[Fact]
	public void StillRendersEntries() =>
		Html.Should().Contain("Add tracing improvements");
}

public class ChangelogReleaseDatesOptionEnabledTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDatesOptionEnabledTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:release-dates:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/1.34.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: apm-agent-dotnet
			  target: 1.34.0
			release-date: "2026-04-09"
			entries:
			- title: Add tracing improvements
			  type: feature
			  products:
			  - product: apm-agent-dotnet
			    target: 1.34.0
			  prs:
			  - "500"
			"""));

	[Fact]
	public void ReleaseDatesEnabledWhenFlagPresent() =>
		Block!.ReleaseDatesEnabled.Should().BeTrue();

	[Fact]
	public void RendersReleasedLineWhenBundleHasReleaseDate() =>
		Html.Should().Contain("Released: April 9, 2026");
}

public class ChangelogReleaseDatesOptionEnabledWithoutBundleDateTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDatesOptionEnabledWithoutBundleDateTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:release-dates:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: New feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "100"
			"""));

	[Fact]
	public void OmitsReleasedLineWhenBundleHasNoReleaseDate() =>
		Html.Should().NotContain("Released:");
}

public class ChangelogReleaseDatesOptionDescriptionStillRendersTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogReleaseDatesOptionDescriptionStillRendersTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/1.34.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: apm-agent-dotnet
			  target: 1.34.0
			release-date: "2026-04-09"
			description: |
			  This release includes tracing improvements and bug fixes.
			entries:
			- title: Add tracing improvements
			  type: feature
			  products:
			  - product: apm-agent-dotnet
			    target: 1.34.0
			  prs:
			  - "500"
			"""));

	[Fact]
	public void OmitsReleasedLineWhenFlagOmitted() =>
		Html.Should().NotContain("Released:");

	[Fact]
	public void RendersBundleDescription() =>
		Html.Should().Contain("This release includes tracing improvements and bug fixes.");
}
