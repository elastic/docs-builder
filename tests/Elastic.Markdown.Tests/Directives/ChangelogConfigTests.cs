// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using AwesomeAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogConfigLoadAutoDiscoverTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigLoadAutoDiscoverTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
		:::
		""")
	{
		// Create bundles with entries of different types
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
			"""));

		// Add changelog config with publish blockers
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_types:
			      - deprecation
			      - known-issue
			"""));
	}

	[Fact]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Regular feature");
		Html.Should().Contain("Deprecation notice");
		Html.Should().Contain("Known issue");
	}

	[Fact]
	public void RendersFeaturesSection() => Html.Should().Contain("Features and enhancements");

	[Fact]
	public void RendersDeprecationsSection() => Html.Should().Contain("Deprecations");

	[Fact]
	public void RendersKnownIssuesSection() => Html.Should().Contain("Known issues");
}

public class ChangelogConfigLoadExplicitPathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigLoadExplicitPathTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:config: custom/path/my-changelog.yml
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
			"""));

		// Add custom config at explicit path
		FileSystem.AddFile("docs/custom/path/my-changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_areas:
			      - Internal
			"""));
	}

	[Fact]
	public void ConfigPathPropertyIsSet() => Block!.ConfigPath.Should().Be("custom/path/my-changelog.yml");

	[Fact]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Regular feature");
		Html.Should().Contain("Internal docs");
	}
}

public class ChangelogConfigLoadFromDocsSubfolderTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigLoadFromDocsSubfolderTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
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
			"""));

		// Add config in docs/docs/changelog.yml (docs subfolder)
		FileSystem.AddFile("docs/docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_types:
			      - other
			"""));
	}

	[Fact]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Regular feature");
		Html.Should().Contain("Other change");
	}
}

public class ChangelogConfigNotFoundTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigNotFoundTests(ITestOutputHelper output) : base(output,
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
		- title: Regular feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		"""));

	[Fact]
	public void PublishBlockerIsNullWhenNoConfig() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void RendersAllEntriesWhenNoConfig() => Html.Should().Contain("Regular feature");

	[Fact]
	public void NoErrorsEmittedForMissingConfig() =>
		Collector.Diagnostics.Should().NotContain(d => d.Message.Contains("changelog.yml"));
}

public class ChangelogConfigExplicitPathNotFoundTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigExplicitPathNotFoundTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:config: nonexistent/config.yml
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		"""));

	[Fact]
	public void PublishBlockerIsNullWhenExplicitConfigNotFound() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void EmitsWarningForMissingExplicitConfig() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("nonexistent/config.yml") && d.Message.Contains("not found"));

	[Fact]
	public void RendersAllEntriesWhenConfigNotFound() => Html.Should().Contain("Regular feature");
}

public class ChangelogConfigPriorityTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigPriorityTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
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
			"""));

		// Add both config files - root should take priority
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_types:
			      - deprecation
			"""));

		FileSystem.AddFile("docs/docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_types:
			      - other
			"""));
	}

	[Fact]
	public void RendersAllEntries_NoPublishFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Regular feature");
		Html.Should().Contain("Deprecation notice");
		Html.Should().Contain("Other change");
	}
}

public class ChangelogConfigEmptyBlockTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigEmptyBlockTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
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
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			"""));

		// Config file exists but has no block section
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			lifecycles:
			  - preview
			  - beta
			  - ga
			"""));
	}

	[Fact]
	public void PublishBlockerIsNullWhenNoBlockSection() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void RendersAllEntriesWhenNoBlockSection() => Html.Should().Contain("Regular feature");
}

public class ChangelogConfigMixedBlockersTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigMixedBlockersTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
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
			"""));

		// Config with both type and area blockers
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_types:
			      - deprecation
			    exclude_areas:
			      - Internal
			"""));
	}

	[Fact]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Regular feature in Search");
		Html.Should().Contain("Deprecation in Search");
		Html.Should().Contain("Feature in Internal");
		Html.Should().Contain("Bug fix");
	}
}

public class ChangelogProductFallbackSingleProductTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	// language=markdown
	"""
		:::{changelog}
		:::
		""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
			"""));

		// Config with product-specific blocker for kibana
		fileSystem.AddFile("docs/changelog.yml", new MockFileData(
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
			"""));
	}

	protected override IReadOnlyList<string>? GetDocsetProducts() => ["kibana"];

	[Fact]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Regular Kibana feature");
		Html.Should().Contain("Internal feature");
		Html.Should().Contain("Observability feature");
	}
}

public class ChangelogProductFallbackMultipleProductsTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	// language=markdown
	"""
		:::{changelog}
		:::
		""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
			"""));

		// Config with product-specific blockers
		fileSystem.AddFile("docs/changelog.yml", new MockFileData(
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
			"""));
	}

	// Docset with multiple products - should fall back to global blocker
	protected override IReadOnlyList<string>? GetDocsetProducts() => ["elasticsearch", "kibana"];

	[Fact]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Regular feature");
		Html.Should().Contain("Internal feature");
	}
}

public class ChangelogProductExplicitOptionOverridesDocsetTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
	// language=markdown
	"""
		:::{changelog}
		:product: elasticsearch
		:::
		""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
			"""));

		// Config with different blockers for different products
		fileSystem.AddFile("docs/changelog.yml", new MockFileData(
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
			"""));
	}

	// Docset has kibana as single product, but directive explicitly requests elasticsearch
	protected override IReadOnlyList<string>? GetDocsetProducts() => ["kibana"];

	[Fact]
	public void ExplicitProductOptionIsSet() =>
		Block!.ProductId.Should().Be("elasticsearch");

	[Fact]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Fact]
	public void EmitsDeprecationWarningForProductOption() =>
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains(":product:") && d.Message.Contains("deprecated"));

	[Fact]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Regular feature");
		Html.Should().Contain("ES Internal feature");
		Html.Should().Contain("Kibana Internal feature");
	}
}
