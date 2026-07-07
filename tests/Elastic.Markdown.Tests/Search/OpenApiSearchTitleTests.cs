// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search;
using Elastic.Markdown.Exporters.Elasticsearch;

namespace Elastic.Markdown.Tests.Search;

/// <summary>
/// OpenApiDocumentExporter builds its own search_title, including the raw operation id
/// (e.g. "_bulk"). CommonEnrichments must not overwrite it with the markdown-tuned
/// CreateSearchTitle, which derives extra tokens from the URL by splitting on '_' among other
/// characters — that would strip the leading underscore from operation ids like "_bulk".
/// </summary>
public class OpenApiSearchTitleTests
{
	[Fact]
	public void ApiDocs_PreserveTheExporterSSearchTitle()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "api",
			Url = "/docs/api/doc/elasticsearch/operation/operation-_bulk",
			Title = "Bulk index or delete documents - Elasticsearch API",
			SearchTitle = "Bulk index or delete documents - Elasticsearch API - _bulk"
		};

		ElasticsearchMarkdownExporter.CommonEnrichments(doc, null);

		doc.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk");
		doc.SearchTitle.Should().Contain("_bulk");
	}

	[Fact]
	public void MarkdownDocs_StillGetTheDerivedSearchTitle()
	{
		var doc = new DocumentationDocument
		{
			Url = "/docs/reference/elasticsearch/settings",
			Title = "Settings",
			SearchTitle = "Settings"
		};

		ElasticsearchMarkdownExporter.CommonEnrichments(doc, null);

		// unaffected by this change — still rebuilt from the URL, not left as the seeded value
		doc.SearchTitle.Should().NotBe("Settings");
		doc.SearchTitle.Should().StartWith("Settings - ");
	}
}
