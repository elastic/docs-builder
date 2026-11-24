// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Indexing.Exporters.Elasticsearch;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Markdown.Tests.Search;

public class OpenApiHeadingsTests
{
	[Test]
	public void ApplyApiPlainText_ExporterTagHeadings_AreKeptForTheIndex()
	{
		var doc = new DocumentationDocument
		{
			Path = "/docs/api/elasticsearch/operation/operation-ccr-follow",
			Title = "Create a follower",
			SearchTitle = "Create a follower - Elasticsearch API - ccr-follow",
			Headings = ["ccr"]
		};

		ElasticsearchMarkdownExporter.ApplyApiPlainText(doc, "Create a follower");

		doc.Headings.Should().Equal("ccr");
		doc.Summary.Should().Be("Create a follower ccr");
	}

	[Test]
	public void ApplyApiBody_PageDocument_KeepsItsPlainTextBody()
	{
		var doc = new DocumentationDocument
		{
			Path = "/docs/api/elasticsearch/group/endpoint-core",
			Title = "Core",
			SearchTitle = "Core",
			Body = "Send a <ping> first.",
			Api = new ApiOperationMetadata { Endpoints = [], Method = "", Path = "", OperationId = "", Page = true }
		};

		ElasticsearchMarkdownExporter.ApplyApiBody(doc, _ => "converted");

		doc.Body.Should().Be("Send a <ping> first.");
	}

	[Test]
	public void ApplyApiBody_OperationDocument_ConvertsItsMarkdownBody()
	{
		var doc = new DocumentationDocument
		{
			Path = "/docs/api/elasticsearch/operation/operation-ping",
			Title = "Ping",
			SearchTitle = "Ping",
			Body = "# Ping",
			Api = new ApiOperationMetadata { Endpoints = [], Method = "GET", Path = "/", OperationId = "ping" }
		};

		ElasticsearchMarkdownExporter.ApplyApiBody(doc, _ => "converted");

		doc.Body.Should().Be("converted");
	}
}
