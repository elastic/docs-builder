// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Documentation.Assembler.Building;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Documentation.Model.Configuration.Assembler;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Links.CrossLinks;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.RepresentationModel;

namespace Documentation.Assembler;
public record TocTopLevelMapping
{
	public required Uri Source { get; init; }
	public required string SourcePathPrefix { get; init; }
	public required Uri TopLevelSource { get; init; }
	public required Uri ParentSource { get; init; }
}

public record TocConfigurationMapping
{
	public required TocTopLevelMapping TopLevel { get; init; }
	public required ConfigurationFile RepositoryConfigurationFile { get; init; }
	public required TableOfContentsConfiguration TableOfContentsConfiguration { get; init; }
}

public class AssembleSources
{
	public AssembleContext AssembleContext { get; }
	public FrozenDictionary<string, AssemblerDocumentationSet> AssembleSets { get; }

	public FrozenDictionary<Uri, TocTopLevelMapping> TocTopLevelMappings { get; }

	public FrozenDictionary<string, string> HistoryMappings { get; }

	public FrozenDictionary<Uri, TocConfigurationMapping> TocConfigurationMapping { get; }

	public TableOfContentsTreeCollector TreeCollector { get; } = new();

	public PublishEnvironmentUriResolver UriResolver { get; }

	public static async Task<AssembleSources> AssembleAsync(AssembleContext context, Checkout[] checkouts, Cancel ctx)
	{
		var sources = new AssembleSources(context, checkouts);
		foreach (var (_, set) in sources.AssembleSets)
			await set.DocumentationSet.ResolveDirectoryTree(ctx);
		return sources;
	}

	private AssembleSources(AssembleContext assembleContext, Checkout[] checkouts)
	{
		AssembleContext = assembleContext;
		TocTopLevelMappings = GetConfiguredSources(assembleContext);
		HistoryMappings = GetHistoryMapping(assembleContext);

		var crossLinkFetcher = new AssemblerCrossLinkFetcher(NullLoggerFactory.Instance, assembleContext.Configuration, assembleContext.Environment);
		UriResolver = new PublishEnvironmentUriResolver(TocTopLevelMappings, assembleContext.Environment);
		var crossLinkResolver = new CrossLinkResolver(crossLinkFetcher, UriResolver);
		AssembleSets = checkouts
			.Where(c => !c.Repository.Skip)
			.Select(c => new AssemblerDocumentationSet(NullLoggerFactory.Instance, assembleContext, c, crossLinkResolver, TreeCollector))
			.ToDictionary(s => s.Checkout.Repository.Name, s => s)
			.ToFrozenDictionary();

		TocConfigurationMapping = TocTopLevelMappings
			.Select(kv =>
			{
				var repo = kv.Value.Source.Scheme;
				if (!AssembleSets.TryGetValue(repo, out var set))
					throw new Exception($"Unable to find repository: {repo}");

				var fs = set.BuildContext.ReadFileSystem;
				var config = set.BuildContext.Configuration;
				var tocDirectory = Path.Combine(config.ScopeDirectory.FullName, kv.Value.Source.Host, kv.Value.Source.AbsolutePath.TrimStart('/'));
				var relative = Path.GetRelativePath(config.ScopeDirectory.FullName, tocDirectory);
				IFileInfo[] tocFiles =
				[
					fs.FileInfo.New(Path.Combine(tocDirectory, "toc.yml")),
					fs.FileInfo.New(Path.Combine(tocDirectory, "_toc.yml")),
					fs.FileInfo.New(Path.Combine(tocDirectory, "docset.yml")),
					fs.FileInfo.New(Path.Combine(tocDirectory, "_docset.yml"))
				];
				var file = tocFiles.FirstOrDefault(f => f.Exists);
				if (file is null)
				{
					assembleContext.Collector.EmitWarning(assembleContext.ConfigurationPath.FullName, $"Unable to find toc file in {tocDirectory}");
					file = tocFiles.First();
				}

				var toc = new TableOfContentsConfiguration(config, file, fs.DirectoryInfo.New(tocDirectory), set.BuildContext, 0, relative);
				var mapping = new TocConfigurationMapping
				{
					TopLevel = kv.Value,
					RepositoryConfigurationFile = config,
					TableOfContentsConfiguration = toc
				};
				return new KeyValuePair<Uri, TocConfigurationMapping>(kv.Value.Source, mapping);
			})
			.ToFrozenDictionary();
	}

	private static FrozenDictionary<string, string> GetHistoryMapping(AssembleContext context)
	{
		var dictionary = new Dictionary<string, string>();
		var reader = new YamlStreamReader(context.HistoryMappingPath, context.Collector);
		string? stack = null;
		foreach (var entry in reader.Read())
		{
			switch (entry.Key)
			{
				case "stack":
					stack = reader.ReadString(entry.Entry);
					break;

				case "mappings":
					ReadHistoryMappings(dictionary, reader, entry, stack);
					break;
			}
		}

		return dictionary.OrderByDescending(x => x.Key.Length).ToFrozenDictionary();

		static void ReadHistoryMappings(IDictionary<string, string> dictionary, YamlStreamReader reader, YamlToplevelKey entry, string? newStack)
		{
			if (entry.Entry.Value is not YamlMappingNode mappings)
			{
				reader.EmitWarning($"It wasn't possible to read the mappings");
				return;
			}

			foreach (var mapping in mappings)
			{
				var mappingKey = $"{((YamlScalarNode)mapping.Key).Value}/";
				var mappingValue = ((YamlScalarNode)mapping.Value).Value;
				if (mappingKey.Length == 1 || mappingValue is null)
				{
					reader.EmitWarning($"'{mapping.Key}' or '{mapping.Value}' is not a valid mapping");
					continue;
				}

				if (mappingValue.Equals("stack", StringComparison.OrdinalIgnoreCase) && newStack is not null)
					mappingValue = newStack;
				if (dictionary.TryGetValue(mappingKey, out _))
					reader.EmitWarning($"'{mappingKey}' is already mapped to '{mappingValue}'");
				else
					dictionary[mappingKey] = mappingValue;
			}
		}
	}


	public static FrozenDictionary<Uri, TocTopLevelMapping> GetConfiguredSources(AssembleContext context)
	{
		var dictionary = new Dictionary<Uri, TocTopLevelMapping>();
		var reader = new YamlStreamReader(context.NavigationPath, context.Collector);
		var entries = new List<KeyValuePair<Uri, TocTopLevelMapping>>();
		foreach (var entry in reader.Read())
		{
			switch (entry.Key)
			{
				case "toc":
					ReadTocBlocks(entries, reader, entry.Entry, null, 0, null, null);
					break;
			}
		}
		foreach (var (source, block) in entries)
			dictionary[source] = block;
		return dictionary.ToFrozenDictionary();

		static void ReadTocBlocks(
			List<KeyValuePair<Uri, TocTopLevelMapping>> entries,
			YamlStreamReader reader,
			KeyValuePair<YamlNode, YamlNode> entry,
			string? parent,
			int depth,
			Uri? topLevelSource,
			Uri? parentSource
		)
		{
			if (entry.Key is not YamlScalarNode { Value: not null } scalarKey)
			{
				reader.EmitWarning($"key '{entry.Key}' is not string");
				return;
			}

			if (entry.Value is not YamlSequenceNode sequence)
			{
				reader.EmitWarning($"'{scalarKey.Value}' is not an array");
				return;
			}

			var i = 0;
			foreach (var tocEntry in sequence.Children.OfType<YamlMappingNode>())
			{
				ReadBlock(entries, reader, tocEntry, parent, depth, i, topLevelSource, parentSource);
				i++;
			}
		}
		static void ReadBlock(
			List<KeyValuePair<Uri, TocTopLevelMapping>> entries,
			YamlStreamReader reader,
			YamlMappingNode tocEntry,
			string? parent,
			int depth,
			int order,
			Uri? topLevelSource,
			Uri? parentSource
		)
		{
			string? repository = null;
			string? source = null;
			string? pathPrefix = null;
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
							source = ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, source);
						}

						break;
					case "repo":
						repository = reader.ReadString(entry);
						break;
					case "path_prefix":
						pathPrefix = reader.ReadString(entry);
						break;
				}
			}

			if (repository is not null)
			{
				if (source is not null)
					reader.EmitError($"toc config defines 'repo' can not be combined with 'toc': {source}", tocEntry);
				pathPrefix = string.Join("/", [parent, repository]);
				source = ContentSourceMoniker.CreateString(repository, parent);
			}

			if (source is null)
				return;

			source = source.EndsWith("://") ? source : source.TrimEnd('/') + "/";
			if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
			{
				reader.EmitError($"Source toc entry is not a valid uri: {source}", tocEntry);
				return;
			}

			var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";
			if (string.IsNullOrEmpty(pathPrefix))
				reader.EmitError($"Path prefix is not defined for: {source}, falling back to {sourcePrefix} which may be incorrect", tocEntry);

			pathPrefix ??= sourcePrefix;
			topLevelSource ??= sourceUri;
			parentSource ??= sourceUri;

			var tocTopLevelMapping = new TocTopLevelMapping
			{
				Source = sourceUri,
				SourcePathPrefix = pathPrefix,
				TopLevelSource = topLevelSource,
				ParentSource = parentSource
			};
			entries.Add(new KeyValuePair<Uri, TocTopLevelMapping>(sourceUri, tocTopLevelMapping));

			foreach (var entry in tocEntry.Children)
			{
				var key = ((YamlScalarNode)entry.Key).Value;
				switch (key)
				{
					case "children":
						if (source is null && pathPrefix is null)
						{
							reader.EmitWarning("toc entry has no toc or path_prefix defined");
							continue;
						}

						ReadTocBlocks(entries, reader, entry, parent, depth + 1, topLevelSource, tocTopLevelMapping.Source);
						break;
				}
			}
		}
	}

}
