// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO.Abstractions;
using Documentation.Assembler.Configuration;
using Documentation.Assembler.Sourcing;
using Elastic.Markdown;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;

namespace Documentation.Assembler.Navigation;

public record GlobalNavigation : IDocumentationFileOutputProvider
{
	private readonly AssembleContext _context;
	private readonly FrozenDictionary<string, Checkout>.AlternateLookup<ReadOnlySpan<char>> _checkoutsLookup;
	private readonly FrozenDictionary<string, Repository>.AlternateLookup<ReadOnlySpan<char>> _repoConfigLookup;
	private readonly FrozenDictionary<string, TableOfContentsReference>.AlternateLookup<ReadOnlySpan<char>> _tocLookup;

	private FrozenDictionary<string, Repository> ConfiguredRepositories { get; }
	private FrozenDictionary<string, TableOfContentsReference> IndexedTableOfContents { get; }

	public GlobalNavigationFile NavigationConfiguration { get; init; }

	private FrozenDictionary<string, Checkout> Checkouts { get; init; }

	private ImmutableSortedSet<string> TableOfContentsPrefixes { get; }

	public GlobalNavigation(AssembleContext context, GlobalNavigationFile navigationConfiguration, Checkout[] checkouts)
	{
		_context = context;
		NavigationConfiguration = navigationConfiguration;
		Checkouts = checkouts.ToDictionary(c => c.Repository.Name, c => c).ToFrozenDictionary();
		_checkoutsLookup = Checkouts.GetAlternateLookup<ReadOnlySpan<char>>();

		var configuration = context.Configuration;
		ConfiguredRepositories = configuration.ReferenceRepositories.Values.Concat<Repository>([configuration.Narrative])
			.ToFrozenDictionary(e => e.Name, e => e);
		_repoConfigLookup = ConfiguredRepositories.GetAlternateLookup<ReadOnlySpan<char>>();

		IndexedTableOfContents = navigationConfiguration.IndexedTableOfContents;
		_tocLookup = IndexedTableOfContents.GetAlternateLookup<ReadOnlySpan<char>>();
		TableOfContentsPrefixes = navigationConfiguration.IndexedTableOfContents.Keys.OrderByDescending(k => k.Length).ToImmutableSortedSet();
	}

	public string GetSubPath(Uri crossLinkUri, ref string path)
	{
		if (!_checkoutsLookup.TryGetValue(crossLinkUri.Scheme, out _))
		{
			_context.Collector.EmitError(_context.ConfigurationPath,
				!_repoConfigLookup.TryGetValue(crossLinkUri.Scheme, out _)
					? $"Repository: '{crossLinkUri.Scheme}' is not defined in assembler.yml"
					: $"Unable to find checkout for repository: {crossLinkUri.Scheme}"
			);
		}

		var lookup = crossLinkUri.ToString().AsSpan();
		if (lookup.EndsWith(".md", StringComparison.Ordinal))
			lookup = lookup[..^3];

		// temporary fix only spotted two instances of this:
		// Error: Unable to find defined toc for url: docs-content:///manage-data/ingest/transform-enrich/set-up-an-enrich-processor.md
		// Error: Unable to find defined toc for url: kibana:///reference/configuration-reference.md
		if (lookup.IndexOf(":///") >= 0)
			lookup = lookup.ToString().Replace(":///", "://").AsSpan();

		string? match = null;
		foreach (var prefix in TableOfContentsPrefixes)
		{
			if (!lookup.StartsWith(prefix, StringComparison.Ordinal))
				continue;
			match = prefix;
			break;
		}

		if (match is null || !_tocLookup.TryGetValue(match, out var toc))
		{
			//TODO remove
			if (crossLinkUri.Scheme != "asciidocalypse")
				_context.Collector.EmitError(_context.NavigationPath, $"Unable to find defined toc for url: {crossLinkUri}");
			return $"reference/{crossLinkUri.Scheme}";
		}

		path = path.AsSpan().TrimStart(toc.SourcePrefix).ToString();

		return toc.PathPrefix;
	}

	public IFileInfo? OutputFile(DocumentationSet documentationSet, IFileInfo defaultOutputFile, string relativePath)
	{
		if (relativePath.StartsWith("_static/", StringComparison.Ordinal))
			return defaultOutputFile;

		var outputDirectory = documentationSet.OutputDirectory;
		var fs = defaultOutputFile.FileSystem;

		var repositoryName = documentationSet.Build.Git.RepositoryName;

		var l = $"{repositoryName}://{relativePath.TrimStart('/')}";
		var lookup = l.AsSpan();
		if (lookup.StartsWith("docs-content://serverless/", StringComparison.Ordinal))
			return null;
		if (lookup.StartsWith("eland://sphinx/", StringComparison.Ordinal))
			return null;
		if (lookup.StartsWith("elasticsearch-py://sphinx/", StringComparison.Ordinal))
			return null;
		if (lookup.StartsWith("elastic-serverless-forwarder://", StringComparison.Ordinal) && lookup.EndsWith(".png"))
			return null;

		//allow files at root for `docs-content` (index.md 404.md)
		if (lookup.StartsWith("docs-content://") && !relativePath.Contains('/'))
			return defaultOutputFile;

		string? match = null;
		foreach (var prefix in TableOfContentsPrefixes)
		{
			if (!lookup.StartsWith(prefix, StringComparison.Ordinal))
				continue;
			match = prefix;
			break;
		}

		if (match is null || !_tocLookup.TryGetValue(match, out var toc))
		{
			if (relativePath.StartsWith("raw-migrated-files/", StringComparison.Ordinal))
				return null;
			if (relativePath.StartsWith("images/", StringComparison.Ordinal))
				return null;
			if (relativePath.StartsWith("examples/", StringComparison.Ordinal))
				return null;
			if (relativePath.StartsWith("docset.yml", StringComparison.Ordinal))
				return null;
			if (relativePath.StartsWith("doc_examples", StringComparison.Ordinal))
				return null;
			if (relativePath.EndsWith(".asciidoc", StringComparison.Ordinal))
				return null;

			var fallBack = fs.Path.Combine(outputDirectory.FullName, "_failed", repositoryName, relativePath);
			_context.Collector.EmitError(_context.NavigationPath, $"No toc for output path: '{lookup}' falling back to: '{fallBack}'");
			return fs.FileInfo.New(fallBack);
		}

		var newPath = relativePath.AsSpan().TrimStart(toc.SourcePrefix.TrimEnd('/')).TrimStart('/').ToString();
		var path = fs.Path.Combine(outputDirectory.FullName, toc.PathPrefix, newPath);
		if (path.Contains("deploy-manage"))
		{
		}

		return fs.FileInfo.New(path);
	}
}
