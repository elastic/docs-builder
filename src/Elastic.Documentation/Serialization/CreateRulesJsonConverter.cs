// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Serialization;

/// <summary>
/// <see cref="CreateRules"/> travels inside the changelog PR-evaluation artifact, which another assembly serializes. The record's setters are
/// <c>internal</c> (the compile-time guard in <c>Constraints-Conventions.md</c>), and a source-generated serializer in another assembly cannot reach
/// them, so the record is read and written here, inside the assembly that owns it, through a wire shape with the same JSON.
/// </summary>
public sealed class CreateRulesJsonConverter : JsonConverter<CreateRules>
{
	public override CreateRules? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		JsonSerializer.Deserialize(ref reader, CreateRulesJsonContext.Default.CreateRulesWire)?.ToDomain();

	public override void Write(Utf8JsonWriter writer, CreateRules value, JsonSerializerOptions options) =>
		JsonSerializer.Serialize(writer, CreateRulesWire.From(value), CreateRulesJsonContext.Default.CreateRulesWire);
}

/// <summary>The JSON shape of <see cref="CreateRules"/>: the same keys and string enums the artifact has always had.</summary>
internal sealed record CreateRulesWire
{
	public IReadOnlyList<string>? Labels { get; init; }
	public FieldMode Mode { get; init; } = FieldMode.Exclude;
	public MatchMode Match { get; init; } = MatchMode.Any;
	public Dictionary<string, CreateRulesWire>? ByProduct { get; init; }

	public static CreateRulesWire From(CreateRules rules) =>
		new()
		{
			Labels = rules.Labels,
			Mode = rules.Mode,
			Match = rules.Match,
			ByProduct = rules.ByProduct?.ToDictionary(kv => kv.Key, kv => From(kv.Value))
		};

	public CreateRules ToDomain() =>
		new() { Labels = Labels, Mode = Mode, Match = Match, ByProduct = ByProduct?.ToDictionary(kv => kv.Key, kv => kv.Value.ToDomain()) };
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(CreateRulesWire))]
internal sealed partial class CreateRulesJsonContext : JsonSerializerContext;
