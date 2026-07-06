// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>
/// A single code sample extracted from the x-codeSamples OpenAPI extension.
/// </summary>
public record CodeSample(string Language, string Source, string HighlightClass)
{
	private static readonly Dictionary<string, string> LanguageHighlightMap = new(StringComparer.OrdinalIgnoreCase)
	{
		["Console"] = "language-console",
		["curl"] = "language-bash",
		["Python"] = "language-python",
		["JavaScript"] = "language-javascript",
		["Ruby"] = "language-ruby",
		["PHP"] = "language-php",
		["Java"] = "language-java",
	};

	public static string GetHighlightClass(string language) =>
		LanguageHighlightMap.GetValueOrDefault(language, $"language-{language.ToLowerInvariant()}");

	/// <summary>Maps a hljs <c>language-*</c> class to the outer Myst-style wrapper, e.g. <c>language-json</c> to <c>highlight-json</c>.</summary>
	public static string GetHighlightGroupClass(string? highlightClass)
	{
		if (string.IsNullOrEmpty(highlightClass) || !highlightClass.StartsWith("language-", StringComparison.Ordinal))
			return "highlight-plaintext";

		var id = highlightClass["language-".Length..];
		return string.IsNullOrEmpty(id) ? "highlight-plaintext" : $"highlight-{id}";
	}
}

public class OperationViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiOperation Operation { get; init; }

	/// <summary>Precomputed structural content of the page; built before the slice renders.</summary>
	public required OperationPageModel Page { get; init; }

	public IReadOnlyList<string>? RequiredAuthItems =>
		OpenApiXReqAuthParser.TryGetPrerequisiteLines(
			Operation.Operation,
			RenderContext.ApiExplorerLog,
			Operation.Route,
			Operation.Operation.OperationId
		);

	protected override IReadOnlyList<ApiTocItem> GetTocItems()
	{
		var operation = Operation.Operation;
		var tocItems = new List<ApiTocItem> { new("Paths", "paths") };

		if (RequiredAuthItems is { Count: > 0 })
			tocItems.Add(new ApiTocItem("Prerequisites", "prerequisites"));

		if (!string.IsNullOrWhiteSpace(operation.Description))
			tocItems.Add(new ApiTocItem("Description", "description"));

		if (Page.QueryParameters.Count > 0)
			tocItems.Add(new ApiTocItem("Query String Parameters", "query-params"));

		if (operation.RequestBody is not null)
			tocItems.Add(new ApiTocItem("Request Body", "request-body"));

		if (operation.Responses is { Count: > 0 })
			tocItems.Add(new ApiTocItem(operation.Responses.Count == 1 ? "Response" : "Responses", "responses"));

		if (Page.CodeSamples.Count > 0)
			tocItems.Add(new ApiTocItem("Code Examples", "code-examples"));

		if (Page.ShowRequestExamples)
			tocItems.Add(new ApiTocItem("Request Examples", "request-examples"));

		if (Page.ShowResponseExamples)
			tocItems.Add(new ApiTocItem("Response Examples", "response-examples"));

		return tocItems;
	}
}
