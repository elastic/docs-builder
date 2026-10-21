// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Converters;

/// <summary>The hint types a <c>suppress</c> list names, with the names it dropped as written (see <c>Constraints-Conventions.md</c>).</summary>
public sealed class HintTypeSet : HashSet<HintType>
{
	/// <summary>Names that are not a hint type, in the order written; a <c>suppress</c> that is not a list is kept here as its text.</summary>
	public List<string> UnknownNames { get; } = [];
}

/// <summary>
/// YAML converter for deserializing a list of strings into a HashSet of HintType enums.
/// </summary>
public class HintTypeSetConverter : YamlConverter<HashSet<HintType>>
{
	public override HashSet<HintType> Read(YamlReader reader)
	{
		var result = new HintTypeSet();

		if (reader.TokenType != YamlTokenType.StartSequence)
		{
			if (reader.TokenType == YamlTokenType.Scalar && reader.ScalarValue is { Length: > 0 } text)
				result.UnknownNames.Add(text);
			reader.Skip();
			return result;
		}

		_ = reader.Read(); // consume StartSequence

		while (reader.TokenType != YamlTokenType.EndSequence)
		{
			if (reader.TokenType == YamlTokenType.Scalar)
			{
				var value = reader.ScalarValue;
				if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<HintType>(value, ignoreCase: true, out var hintType))
					_ = result.Add(hintType);
				else
					result.UnknownNames.Add(value ?? string.Empty);
			}
			_ = reader.Read();
		}

		_ = reader.Read(); // consume EndSequence

		return result;
	}

	public override void Write(YamlWriter writer, HashSet<HintType> value)
	{
		writer.WriteStartSequence();
		foreach (var hint in value)
			writer.WriteScalar(hint.ToString());
		writer.WriteEndSequence();
	}
}
