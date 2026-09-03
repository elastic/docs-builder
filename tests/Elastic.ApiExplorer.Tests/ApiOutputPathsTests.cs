// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiOutputPathsTests
{
	[Theory]
	[InlineData("/api/doc/elasticsearch/", "/api/doc/elasticsearch.md")]
	[InlineData("/api/doc/elasticsearch/operation/operation-search", "/api/doc/elasticsearch/operation/operation-search.md")]
	[InlineData("/api/", "/api.md")]
	[InlineData("/", "/index.md")]
	public void MarkdownUrl_AppendsMdToTrimmedPageUrl(string pageUrl, string expected) =>
		ApiOutputPaths.MarkdownUrl(pageUrl).Should().Be(expected);

	[Theory]
	[InlineData("docs/api/doc/elasticsearch", "docs", "api/doc/elasticsearch/index.html")]
	[InlineData("/api/doc/elasticsearch/operation/operation-search", "", "api/doc/elasticsearch/operation/operation-search/index.html")]
	public void RelativeHtmlFile_StripsPrefix(string pageUrl, string prefix, string expected) =>
		ApiOutputPaths.RelativeHtmlFile(pageUrl, prefix).Should().Be(expected);

	[Theory]
	[InlineData("docs/api/doc/elasticsearch", "docs", "api/doc/elasticsearch.md")]
	[InlineData("docs/api/", "docs", "api.md")]
	[InlineData("/api/doc/elasticsearch/operation/operation-search", "", "api/doc/elasticsearch/operation/operation-search.md")]
	public void RelativeMarkdownFile_StripsPrefix(string pageUrl, string prefix, string expected) =>
		ApiOutputPaths.RelativeMarkdownFile(pageUrl, prefix).Should().Be(expected);
}
