// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;

namespace Elastic.Changelog.Tests.Snapshots;

/// <summary>
/// Dumps a loaded <see cref="ChangelogConfiguration"/> by reflection, so a property added to the model shows up in every
/// snapshot without this helper changing. Enums read as names and a <see cref="Product"/> as its id.
/// </summary>
internal static class ChangelogConfigurationDump
{
	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true,
		IncludeFields = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		Converters = { new JsonStringEnumConverter(), new ProductIdConverter() }
	};

	/// <summary>The configuration (or <c>null</c>), the diagnostics in order and, when loading threw, the exception.</summary>
	public static string ToJson(ChangelogConfiguration? configuration, IEnumerable<Diagnostic> diagnostics, Exception? exception = null)
	{
		var root = new JsonObject
		{
			["configuration"] = configuration is null ? null : JsonSerializer.SerializeToNode(configuration, Options),
			["diagnostics"] = new JsonArray(
				diagnostics
					.ToSnapshotText(firstLineOnly: true)
					.Split('\n', StringSplitOptions.RemoveEmptyEntries)
					.Select(l => (JsonNode?)JsonValue.Create(l))
					.ToArray()
			)
		};
		if (exception is not null)
			root["exception"] = $"{exception.GetType().Name}: {exception.Message}";
		return root.ToJsonString(Options);
	}

	public static string ToJson(object? value) => JsonSerializer.Serialize(value, Options);

	private sealed class ProductIdConverter : JsonConverter<Product>
	{
		public override Product? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			throw new NotSupportedException();

		public override void Write(Utf8JsonWriter writer, Product value, JsonSerializerOptions options) =>
			writer.WriteStringValue(value.Id);
	}
}
