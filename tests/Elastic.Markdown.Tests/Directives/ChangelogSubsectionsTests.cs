// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogSubsectionsDisabledByDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSubsectionsDisabledByDefaultTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature in Search
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Search
		  prs:
		  - "111111"
		- title: Feature in Indexing
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Indexing
		  prs:
		  - "222222"
		"""));

	[Fact]
	public void SubsectionsPropertyDefaultsToFalse() => Block!.Subsections.Should().BeFalse();

	[Fact]
	public void DoesNotRenderAreaHeaders()
	{
		// When subsections is false, area headers should not be rendered
		Html.Should().NotContain("<strong>Search</strong>");
		Html.Should().NotContain("<strong>Indexing</strong>");
	}

	[Fact]
	public void RendersEntriesWithoutGrouping()
	{
		// Both entries should be rendered without area grouping
		Html.Should().Contain("Feature in Search");
		Html.Should().Contain("Feature in Indexing");
	}
}

public class ChangelogSubsectionsEnabledTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSubsectionsEnabledTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:subsections:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature in Search
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Search
		  prs:
		  - "111111"
		- title: Feature in Indexing
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Indexing
		  prs:
		  - "222222"
		"""));

	[Fact]
	public void SubsectionsPropertyIsTrue() => Block!.Subsections.Should().BeTrue();

	[Fact]
	public void RendersAreaHeaders()
	{
		// When subsections is true, area headers should be rendered
		Html.Should().Contain("<strong>Search</strong>");
		Html.Should().Contain("<strong>Indexing</strong>");
	}

	[Fact]
	public void RendersEntriesUnderCorrectAreas()
	{
		// Both entries should be rendered
		Html.Should().Contain("Feature in Search");
		Html.Should().Contain("Feature in Indexing");
	}
}

public class ChangelogSubsectionsExplicitFalseTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSubsectionsExplicitFalseTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:subsections: false
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature in Search
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Search
		  prs:
		  - "111111"
		"""));

	[Fact]
	public void SubsectionsPropertyIsFalse() => Block!.Subsections.Should().BeFalse();

	[Fact]
	public void DoesNotRenderAreaHeaders() => Html.Should().NotContain("<strong>Search</strong>");
}

/// <summary>
/// Tests that when :subsections: is enabled and publish rules with include_areas or exclude_areas
/// are active, entries with multiple areas are grouped under the first area that aligns with those rules.
/// </summary>
/// <summary>
/// Tests that when :subsections: is enabled and no publish rules with areas exist,
/// entries with multiple areas use the first area (unchanged behavior).
/// </summary>
public class ChangelogSubsectionsNoAreaRulesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSubsectionsNoAreaRulesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:subsections:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Multi-area feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Search
		  - Monitoring
		  - Security
		  pr: "111111"
		"""));

	[Fact]
	public void GroupsUnderFirstArea()
	{
		// No publish rules with areas → use first area
		Html.Should().Contain("<strong>Search</strong>");
		Html.Should().Contain("Multi-area feature");
	}
}
