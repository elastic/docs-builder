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
}

public class OperationViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiOperation Operation { get; init; }

	/// <summary>
	/// Code samples parsed from the x-codeSamples extension, ordered with Console first.
	/// Populated during <see cref="GetTocItems"/>, which runs before the template body.
	/// </summary>
	public IReadOnlyList<CodeSample> CodeSamples { get; private set; } = [];

	protected override IReadOnlyList<ApiTocItem> GetTocItems()
	{
		CodeSamples = ParseCodeSamples(Operation.Operation);

		var operation = Operation.Operation;
		var tocItems = new List<ApiTocItem> { new("Paths", "paths") };

		if (!string.IsNullOrWhiteSpace(operation.Description))
			tocItems.Add(new ApiTocItem("Description", "description"));

		var queryParams = operation.Parameters?.Where(p => p.In == ParameterLocation.Query).ToArray() ?? [];
		if (queryParams.Length > 0)
			tocItems.Add(new ApiTocItem("Query String Parameters", "query-params"));

		if (operation.RequestBody is not null)
			tocItems.Add(new ApiTocItem("Request Body", "request-body"));

		if (operation.Responses is { Count: > 0 })
			tocItems.Add(new ApiTocItem(operation.Responses.Count == 1 ? "Response" : "Responses", "responses"));

		// Request body examples
		var reqContent = operation.RequestBody?.Content?.FirstOrDefault().Value;
		if (reqContent?.Examples is { Count: > 0 })
			tocItems.Add(new ApiTocItem("Request Examples", "request-examples"));

		// Response examples
		var successResp = operation.Responses?.FirstOrDefault(r => r.Key.StartsWith('2')).Value;
		var respContent = successResp?.Content?.FirstOrDefault().Value;
		if (respContent?.Examples is { Count: > 0 })
			tocItems.Add(new ApiTocItem("Response Examples", "response-examples"));

		return tocItems;
	}

	public static IReadOnlyList<CodeSample> ParseCodeSamples(OpenApiOperation operation)
	{
		if (operation.Extensions?.TryGetValue("x-codeSamples", out var ext) != true
			|| ext is not JsonNodeExtension jsonExt
			|| jsonExt.Node is not JsonArray samplesArray)
			return [];

		var samples = new List<CodeSample>();
		foreach (var item in samplesArray)
		{
			if (item is not JsonObject obj)
				continue;

			var lang = obj["lang"]?.GetValue<string>();
			var source = obj["source"]?.GetValue<string>();

			if (string.IsNullOrEmpty(lang) || string.IsNullOrEmpty(source))
				continue;

			samples.Add(new CodeSample(lang, source, CodeSample.GetHighlightClass(lang)));
		}

		// Console first when present, then preserve spec order
		samples.Sort((a, b) =>
		{
			var aIsConsole = string.Equals(a.Language, "Console", StringComparison.OrdinalIgnoreCase);
			var bIsConsole = string.Equals(b.Language, "Console", StringComparison.OrdinalIgnoreCase);
			if (aIsConsole && !bIsConsole)
				return -1;
			if (!aIsConsole && bIsConsole)
				return 1;
			return 0;
		});

		return samples;
	}
}
