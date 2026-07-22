// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>The parsed <c>x-tagGroups</c> document extension.</summary>
public sealed record XTagGroups(IReadOnlyList<string> OrderedGroupNames, IReadOnlyDictionary<string, string> TagToGroup);

/// <summary>Metadata parsed from a global OpenAPI tag object.</summary>
public sealed record OpenApiTagMetadata(string DisplayName, string Description, ApiTagExternalDoc? ExternalDocs);

/// <summary>
/// The single home for reading Elastic's <c>x-*</c> OpenAPI extensions
/// (<c>x-tagGroups</c>, <c>x-displayName</c>, <c>x-namespace</c>, <c>x-api-name</c>,
/// <c>x-beta</c>, <c>x-codeSamples</c>) out of the raw JsonNode extension values.
/// </summary>
public static class OpenApiExtensionReader
{
	/// <summary>Reads a string-valued extension; null when absent or not a string.</summary>
	public static string? TryGetString(IDictionary<string, IOpenApiExtension>? extensions, string key) =>
		extensions?.TryGetValue(key, out var value) == true && value is JsonNodeExtension json
			? json.Node.GetValue<string>()
			: null;

	/// <summary>The <c>x-namespace</c> and <c>x-api-name</c> pair used to group operations into endpoints.</summary>
	public static (string? Namespace, string? ApiName) GetNamespaceAndApiName(OpenApiOperation operation) =>
		(TryGetString(operation.Extensions, "x-namespace"), TryGetString(operation.Extensions, "x-api-name"));

	/// <summary>Whether the operation is marked <c>x-beta: true</c>.</summary>
	public static bool IsBeta(OpenApiOperation operation) =>
		operation.Extensions?.TryGetValue("x-beta", out var betaValue) == true
		&& betaValue is JsonNodeExtension betaExtension
		&& betaExtension.Node is JsonValue betaJsonValue
		&& betaJsonValue.TryGetValue<bool>(out var betaFlag) && betaFlag;

	/// <summary>Parses the document-level <c>x-tagGroups</c> extension; null when absent or empty.</summary>
	public static XTagGroups? ParseXTagGroups(OpenApiDocument openApiDocument)
	{
		if (openApiDocument.Extensions?.TryGetValue("x-tagGroups", out var extension) != true || extension is not JsonNodeExtension jsonExt)
			return null;

		if (jsonExt.Node is not JsonArray array || array.Count == 0)
			return null;

		var orderedGroupNames = new List<string>();
		var tagToGroup = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach (var element in array)
		{
			if (element is not JsonObject groupObj)
				continue;

			if (!groupObj.TryGetPropertyValue("name", out var nameNode))
				continue;

			var groupName = nameNode?.GetValue<string>();
			if (string.IsNullOrWhiteSpace(groupName))
				continue;

			if (!orderedGroupNames.Contains(groupName))
				orderedGroupNames.Add(groupName);

			if (!groupObj.TryGetPropertyValue("tags", out var tagsNode) || tagsNode is not JsonArray tagNames)
				continue;

			foreach (var tagElement in tagNames)
			{
				var tagName = tagElement?.GetValue<string>();
				if (string.IsNullOrEmpty(tagName))
					continue;

				if (!tagToGroup.ContainsKey(tagName))
					tagToGroup[tagName] = groupName;
			}
		}

		if (orderedGroupNames.Count == 0 || tagToGroup.Count == 0)
			return null;

		return new XTagGroups(orderedGroupNames, tagToGroup);
	}

	/// <summary>
	/// Parses global OpenAPI tag objects: description, <c>externalDocs</c>, and <c>x-displayName</c>.
	/// </summary>
	public static Dictionary<string, OpenApiTagMetadata> ParseTagMetadata(OpenApiDocument openApiDocument)
	{
		var result = new Dictionary<string, OpenApiTagMetadata>(StringComparer.Ordinal);

		if (openApiDocument.Tags is null)
			return result;

		foreach (var tag in openApiDocument.Tags)
		{
			var tagName = tag.Name;
			if (string.IsNullOrEmpty(tagName))
				continue;

			var displayName = tagName;
			var xDisplayName = TryGetString(tag.Extensions, "x-displayName");
			if (!string.IsNullOrWhiteSpace(xDisplayName))
				displayName = xDisplayName;

			var description = tag.Description ?? string.Empty;
			ApiTagExternalDoc? extDoc = null;
			if (tag.ExternalDocs?.Url is not null)
			{
				var url = tag.ExternalDocs.Url.ToString();
				if (!string.IsNullOrEmpty(url))
					extDoc = new ApiTagExternalDoc(tag.ExternalDocs.Description, url);
			}

			result[tagName] = new OpenApiTagMetadata(displayName, description, extDoc);
		}

		return result;
	}

	/// <summary>
	/// Parses the <c>x-codeSamples</c> extension into code samples, ordered with Console first.
	/// </summary>
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
		samples.Sort(static (a, b) =>
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
