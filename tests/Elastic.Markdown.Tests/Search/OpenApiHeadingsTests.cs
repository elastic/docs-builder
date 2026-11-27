// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
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
	public void ApplyApiMarkdown_PageDocument_ReadsItsSectionsAndPlainTextFromTheExport()
	{
		var doc = new DocumentationDocument
		{
			Path = "/docs/api/elasticsearch/group/endpoint-admin",
			Title = "HTTP <code> responses",
			SearchTitle = "HTTP <code> responses",
			Body =
				"""
				# HTTP \<code\> responses

				Send a `<ping>` first.

				## Connecting

				- [Get the status]({api-root}/operation/operation-status): `GET` `/_status`
				""",
			Api = new ApiOperationMetadata { Endpoints = [], Method = "", Path = "", OperationId = "", Page = true }
		};

		ElasticsearchMarkdownExporter.ApplyApiMarkdown(doc, Context());

		doc.Headings.Should().Equal("Connecting");
		doc.Body.Should().Contain("HTTP <code> responses").And.Contain("Send a <ping> first.").And.NotContain("api-root");
	}

	[Test]
	public void ApplyApiMarkdown_OperationDocument_KeepsTheExporterHeadings()
	{
		var doc = new DocumentationDocument
		{
			Path = "/docs/api/elasticsearch/operation/operation-ping",
			Title = "Ping",
			SearchTitle = "Ping",
			Headings = ["core"],
			Body = "# Ping\n\n## Parameters\n",
			Api = new ApiOperationMetadata { Endpoints = [], Method = "GET", Path = "/", OperationId = "ping" }
		};

		ElasticsearchMarkdownExporter.ApplyApiMarkdown(doc, Context());

		doc.Headings.Should().Equal("core");
	}

	private static BuildContext Context()
	{
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData> { { "docs/docset.yml", new MockFileData("project: test\ntoc:\n- file: index.md\n") } },
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		return new BuildContext(
			new DiagnosticsCollector([]),
			TestHelpers.CreateDocumentationFileSystem(fileSystem),
			TestHelpers.CreateConfigurationContext(fileSystem)
		);
	}
}
