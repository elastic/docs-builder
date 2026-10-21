// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.Extensions;
using Elastic.Markdown.Extensions.CliReference;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.Extensions.Listing;
using Elastic.Markdown.Myst;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.IO;

//create all documentation sets
// create a codexrootnodenavigationitem
// foreach doc set.
//		root.items.add(set.navigation);
// root.prefix = new provider('prefix)'
// root.prefixprovider.prefix = 'prefix';
public class DocumentationSet : INavigationTraversable
{
	private readonly ILogger<DocumentationSet> _logger;
	public BuildContext Context { get; }
	public string Name { get; }
	public IFileInfo OutputStateFile { get; }
	public IFileInfo LinkReferenceFile { get; }

	public IDirectoryInfo SourceDirectory { get; }
	public IDirectoryInfo OutputDirectory { get; }

	public DateTimeOffset LastWrite { get; }

	public ResolvedDocumentationSet Configuration { get; }

	public MarkdownParser MarkdownParser { get; }

	public ICrossLinkResolver CrossLinkResolver { get; }

	public IReleaseNotesResolver ReleaseNotesResolver { get; }

	public FrozenDictionary<FilePath, DocumentationFile> Files { get; }

	public ConditionalWeakTable<IDocumentationFile, INavigationItem> NavigationDocumentationFileLookup { get; }

	public IReadOnlyCollection<IDocsBuilderExtension> EnabledExtensions { get; }

	public DocumentationSet(
		BuildContext context,
		ILoggerFactory logFactory,
		ICrossLinkResolver linkResolver,
		IReleaseNotesResolver? releaseNotesResolver = null
	)
	{
		_logger = logFactory.CreateLogger<DocumentationSet>();
		Context = context;
		SourceDirectory = context.DocumentationSourceDirectory;
		OutputDirectory = context.OutputDirectory;
		CrossLinkResolver = linkResolver;
		ReleaseNotesResolver = releaseNotesResolver ?? NoopReleaseNotesResolver.Instance;
		Configuration = context.Configuration;
		var resolver = new ParserResolvers
		{
			CrossLinkResolver = CrossLinkResolver,
			ReleaseNotesResolver = ReleaseNotesResolver,
			TryFindDocument = TryFindDocument,
			TryFindDocumentByRelativePath = TryFindDocumentByRelativePath,
			NavigationTraversable = this
		};
		MarkdownParser = new MarkdownParser(context, resolver);

		EnabledExtensions = InstantiateExtensions();

		var fileFactory = new MarkdownFileFactory(context, MarkdownParser, EnabledExtensions);
		Navigation = new DocumentationSetNavigation<MarkdownFile>(
			context.Definition,
			context,
			fileFactory,
			null,
			null,
			context.UrlPathPrefix,
			CrossLinkResolver
		);
		VisitNavigation(Navigation);

		Name = Context.Git != GitCheckoutInformation.Unavailable
			? Context.Git.RepositoryName
			: Context.DocumentationCheckoutDirectory.Name
				?? Context.DocumentationSourceDirectory.Parent?.Name
				?? Context.DocumentationSourceDirectory.Name;
		OutputStateFile = OutputDirectory.FileSystem.FileInfo.New(Path.Join(OutputDirectory.FullName, ".doc.state"));
		LinkReferenceFile = OutputDirectory.FileSystem.FileInfo.New(Path.Join(OutputDirectory.FullName, "links.json"));

		Files = fileFactory.Files;
		var files = Files.Values.ToArray();
		LastWrite = files.Max(f => f.SourceFile.LastWriteTimeUtc);

		var markdownFiles = files.OfType<MarkdownFile>().ToArray();
		MarkdownFiles = markdownFiles.ToFrozenSet();

		NavigationDocumentationFileLookup = [];
		NavigationIndexedByOrder = Navigation.BuildNavigationLookups(NavigationDocumentationFileLookup);

		ValidateRedirectsExists();
		ValidateRootIndexExists();
	}

	private void ValidateRootIndexExists()
	{
		if (Context.BuildType != BuildType.Isolated || Configuration.Registry == DocSetRegistry.Public)
			return;

		var indexFile = Context.ReadFileSystem.FileInfo.New(Path.Join(SourceDirectory.FullName, "index.md"));

		if (!indexFile.Exists)
			Context.EmitError(Configuration.SourceFile, "Non-public documentation sets require a root index.md file");
	}

	public DocumentationSetNavigation<MarkdownFile> Navigation { get; }

	public FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; }

	private void VisitNavigation(INavigationItem item)
	{
		switch (item)
		{
			case ILeafNavigationItem<IDocumentationFile> markdownLeaf:
				foreach (var extension in EnabledExtensions)
					extension.VisitNavigation(item, markdownLeaf.Model);
				break;
			case INodeNavigationItem<IDocumentationFile, INavigationItem> node:
				// Index is a null sentinel when this node's table of contents produced no items
				// (a validation error is emitted upstream); skip visiting a missing index.
				if (node.Index is { } nodeIndex)
				{
					foreach (var extension in EnabledExtensions)
						extension.VisitNavigation(node, nodeIndex.Model);
				}
				foreach (var child in node.NavigationItems)
					VisitNavigation(child);
				break;
		}
	}

	private void ValidateRedirectsExists()
	{
		if (Configuration.Redirects is null || Configuration.Redirects.Count == 0)
			return;
		foreach (var redirect in Configuration.Redirects)
		{
			if (redirect.Value.To is not null)
				ValidateExists(redirect.Key, redirect.Value.To, redirect.Value.Anchors, Severity.Error);

			// Next to a top-level 'to', 'many' targets were never checked; they are reported as warnings until that becomes an error
			var manySeverity = redirect.Value.To is null ? Severity.Error : Severity.Warning;
			foreach (var r in redirect.Value.Many ?? [])
			{
				if (r.To is not null)
					ValidateExists(redirect.Key, r.To, r.Anchors, manySeverity);
			}
		}

		void ValidateExists(string from, string to, IReadOnlyDictionary<string, string?>? valueAnchors, Severity severity)
		{
			// An external target that is not a URI, and a local target that does not exist, are reported by the DocumentationSetInCatalog constraints
			if (to.Contains("://"))
				return;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				to = to.Replace('/', Path.DirectorySeparatorChar);

			var fp = new FilePath(to, SourceDirectory);
			if (!Files.TryGetValue(fp, out var file))
				return;

			if (file is not MarkdownFile markdownFile)
			{
				Context.Collector.Emit(severity, Configuration.SourceFile, $"Redirect {from} points to {to} which is not a markdown file");
				return;
			}

			// Anchors on 'many' targets next to a top-level 'to' were never remapped; checking them doesn't change the output
			if (severity != Severity.Error || valueAnchors is null or { Count: 0 })
				return;

			markdownFile.AnchorRemapping = markdownFile.AnchorRemapping?.Concat(valueAnchors)
				.DistinctBy(kv => kv.Key)
				.ToDictionary(kv => kv.Key, kv => kv.Value)
				?? valueAnchors;
		}
	}

	public FrozenSet<MarkdownFile> MarkdownFiles { get; }

	public string FirstInterestingUrl =>
		NavigationIndexedByOrder.Values.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().First().Url;

	public DocumentationFile? TryFindDocument(IFileInfo sourceFile)
	{
		var relativePath = Path.GetRelativePath(SourceDirectory.FullName, sourceFile.FullName);
		return TryFindDocumentByRelativePath(relativePath);
	}
	public DocumentationFile? TryFindDocumentByRelativePath(string relativePath)
	{
		var fp = new FilePath(relativePath, SourceDirectory);
		return Files.GetValueOrDefault(fp);
	}

	public INavigationItem FindNavigationByMarkdown(MarkdownFile markdown)
	{
		if (NavigationDocumentationFileLookup.TryGetValue(markdown, out var navigation))
			return navigation;
		throw new Exception($"Could not find navigation item for {markdown.CrossLink}");
	}

	private bool _resolved;

	public async Task ResolveDirectoryTree(Cancel ctx)
	{
		if (_resolved)
			return;

		// MinimalParseAsync is ~60 % blocked on open() / file I/O; the default
		// ProcessorCount cap starves the IO queue.  4× gives a wide-enough flight
		// without runaway RSS growth (each in-flight parse holds a MarkdownDocument).
		var options = new ParallelOptions
		{
			MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount * 4, 32),
			CancellationToken = ctx
		};
		await Parallel.ForEachAsync(
			MarkdownFiles,
			options,
			async (file, token) => await file.MinimalParseAsync(TryFindDocumentByRelativePath, token)
		);

		_resolved = true;
	}

	/// <summary>
	/// The <c>links.json</c> this build publishes, checked by <see cref="LinkManifestConstraints"/> (see <see cref="LinkManifest.From"/>). The value is
	/// there even when a rule of error severity is broken; the findings are the caller's to report.
	/// </summary>
	public Result<LinkManifest> CreateLinkReference()
	{
		var leafs = NavigationIndexedByOrder.Values.OfType<ILeafNavigationItem<MarkdownFile>>().ToArray();
		var nodes = NavigationIndexedByOrder.Values.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToArray();

		var markdownInNavigation = leafs.Select(m => (Markdown: m.Model, Navigation: (INavigationItem)m)).Concat(
			nodes.Select(g => (Markdown: (MarkdownFile)g.Index.Model, Navigation: (INavigationItem)g))
		);

		var pages = markdownInNavigation.Select(
			tuple => new LinkPage(
				RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
					? tuple.Markdown.LinkReferenceRelativePath.Replace('\\', '/')
					: tuple.Markdown.LinkReferenceRelativePath,
				[.. tuple.Markdown.Anchors],
				tuple.Navigation.ExcludeFromIndexing,
				tuple.Markdown.DuplicateAnchors
			)
		);

		return LinkManifest.From(Context.Git, Context.UrlPathPrefix, pages, Context.Collector.CrossLinks, Configuration.Redirects);
	}

	public void ClearOutputDirectory()
	{
		_logger.LogInformation("Clearing output directory {OutputDirectory}", OutputDirectory.Name);
		if (OutputDirectory.Exists)
			OutputDirectory.Delete(true);
		OutputDirectory.Create();
	}

	private IReadOnlyCollection<IDocsBuilderExtension> InstantiateExtensions()
	{
		var list = new List<IDocsBuilderExtension>();
		foreach (var extension in Configuration.Extensions.Enabled)
		{
			switch (extension.ToLowerInvariant())
			{
				case "detection-rules":
					list.Add(new DetectionRulesDocsBuilderExtension(Context));
					continue;
			}
		}

		// Auto-enable CLI reference extension when the TOC contains a cli: entry
		if (HasCliReferenceRef(Context.Definition.TableOfContents))
			list.Add(new CliReferenceDocsBuilderExtension(Context));

		// Auto-enable listing extension when the TOC contains a listing: entry
		if (HasListingRef(Context.Definition.TableOfContents))
			list.Add(new ListingDocsBuilderExtension(Context, MarkdownParser));

		return list.AsReadOnly();
	}

	private static bool HasCliReferenceRef(IReadOnlyList<TocItem> items)
	{
		foreach (var item in items)
		{
			if (item is CliReferenceRef)
				return true;

			var children = item switch
			{
				FileRef f => f.Children,
				FolderRef f => f.Children,
				IsolatedTableOfContentsRef t => t.Children,
				_ => null
			};
			if (children is { Count: > 0 } && HasCliReferenceRef(children))
				return true;
		}
		return false;
	}

	private static bool HasListingRef(IReadOnlyList<TocItem> items)
	{
		foreach (var item in items)
		{
			if (item is ListingRef)
				return true;

			var children = item switch
			{
				FileRef f => f.Children,
				FolderRef f => f.Children,
				IsolatedTableOfContentsRef t => t.Children,
				_ => null
			};
			if (children is { Count: > 0 } && HasListingRef(children))
				return true;
		}
		return false;
	}
}
