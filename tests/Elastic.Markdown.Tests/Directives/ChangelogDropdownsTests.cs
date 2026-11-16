// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Changelog;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests for the :dropdowns: parameter on the changelog directive.
/// By default (omitted), separated types (breaking changes, deprecations, known issues, highlights) 
/// are rendered as flattened bulleted lists.
/// With :dropdowns:, they render as Myst dropdown sections.
/// </summary>
[InheritsTests]
public class ChangelogDropdownsDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsDefaultTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: breaking-change
		:description-visibility: keep-descriptions
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
		"""
			)
		);

	[Test]
	public void DefaultBehaviorDoesNotParseDropdownsOption() => Block!.DropdownsEnabled.Should().BeFalse();

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests for explicit :dropdowns: - should render separated types as Myst dropdown sections.
/// </summary>
[InheritsTests]
public class ChangelogDropdownsEnabledTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsEnabledTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: breaking-change
		:dropdowns:
		:description-visibility: keep-descriptions
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
		"""
			)
		);

	[Test]
	public void ExplicitDropdownsParsesCorrectly() => Block!.DropdownsEnabled.Should().BeTrue();

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests interaction between :dropdowns: and :description-visibility: for flattened rendering.
/// </summary>
[InheritsTests]
public class ChangelogDropdownsWithHiddenDescriptionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsWithHiddenDescriptionsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: breaking-change
		:description-visibility: hide-descriptions
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
		"""
			)
		);

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="9.3.0">
			<h2>
				<a class="headerlink" href="#9.3.0">9.3.0</a>
			</h2>
		</div>
		<ul>
			<li>
				<p>Breaking API change.
					<a href="https://github.com/elastic/elasticsearch/pull/333333">#333333</a>
				</p>
				<p>
					<strong>Impact:</strong>
					Existing API calls will fail.</p>
				<p>
					<strong>Action:</strong>
					Update your code to use the new API endpoints.</p>
			</li>
		</ul>
		"""
		);
}

/// <summary>
/// Tests interaction between :dropdowns: and :description-visibility: for dropdown rendering.
/// </summary>
[InheritsTests]
public class ChangelogDropdownsEnabledWithHiddenDescriptionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsEnabledWithHiddenDescriptionsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: breaking-change
		:dropdowns:
		:description-visibility: hide-descriptions
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
		"""
			)
		);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests that :dropdowns: works with different separated types (deprecations, known issues).
/// </summary>
[InheritsTests]
public class ChangelogDropdownsWithDifferentTypesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsWithDifferentTypesTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
		"""
			)
		);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests that :dropdowns: works with different separated types using explicit dropdown rendering.
/// </summary>
[InheritsTests]
public class ChangelogDropdownsExplicitWithDifferentTypesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsExplicitWithDifferentTypesTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:dropdowns:
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
		"""
			)
		);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Changelog-generated dropdown titles pass through the dropdown parser, which strips inline markdown markers.
/// </summary>
[InheritsTests]
public class ChangelogDropdownsPlainTextTitleTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDropdownsPlainTextTitleTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: known-issue
		:dropdowns:
		:description-visibility: keep-descriptions
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: "The `ElasticAgentVersion` parameter is malformed"
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: The default value has a space instead of a plus sign.
		  impact: Deployments fail.
		  action: Update the template.
		  prs:
		  - "242365"
		"""
			)
		);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}
