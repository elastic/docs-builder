// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Changelog;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class ChangelogConfigLoadAutoDiscoverTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigLoadAutoDiscoverTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:::
		"""
		)
	{
		// Create bundles with entries of different types
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			- title: Deprecation notice
			  type: deprecation
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: This API is deprecated.
			  impact: Users should migrate.
			  action: Use the new API.
			  prs:
			  - "222222"
			- title: Known issue
			  type: known-issue
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: There is a known issue.
			  impact: Some users may be affected.
			  prs:
			  - "333333"
			"""
			)
		);

		// Add changelog config with publish blockers
		FileSystem.AddFile(
			"docs/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_types:
			      - deprecation
			      - known-issue
			"""
			)
		);
	}

	[Test]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class ChangelogConfigLoadExplicitPathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigLoadExplicitPathTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:config: custom/path/my-changelog.yml
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			- title: Internal docs
			  type: docs
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Internal
			  prs:
			  - "222222"
			"""
			)
		);

		// Add custom config at explicit path
		FileSystem.AddFile(
			"docs/custom/path/my-changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_areas:
			      - Internal
			"""
			)
		);
	}

	[Test]
	public void ConfigPathPropertyIsSet() => Block!.ConfigPath.Should().Be("custom/path/my-changelog.yml");

	[Test]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class ChangelogConfigLoadFromDocsSubfolderTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigLoadFromDocsSubfolderTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			- title: Other change
			  type: other
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "222222"
			"""
			)
		);

		// Add config in docs/docs/changelog.yml (docs subfolder)
		FileSystem.AddFile(
			"docs/docs/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_types:
			      - other
			"""
			)
		);
	}

	[Test]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class ChangelogConfigNotFoundTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigNotFoundTests() : base(
			// language=markdown
			"""
		:::{changelog}
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
		- title: Regular feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		"""
			)
		);

	[Test]
	public void PublishBlockerIsNullWhenNoConfig() => Block!.PublishBlocker.Should().BeNull();

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
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Regular feature.
				<a href="https://github.com/elastic/elasticsearch/pull/111111">#111111</a>
			</li>
		</ul>
		"""
		);

	[Test]
	public void NoErrorsEmittedForMissingConfig() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class ChangelogConfigExplicitPathNotFoundTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigExplicitPathNotFoundTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:config: nonexistent/config.yml
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
		- title: Regular feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		"""
			)
		);

	[Test]
	public void PublishBlockerIsNullWhenExplicitConfigNotFound() => Block!.PublishBlocker.Should().BeNull();

	[Test]
	public void EmitsWarningForMissingExplicitConfig() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("warning {repo}/docs/index.md:3 Specified changelog config path 'nonexistent/config.yml' not found.");

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
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Regular feature.
				<a href="https://github.com/elastic/elasticsearch/pull/111111">#111111</a>
			</li>
		</ul>
		"""
		);
}

[InheritsTests]
public class ChangelogConfigPriorityTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigPriorityTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			- title: Deprecation notice
			  type: deprecation
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: Deprecated.
			  impact: None.
			  action: Upgrade.
			  prs:
			  - "222222"
			- title: Other change
			  type: other
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "333333"
			"""
			)
		);

		// Add both config files - root should take priority
		FileSystem.AddFile(
			"docs/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_types:
			      - deprecation
			"""
			)
		);

		FileSystem.AddFile(
			"docs/docs/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_types:
			      - other
			"""
			)
		);
	}

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class ChangelogConfigEmptyBlockTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigEmptyBlockTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			"""
			)
		);

		// Config file exists but has no block section
		FileSystem.AddFile(
			"docs/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			lifecycles:
			  - preview
			  - beta
			  - ga
			  - experimental
			"""
			)
		);
	}

	[Test]
	public void PublishBlockerIsNullWhenNoBlockSection() => Block!.PublishBlocker.Should().BeNull();

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
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Regular feature.
				<a href="https://github.com/elastic/elasticsearch/pull/111111">#111111</a>
			</li>
		</ul>
		"""
		);
}

[InheritsTests]
public class ChangelogConfigMixedBlockersTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigMixedBlockersTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Regular feature in Search
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Search
			  prs:
			  - "111111"
			- title: Deprecation in Search
			  type: deprecation
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Search
			  description: Deprecated.
			  impact: None.
			  action: Upgrade.
			  prs:
			  - "222222"
			- title: Feature in Internal
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Internal
			  prs:
			  - "333333"
			- title: Bug fix
			  type: bug-fix
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "444444"
			"""
			)
		);

		// Config with both type and area blockers
		FileSystem.AddFile(
			"docs/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_types:
			      - deprecation
			    exclude_areas:
			      - Internal
			"""
			)
		);
	}

	[Test]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class ChangelogProductFallbackSingleProductTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
		:::{changelog}
		:::
		"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: kibana
			  target: 9.3.0
			entries:
			- title: Regular Kibana feature
			  type: feature
			  products:
			  - product: kibana
			    target: 9.3.0
			  prs:
			  - "111111"
			- title: Internal feature
			  type: feature
			  products:
			  - product: kibana
			    target: 9.3.0
			  areas:
			  - Internal
			  prs:
			  - "222222"
			- title: Observability feature
			  type: feature
			  products:
			  - product: kibana
			    target: 9.3.0
			  areas:
			  - Elastic Observability
			  prs:
			  - "333333"
			"""
			)
		);

		// Config with product-specific blocker for kibana
		fileSystem.AddFile(
			"docs/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_areas:
			      - Global Area
			    products:
			      kibana:
			        exclude_areas:
			          - Internal
			          - Elastic Observability
			"""
			)
		);
	}

	protected override IReadOnlyList<string>? GetDocsetProducts() => ["kibana"];

	[Test]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class ChangelogProductFallbackMultipleProductsTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
		:::{changelog}
		:::
		"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			- title: Internal feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Internal
			  prs:
			  - "222222"
			"""
			)
		);

		// Config with product-specific blockers
		fileSystem.AddFile(
			"docs/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_areas:
			      - Global Area
			    products:
			      elasticsearch:
			        exclude_areas:
			          - Internal
			"""
			)
		);
	}

	// Docset with multiple products - should fall back to global blocker
	protected override IReadOnlyList<string>? GetDocsetProducts() => ["elasticsearch", "kibana"];

	[Test]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

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
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Regular feature.
				<a href="https://github.com/elastic/elasticsearch/pull/111111">#111111</a>
			</li>
			<li>Internal feature.
				<a href="https://github.com/elastic/elasticsearch/pull/222222">#222222</a>
			</li>
		</ul>
		"""
		);
}

[InheritsTests]
public class ChangelogProductExplicitOptionOverridesDocsetTests() : DirectiveTest<ChangelogBlock>(
	// language=markdown
	"""
		:::{changelog}
		:product: elasticsearch
		:::
		"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			- title: ES Internal feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - ES Internal
			  prs:
			  - "222222"
			- title: Kibana Internal feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Kibana Internal
			  prs:
			  - "333333"
			"""
			)
		);

		// Config with different blockers for different products
		fileSystem.AddFile(
			"docs/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    products:
			      elasticsearch:
			        exclude_areas:
			          - ES Internal
			      kibana:
			        exclude_areas:
			          - Kibana Internal
			"""
			)
		);
	}

	// Docset has kibana as single product, but directive explicitly requests elasticsearch
	protected override IReadOnlyList<string>? GetDocsetProducts() => ["kibana"];

	[Test]
	public void ExplicitProductOptionIsSet() => Block!.ProductId.Should().Be("elasticsearch");

	[Test]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Test]
	public void EmitsDeprecationWarningForProductOption() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"warning {repo}/docs/index.md:3 The :product: option is deprecated and has no effect. The directive does not apply rules.publish. Move type/area filtering to rules.bundle so it applies at bundle time."
			);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}
