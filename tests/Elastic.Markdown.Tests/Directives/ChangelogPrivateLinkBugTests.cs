// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests for the bug where entries with only PRIVATE PR/issue references
/// produce incomplete "For more information, check." sentences.
/// </summary>
public class ChangelogPrivateLinkBugTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogPrivateLinkBugTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: deprecation
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: The v1 Costs API has been deprecated. Customers should migrate to the v2 Costs API.
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "# PRIVATE: https://github.com/elastic/cloud/pull/153728"
		  description: This API will be removed in a future version.
		  impact: Users must update their integration.
		  action: Follow the migration guide.
		"""));

	[Fact]
	public void DoesNotRenderIncompleteForMoreInformationSentence()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// Should not contain the incomplete sentence
		markdown.Should().NotContain("For more information, check.");

		// Should not contain any dangling "check" references
		markdown.Should().NotContain("check .");
		markdown.Should().NotContain("check.");
	}

	[Fact]
	public void StillRendersEntryWithoutLinkSection()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// Entry content should still be present
		markdown.Should().Contain("The v1 Costs API has been deprecated");
		markdown.Should().Contain("This API will be removed");
		markdown.Should().Contain("**Impact**");
		markdown.Should().Contain("Users must update");
		markdown.Should().Contain("**Action**");
		markdown.Should().Contain("Follow the migration guide");
	}
}

/// <summary>
/// Test mixed scenarios with both private and public links
/// </summary>
public class ChangelogMixedLinkBugTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMixedLinkBugTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: deprecation
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Mixed links deprecation
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "# PRIVATE: https://github.com/elastic/cloud/pull/153728"
		  - "123456"
		  issues:
		  - "# PRIVATE: https://github.com/elastic/cloud/issues/789"
		  - "654321"
		"""));

	[Fact]
	public void RendersForMoreInformationWithOnlyVisibleLinks()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// Should contain proper "For more information" with visible links only
		markdown.Should().Contain("For more information, check");
		markdown.Should().Contain("#123456");
		markdown.Should().Contain("#654321");

		// Should not contain incomplete sentence
		markdown.Should().NotContain("For more information, check.");

		// Should end with period after the links
		markdown.Should().Contain("654321](https://github.com/elastic/elasticsearch/issues/654321).");
	}
}

/// <summary>
/// Test entries with no PR/issue references at all
/// </summary>
public class ChangelogNoLinksTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogNoLinksTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: deprecation
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: No links deprecation
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: This has no PR or issue references.
		"""));

	[Fact]
	public void DoesNotRenderForMoreInformationSection()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		// Should not contain any "For more information" section
		markdown.Should().NotContain("For more information");

		// Should still render entry content
		markdown.Should().Contain("No links deprecation");
		markdown.Should().Contain("This has no PR or issue references");
	}
}
