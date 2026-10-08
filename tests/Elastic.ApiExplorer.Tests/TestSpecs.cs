// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using Elastic.ApiExplorer.Components.PropertyTree;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Tests;

/// <summary>Loads a small inline OpenAPI spec and a property tree builder for it.</summary>
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

	public static ApiPropertyTreeBuilder BuilderFor(OpenApiDocument document) =>
		new(document, new PropertyDisplayOptions { RenderMarkdown = s => new HtmlString($"<p>{s}</p>"), ApiRootUrl = "/api/doc/fixture" });
}
