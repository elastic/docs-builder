// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.LegacyDocs.Migration;

/// <summary>Handles mixed-type branch sequences: plain strings and alias dicts like <c>{alias: branch}</c>.</summary>
public class BranchRefListConverter : YamlConverter<List<BranchRef>>
{
	public override List<BranchRef> Read(YamlReader reader)
	{
		var list = new List<BranchRef>();

		if (reader.TokenType != YamlTokenType.StartSequence)
		{
			reader.Skip();
			return list;
		}

		_ = reader.Read(); // consume StartSequence

		while (reader.TokenType != YamlTokenType.EndSequence)
		{
			if (reader.TokenType == YamlTokenType.Scalar)
			{
				var value = reader.ScalarValue ?? string.Empty;
				_ = reader.Read();
				list.Add(new BranchRef(value));
			}
			else if (reader.TokenType == YamlTokenType.StartMapping)
			{
				_ = reader.Read();

				// {alias: branch} — first key-value pair wins
				var key = reader.GetScalarValue();
				_ = reader.Read();
				var value = reader.GetScalarValue();
				_ = reader.Read();
				list.Add(new BranchRef(Name: value, Alias: key));

				// Consume remaining pairs (shouldn't exist but be safe)
				while (reader.TokenType != YamlTokenType.EndMapping)
					reader.Skip();
				_ = reader.Read(); // consume EndMapping
			}
			else
				reader.Skip();
		}
		_ = reader.Read(); // consume EndSequence

		return list;
	}

	// Fully qualified: this project's own YamlWriter (string-building helper, unrelated to YAML
	// parsing) shares a name with SharpYaml.Serialization.YamlWriter and wins unqualified lookups
	// since both are in-scope and this one is in the same namespace.
	public override void Write(SharpYaml.Serialization.YamlWriter writer, List<BranchRef> value)
	{
		writer.WriteStartSequence();

		foreach (var branch in value)
		{
			if (branch.Alias is not null)
			{
				writer.WriteStartMapping();
				writer.WritePropertyName(branch.Alias);
				writer.WriteScalar(branch.Name);
				writer.WriteEndMapping();
			}
			else
				writer.WriteScalar(branch.Name);
		}

		writer.WriteEndSequence();
	}
}

/// <summary>Handles a single <see cref="BranchRef"/> scalar or mapping node.</summary>
public class BranchRefConverter : YamlConverter<BranchRef>
{
	public override BranchRef Read(YamlReader reader)
	{
		if (reader.TokenType == YamlTokenType.Scalar)
		{
			var value = reader.ScalarValue ?? string.Empty;
			_ = reader.Read();
			return new BranchRef(value);
		}

		if (reader.TokenType == YamlTokenType.StartMapping)
		{
			_ = reader.Read();
			var key = reader.GetScalarValue();
			_ = reader.Read();
			var value = reader.GetScalarValue();
			_ = reader.Read();
			if (reader.TokenType != YamlTokenType.EndMapping)
				throw new YamlException(reader.SourceName, reader.Start, reader.End, "Expected end of mapping for BranchRef");
			_ = reader.Read();
			return new BranchRef(Name: value, Alias: key);
		}

		throw new YamlException(reader.SourceName, reader.Start, reader.End, "Expected scalar or mapping for BranchRef");
	}

	public override void Write(SharpYaml.Serialization.YamlWriter writer, BranchRef value)
	{
		if (value.Alias is not null)
		{
			writer.WriteStartMapping();
			writer.WritePropertyName(value.Alias);
			writer.WriteScalar(value.Name);
			writer.WriteEndMapping();
		}
		else
			writer.WriteScalar(value.Name);
	}
}
