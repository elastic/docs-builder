// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// YAML type converter for TypeEntryYaml that handles both string and object forms.
/// String form: "label1, label2"
/// Object form: { labels: "label1", subtypes: { api: "label" } }
/// Labels and subtype values accept both scalar (comma-separated) and sequence (list) forms.
/// </summary>
public class TypeEntryYamlConverter : YamlConverter<TypeEntryYaml>
{
	private static readonly string[] KnownKeys = ["labels", "subtypes"];

	public override TypeEntryYaml Read(YamlReader reader)
	{
		// Handle null value
		if (reader.TokenType == YamlTokenType.Scalar)
		{
			var scalarValue = reader.ScalarValue;
			_ = reader.Read();

			// Handle empty scalar (null/empty value)
			if (string.IsNullOrEmpty(scalarValue) || scalarValue == "~")
				return new TypeEntryYaml();

			// Handle string form: just labels
			return TypeEntryYaml.FromLabels(scalarValue);
		}

		// Handle object form: { labels: "...", subtypes: {...} }
		if (reader.TokenType == YamlTokenType.StartMapping)
		{
			_ = reader.Read();
			var entry = new TypeEntryYaml();

			while (reader.TokenType != YamlTokenType.EndMapping)
			{
				var key = reader.GetScalarValue();
				_ = reader.Read();

				switch (key)
				{
					case "labels":
						// Accept both scalar and sequence for labels
						entry = entry with { Labels = YamlLenientListConverter.ReadAsString(reader) };
						break;
					case "subtypes":
						entry = entry with { Subtypes = ParseSubtypes(reader) };
						break;
					default:
						reader.SkipOrReportUnmapped(typeof(TypeEntryYaml), KnownKeys, key, entry);
						break;
				}
			}
			_ = reader.Read(); // consume EndMapping

			return entry;
		}

		// Unknown format
		reader.Skip();
		return new TypeEntryYaml();
	}

	private static Dictionary<string, YamlLenientList?>? ParseSubtypes(YamlReader reader)
	{
		if (reader.TokenType != YamlTokenType.StartMapping)
		{
			// Handle null/scalar value
			reader.Skip();
			return null;
		}

		_ = reader.Read(); // consume StartMapping
		var subtypes = new Dictionary<string, YamlLenientList?>();

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();

			// Accept both scalar and sequence for each subtype value
			if (reader.TokenType == YamlTokenType.Scalar)
			{
				var valueScalar = reader.ScalarValue;
				_ = reader.Read();
				if (string.IsNullOrEmpty(valueScalar) || valueScalar == "~")
					subtypes[key] = null;
				else
				{
					var items = valueScalar.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
					subtypes[key] = new YamlLenientList(items.Count > 0 ? items : null);
				}
			}
			else if (reader.TokenType == YamlTokenType.StartSequence)
			{
				_ = reader.Read();
				var items = new List<string>();
				while (reader.TokenType != YamlTokenType.EndSequence)
				{
					var item = reader.GetScalarValue();
					_ = reader.Read();
					if (!string.IsNullOrWhiteSpace(item))
						items.Add(item.Trim());
				}
				_ = reader.Read(); // consume EndSequence
				subtypes[key] = new YamlLenientList(items.Count > 0 ? items : null);
			}
			else
			{
				reader.Skip();
				subtypes[key] = null;
			}
		}
		_ = reader.Read(); // consume EndMapping

		return subtypes;
	}

	public override void Write(YamlWriter writer, TypeEntryYaml value)
	{
		// If only labels and no subtypes, emit as simple string
		if (value.Subtypes == null || value.Subtypes.Count == 0)
		{
			writer.WriteScalar(value.Labels ?? string.Empty);
			return;
		}

		// Otherwise emit as object
		writer.WriteStartMapping();

		if (!string.IsNullOrEmpty(value.Labels))
		{
			writer.WritePropertyName("labels");
			writer.WriteString(value.Labels);
		}

		if (value.Subtypes is { Count: > 0 })
		{
			writer.WritePropertyName("subtypes");
			writer.WriteStartMapping();

			foreach (var (subKey, subValue) in value.Subtypes)
			{
				writer.WritePropertyName(subKey);
				var joinedValue = subValue?.Values is { Count: > 0 } vals ? string.Join(", ", vals) : string.Empty;
				writer.WriteScalar(joinedValue);
			}

			writer.WriteEndMapping();
		}

		writer.WriteEndMapping();
	}
}
