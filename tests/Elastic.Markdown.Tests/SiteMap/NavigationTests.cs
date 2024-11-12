// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.SiteMap;

public class NavigationTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void ParsesATableOfContents() =>
		Configuration.TableOfContents.Should().NotBeNullOrEmpty();

	[Fact]
	public void ParsesNestedFoldersAndPrefixesPaths()
	{
		Configuration.Folders.Should().NotBeNullOrEmpty();
		Configuration.Folders.Should()
			.Contain("markup")
			.And.Contain("elastic/observability");
	}
	[Fact]
	public void ParsesFilesAndPrefixesPaths() =>
		Configuration.Files.Should()
			.Contain("index.md")
			.And.Contain("elastic/search-labs/search/req.md");
}
