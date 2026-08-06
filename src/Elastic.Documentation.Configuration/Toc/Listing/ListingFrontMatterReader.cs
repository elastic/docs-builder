// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

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
			var reader = new StringReader(yaml);
			var parser = new Parser(reader);
			_ = parser.TryConsume<StreamStart>(out _);
			_ = parser.TryConsume<DocumentStart>(out _);
			if (!parser.TryConsume<MappingStart>(out _))
				return null;

			while (!parser.TryConsume<MappingEnd>(out _) && !parser.Accept<DocumentEnd>(out _) && !parser.Accept<StreamEnd>(out _))
			{
				if (!parser.TryConsume<Scalar>(out var key))
				{
					parser.SkipThisAndNestedEvents();
					continue;
				}

				if (key.Value != "listing")
				{
					parser.SkipThisAndNestedEvents();
					continue;
				}

				// Found the listing key — value is either a scalar (group name) or a mapping {group: name}
				if (parser.TryConsume<Scalar>(out var scalar))
					return string.IsNullOrWhiteSpace(scalar.Value) ? null : scalar.Value;

				if (parser.TryConsume<MappingStart>(out _))
				{
					while (!parser.TryConsume<MappingEnd>(out _))
					{
						if (!parser.TryConsume<Scalar>(out var mapKey))
						{
							parser.SkipThisAndNestedEvents();
							continue;
						}
						if (mapKey.Value == "group" && parser.TryConsume<Scalar>(out var groupVal))
							return string.IsNullOrWhiteSpace(groupVal.Value) ? null : groupVal.Value;
						parser.SkipThisAndNestedEvents();
					}
				}
				else
				{
					parser.SkipThisAndNestedEvents();
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
