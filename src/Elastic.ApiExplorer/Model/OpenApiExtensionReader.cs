// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Topics;
using Elastic.Documentation.Extensions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>The parsed <c>x-tagGroups</c> document extension.</summary>
public sealed record XTagGroups(IReadOnlyList<string> OrderedGroupNames, IReadOnlyDictionary<string, string> TagToGroup);

/// <summary>Metadata parsed from a global OpenAPI tag object.</summary>
public sealed record OpenApiTagMetadata(string DisplayName, string Description, ApiTagExternalDoc? ExternalDocs);

/// <summary>
/// The single home for reading Elastic's <c>x-*</c> OpenAPI extensions
/// (<c>x-tagGroups</c>, <c>x-topics</c>, <c>x-displayName</c>, <c>x-namespace</c>, <c>x-api-name</c>,
/// <c>x-beta</c>, <c>x-codeSamples</c>) out of the raw JsonNode extension values.
/// </summary>
public static class OpenApiExtensionReader
{
	/// <summary>
	/// The tag an operation references, read from the reference id the way navigation does, so an operation
	/// tagged with a name the document never declares still has one. Null when blank.
	/// </summary>
	public static string? TagName(OpenApiTagReference tag)
	{
		var name = tag.Reference?.Id ?? tag.Name;
		return string.IsNullOrWhiteSpace(name) ? null : name;
	}

	/// <summary>The metadata of a tag as navigation shows it: the declared entry, else the capitalized name alone.</summary>
	public static OpenApiTagMetadata TagMetadataFor(string tag, IReadOnlyDictionary<string, OpenApiTagMetadata> metadata) =>
		metadata.TryGetValue(tag, out var meta) ? meta : new OpenApiTagMetadata(tag.CapitalizeFirstLetter(), string.Empty, null);

	/// <summary>Reads a string-valued extension; null when absent or not a string.</summary>
	public static string? TryGetString(IDictionary<string, IOpenApiExtension>? extensions, string key) =>
		extensions?.TryGetValue(key, out var value) == true
			&& value is JsonNodeExtension { Node: JsonValue jsonValue }
			&& jsonValue.TryGetValue<string>(out var text) ? text : null;

	/// <summary>
	/// Reads the raw <c>x-state</c> label (for example "Generally available; Added in 7.7.0") of an operation, a parameter or
	/// a schema; null when absent or blank.
	/// </summary>
	public static string? GetState(IDictionary<string, IOpenApiExtension>? extensions) =>
		TryGetString(extensions, "x-state") is { } state && !string.IsNullOrWhiteSpace(state) ? state.Trim() : null;

	/// <summary>The <c>x-namespace</c> and <c>x-api-name</c> pair used to group operations into endpoints.</summary>
	public static (string? Namespace, string? ApiName) GetNamespaceAndApiName(OpenApiOperation operation) =>
		(TryGetString(operation.Extensions, "x-namespace"), TryGetString(operation.Extensions, "x-api-name"));

	/// <summary>The operation summary, trimmed; null when absent or blank (specs sometimes emit <c>summary: ''</c>).</summary>
	public static string? GetSummary(OpenApiOperation operation) =>
		string.IsNullOrWhiteSpace(operation.Summary) ? null : operation.Summary.Trim();

	/// <summary>Whether the operation is marked <c>x-beta: true</c>.</summary>
	public static bool IsBeta(OpenApiOperation operation) =>
		operation.Extensions?.TryGetValue("x-beta", out var betaValue) == true
			&& betaValue is JsonNodeExtension betaExtension
			&& betaExtension.Node is JsonValue betaJsonValue
			&& betaJsonValue.TryGetValue<bool>(out var betaFlag)
			&& betaFlag;

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

	/// <summary>Parses the <c>x-topics</c> document extension in spec order. Entries without a title are skipped.</summary>
	public static IReadOnlyList<ApiTopicPage> ParseXTopics(OpenApiDocument openApiDocument)
	{
		if (openApiDocument.Extensions?.TryGetValue("x-topics", out var extension) != true || extension is not JsonNodeExtension jsonExt)
			return [];

		if (jsonExt.Node is not JsonArray array)
			return [];

		var topics = new List<ApiTopicPage>(array.Count);
		foreach (var element in array)
		{
			if (element is not JsonObject topic)
				continue;

			var title = ReadString(topic, "title");
			if (string.IsNullOrWhiteSpace(title))
				continue;

			topics.Add(new ApiTopicPage(title.Trim(), ReadString(topic, "content") ?? string.Empty));
		}

		return topics;
	}

	private static string? ReadString(JsonObject obj, string property) =>
		obj.TryGetPropertyValue(property, out var node) && node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

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

			var displayName = tagName.CapitalizeFirstLetter();
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
	public static IReadOnlyList<CodeSample> ParseCodeSamples(OpenApiOperation operation) => ParseCodeSamples(operation.Extensions);

	/// <summary>Parses the <c>x-codeSamples</c> extension of any extensible OpenAPI object, such as a request example.</summary>
	public static IReadOnlyList<CodeSample> ParseCodeSamples(IDictionary<string, IOpenApiExtension>? extensions)
	{
		if (
			extensions?.TryGetValue("x-codeSamples", out var ext) != true
			|| ext is not JsonNodeExtension jsonExt
			|| jsonExt.Node is not JsonArray samplesArray
		)
			return [];

		var samples = new List<(CodeSample Sample, string? Label)>();
		foreach (var item in samplesArray)
		{
			if (item is not JsonObject obj)
				continue;

			var lang = obj["lang"]?.GetValue<string>();
			var source = obj["source"]?.GetValue<string>();

			if (string.IsNullOrEmpty(lang) || string.IsNullOrEmpty(source))
				continue;

			var (language, scenario) = CodeSample.SplitLanguage(lang);
			var displaySource = string.Equals(language, "curl", StringComparison.OrdinalIgnoreCase)
				? CurlSourceFormatter.Format(source)
				: source;
			var label = scenario is null ? obj["label"]?.GetValue<string>()?.Trim() : null;
			if (string.Equals(label, lang, StringComparison.OrdinalIgnoreCase))
				label = null;

			var sample = new CodeSample(language, displaySource, CodeSample.GetHighlightClass(language)) { Scenario = scenario };
			samples.Add((sample, label));
		}

		// A label only says which example a sample belongs to when its language shows up more than once.
		// Elsewhere it is a caption (often just the language name) and the samples stay one set.
		var repeated = samples
			.GroupBy(static s => s.Sample.Language, StringComparer.OrdinalIgnoreCase)
			.Where(static g => g.Count() > 1)
			.Select(static g => g.Key)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var tied = samples.Select(
			s => !string.IsNullOrWhiteSpace(s.Label) && repeated.Contains(s.Sample.Language)
				? s.Sample with { ExampleTitle = s.Label }
				: s.Sample
		);

		// Console first, then by language popularity; OrderBy is stable, so unranked languages keep spec order.
		return [.. tied.OrderBy(static s => s.Rank)];
	}
}
