// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests for the :dropdowns: parameter on the changelog directive.
/// By default (omitted), separated types (breaking changes, deprecations, known issues, highlights) 
/// are rendered as flattened bulleted lists.
/// With :dropdowns:, they render as Myst dropdown sections.
/// </summary>
public class ChangelogDropdownsDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsDefaultTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: breaking-change
		:description-visibility: keep-descriptions
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API has been changed to improve performance.
		  impact: Existing API calls will fail.
		  action: Update your code to use the new API endpoints.
		  prs:
		  - "333333"
		- title: Another breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Removed deprecated parameter.
		  impact: Scripts using the old parameter will fail.
		  action: Remove references to the deprecated parameter.
		  prs:
		  - "444444"
		"""));

	[Fact]
	public void DefaultBehaviorDoesNotParseDropdownsOption()
	{
		Block!.DropdownsEnabled.Should().BeFalse();
	}

	[Fact]
	public void DefaultBehaviorRendersFlattened()
	{
		// Should NOT contain dropdown HTML structure
		Html.Should().NotContain("<details class=\"dropdown\">");
		Html.Should().NotContain("dropdown-title__summary-text");

		// Should contain bulleted list format without bold titles (matching regular entries)
		Html.Should().Contain("Breaking API change.");
		Html.Should().Contain("Another breaking change.");
	}

	[Fact]
	public void DefaultBehaviorIncludesImpactAndActionSections()
	{
		Html.Should().Contain("<strong>Impact:</strong> Existing API calls will fail.");
		Html.Should().Contain("<strong>Action:</strong> Update your code to use the new API endpoints.");
		Html.Should().Contain("<strong>Impact:</strong> Scripts using the old parameter will fail.");
		Html.Should().Contain("<strong>Action:</strong> Remove references to the deprecated parameter.");
	}

	[Fact]
	public void DefaultBehaviorIncludesDescriptions()
	{
		// Note: Descriptions may be hidden by default due to :description-visibility: auto behavior
		// This test validates that when descriptions are shown, they appear in the correct format
		Html.Should().Contain("API has been changed to improve performance.");
		Html.Should().Contain("Removed deprecated parameter.");
	}
}

/// <summary>
/// Tests for explicit :dropdowns: - should render separated types as Myst dropdown sections.
/// </summary>
public class ChangelogDropdownsEnabledTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsEnabledTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: breaking-change
		:dropdowns:
		:description-visibility: keep-descriptions
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API has been changed to improve performance.
		  impact: Existing API calls will fail.
		  action: Update your code to use the new API endpoints.
		  prs:
		  - "333333"
		"""));

	[Fact]
	public void ExplicitDropdownsParsesCorrectly()
	{
		Block!.DropdownsEnabled.Should().BeTrue();
	}

	[Fact]
	public void ExplicitDropdownsRendersDropdownFormat()
	{
		// Should contain dropdown HTML structure
		Html.Should().Contain("<details class=\"dropdown\">");
		Html.Should().Contain("dropdown-title__summary-text");
		Html.Should().Contain("Breaking API change.");

		// Should NOT contain bulleted list format
		Html.Should().NotContain("<li><p>Breaking API change.");
	}

	[Fact]
	public void ExplicitDropdownsIncludesDescriptionInDropdown()
	{
		Html.Should().Contain("API has been changed to improve performance.");
	}

	[Fact]
	public void ExplicitDropdownsIncludesImpactAndActionInDropdown()
	{
		Html.Should().Contain("<strong>Impact</strong><br>Existing API calls will fail.");
		Html.Should().Contain("<strong>Action</strong><br>Update your code to use the new API endpoints.");
	}
}

/// <summary>
/// Tests interaction between :dropdowns: and :description-visibility: for flattened rendering.
/// </summary>
public class ChangelogDropdownsWithHiddenDescriptionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsWithHiddenDescriptionsTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: breaking-change
		:description-visibility: hide-descriptions
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: This description should be hidden.
		  impact: Existing API calls will fail.
		  action: Update your code to use the new API endpoints.
		  prs:
		  - "333333"
		"""));

	[Fact]
	public void FlattendRenderingHidesDescriptionsButKeepsImpactAction()
	{
		// Should render as flattened (no dropdowns by default)
		Html.Should().Contain("Breaking API change.");
		Html.Should().NotContain("<details class=\"dropdown\">");

		// Description should be hidden due to :description-visibility: hide-descriptions
		Html.Should().NotContain("This description should be hidden.");

		// Impact and Action should still be shown in flattened format
		Html.Should().Contain("<strong>Impact:</strong> Existing API calls will fail.");
		Html.Should().Contain("<strong>Action:</strong> Update your code to use the new API endpoints.");
	}
}

/// <summary>
/// Tests interaction between :dropdowns: and :description-visibility: for dropdown rendering.
/// </summary>
public class ChangelogDropdownsEnabledWithHiddenDescriptionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsEnabledWithHiddenDescriptionsTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: breaking-change
		:dropdowns:
		:description-visibility: hide-descriptions
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: This description should be hidden.
		  impact: Existing API calls will fail.
		  action: Update your code to use the new API endpoints.
		  prs:
		  - "333333"
		"""));

	[Fact]
	public void DropdownRenderingHidesDescriptionsButKeepsImpactAction()
	{
		// Should render as dropdown due to explicit :dropdowns:
		Html.Should().Contain("<details class=\"dropdown\">");
		Html.Should().Contain("Breaking API change.");
		Html.Should().NotContain("<li><p>Breaking API change.");

		// Description should be hidden due to :description-visibility: hide-descriptions
		Html.Should().NotContain("This description should be hidden.");

		// Impact and Action should still be shown in dropdown format
		Html.Should().Contain("<strong>Impact</strong><br>Existing API calls will fail.");
		Html.Should().Contain("<strong>Action</strong><br>Update your code to use the new API endpoints.");
	}
}

/// <summary>
/// Tests that :dropdowns: works with different separated types (deprecations, known issues).
/// </summary>
public class ChangelogDropdownsWithDifferentTypesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsWithDifferentTypesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature addition
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Added a new feature.
		  prs:
		  - "111111"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  prs:
		  - "222222"
		- title: Known issue with search
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Search may fail in some cases.
		  impact: Search results incomplete.
		  action: Use workaround.
		  prs:
		  - "333333"
		- title: Deprecated API
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Old API is deprecated.
		  impact: Will be removed in future.
		  action: Use new API.
		  prs:
		  - "444444"
		"""));

	[Fact]
	public void DefaultRendersMixedTypesCorrectly()
	{
		// Regular types should render as bulleted lists (unchanged behavior)
		Html.Should().Contain("Feature addition.");  // Regular feature type (in <li> tags)

		// Separated types should render as flattened lists (new behavior) - no bold titles
		Html.Should().Contain("Breaking API change.");
		Html.Should().Contain("Known issue with search.");
		Html.Should().Contain("Deprecated API.");

		// Should NOT contain dropdown HTML structure for separated types
		Html.Should().NotContain("<details class=\"dropdown\">");
	}
}

/// <summary>
/// Tests that :dropdowns: works with different separated types using explicit dropdown rendering.
/// </summary>
public class ChangelogDropdownsExplicitWithDifferentTypesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsExplicitWithDifferentTypesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
		:dropdowns:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Feature addition
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Added a new feature.
		  prs:
		  - "111111"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  prs:
		  - "222222"
		- title: Known issue with search
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Search may fail in some cases.
		  impact: Search results incomplete.
		  action: Use workaround.
		  prs:
		  - "333333"
		"""));

	[Fact]
	public void ExplicitDropdownsRendersMixedTypesCorrectly()
	{
		// Regular types should still render as bulleted lists (unchanged behavior)
		Html.Should().Contain("Feature addition.");  // Regular feature type (in <li> tags)

		// Separated types should render as dropdowns (explicit :dropdowns:)
		Html.Should().Contain("<details class=\"dropdown\">");
		Html.Should().Contain("Breaking API change.");
		Html.Should().Contain("Known issue with search.");

		// Should NOT contain flattened format for separated types (check they're in dropdown, not flat list)
		Html.Should().NotContain("<li><p>Breaking API change.");
		Html.Should().NotContain("<li><p>Known issue with search.");
	}
}
