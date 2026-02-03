// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

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
		  pr: "111111"
		- title: Feature in Indexing
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Indexing
		  pr: "222222"
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
		  pr: "111111"
		- title: Feature in Indexing
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Indexing
		  pr: "222222"
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
		  pr: "111111"
		"""));

	[Fact]
	public void SubsectionsPropertyIsFalse() => Block!.Subsections.Should().BeFalse();

	[Fact]
	public void DoesNotRenderAreaHeaders() => Html.Should().NotContain("<strong>Search</strong>");
}
