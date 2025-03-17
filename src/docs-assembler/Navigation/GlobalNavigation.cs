// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using DotNet.Globbing;
using Elastic.Markdown.IO.Configuration;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Documentation.Assembler.Navigation;

public record TableOfContentsReference
{
	public required string Source { get; init; }
	public required string? PathPrefix { get; init; }
	public required IReadOnlyCollection<TableOfContentsReference> Children { get; init; }
}

public record GlobalNavigation
{
	public IReadOnlyCollection<TableOfContentsReference> References { get; init; } = [];

	public static GlobalNavigation Deserialize(AssembleContext context)
	{
		var globalConfig = new GlobalNavigation();
		var reader = new YamlStreamReader(context.NavigationPath, context.Collector);
		try
		{
			foreach (var entry in reader.Read())
			{
				switch (entry.Key)
				{
					case "toc":
						globalConfig = globalConfig with
						{
							References = ReadChildren(reader, entry.Entry, new Queue<string>())
						};
						break;
				}
			}
		}
		catch (Exception e)
		{
			reader.EmitError("Could not load docset.yml", e);
			throw;
		}

		return globalConfig;
	}

	private static IReadOnlyCollection<TableOfContentsReference> ReadChildren(
		YamlStreamReader reader,
		KeyValuePair<YamlNode, YamlNode> entry,
		Queue<string> parents
	)
	{
		var entries = new List<TableOfContentsReference>();
		if (entry.Value is not YamlSequenceNode sequence)
		{
			if (entry.Key is YamlScalarNode scalarKey)
			{
				var key = scalarKey.Value;
				reader.EmitWarning($"'{key}' is not an array");
			}
			else
				reader.EmitWarning($"'{entry.Key}' is not an array");

			return entries;
		}

		foreach (var tocEntry in sequence.Children.OfType<YamlMappingNode>())
		{
			var child = ReadChild(reader, tocEntry, parents);
			if (child is not null)
				entries.Add(child);
		}

		//TableOfContents = entries;
		return entries;
	}

	private static TableOfContentsReference? ReadChild(YamlStreamReader reader, YamlMappingNode tocEntry, Queue<string> parents)
	{
		string? source = null;
		string? pathPrefix = null;
		IReadOnlyCollection<TableOfContentsReference>? children = null;
		foreach (var entry in tocEntry.Children)
		{
			var key = ((YamlScalarNode)entry.Key).Value;
			switch (key)
			{
				case "toc":
					source = reader.ReadString(entry);
					break;
				case "path_prefix":
					pathPrefix = reader.ReadString(entry);
					break;
				case "children":
					if (source is null && pathPrefix is null)
					{
						reader.EmitWarning("toc entry has no toc or path_prefix defined");
						continue;
					}
					var path = source ?? pathPrefix;
					parents.Enqueue(path!);
					children = ReadChildren(reader, entry, parents);
					break;
			}
		}

		if (source is not null)
		{
			return new TableOfContentsReference
			{
				Source = source,
				Children = children ?? [],
				PathPrefix = pathPrefix
			};
		}

		return null;
	}
}
