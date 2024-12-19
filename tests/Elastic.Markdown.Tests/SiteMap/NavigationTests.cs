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
		Configuration.ImplicitFolders.Should().NotBeNullOrEmpty();
		Configuration.ImplicitFolders.Should()
			.Contain("testing")
			.And.Contain("testing/req.md");
	}
	[Fact]
	public void ParsesFilesAndPrefixesPaths() =>
		Configuration.Files.Should()
			.Contain("index.md")
			.And.Contain("syntax");
}
