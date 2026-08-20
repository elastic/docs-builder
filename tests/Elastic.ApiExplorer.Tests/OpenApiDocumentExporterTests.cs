// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;
using static System.StringComparison;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiDocumentExporterTests
{
	private static readonly VersionsConfiguration VersionsConfiguration =
		TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9, currentMinor: 2);

	private static OpenApiConvertContext ElasticsearchContext(string moniker = "main", SemVersion? ceiling = null) =>
		new("elasticsearch", moniker, ceiling ?? new SemVersion(9, 2, 0), "Elasticsearch", "elasticsearch");

	private static OpenApiDocument PingSpec(string? xState = null, string? description = null)
	{
		var operation = new OpenApiOperation
		{
			OperationId = "ping",
			Summary = "Ping",
			Description = description
		};
		if (xState is not null)
			operation.Extensions = new Dictionary<string, IOpenApiExtension> { ["x-state"] = new JsonNodeExtension(JsonValue.Create(xState)) };

		return new OpenApiDocument
		{
			Paths = new OpenApiPaths
			{
				["/ping"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = operation
					}
				}
			}
		};
	}

	[Fact]
	public void ConvertToDocuments_Main_UsesUnversionedOperationUrl()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(PingSpec(), ElasticsearchContext()).ToArray();

		docs.Should().ContainSingle();
		docs[0].Path.Should().Be("/docs/api/doc/elasticsearch/operation/operation-ping");
		docs[0].Title.Should().Be("Ping - Elasticsearch API");
		docs[0].Parents.Should().Contain(p => p.Path == "/docs/api/doc/elasticsearch");
	}

	[Fact]
	public void ConvertToDocuments_NumericMoniker_UsesVersionPrefixedUrlAndTitle()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(PingSpec(), ElasticsearchContext("8", new SemVersion(8, 19, 0))).ToArray();

		docs.Should().ContainSingle();
		docs[0].Path.Should().Be("/docs/api/doc/elasticsearch/v8/operation/operation-ping");
		docs[0].Title.Should().Be("Ping - Elasticsearch 8.x API");
		docs[0].Parents.Should().Contain(p => p.Path == "/docs/api/doc/elasticsearch/v8");
	}

	[Fact]
	public void ConvertToDocuments_AddedInAfterCeiling_IsExcluded()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);
		var spec = PingSpec("Generally available; Added in 8.19.0");

		var docs = exporter.ConvertToDocuments(spec, ElasticsearchContext("8", new SemVersion(8, 18, 0))).ToArray();

		docs.Should().BeEmpty();
	}

	[Fact]
	public void ParseFilterCeiling_MajorMinor_AppendsPatchZero()
	{
		var fallback = new SemVersion(9, 2, 0);

		OpenApiDocumentExporter.ParseFilterCeiling("8.19", fallback).Should().Be(new SemVersion(8, 19, 0));
		OpenApiDocumentExporter.ParseFilterCeiling("8.19.0", fallback).Should().Be(new SemVersion(8, 19, 0));
	}

	[Fact]
	public void ConvertToDocuments_AddedInAtCeiling_IsIncluded()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);
		var spec = PingSpec("Generally available; Added in 8.19.0");

		var docs = exporter.ConvertToDocuments(spec, ElasticsearchContext("8", new SemVersion(8, 19, 0))).ToArray();

		docs.Should().ContainSingle();
	}

	[Fact]
	public void DescriptionWithHtmlOperationsListShouldTransformToMarkdownAtEnd()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);
		var description = """
			**All methods and paths for this operation:**
			<div>
			<span class="operation-verb get">GET</span> <span class="operation-path">/_ping</span>
			</div>
			""";
		var spec = PingSpec(description: description);

		var docs = exporter.ConvertToDocuments(spec, ElasticsearchContext()).ToArray();

		docs.Should().ContainSingle();
		var doc = docs[0];
		doc.Description.Should().NotContain("<div>");
		doc.Description.Should().NotContain("<span");
		doc.Description.Should().Contain("- **GET** `/_ping`");
		var lastNonEmptyLines = doc.Description.Split('\n', StringSplitOptions.TrimEntries)
			.Where(l => !string.IsNullOrWhiteSpace(l))
			.TakeLast(5)
			.ToList();
		lastNonEmptyLines.Any(l => l.StartsWith("- **", InvariantCulture)).Should().BeTrue();
	}
}
