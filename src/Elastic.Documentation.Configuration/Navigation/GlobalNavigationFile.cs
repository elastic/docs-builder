// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using YamlDotNet.RepresentationModel;

namespace Elastic.Documentation.Configuration.Navigation;

public record NavigationTocMapping
{
	public required Uri Source { get; init; }
	public required string SourcePathPrefix { get; init; }
	public required Uri TopLevelSource { get; init; }
	public required Uri ParentSource { get; init; }
}

public record TocConfigurationMapping
{
	public required NavigationTocMapping TopLevel { get; init; }
	public required ConfigurationFile RepositoryConfigurationFile { get; init; }
	public required TableOfContentsConfiguration TableOfContentsConfiguration { get; init; }
}

public record GlobalNavigationFile : ITableOfContentsScope
{
	//private readonly AssembleContext _context;
	private readonly IDiagnosticsCollector _collector;
	private readonly ConfigurationFileProvider _configurationFileProvider;
	private readonly AssemblyConfiguration _configuration;

	private readonly FrozenDictionary<Uri, TocConfigurationMapping> _tocConfigurationMappings;
	//private readonly AssembleSources _assembleSources;

	public IReadOnlyCollection<TocReference> TableOfContents { get; }
	public IReadOnlyCollection<TocReference> Phantoms { get; }

	public IDirectoryInfo ScopeDirectory { get; }

	public GlobalNavigationFile(
		IDiagnosticsCollector collector,
		ConfigurationFileProvider configurationFileProvider,
		AssemblyConfiguration configuration,
		FrozenDictionary<Uri, TocConfigurationMapping> tocConfigurationMappings
	)
	{
		//_context = context;
		_collector = collector;
		_configurationFileProvider = configurationFileProvider;
		_configuration = configuration;
		_tocConfigurationMappings = tocConfigurationMappings;
		NavigationFile = configurationFileProvider.CreateNavigationFile(configuration);
		TableOfContents = Deserialize("toc");
		Phantoms = Deserialize("phantoms");
		ScopeDirectory = NavigationFile.Directory!;
	}

	private IFileInfo NavigationFile { get; }

	public static bool ValidatePathPrefixes(
		IDiagnosticsCollector collector,
		ConfigurationFileProvider configurationFileProvider,
		AssemblyConfiguration configuration
	)
	{
		var sourcePathPrefixes = GetAllPathPrefixes(collector, configurationFileProvider, configuration);
		var pathPrefixSet = new HashSet<string>();
		var valid = true;
		foreach (var pathPrefix in sourcePathPrefixes)
		{
			var prefix = $"{pathPrefix.Host}/{pathPrefix.AbsolutePath.Trim('/')}/";
			if (pathPrefixSet.Add(prefix))
				continue;
			var duplicateOf = sourcePathPrefixes.First(p => p.Host == pathPrefix.Host && p.AbsolutePath == pathPrefix.AbsolutePath);
			collector.EmitError(configurationFileProvider.NavigationFile, $"Duplicate path prefix: {pathPrefix} duplicate: {duplicateOf}");
			valid = false;
		}
		return valid;
	}


	public static ImmutableHashSet<Uri> GetAllPathPrefixes(
		IDiagnosticsCollector collector,
		ConfigurationFileProvider configurationFileProvider,
		AssemblyConfiguration configuration
	) =>
		GetSourceUris("toc", collector, configurationFileProvider, configuration);

	public static ImmutableHashSet<Uri> GetPhantomPrefixes(
		IDiagnosticsCollector collector,
		ConfigurationFileProvider configurationFileProvider,
		AssemblyConfiguration configuration
	) =>
		GetSourceUris("phantoms", collector, configurationFileProvider, configuration);

	private static ImmutableHashSet<Uri> GetSourceUris(
		string key,
		IDiagnosticsCollector collector,
		ConfigurationFileProvider configurationFileProvider,
		AssemblyConfiguration configuration
	)
	{
		var navigationFile = configurationFileProvider.CreateNavigationFile(configuration);
		var reader = new YamlStreamReader(navigationFile, collector);
		var set = new HashSet<Uri>();
		foreach (var entry in reader.Read())
		{
			if (entry.Key == key && key == "toc")
				ReadPathPrefixes(reader, entry.Entry, set);
			if (entry.Key == key && key == "phantoms")
				ReadPhantomTocs(reader, entry.Entry, set);
		}
		return set.ToImmutableHashSet();

		static void ReadPhantomTocs(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, HashSet<Uri> hashSet)
		{
			if (entry.Value is not YamlSequenceNode sequence)
			{
				reader.EmitWarning($"'{entry.Value}' is not an array");
				return;
			}

			foreach (var tocEntry in sequence.Children.OfType<YamlMappingNode>())
			{
				foreach (var child in tocEntry.Children)
				{
					var key = ((YamlScalarNode)child.Key).Value;
					switch (key)
					{
						case "toc":
							var source = reader.ReadString(child);
							if (source != null && !source.Contains("://"))
								source = ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, source);
							if (source is not null)
								_ = hashSet.Add(new Uri(source));
							break;
					}
				}
			}
		}

		static void ReadPathPrefixes(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, HashSet<Uri> hashSet, string? parent = null)
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

			foreach (var tocEntry in sequence.Children.OfType<YamlMappingNode>())
			{
				var source = ReadToc(reader, tocEntry, ref parent, out var pathPrefix, out var sourceUri);
				if (sourceUri is not null && pathPrefix is not null)
				{
					var pathUri = new Uri($"{sourceUri.Scheme}://{pathPrefix.TrimEnd('/')}/");
					if (!hashSet.Add(pathUri))
						reader.EmitError($"Duplicate path prefix in the same repository: {pathUri}", tocEntry);
				}

				foreach (var child in tocEntry.Children)
				{
					var key = ((YamlScalarNode)child.Key).Value;
					switch (key)
					{
						case "children":
							if (source is null && pathPrefix is null)
							{
								reader.EmitWarning("toc entry has no toc or path_prefix defined");
								continue;
							}

							ReadPathPrefixes(reader, child, hashSet);
							break;
					}
				}
			}
		}
	}
	public void EmitWarning(string message) =>
		_collector.EmitWarning(NavigationFile, message);

	public void EmitError(string message) =>
		_collector.EmitError(NavigationFile, message);

	private IReadOnlyCollection<TocReference> Deserialize(string key)
	{
		var navigationFile = _configurationFileProvider.CreateNavigationFile(_configuration);
		var reader = new YamlStreamReader(navigationFile, _collector);
		try
		{
			foreach (var entry in reader.Read())
			{
				if (entry.Key == key)
					return ReadChildren(key, reader, entry.Entry, null, 0);
			}
		}
		catch (Exception e)
		{
			reader.EmitError("Could not load docset.yml", e);
			throw;
		}

		return [];
	}

	private IReadOnlyCollection<TocReference> ReadChildren(string key, YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string? parent,
		int depth)
	{
		var entries = new List<TocReference>();
		if (entry.Key is not YamlScalarNode { Value: not null } scalarKey)
		{
			reader.EmitWarning($"key '{entry.Key}' is not string");
			return [];
		}

		if (entry.Value is not YamlSequenceNode sequence)
		{
			reader.EmitWarning($"'{scalarKey.Value}' is not an array");
			return [];
		}

		foreach (var tocEntry in sequence.Children.OfType<YamlMappingNode>())
		{

			var child =
				key == "toc"
					? ReadTocDefinition(reader, tocEntry, parent, depth)
					: ReadPhantomDefinition(reader, tocEntry);
			if (child is not null)
				entries.Add(child);
		}

		return entries;
	}

	private TocReference? ReadPhantomDefinition(YamlStreamReader reader, YamlMappingNode tocEntry)
	{
		foreach (var entry in tocEntry.Children)
		{
			var key = ((YamlScalarNode)entry.Key).Value;
			switch (key)
			{
				case "toc":
					var source = reader.ReadString(entry);
					if (source != null && !source.Contains("://"))
						source = ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, source);
					var sourceUri = new Uri(source!);
					var tocReference = new TocReference(sourceUri, this, "", [])
					{
						IsPhantom = true
					};
					return tocReference;
			}
		}

		return null;
	}

	private TocReference? ReadTocDefinition(YamlStreamReader reader, YamlMappingNode tocEntry, string? parent, int depth)
	{
		var source = ReadToc(reader, tocEntry, ref parent, out var pathPrefix, out var sourceUri);

		if (sourceUri is null)
			return null;


		if (!_tocConfigurationMappings.TryGetValue(sourceUri, out var mapping))
		{
			reader.EmitError($"Toc entry '{sourceUri}' is could not be located", tocEntry);
			return null;
		}

		var navigationItems = new List<ITocItem>();

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

					var children = ReadChildren("toc", reader, entry, parent, depth + 1);
					navigationItems.AddRange(children);
					break;
			}
		}

		var rootConfig = mapping.RepositoryConfigurationFile.SourceFile.Directory!;
		var path = Path.GetRelativePath(rootConfig.FullName, mapping.TableOfContentsConfiguration.ScopeDirectory.FullName);
		var tocReference = new TocReference(sourceUri, mapping.TableOfContentsConfiguration, path, navigationItems);
		return tocReference;
	}

	private static string? ReadTocSourcePathPrefix(YamlStreamReader reader, YamlMappingNode tocEntry, string? source, out Uri? sourceUri, string? pathPrefix)
	{
		sourceUri = null;
		if (source is null)
			return pathPrefix;

		source = source.EndsWith("://", StringComparison.OrdinalIgnoreCase) ? source : source.TrimEnd('/') + "/";
		if (!Uri.TryCreate(source, UriKind.Absolute, out sourceUri))
		{
			reader.EmitError($"Source toc entry is not a valid uri: {source}", tocEntry);
			return pathPrefix;
		}

		var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";
		if (string.IsNullOrEmpty(pathPrefix))
			reader.EmitError($"Path prefix is not defined for: {source}, falling back to {sourcePrefix} which may be incorrect", tocEntry);

		pathPrefix ??= sourcePrefix;
		return pathPrefix;
	}

	private static string? ReadToc(
		YamlStreamReader reader,
		YamlMappingNode tocEntry,
		ref string? parent,
		out string? pathPrefix,
		out Uri? sourceUri
	)
	{
		string? repository = null;
		string? source = null;
		pathPrefix = null;
		foreach (var entry in tocEntry.Children)
		{
			var key = ((YamlScalarNode)entry.Key).Value;
			switch (key)
			{
				case "toc":
					source = reader.ReadString(entry);
					if (source != null && !source.Contains("://"))
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

		pathPrefix = ReadTocSourcePathPrefix(reader, tocEntry, source, out sourceUri, pathPrefix);

		return source;
	}
}
