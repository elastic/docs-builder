// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.Documentation;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Tests;

/// <summary>Loads a small inline OpenAPI spec, a generator for it and a property tree builder.</summary>
internal static class TestSpecs
{
	public static async Task<OpenApiDocument> LoadSpecAsync(string json)
	{
		var path = Path.Join(Path.GetTempPath(), $"api-explorer-spec-{Guid.NewGuid():N}.json");
		await File.WriteAllTextAsync(path, json, TestContext.Current!.Execution.CancellationToken);
		try
		{
			var loaded = await OpenApiDocument.LoadAsync(
				path,
				new OpenApiReaderSettings { LeaveStreamOpen = false },
				TestContext.Current!.Execution.CancellationToken
			);
			return loaded.Document!;
		}
		finally
		{
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	/// <summary>Like <see cref="LoadSpecAsync"/>, but reads from memory and throws when the spec has parse errors.</summary>
	public static async Task<OpenApiDocument> LoadValidSpecAsync(string json)
	{
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
		var result = await OpenApiDocument.LoadAsync(stream, settings: new OpenApiReaderSettings { LeaveStreamOpen = false });
		if (result.Diagnostic?.Errors is { Count: > 0 } errors)
			throw new InvalidOperationException($"OpenAPI parsing failed: {string.Join(", ", errors.Select(e => e.Message))}");
		return result.Document!;
	}

	public static async Task<(OpenApiGenerator generator, OpenApiDocument document)> CreateGeneratorWithSpecAsync(
		string json,
		IMarkdownStringRenderer? markdownRenderer = null
	)
	{
		var context = TestHelpers.CreateBuildContext();
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, markdownRenderer ?? NoopMarkdownStringRenderer.Instance);
		return (generator, await LoadValidSpecAsync(json));
	}

	public static ApiPropertyTreeBuilder BuilderFor(OpenApiDocument document) =>
		new(document, new PropertyDisplayOptions { RenderMarkdown = s => new HtmlString($"<p>{s}</p>"), ApiRootUrl = "/api/doc/fixture" });
}
