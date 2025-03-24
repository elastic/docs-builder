// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Documentation.Assembler.Configuration;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;
using YamlDotNet.RepresentationModel;

namespace Documentation.Assembler.Navigation;

public record GlobalNavigationFile
{
	private readonly AssembleContext _context;
	private readonly AssembleSources _assembleSources;

	public IReadOnlyCollection<TocReference> TableOfContents { get; }

	public GlobalNavigationFile(AssembleContext context, AssembleSources assembleSources)
	{
		_context = context;
		_assembleSources = assembleSources;
		TableOfContents = Deserialize();
	}

	public void EmitWarning(string message) =>
		_context.Collector.EmitWarning(_context.NavigationPath.FullName, message);

	public void EmitError(string message) =>
		_context.Collector.EmitWarning(_context.NavigationPath.FullName, message);


	private IReadOnlyCollection<TocReference> Deserialize()
	{
		var reader = new YamlStreamReader(_context.NavigationPath, _context.Collector);
		try
		{
			foreach (var entry in reader.Read())
			{
				switch (entry.Key)
				{
					case "toc":
						return ReadChildren(reader, entry.Entry, null, 0);
				}
			}
		}
		catch (Exception e)
		{
			reader.EmitError("Could not load docset.yml", e);
			throw;
		}

		return [];
	}

	private IReadOnlyCollection<TocReference> ReadChildren(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string? parent, int depth)
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
			var child = ReadChild(reader, tocEntry, parent, depth);
			if (child is not null)
				entries.Add(child);
		}

		return entries;
	}

	private TocReference? ReadChild(YamlStreamReader reader, YamlMappingNode tocEntry, string? parent, int depth)
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

		if (source is null)
			return null;

		if (!Uri.TryCreate(source.TrimEnd('/') + "/", UriKind.Absolute, out var sourceUri))
		{
			reader.EmitError($"Source toc entry is not a valid uri: {source}", tocEntry);
			return null;
		}


		var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";
		if (string.IsNullOrEmpty(pathPrefix))
			reader.EmitError($"Path prefix is not defined for: {source}, falling back to {sourcePrefix} which may be incorrect", tocEntry);

		pathPrefix ??= sourcePrefix;

		if (!_assembleSources.TocConfigurationMapping.TryGetValue(sourceUri, out var mapping))
		{
			reader.EmitError($"Toc entry '{sourceUri}' is could not be located", tocEntry);
			return null;
		}

		var navigationItems = new List<ITocItem>();
		//TODO not needed
		//var tocChildren = mapping.TableOfContentsConfiguration.TableOfContents;
		//navigationItems.AddRange(tocChildren);

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

					var children = ReadChildren(reader, entry, parent, depth + 1);
					navigationItems.AddRange(children);
					break;
			}
		}

		var rootConfig = mapping.RepositoryConfigurationFile.SourceFile.Directory!;
		var path = Path.GetRelativePath(rootConfig.FullName, mapping.TableOfContentsConfiguration.ScopeDirectory.FullName);
		var tocReference = new TocReference(sourceUri, mapping.TableOfContentsConfiguration, path, true, navigationItems);
		return tocReference;
	}
}
