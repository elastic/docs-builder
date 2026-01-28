// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogHideLinksDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideLinksDefaultTests(ITestOutputHelper output) : base(output,
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
		- title: Feature with PR and issues
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "123456"
		  issues:
		  - "78901"
		  - "78902"
		"""));

	[Fact]
	public void HideLinksPropertyDefaultsToFalse() => Block!.HideLinks.Should().BeFalse();

	[Fact]
	public void RendersPrLinksWhenNotHidden()
	{
		// PR link should be visible in the output
		Html.Should().Contain("123456");
		Html.Should().Contain("github.com");
	}

	[Fact]
	public void RendersIssueLinksWhenNotHidden()
	{
		// Issue links should be visible
		Html.Should().Contain("78901");
		Html.Should().Contain("78902");
	}
}

public class ChangelogHideLinksEnabledTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideLinksEnabledTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-links:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature with PR and issues
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "123456"
		  issues:
		  - "78901"
		  - "78902"
		"""));

	[Fact]
	public void HideLinksPropertyIsTrue() => Block!.HideLinks.Should().BeTrue();

	[Fact]
	public void RendersEntryTitle() => Html.Should().Contain("Feature with PR and issues");

	[Fact]
	public void HidesPrLinksAsComments() =>
		// When hide-links is enabled, links should be commented out
		// The ChangelogTextUtilities.FormatPrLink with hidePrivateLinks=true returns "% [#123456](...)"
		Html.Should().NotContain("<a href=\"https://github.com/elastic/elasticsearch/pull/123456\"");

	[Fact]
	public void HidesIssueLinksAsComments()
	{
		// Issue links should also be commented out
		Html.Should().NotContain("<a href=\"https://github.com/elastic/elasticsearch/issues/78901\"");
		Html.Should().NotContain("<a href=\"https://github.com/elastic/elasticsearch/issues/78902\"");
	}
}

public class ChangelogHideLinksExplicitFalseTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideLinksExplicitFalseTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-links: false
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature with PR
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "123456"
		"""));

	[Fact]
	public void HideLinksPropertyIsFalse() => Block!.HideLinks.Should().BeFalse();

	[Fact]
	public void RendersPrLinksWhenExplicitlyNotHidden()
	{
		Html.Should().Contain("123456");
		Html.Should().Contain("github.com");
	}
}

public class ChangelogHideLinksInDetailedEntriesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideLinksInDetailedEntriesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-links:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Breaking change with PR
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API has changed.
		  impact: Users must update.
		  action: Follow migration guide.
		  pr: "999888"
		  issues:
		  - "777666"
		- title: Deprecation with PR
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Old API deprecated.
		  impact: Will be removed.
		  action: Use new API.
		  pr: "555444"
		"""));

	[Fact]
	public void HidesLinksInBreakingChangesSection()
	{
		// The breaking change section should have the links hidden
		Html.Should().Contain("Breaking change with PR");
		Html.Should().NotContain("<a href=\"https://github.com/elastic/elasticsearch/pull/999888\"");
	}

	[Fact]
	public void HidesLinksInDeprecationsSection()
	{
		// The deprecation section should have the links hidden
		Html.Should().Contain("Deprecation with PR");
		Html.Should().NotContain("<a href=\"https://github.com/elastic/elasticsearch/pull/555444\"");
	}

	[Fact]
	public void RendersImpactAndActionSections()
	{
		// Impact and action should still be rendered
		Html.Should().Contain("Impact");
		Html.Should().Contain("Users must update");
		Html.Should().Contain("Action");
		Html.Should().Contain("Follow migration guide");
	}
}

public class ChangelogHideLinksWithMultipleEntriesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideLinksWithMultipleEntriesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-links:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature one
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "111111"
		- title: Feature two
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "222222"
		- title: Bug fix
		  type: bug-fix
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "333333"
		  issues:
		  - "444444"
		"""));

	[Fact]
	public void RendersAllEntryTitles()
	{
		Html.Should().Contain("Feature one");
		Html.Should().Contain("Feature two");
		Html.Should().Contain("Bug fix");
	}

	[Fact]
	public void HidesAllPrLinks()
	{
		// None of the PR links should be rendered as clickable links
		Html.Should().NotContain("<a href=\"https://github.com/elastic/elasticsearch/pull/111111\"");
		Html.Should().NotContain("<a href=\"https://github.com/elastic/elasticsearch/pull/222222\"");
		Html.Should().NotContain("<a href=\"https://github.com/elastic/elasticsearch/pull/333333\"");
	}

	[Fact]
	public void HidesAllIssueLinks() =>
		Html.Should().NotContain("<a href=\"https://github.com/elastic/elasticsearch/issues/444444\"");
}
