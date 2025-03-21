// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using Documentation.Assembler.Configuration;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;
using YamlDotNet.RepresentationModel;

namespace Documentation.Assembler.Navigation;

[DebuggerDisplay("Toc >{Depth} #{Order} {Source}")]
public record TableOfContentsReference : INavigationItem
{
	public required Uri Source { get; init; }
	public required string SourcePrefix { get; init; }
	public required string PathPrefix { get; init; }
	public required IReadOnlyCollection<TableOfContentsReference> Children { get; init; }

	public required int Order { get; init; }
	public required int Depth { get; init; }
	public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
}

public record GlobalNavigationConfiguration
{
	public IReadOnlyCollection<TableOfContentsReference> TableOfContents { get; init; } = [];

	public FrozenDictionary<string, TableOfContentsReference> IndexedTableOfContents { get; init; } =
		new Dictionary<string, TableOfContentsReference>().ToFrozenDictionary();

	public static GlobalNavigationConfiguration Deserialize(AssembleContext context)
	{
		var globalConfig = new GlobalNavigationConfiguration();
		var reader = new YamlStreamReader(context.NavigationPath, context.Collector);
		try
		{
			foreach (var entry in reader.Read())
			{
				switch (entry.Key)
				{
					case "toc":
						var toc = ReadChildren(reader, entry.Entry, null, 0);
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

	private static IReadOnlyCollection<TableOfContentsReference> ReadChildren(
		YamlStreamReader reader,
		KeyValuePair<YamlNode, YamlNode> entry,
		string? parent,
		int depth
	)
	{
		var entries = new List<TableOfContentsReference>();
		if (entry.Key is not YamlScalarNode { Value: { } key } scalarKey)
		{
			reader.EmitWarning($"key '{entry.Key}' is not string");
			return [];
		}

		if (entry.Value is not YamlSequenceNode sequence)
		{
			reader.EmitWarning($"'{scalarKey.Value}' is not an array");
			return [];
		}

		var i = 0;
		foreach (var tocEntry in sequence.Children.OfType<YamlMappingNode>())
		{
			var child = ReadChild(reader, tocEntry, parent, depth, i);
			if (child is not null)
				entries.Add(child);
			i++;
		}

		return entries;
	}

	private static TableOfContentsReference? ReadChild(YamlStreamReader reader, YamlMappingNode tocEntry, string? parent, int depth, int order)
	{
		string? repository = null;
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
						parent = source;
						pathPrefix = source;
						source = $"{NarrativeRepository.RepositoryName}://{source}";
					}

					break;
				case "repo":
					repository = reader.ReadString(entry);
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

					children = ReadChildren(reader, entry, parent, depth + 1);
					break;
			}
		}

		if (repository is not null)
		{
			if (source is not null)
				reader.EmitError($"toc config defines 'repo' can not be combined with 'toc': {source}", tocEntry);
			if (children is not null)
				reader.EmitError($"toc config defines 'repo' can not be combined with 'children'", tocEntry);
			pathPrefix = string.Join("/", [parent, repository]);
			source = $"{repository}://{parent}";
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
			PathPrefix = pathPrefix,
			Depth = depth,
			Order = order
		};
	}
}
