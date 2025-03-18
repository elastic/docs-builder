// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Documentation.Assembler.Configuration;
using Elastic.Markdown.IO.Configuration;
using YamlDotNet.RepresentationModel;

namespace Documentation.Assembler.Navigation;

public record TableOfContentsReference
{
	public required Uri Source { get; init; }
	public required string SourcePrefix { get; init; }
	public required string PathPrefix { get; init; }
	public required IReadOnlyCollection<TableOfContentsReference> Children { get; init; }
}

public record GlobalNavigationFile
{
	public IReadOnlyCollection<TableOfContentsReference> TableOfContents { get; init; } = [];

	public FrozenDictionary<string, TableOfContentsReference> IndexedTableOfContents { get; init; } =
		new Dictionary<string, TableOfContentsReference>().ToFrozenDictionary();

	public static GlobalNavigationFile Deserialize(AssembleContext context)
	{
		var globalConfig = new GlobalNavigationFile();
		var reader = new YamlStreamReader(context.NavigationPath, context.Collector);
		try
		{
			foreach (var entry in reader.Read())
			{
				switch (entry.Key)
				{
					case "toc":
						var toc = ReadChildren(reader, entry.Entry);
						var indexed = toc
							.SelectMany(YieldAll)
							.ToDictionary(t => t.Source.ToString(), t => t)
							.ToFrozenDictionary();
						globalConfig = globalConfig with
						{
							TableOfContents = toc,
							IndexedTableOfContents = indexed
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

	private static IEnumerable<TableOfContentsReference> YieldAll(TableOfContentsReference toc)
	{
		yield return toc;
		foreach (var tocEntry in toc.Children)
		{
			foreach (var child in YieldAll(tocEntry))
				yield return child;
		}
	}

	private static IReadOnlyCollection<TableOfContentsReference> ReadChildren(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry)
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
			var child = ReadChild(reader, tocEntry);
			if (child is not null)
				entries.Add(child);
		}
		return entries;
	}

	private static TableOfContentsReference? ReadChild(YamlStreamReader reader, YamlMappingNode tocEntry)
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
					if (source.AsSpan().IndexOf("://") == -1)
					{
						source = $"{NarrativeRepository.RepositoryName}://{source}";
						pathPrefix = source;
					}
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
					children = ReadChildren(reader, entry);
					break;
			}
		}

		if (source is null)
			return null;

		if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
		{
			reader.EmitError($"Source toc entry is not a valid uri: {source}", tocEntry);
			return null;
		}
		var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";
		if (string.IsNullOrEmpty(pathPrefix))
			reader.EmitError($"Path prefix is not defined for: {source}, falling back to {sourcePrefix} which may be incorrect", tocEntry);

		pathPrefix ??= sourcePrefix;

		return new TableOfContentsReference
		{
			Source = sourceUri,
			SourcePrefix = sourcePrefix,
			Children = children ?? [],
			PathPrefix = pathPrefix
		};

	}
}
