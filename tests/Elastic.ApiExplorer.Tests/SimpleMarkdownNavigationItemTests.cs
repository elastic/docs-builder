// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;

namespace Elastic.ApiExplorer.Tests;

public class SimpleMarkdownNavigationItemTests
{
	[Test]
	[Arguments("intro.md", "intro")]
	[Arguments("getting-started.md", "getting-started")]
	[Arguments("getting_started.md", "getting-started")]
	[Arguments("Getting Started.md", "getting-started")]
	[Arguments("API_Overview.md", "api-overview")]
	public void CreateSlugFromFile_GeneratesCorrectSlug(string fileName, string expectedSlug)
	{
		var fileSystem = new MockFileSystem();
		var file = fileSystem.FileInfo.New($"/docs/{fileName}");

		var slug = SimpleMarkdownNavigationItem.CreateSlugFromFile(file);

		slug.Should().Be(expectedSlug);
	}

	[Test]
	[Arguments("knn-guide.v9.md", "knn-guide")]
	[Arguments("migration-from-v7.v8.md", "migration-from-v7")]
	public void CreateSlugFromFile_StripsVersionSuffix(string fileName, string expectedSlug)
	{
		var fileSystem = new MockFileSystem();
		var file = fileSystem.FileInfo.New($"/docs/{fileName}");

		var slug = SimpleMarkdownNavigationItem.CreateSlugFromFile(file);

		slug.Should().Be(expectedSlug);
	}

	[Test]
	[Arguments("types", "types")]
	[Arguments("group", "group")]
	[Arguments("operation", "operation")]
	[Arguments("authentication", "authentication")]
	[Arguments("servers", "servers")]
	public void ValidateSlugForCollisions_ThrowsForReservedSegments(string slug, string reservedSegment)
	{
		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions(slug, "elasticsearch", "/docs/file.md");

		act.Should().Throw<InvalidOperationException>().WithMessage($"*conflicts with reserved API Explorer segment*{reservedSegment}*");
	}

	[Test]
	public void ValidateSlugForCollisions_AllowsSlugThatMatchesOperationId()
	{
		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("search", "elasticsearch", "/docs/search.md");

		act.Should().NotThrow();
	}

	[Test]
	public void ValidateSlugForCollisions_AllowsValidSlug()
	{
		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("overview", "elasticsearch", "/docs/overview.md");

		act.Should().NotThrow();
	}
}
