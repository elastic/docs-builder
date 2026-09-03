// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Http;

namespace Elastic.Documentation.Tests.Http;

public class MarkdownAcceptTests
{
	[Theory]
	[InlineData("text/markdown")]
	[InlineData("text/markdown; charset=utf-8")]
	[InlineData("text/markdown, text/html;q=0.9")]
	public void PrefersMarkdown_WhenMarkdownOutranksHtml(string accept) => MarkdownAccept.PrefersMarkdown(accept).Should().BeTrue();

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
	[InlineData("text/html, text/markdown;q=0.5")]
	[InlineData("text/html, text/markdown")]
	public void PrefersMarkdown_WhenHtmlWinsOrMarkdownMissing(string? accept) => MarkdownAccept.PrefersMarkdown(accept).Should().BeFalse();
}

public class ApiMarkdownRequestTests
{
	[Fact]
	public void ResolveFile_CatalogSlugUsesParentApiMd()
	{
		var apiRoot = Path.GetFullPath(Path.Join(Path.GetTempPath(), "out", "api"));
		var expected = Path.GetFullPath(Path.Join(Path.GetTempPath(), "out", "api.md"));

		ApiMarkdownRequest.ResolveFile(apiRoot, "").Should().Be(expected);
		ApiMarkdownRequest.ResolveFile(apiRoot, "api.md").Should().Be(expected);
	}

	[Fact]
	public void ResolveFile_PageSlugUsesSiblingMd()
	{
		var apiRoot = Path.GetFullPath(Path.Join(Path.GetTempPath(), "out", "api"));

		ApiMarkdownRequest
			.ResolveFile(apiRoot, "doc/elasticsearch")
			.Should()
			.Be(Path.GetFullPath(Path.Join(apiRoot, "doc", "elasticsearch.md")));
		ApiMarkdownRequest
			.ResolveFile(apiRoot, "doc/elasticsearch.md")
			.Should()
			.Be(Path.GetFullPath(Path.Join(apiRoot, "doc", "elasticsearch.md")));
		ApiMarkdownRequest
			.ResolveFile(apiRoot, "doc/elasticsearch/operation/operation-search")
			.Should()
			.Be(Path.GetFullPath(Path.Join(apiRoot, "doc", "elasticsearch", "operation", "operation-search.md")));
	}

	[Fact]
	public void SiblingOfDirectory_UsesParentNameMd()
	{
		var directory = Path.Join(Path.GetTempPath(), "out", "api", "doc", "elasticsearch");
		ApiMarkdownRequest
			.SiblingOfDirectory(directory)
			.Should()
			.Be(Path.GetFullPath(Path.Join(Path.GetTempPath(), "out", "api", "doc", "elasticsearch.md")));
	}
}
