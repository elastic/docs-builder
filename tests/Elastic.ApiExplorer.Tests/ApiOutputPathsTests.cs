// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiOutputPathsTests
{
	[Test]
	[Arguments("/api/doc/elasticsearch/", "/api/doc/elasticsearch.md")]
	[Arguments("/api/doc/elasticsearch/operation/operation-search", "/api/doc/elasticsearch/operation/operation-search.md")]
	[Arguments("/api/", "/api.md")]
	[Arguments("/", "/index.md")]
	public void MarkdownUrl_AppendsMdToTrimmedPageUrl(string pageUrl, string expected) =>
		ApiOutputPaths.MarkdownUrl(pageUrl).Should().Be(expected);

	[Test]
	[Arguments("docs/api/doc/elasticsearch", "docs", "api/doc/elasticsearch/index.html")]
	[Arguments("/api/doc/elasticsearch/operation/operation-search", "", "api/doc/elasticsearch/operation/operation-search/index.html")]
	public void RelativeHtmlFile_StripsPrefix(string pageUrl, string prefix, string expected) =>
		ApiOutputPaths.RelativeHtmlFile(pageUrl, prefix).Should().Be(expected);

	[Test]
	[Arguments("docs/api/doc/elasticsearch", "docs", "api/doc/elasticsearch.md")]
	[Arguments("docs/api/", "docs", "api.md")]
	[Arguments("/api/doc/elasticsearch/operation/operation-search", "", "api/doc/elasticsearch/operation/operation-search.md")]
	public void RelativeMarkdownFile_StripsPrefix(string pageUrl, string prefix, string expected) =>
		ApiOutputPaths.RelativeMarkdownFile(pageUrl, prefix).Should().Be(expected);

	[Test]
	[Arguments("/docs/api/doc/elasticsearch/", "/docs/api/doc/elasticsearch.json")]
	[Arguments("/docs/api/doc/elasticsearch/v8/", "/docs/api/doc/elasticsearch/v8.json")]
	[Arguments("/api/", "/api.json")]
	[Arguments("/", "/index.json")]
	public void JsonUrl_AppendsJsonToTrimmedPageUrl(string pageUrl, string expected) =>
		ApiOutputPaths.JsonUrl(pageUrl).Should().Be(expected);

	[Test]
	[Arguments("/docs/api/doc/elasticsearch/", "/docs/api/doc/elasticsearch.yaml")]
	[Arguments("/docs/api/doc/elasticsearch/v8/", "/docs/api/doc/elasticsearch/v8.yaml")]
	[Arguments("/api/", "/api.yaml")]
	[Arguments("/", "/index.yaml")]
	public void YamlUrl_AppendsYamlToTrimmedPageUrl(string pageUrl, string expected) =>
		ApiOutputPaths.YamlUrl(pageUrl).Should().Be(expected);

	[Test]
	[Arguments("docs/api/doc/elasticsearch", "docs", "api/doc/elasticsearch.json")]
	[Arguments("/api/doc/elasticsearch/v8/", "", "api/doc/elasticsearch/v8.json")]
	public void RelativeJsonFile_StripsPrefix(string pageUrl, string prefix, string expected) =>
		ApiOutputPaths.RelativeJsonFile(pageUrl, prefix).Should().Be(expected);

	[Test]
	[Arguments("docs/api/doc/elasticsearch", "docs", "api/doc/elasticsearch.yaml")]
	[Arguments("/api/doc/elasticsearch/v8/", "", "api/doc/elasticsearch/v8.yaml")]
	public void RelativeYamlFile_StripsPrefix(string pageUrl, string prefix, string expected) =>
		ApiOutputPaths.RelativeYamlFile(pageUrl, prefix).Should().Be(expected);
}
