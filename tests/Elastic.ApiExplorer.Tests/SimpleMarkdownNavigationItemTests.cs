// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Navigation;
using Elastic.ApiExplorer.Operations;

namespace Elastic.ApiExplorer.Tests;

public class SimpleMarkdownNavigationItemTests
{
	[Fact]
	public void GetMetadata_WithTitleFields_SeparatesPageAndNavigationTitles()
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["/docs/intro.md"] = new(
				"""
					---
					navigation_title: Short title
					meta_title: Search APIs - Elasticsearch
					---

					# Search APIs
					"""
			)
		});
		var file = fileSystem.FileInfo.New("/docs/intro.md");

		var metadata = MarkdownNavigationTitleReader.GetMetadata(fileSystem, file);

		metadata.NavigationTitle.Should().Be("Short title");
		metadata.MetaTitle.Should().Be("Search APIs - Elasticsearch");
	}

	[Fact]
	public void GetMetadata_WithHeadingLikeFrontMatterContent_DoesNotTreatItAsPageTitle()
	{
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			["/docs/intro.md"] = new(
				"""
					---
					description: |
					  # Internal note
					---

					# Real page title
					"""
			)
		});
		var file = fileSystem.FileInfo.New("/docs/intro.md");

		var metadata = MarkdownNavigationTitleReader.GetMetadata(fileSystem, file);

		metadata.NavigationTitle.Should().Be("Intro");
		metadata.MetaTitle.Should().BeNull();
	}

	[Theory]
	[InlineData("intro.md", "intro")]
	[InlineData("getting-started.md", "getting-started")]
	[InlineData("getting_started.md", "getting-started")]
	[InlineData("Getting Started.md", "getting-started")]
	[InlineData("API_Overview.md", "api-overview")]
	public void CreateSlugFromFile_GeneratesCorrectSlug(string fileName, string expectedSlug)
	{
		var fileSystem = new MockFileSystem();
		var file = fileSystem.FileInfo.New($"/docs/{fileName}");

		var slug = SimpleMarkdownNavigationItem.CreateSlugFromFile(file);

		slug.Should().Be(expectedSlug);
	}

	[Theory]
	[InlineData("knn-guide.v9.md", "knn-guide")]
	[InlineData("migration-from-v7.v8.md", "migration-from-v7")]
	public void CreateSlugFromFile_StripsVersionSuffix(string fileName, string expectedSlug)
	{
		var fileSystem = new MockFileSystem();
		var file = fileSystem.FileInfo.New($"/docs/{fileName}");

		var slug = SimpleMarkdownNavigationItem.CreateSlugFromFile(file);

		slug.Should().Be(expectedSlug);
	}

	[Theory]
	[InlineData("types", "types")]
	[InlineData("group", "group")]
	[InlineData("operation", "operation")]
	[InlineData("authentication", "authentication")]
	[InlineData("servers", "servers")]
	public void ValidateSlugForCollisions_ThrowsForReservedSegments(string slug, string reservedSegment)
	{
		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions(slug, "elasticsearch", "/docs/file.md");

		act.Should().Throw<InvalidOperationException>().WithMessage($"*conflicts with reserved API Explorer segment*{reservedSegment}*");
	}

	[Fact]
	public void ValidateSlugForCollisions_AllowsSlugThatMatchesOperationId()
	{
		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("search", "elasticsearch", "/docs/search.md");

		act.Should().NotThrow();
	}

	[Fact]
	public void ValidateSlugForCollisions_AllowsValidSlug()
	{
		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("overview", "elasticsearch", "/docs/overview.md");

		act.Should().NotThrow();
	}
}
