// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogConfigLoadAutoDiscoverTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigLoadAutoDiscoverTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
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
			  pr: "111111"
			- title: Deprecation notice
			  type: deprecation
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: This API is deprecated.
			  impact: Users should migrate.
			  action: Use the new API.
			  pr: "222222"
			- title: Known issue
			  type: known-issue
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: There is a known issue.
			  impact: Some users may be affected.
			  pr: "333333"
			"""));

		// Add changelog config with publish blockers
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    types:
			      - deprecation
			      - known-issue
			"""));
	}

	[Fact]
	public void LoadsPublishBlockerFromConfig() => Block!.PublishBlocker.Should().NotBeNull();

	[Fact]
	public void PublishBlockerHasCorrectTypes()
	{
		Block!.PublishBlocker!.Types.Should().NotBeNull();
		Block!.PublishBlocker!.Types.Should().Contain("deprecation");
		Block!.PublishBlocker!.Types.Should().Contain("known-issue");
	}

	[Fact]
	public void FilteredEntriesExcludeBlockedTypes()
	{
		// Deprecation and known-issue entries should be filtered out
		Html.Should().Contain("Regular feature");
		Html.Should().NotContain("Deprecation notice");
		Html.Should().NotContain("Known issue");
	}

	[Fact]
	public void RendersFeaturesSection() => Html.Should().Contain("Features and enhancements");

	[Fact]
	public void DoesNotRenderDeprecationsSection() => Html.Should().NotContain("Deprecations");

	[Fact]
	public void DoesNotRenderKnownIssuesSection() => Html.Should().NotContain("Known issues");
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
			  pr: "111111"
			- title: Internal docs
			  type: docs
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Internal
			  pr: "222222"
			"""));

		// Add custom config at explicit path
		FileSystem.AddFile("docs/custom/path/my-changelog.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    areas:
			      - Internal
			"""));
	}

	[Fact]
	public void ConfigPathPropertyIsSet() => Block!.ConfigPath.Should().Be("custom/path/my-changelog.yml");

	[Fact]
	public void LoadsPublishBlockerFromExplicitConfig() => Block!.PublishBlocker.Should().NotBeNull();

	[Fact]
	public void PublishBlockerHasCorrectAreas()
	{
		Block!.PublishBlocker!.Areas.Should().NotBeNull();
		Block!.PublishBlocker!.Areas.Should().Contain("Internal");
	}

	[Fact]
	public void FilteredEntriesExcludeBlockedAreas()
	{
		Html.Should().Contain("Regular feature");
		Html.Should().NotContain("Internal docs");
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
			  pr: "111111"
			- title: Other change
			  type: other
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "222222"
			"""));

		// Add config in docs/docs/changelog.yml (docs subfolder)
		FileSystem.AddFile("docs/docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    types:
			      - other
			"""));
	}

	[Fact]
	public void LoadsPublishBlockerFromDocsSubfolder() => Block!.PublishBlocker.Should().NotBeNull();

	[Fact]
	public void FilteredEntriesExcludeBlockedTypes()
	{
		Html.Should().Contain("Regular feature");
		Html.Should().NotContain("Other change");
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
		  pr: "111111"
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
		  pr: "111111"
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
			  pr: "111111"
			- title: Deprecation notice
			  type: deprecation
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: Deprecated.
			  impact: None.
			  action: Upgrade.
			  pr: "222222"
			- title: Other change
			  type: other
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "333333"
			"""));

		// Add both config files - root should take priority
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    types:
			      - deprecation
			"""));

		FileSystem.AddFile("docs/docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    types:
			      - other
			"""));
	}

	[Fact]
	public void RootConfigTakesPriorityOverDocsSubfolder()
	{
		// Root config blocks deprecation, not other
		Html.Should().Contain("Regular feature");
		Html.Should().NotContain("Deprecation notice");
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
			  pr: "111111"
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
			  pr: "111111"
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
			  pr: "222222"
			- title: Feature in Internal
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Internal
			  pr: "333333"
			- title: Bug fix
			  type: bug-fix
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "444444"
			"""));

		// Config with both type and area blockers
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    types:
			      - deprecation
			    areas:
			      - Internal
			"""));
	}

	[Fact]
	public void PublishBlockerHasBothTypesAndAreas()
	{
		Block!.PublishBlocker.Should().NotBeNull();
		Block!.PublishBlocker!.Types.Should().Contain("deprecation");
		Block!.PublishBlocker!.Areas.Should().Contain("Internal");
	}

	[Fact]
	public void FiltersEntriesMatchingBlockedTypes() => Html.Should().NotContain("Deprecation in Search");

	[Fact]
	public void FiltersEntriesMatchingBlockedAreas() => Html.Should().NotContain("Feature in Internal");

	[Fact]
	public void RendersNonBlockedEntries()
	{
		Html.Should().Contain("Regular feature in Search");
		Html.Should().Contain("Bug fix");
	}
}
