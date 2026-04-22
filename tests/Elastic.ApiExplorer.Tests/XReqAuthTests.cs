// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Tests;

public class XReqAuthTests
{
	private static string TestDataPath(string fileName) =>
		Path.Join(AppContext.BaseDirectory, "TestData", fileName);

	[Fact]
	public async Task TryGetPrerequisiteLines_MinimalOpenApi3Spec_MatchesElasticsearchShape()
	{
		var json = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.0",
		  "info": { "title": "t", "version": "1" },
		  "paths": {
		    "/a": {
		      "get": {
		        "operationId": "op-a",
		        "responses": { "200": { "description": "ok" } },
		        "x-req-auth": [
		          "Index privileges: `monitor`\n",
		          "Cluster privileges: `monitor`\n"
		        ]
		      }
		    }
		  }
		}
		""";
		var jsonPath = Path.Join(Path.GetTempPath(), $"xreqauth-{Guid.NewGuid():N}.json");
		try
		{
			await File.WriteAllTextAsync(jsonPath, json, TestContext.Current.CancellationToken);
			var loaded = await OpenApiDocument.LoadAsync(
				jsonPath,
				new OpenApiReaderSettings
				{
					LeaveStreamOpen = false
				},
				TestContext.Current.CancellationToken
			);
			var op = loaded.Document!.Paths!["/a"].Operations![HttpMethod.Get]!;

			var lines = OpenApiXReqAuthParser.TryGetPrerequisiteLines(op, null, "/a", "op-a");

			lines.Should().NotBeNull();
			lines!.Should().HaveCount(2);
			lines[0].Should().Contain("Index");
			lines[0].Should().Contain("monitor");
			lines[1].Should().Contain("Cluster");
			lines[1].Should().Contain("monitor");
		}
		finally
		{
			if (File.Exists(jsonPath))
				File.Delete(jsonPath);
		}
	}

	[Fact]
	public async Task TryGetPrerequisiteLines_EmptyArray_ReturnsNull()
	{
		var json = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.0",
		  "info": { "title": "t", "version": "1" },
		  "paths": {
		    "/a": {
		      "get": {
		        "operationId": "op-a",
		        "responses": { "200": { "description": "ok" } },
		        "x-req-auth": []
		      }
		    }
		  }
		}
		""";
		var jsonPath = Path.Join(Path.GetTempPath(), $"xreqauth-{Guid.NewGuid():N}.json");
		try
		{
			await File.WriteAllTextAsync(jsonPath, json, TestContext.Current.CancellationToken);
			var loaded = await OpenApiDocument.LoadAsync(
				jsonPath,
				new OpenApiReaderSettings
				{
					LeaveStreamOpen = false
				},
				TestContext.Current.CancellationToken
			);
			var op = loaded.Document!.Paths!["/a"].Operations![HttpMethod.Get]!;

			OpenApiXReqAuthParser.TryGetPrerequisiteLines(op, null, "/a", "op-a")
				.Should().BeNull("empty x-req-auth should not show Prerequisites");
		}
		finally
		{
			if (File.Exists(jsonPath))
				File.Delete(jsonPath);
		}
	}

	[Fact]
	public async Task ElasticsearchSample_CatIndicesOperations_HaveXReqAuth()
	{
		var specPath = TestDataPath("elasticsearch-x-req-auth-cat-indices-sample.json");
		File.Exists(specPath).Should().BeTrue($"Fixture missing: {specPath}");

		var loaded = await OpenApiDocument.LoadAsync(
			specPath,
			new OpenApiReaderSettings
			{
				LeaveStreamOpen = false
			},
			TestContext.Current.CancellationToken
		);
		var doc = loaded.Document!;
		var getIndices = doc.Paths!["/_cat/indices"].Operations![HttpMethod.Get]!;
		var a = OpenApiXReqAuthParser.TryGetPrerequisiteLines(getIndices, null, "/_cat/indices", getIndices.OperationId);
		a.Should().NotBeNull();
		a!.Should().NotBeEmpty();
		a.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s));

		var getIndicesIndex = doc.Paths!["/_cat/indices/{index}"].Operations![HttpMethod.Get]!;
		var b = OpenApiXReqAuthParser.TryGetPrerequisiteLines(
			getIndicesIndex,
			null,
			"/_cat/indices/{index}",
			getIndicesIndex.OperationId
		);
		b.Should().NotBeNull();
		b!.Should().NotBeEmpty();
	}

	[Fact]
	public async Task KibanaStyleSample_FirstPathsLackXReqAuth()
	{
		var specPath = TestDataPath("kibana-openapi-no-x-req-auth-sample.json");
		File.Exists(specPath).Should().BeTrue($"Fixture missing: {specPath}");

		var loaded = await OpenApiDocument.LoadAsync(
			specPath,
			new OpenApiReaderSettings
			{
				LeaveStreamOpen = false
			},
			TestContext.Current.CancellationToken
		);
		var doc = loaded.Document!;

		var opCount = 0;
		var pathCount = 0;
		foreach (var p in doc.Paths)
		{
			if (pathCount >= 3)
				break;
			pathCount++;
			if (p.Value.Operations is null)
				continue;
			foreach (var httpOp in p.Value.Operations)
			{
				if (opCount >= 5)
					goto done;
				var lines = OpenApiXReqAuthParser.TryGetPrerequisiteLines(
					httpOp.Value,
					null,
					p.Key,
					httpOp.Value.OperationId
				);
				lines.Should().BeNull("sample Kibana-style spec has no x-req-auth on sampled operations");
				opCount++;
			}
		}
	done:
		opCount.Should().BeGreaterThan(0, "sample should have at least one operation in the first 3 paths");
	}
}
