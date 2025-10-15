// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Configuration.Navigation;

public record NavigationTocMapping
{
	public required Uri Source { get; init; }
	public required string SourcePathPrefix { get; init; }
	public required Uri TopLevelSource { get; init; }
	public required Uri ParentSource { get; init; }
}

public record GlobalNavigationFile : ITableOfContentsScope
{
	private readonly IDiagnosticsCollector _collector;

	public IReadOnlyCollection<SiteTableOfContentsRef> TableOfContents { get; }
	public IReadOnlyCollection<PhantomRegistration> Phantoms { get; }

	public IDirectoryInfo ScopeDirectory { get; }

	public GlobalNavigationFile(IDiagnosticsCollector collector, SiteNavigationFile siteNavigation, IFileInfo navigationFile)
	{
		_collector = collector;

		// Read directly from SiteNavigationFile
		TableOfContents = siteNavigation.TableOfContents;
		Phantoms = siteNavigation.Phantoms;
		ScopeDirectory = navigationFile.Directory!;
	}

	public static bool ValidatePathPrefixes(IDiagnosticsCollector collector, SiteNavigationFile siteNavigation, IFileInfo navigationFile)
	{
		var sourcePathPrefixes = GetAllPathPrefixes(siteNavigation);
		var pathPrefixSet = new HashSet<string>();
		var valid = true;

		foreach (var pathPrefix in sourcePathPrefixes)
		{
			var prefix = $"{pathPrefix.Host}/{pathPrefix.AbsolutePath.Trim('/')}/";
			if (pathPrefixSet.Add(prefix))
				continue;

			var duplicateOf = sourcePathPrefixes.First(p => p.Host == pathPrefix.Host && p.AbsolutePath == pathPrefix.AbsolutePath);
			collector.EmitError(navigationFile, $"Duplicate path prefix: {pathPrefix} duplicate: {duplicateOf}");
			valid = false;
		}

		return valid;
	}

	public static ImmutableHashSet<Uri> GetAllPathPrefixes(SiteNavigationFile siteNavigation)
	{
		var set = new HashSet<Uri>();

		foreach (var tocRef in siteNavigation.TableOfContents)
			CollectPathPrefixes(tocRef, set);

		return set.ToImmutableHashSet();
	}

	private static void CollectPathPrefixes(SiteTableOfContentsRef tocRef, HashSet<Uri> set)
	{
		// Add path prefix for this toc ref
		if (!string.IsNullOrEmpty(tocRef.PathPrefix))
		{
			var pathUri = new Uri($"{tocRef.Source.Scheme}://{tocRef.PathPrefix.TrimEnd('/')}/");
			_ = set.Add(pathUri);
		}

		// Recursively collect from children
		foreach (var child in tocRef.Children)
			CollectPathPrefixes(child, set);
	}

	public static ImmutableHashSet<Uri> GetPhantomPrefixes(SiteNavigationFile siteNavigation)
	{
		var set = new HashSet<Uri>();

		foreach (var phantom in siteNavigation.Phantoms)
		{
			var source = phantom.Source;
			if (!source.Contains("://"))
				source = ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, source);

			_ = set.Add(new Uri(source));
		}

		return set.ToImmutableHashSet();
	}

	public void EmitWarning(string message) =>
		_collector.EmitWarning(ScopeDirectory.FullName, message);

	public void EmitError(string message) =>
		_collector.EmitError(ScopeDirectory.FullName, message);
}
