// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// YAML type converter for <see cref="YamlLenientList"/> that accepts both string and list forms.
/// <list type="bullet">
/// <item>Scalar: splits by comma, trims entries, removes empties.</item>
/// <item>Sequence: reads each scalar item into a list.</item>
/// <item>Null/empty: returns <c>YamlLenientList(null)</c>.</item>
/// </list>
/// </summary>
public class YamlLenientListConverter : YamlConverter<YamlLenientList>
{
	public override YamlLenientList Read(YamlReader reader)
	{
		if (reader.TokenType == YamlTokenType.Scalar)
		{
			var scalarValue = reader.ScalarValue;
			_ = reader.Read();

			if (string.IsNullOrEmpty(scalarValue) || scalarValue == "~")
				return new YamlLenientList(null);

			var items = scalarValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

			return new YamlLenientList(items.Count > 0 ? items : null);
		}

		if (reader.TokenType == YamlTokenType.StartSequence)
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

			return new YamlLenientList(items.Count > 0 ? items : null);
		}

		reader.Skip();
		return new YamlLenientList(null);
	}

	public override void Write(YamlWriter writer, YamlLenientList value)
	{
		if (value is not { Values: { Count: > 0 } values })
		{
			writer.WriteScalar(string.Empty);
			return;
		}

		writer.WriteStartSequence();

		foreach (var item in values)
			writer.WriteScalar(item);

		writer.WriteEndSequence();
	}

	/// <summary>
	/// Reads a scalar-or-sequence value from the reader and returns a joined comma-separated string.
	/// Useful for fields that stay as <c>string?</c> in the domain model (e.g., ChangelogTypeEntryDto.Labels).
	/// </summary>
	internal static string? ReadAsString(YamlReader reader)
	{
		if (reader.TokenType == YamlTokenType.Scalar)
		{
			var scalarValue = reader.ScalarValue;
			_ = reader.Read();
			return string.IsNullOrEmpty(scalarValue) || scalarValue == "~" ? null : scalarValue;
		}

		if (reader.TokenType == YamlTokenType.StartSequence)
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

			return items.Count > 0 ? string.Join(", ", items) : null;
		}

		reader.Skip();
		return null;
	}
}
