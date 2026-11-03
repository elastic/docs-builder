// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Indexing.Exporters.Elasticsearch;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Markdown.Tests.Search;

public class OpenApiHeadingsTests
{
	[Test]
	public void ApplyApiPlainText_ExporterTagHeadings_AreKeptForTheIndex()
	{
		const string markdown = "# Create a follower\n\n**Method:** PUT\n\n## Parameters\n- **index** (path): The follower index\n";
		var doc = new DocumentationDocument
		{
			ContentType = "api",
			Path = "/docs/api/doc/elasticsearch/operation/operation-ccr-follow",
			Title = "Create a follower",
			SearchTitle = "Create a follower - Elasticsearch API - ccr-follow",
			Body = markdown,
			Headings = ["ccr"]
		};

		ElasticsearchMarkdownExporter.ApplyApiPlainText(doc, "Create a follower");

		doc.Headings.Should().Equal("ccr");
		doc.Body.Should().Be("Create a follower");
		doc.Summary.Should().Be("Create a follower ccr");
	}
}
