// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Navigation;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links.CrossLinks;
using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace Elastic.Documentation.Assembler;

public class AssembleSources
{
	public AssembleContext AssembleContext { get; }

	public FrozenDictionary<string, AssemblerDocumentationSet> AssembleSets { get; }

	public FrozenDictionary<Uri, NavigationTocMapping> NavigationTocMappings { get; }

	public LegacyUrlMappingConfiguration LegacyUrlMappings { get; }

	public FrozenDictionary<Uri, TocConfigurationMapping> TocConfigurationMapping { get; }

	public PublishEnvironmentUriResolver UriResolver { get; }

	public static async Task<AssembleSources> AssembleAsync(
		ILoggerFactory logFactory,
		AssembleContext context,
		Checkout[] checkouts,
		IConfigurationContext configurationContext,
		IReadOnlySet<Exporter> availableExporters,
		Cancel ctx
	)
	{
		var linkIndexProvider = Aws3LinkIndexReader.CreateAnonymous();
		var navigationTocMappings = GetTocMappings(context);
		var uriResolver = new PublishEnvironmentUriResolver(navigationTocMappings, context.Environment);

		var crossLinkFetcher = new AssemblerCrossLinkFetcher(logFactory, context.Configuration, context.Environment, linkIndexProvider);
		var crossLinks = await crossLinkFetcher.FetchCrossLinks(ctx);
		var crossLinkResolver = new CrossLinkResolver(crossLinks, uriResolver);

		var sources = new AssembleSources(
			logFactory,
			context,
			checkouts,
			configurationContext,
			navigationTocMappings,
			configurationContext.LegacyUrlMappings,
			uriResolver,
			crossLinkResolver,
			availableExporters
		);
		foreach (var (_, set) in sources.AssembleSets)
			await set.DocumentationSet.ResolveDirectoryTree(ctx);
		return sources;
	}

	private AssembleSources(
		ILoggerFactory logFactory,
		AssembleContext assembleContext,
		Checkout[] checkouts,
		IConfigurationContext configurationContext,
		FrozenDictionary<Uri, NavigationTocMapping> navigationTocMappings,
		LegacyUrlMappingConfiguration legacyUrlMappings,
		PublishEnvironmentUriResolver uriResolver,
		ICrossLinkResolver crossLinkResolver,
		IReadOnlySet<Exporter> availableExporters
	)
	{
		NavigationTocMappings = navigationTocMappings;
		LegacyUrlMappings = legacyUrlMappings;
		UriResolver = uriResolver;
		AssembleContext = assembleContext;
		AssembleSets = checkouts
			.Where(c => c.Repository is { Skip: false })
			.Select(c => new AssemblerDocumentationSet(logFactory, assembleContext, c, crossLinkResolver, configurationContext,
				availableExporters))
			.ToDictionary(s => s.Checkout.Repository.Name, s => s)
			.ToFrozenDictionary();

		TocConfigurationMapping = NavigationTocMappings
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
					assembleContext.Collector.EmitWarning(assembleContext.ConfigurationFileProvider.AssemblerFile,
						$"Unable to find toc file in {tocDirectory}");
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

	public static FrozenDictionary<Uri, NavigationTocMapping> GetTocMappings(AssembleContext context)
	{
		var dictionary = new Dictionary<Uri, NavigationTocMapping>();
		var file = context.ConfigurationFileProvider.CreateNavigationFile(context.Configuration);
		var reader = new YamlStreamReader(file, context.Collector);
		var entries = new List<KeyValuePair<Uri, NavigationTocMapping>>();
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
			List<KeyValuePair<Uri, NavigationTocMapping>> entries,
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
			List<KeyValuePair<Uri, NavigationTocMapping>> entries,
			YamlStreamReader reader,
			YamlMappingNode tocEntry,
			string? parent,
			int depth,
			int order, //TODO Remove this parameter
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

			source = source.EndsWith("://", StringComparison.OrdinalIgnoreCase) ? source : source.TrimEnd('/') + "/";
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

			var tocTopLevelMapping = new NavigationTocMapping
			{
				Source = sourceUri,
				SourcePathPrefix = pathPrefix,
				TopLevelSource = topLevelSource,
				ParentSource = parentSource
			};
			entries.Add(new KeyValuePair<Uri, NavigationTocMapping>(sourceUri, tocTopLevelMapping));

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
