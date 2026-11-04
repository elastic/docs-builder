// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Toc.Listing;

/// <summary>
/// Reads only the <c>listing:</c> frontmatter key from a Markdown file without fully parsing it.
/// Supports both shorthand (<c>listing: group-name</c>) and full form (<c>listing: {group: name}</c>).
/// </summary>
public static class ListingFrontMatterReader
{
	/// <summary>
	/// Returns the listing group name declared in the file's frontmatter, or <c>null</c> if none.
	/// </summary>
	public static string? ReadGroup(IFileInfo file)
	{
		try
		{
			var content = file.FileSystem.File.ReadAllText(file.FullName);
			return ReadGroupFromContent(content);
		}
		catch
		{
			return null;
		}
	}

	internal static string? ReadGroupFromContent(string content)
	{
		// Fast path: no frontmatter delimiter
		if (!content.StartsWith("---", StringComparison.Ordinal))
			return null;

		// Extract frontmatter between the two --- delimiters
		var start = content.IndexOf('\n');
		if (start < 0)
			return null;
		var end = content.IndexOf("\n---", start, StringComparison.Ordinal);
		if (end < 0)
			return null;

		var yaml = content[(start + 1)..end];

		try
		{
			var reader = YamlReader.Create(yaml);
			if (!reader.Read() || reader.TokenType != YamlTokenType.StartMapping)
				return null;
			_ = reader.Read(); // consume StartMapping

			while (reader.TokenType is not YamlTokenType.EndMapping and not YamlTokenType.None)
			{
				if (reader.TokenType != YamlTokenType.Scalar)
				{
					reader.Skip();
					continue;
				}

				var key = reader.ScalarValue;
				_ = reader.Read();

				if (key != "listing")
				{
					reader.Skip();
					continue;
				}

				// Found the listing key — value is either a scalar (group name) or a mapping {group: name}
				if (reader.TokenType == YamlTokenType.Scalar)
				{
					var scalarValue = reader.ScalarValue;
					return string.IsNullOrWhiteSpace(scalarValue) ? null : scalarValue;
				}

				if (reader.TokenType == YamlTokenType.StartMapping)
				{
					_ = reader.Read(); // consume StartMapping
					while (reader.TokenType != YamlTokenType.EndMapping)
					{
						if (reader.TokenType != YamlTokenType.Scalar)
						{
							reader.Skip();
							continue;
						}
						var mapKey = reader.ScalarValue;
						_ = reader.Read();
						if (mapKey == "group" && reader.TokenType == YamlTokenType.Scalar)
							return string.IsNullOrWhiteSpace(reader.ScalarValue) ? null : reader.ScalarValue;
						reader.Skip();
					}
				}

				return null;
			}
		}
		catch
		{
			// Ignore parse errors — the frontmatter will be validated properly when the file is parsed
		}

		return null;
	}
}
