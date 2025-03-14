// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;

namespace Elastic.Markdown.Tests.DocSet;

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
			.Contain(Path.Combine("testing", "nested"));
	}

	[Fact]
	public void ParsesFilesAndPrefixesPaths() =>
		Configuration.Files.Should()
			.Contain("index.md")
			.And.Contain(Path.Combine("syntax", "index.md"));

	[Fact]
	public void ParsesRedirects()
	{
		Configuration.Redirects.Should()
			.NotBeNullOrEmpty()
			.And.ContainKey(Path.Combine("testing", "redirects", "first-page-old.md"))
			.And.ContainKey(Path.Combine("testing", "redirects", "second-page-old.md"))
			.And.ContainKey(Path.Combine("testing", "redirects", "4th-page.md"))
			.And.ContainKey(Path.Combine("testing", "redirects", "third-page.md"));

		var redirect1 = Configuration.Redirects![Path.Combine("testing", "redirects", "first-page-old.md")];
		redirect1.To.Should().Be(Path.Combine("testing", "redirects", "second-page.md"));

		var redirect2 = Configuration.Redirects![Path.Combine("testing", "redirects", "second-page-old.md")];
		redirect2.Many.Should().NotBeNullOrEmpty().And.HaveCount(2);
		redirect2.Many![0].To.Should().Be(Path.Combine("testing", "redirects", "second-page.md"));
		redirect2.Many![1].To.Should().Be(Path.Combine("testing", "redirects", "third-page.md"));
		redirect2.To.Should().BeNullOrEmpty();

		var redirect3 = Configuration.Redirects![Path.Combine("testing", "redirects", "third-page.md")];
		redirect3.To.Should().Be(Path.Combine("testing", "redirects", "third-page.md"));

		var redirect4 = Configuration.Redirects![Path.Combine("testing", "redirects", "4th-page.md")];
		redirect4.To.Should().Be(Path.Combine("testing", "redirects", "5th-page.md"));
	}
}
