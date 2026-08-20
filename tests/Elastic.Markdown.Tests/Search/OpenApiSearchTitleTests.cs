// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
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
			Path = "/docs/api/doc/elasticsearch/operation/operation-_bulk",
			Title = "Bulk index or delete documents - Elasticsearch API",
			SearchTitle = "Bulk index or delete documents - Elasticsearch API - _bulk"
		};

		ElasticsearchMarkdownExporter.CommonEnrichments(doc, null);

		doc.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk");
		doc.SearchTitle.Should().Contain("_bulk");
	}

	[Fact]
	public void ApiDocs_VersionedPath_RaisesNavigationDepth()
	{
		var current = new DocumentationDocument
		{
			ContentType = "api",
			Path = "/docs/api/doc/elasticsearch/operation/operation-ping",
			Title = "Ping - Elasticsearch API",
			SearchTitle = "Ping - Elasticsearch API - ping"
		};
		var versioned = new DocumentationDocument
		{
			ContentType = "api",
			Path = "/docs/api/doc/elasticsearch/v8/operation/operation-ping",
			Title = "Ping - Elasticsearch 8.x API",
			SearchTitle = "Ping - Elasticsearch 8.x API - ping"
		};

		ElasticsearchMarkdownExporter.CommonEnrichments(current, null);
		ElasticsearchMarkdownExporter.CommonEnrichments(versioned, null);

		current.Navigation.Depth.Should().Be(20);
		versioned.Navigation.Depth.Should().Be(40);
		versioned.Navigation.Depth.Should().BeGreaterThan(current.Navigation.Depth);
	}

	[Fact]
	public void ApiDocs_ProductLanding_AssignsNavigationDepth()
	{
		var current = new DocumentationDocument
		{
			ContentType = "api",
			Path = "/docs/api/doc/elasticsearch",
			Title = "Elasticsearch API",
			SearchTitle = "Elasticsearch API"
		};
		var versioned = new DocumentationDocument
		{
			ContentType = "api",
			Path = "/docs/api/doc/elasticsearch/v8",
			Title = "Elasticsearch 8.x API",
			SearchTitle = "Elasticsearch 8.x API"
		};

		ElasticsearchMarkdownExporter.CommonEnrichments(current, null);
		ElasticsearchMarkdownExporter.CommonEnrichments(versioned, null);

		current.Navigation.Depth.Should().Be(10);
		versioned.Navigation.Depth.Should().Be(30);
	}

	[Fact]
	public void ApiDocs_ProductLanding_RanksAboveMatchingOperation()
	{
		var currentLanding = new DocumentationDocument
		{
			ContentType = "api",
			Path = "/docs/api/doc/elasticsearch",
			Title = "Elasticsearch API",
			SearchTitle = "Elasticsearch API"
		};
		var currentOperation = new DocumentationDocument
		{
			ContentType = "api",
			Path = "/docs/api/doc/elasticsearch/operation/operation-ping",
			Title = "Ping - Elasticsearch API",
			SearchTitle = "Ping - Elasticsearch API - ping"
		};
		var versionedLanding = new DocumentationDocument
		{
			ContentType = "api",
			Path = "/docs/api/doc/elasticsearch/v8",
			Title = "Elasticsearch 8.x API",
			SearchTitle = "Elasticsearch 8.x API"
		};
		var versionedOperation = new DocumentationDocument
		{
			ContentType = "api",
			Path = "/docs/api/doc/elasticsearch/v8/operation/operation-ping",
			Title = "Ping - Elasticsearch 8.x API",
			SearchTitle = "Ping - Elasticsearch 8.x API - ping"
		};

		ElasticsearchMarkdownExporter.CommonEnrichments(currentLanding, null);
		ElasticsearchMarkdownExporter.CommonEnrichments(currentOperation, null);
		ElasticsearchMarkdownExporter.CommonEnrichments(versionedLanding, null);
		ElasticsearchMarkdownExporter.CommonEnrichments(versionedOperation, null);

		currentLanding.Navigation.Depth.Should().BeLessThan(currentOperation.Navigation.Depth);
		versionedLanding.Navigation.Depth.Should().BeLessThan(versionedOperation.Navigation.Depth);
	}

	[Fact]
	public void MarkdownDocs_StillGetTheDerivedSearchTitle()
	{
		var doc = new DocumentationDocument
		{
			Path = "/docs/reference/elasticsearch/settings",
			Title = "Settings",
			SearchTitle = "Settings"
		};

		ElasticsearchMarkdownExporter.CommonEnrichments(doc, null);

		// unaffected by this change — still rebuilt from the URL, not left as the seeded value
		doc.SearchTitle.Should().NotBe("Settings");
		doc.SearchTitle.Should().StartWith("Settings - ");
	}
}
