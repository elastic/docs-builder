// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogHideFeaturesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideFeaturesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-features: experimental-api, internal-feature
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Regular feature without feature ID
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "111111"
		- title: Experimental API feature
		  type: feature
		  feature-id: experimental-api
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "222222"
		- title: Internal feature for testing
		  type: enhancement
		  feature-id: internal-feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "333333"
		- title: Another regular feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "444444"
		- title: Feature with different feature ID
		  type: feature
		  feature-id: public-feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "555555"
		"""));

	[Fact]
	public void FeatureIdsToHidePropertyIsPopulated()
	{
		Block!.FeatureIdsToHide.Should().HaveCount(2);
		Block!.FeatureIdsToHide.Should().Contain("experimental-api");
		Block!.FeatureIdsToHide.Should().Contain("internal-feature");
	}

	[Fact]
	public void FiltersEntriesWithMatchingFeatureIds()
	{
		// Entries with hidden feature IDs should not be rendered
		Html.Should().NotContain("Experimental API feature");
		Html.Should().NotContain("Internal feature for testing");
	}

	[Fact]
	public void RendersEntriesWithoutFeatureId()
	{
		// Entries without feature IDs should be rendered
		Html.Should().Contain("Regular feature without feature ID");
		Html.Should().Contain("Another regular feature");
	}

	[Fact]
	public void RendersEntriesWithNonMatchingFeatureId() =>
		// Entries with feature IDs not in the hide list should be rendered
		Html.Should().Contain("Feature with different feature ID");
}

public class ChangelogHideFeaturesDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideFeaturesDefaultTests(ITestOutputHelper output) : base(output,
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
		- title: Feature with feature ID
		  type: feature
		  feature-id: some-feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "111111"
		"""));

	[Fact]
	public void FeatureIdsToHidePropertyIsEmptyByDefault() => Block!.FeatureIdsToHide.Should().BeEmpty();

	[Fact]
	public void RendersAllEntriesWhenNoFeaturesHidden() =>
		Html.Should().Contain("Feature with feature ID");
}

public class ChangelogHideFeaturesCaseInsensitiveTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideFeaturesCaseInsensitiveTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-features: EXPERIMENTAL-API
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature with lowercase feature ID
		  type: feature
		  feature-id: experimental-api
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "111111"
		- title: Regular feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "222222"
		"""));

	[Fact]
	public void FeatureIdMatchingIsCaseInsensitive()
	{
		// Should hide entry even with different case
		Html.Should().NotContain("Feature with lowercase feature ID");
		Html.Should().Contain("Regular feature");
	}
}

public class ChangelogHideFeaturesSingleValueTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideFeaturesSingleValueTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-features: single-feature
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature to hide
		  type: feature
		  feature-id: single-feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "111111"
		- title: Feature to show
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "222222"
		"""));

	[Fact]
	public void ParsesSingleFeatureId() => Block!.FeatureIdsToHide.Should().ContainSingle().Which.Should().Be("single-feature");

	[Fact]
	public void HidesSingleFeatureId()
	{
		Html.Should().NotContain("Feature to hide");
		Html.Should().Contain("Feature to show");
	}
}

public class ChangelogHideFeaturesWithPublishBlockerTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideFeaturesWithPublishBlockerTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-features: experimental-api
		:::
		""")
	{
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature to show
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "111111"
			- title: Feature hidden by feature ID
			  type: feature
			  feature-id: experimental-api
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "222222"
			- title: Deprecation hidden by config
			  type: deprecation
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: Old API deprecated.
			  impact: Will be removed.
			  action: Use new API.
			  pr: "333333"
			"""));

		// Add config with publish blocker
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    types:
			      - deprecation
			"""));
	}

	[Fact]
	public void BothFiltersAreApplied()
	{
		// Feature hidden by feature ID
		Html.Should().NotContain("Feature hidden by feature ID");
		// Feature hidden by publish blocker
		Html.Should().NotContain("Deprecation hidden by config");
		// Regular feature should be shown
		Html.Should().Contain("Feature to show");
	}

	[Fact]
	public void PublishBlockerIsLoaded() => Block!.PublishBlocker.Should().NotBeNull();

	[Fact]
	public void FeatureIdsToHideIsPopulated() => Block!.FeatureIdsToHide.Should().Contain("experimental-api");
}

public class ChangelogHideFeaturesWithWhitespaceTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideFeaturesWithWhitespaceTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-features:   feat-1  ,  feat-2  ,   feat-3
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature 1
		  type: feature
		  feature-id: feat-1
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "111111"
		- title: Feature 2
		  type: feature
		  feature-id: feat-2
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "222222"
		- title: Feature 3
		  type: feature
		  feature-id: feat-3
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "333333"
		- title: Regular feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "444444"
		"""));

	[Fact]
	public void TrimsWhitespaceFromFeatureIds()
	{
		Block!.FeatureIdsToHide.Should().HaveCount(3);
		Block!.FeatureIdsToHide.Should().Contain("feat-1");
		Block!.FeatureIdsToHide.Should().Contain("feat-2");
		Block!.FeatureIdsToHide.Should().Contain("feat-3");
	}

	[Fact]
	public void HidesAllSpecifiedFeatures()
	{
		Html.Should().NotContain("Feature 1");
		Html.Should().NotContain("Feature 2");
		Html.Should().NotContain("Feature 3");
		Html.Should().Contain("Regular feature");
	}
}

public class ChangelogHideFeaturesInBreakingChangesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideFeaturesInBreakingChangesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:hide-features: internal-breaking
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Public breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  pr: "111111"
		- title: Internal breaking change to hide
		  type: breaking-change
		  feature-id: internal-breaking
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Internal API changed.
		  impact: Internal only.
		  action: No action needed.
		  pr: "222222"
		"""));

	[Fact]
	public void FiltersBreakingChangesWithHiddenFeatureId()
	{
		Html.Should().Contain("Public breaking change");
		Html.Should().NotContain("Internal breaking change to hide");
	}

	[Fact]
	public void RendersBreakingChangesSection() => Html.Should().Contain("Breaking changes");
}
