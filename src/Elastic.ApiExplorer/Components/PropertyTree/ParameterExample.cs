// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Nodes;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>
/// The example a parameter row shows. Only one that says something the row's type does not: a spec that gives every
/// parameter an example would otherwise put a line of noise under each one.
/// </summary>
internal static class ParameterExample
{
	// A longer value needs more than a line; the page's examples panel shows such values in context.
	private const int MaxLength = 60;

	/// <summary>
	/// The first usable example of a parameter: its <c>example</c>, then its <c>examples</c>, then its schema's. Null when
	/// none is worth showing. <paramref name="schema"/> is the resolved schema, and <paramref name="enumValues"/> the
	/// literals its values can take.
	/// </summary>
	public static string? Pick(IOpenApiParameter parameter, IOpenApiSchema schema, IReadOnlyCollection<string> enumValues)
	{
		var candidates = new[] { parameter.Example }.Concat(parameter.Examples?.Values.Select(static e => e.Value) ?? []).Append(
			SchemaHelpers.FirstExample(schema)
		);
		return candidates
			.Where(SchemaHelpers.IsValue)
			.Select(example => Display(example!, schema, enumValues))
			.FirstOrDefault(static d => d is not null);
	}

	private static string? Display(JsonNode example, IOpenApiSchema schema, IReadOnlyCollection<string> enumValues)
	{
		JsonNode?[] leaves = example is JsonArray array ? [.. array] : [example];
		if (
			leaves.Length == 0
			|| leaves.Any(static l => Text(l) is null or { Length: 0 })
			|| IsSelfEvident(example, leaves, schema, enumValues)
		)
			return null;

		var text = Text(example) ?? example.ToJsonString();
		return text.Length <= MaxLength ? text : null;
	}

	/// <summary>
	/// A value the reader could guess: a boolean, a bare number (<c>page: 1</c>), one of the listed values, or the default.
	/// A boolean spelled as text (<c>kbn-xsrf: "true"</c>) counts too. The value decides, not the schema's type, since a
	/// schema of <c>type: [string, boolean]</c> can still have an identifier as its example.
	/// </summary>
	private static bool IsSelfEvident(JsonNode example, JsonNode?[] leaves, IOpenApiSchema schema, IReadOnlyCollection<string> enumValues)
	{
		var isBoolean = leaves.All(
			static l => l?.GetValueKind() is JsonValueKind.True or JsonValueKind.False || Text(l) is "true" or "false"
		);
		// A number with a format (an int64 timestamp) says what it is; an array's format sits on its items.
		var isBareNumber = leaves.All(static l => l?.GetValueKind() is JsonValueKind.Number)
			&& string.IsNullOrEmpty(ApiPropertyTreeBuilder.Format(schema));
		var isListed = enumValues.Count > 0 && leaves.All(l => enumValues.Contains(Text(l)));
		var isDefault = schema.Default is { } fallback && fallback.ToJsonString() == example.ToJsonString();
		return isBoolean || isBareNumber || isListed || isDefault;
	}

	/// <summary>A string as written, any other value as JSON; null for nothing.</summary>
	private static string? Text(JsonNode? node) =>
		node is JsonValue value && value.TryGetValue<string>(out var text) ? text : node?.ToJsonString();
}
