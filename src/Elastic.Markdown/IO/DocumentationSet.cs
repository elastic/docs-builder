// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Abstractions;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.Extensions;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO.NewNavigation;
using Elastic.Markdown.Myst;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.IO;

public class DocumentationSet : IPositionalNavigation
{
	private readonly ILogger<DocumentationSet> _logger;
	public BuildContext Context { get; }
	public string Name { get; }
	public IFileInfo OutputStateFile { get; }
	public IFileInfo LinkReferenceFile { get; }

	public IDirectoryInfo SourceDirectory { get; }
	public IDirectoryInfo OutputDirectory { get; }

	public DateTimeOffset LastWrite { get; }

	public ConfigurationFile Configuration { get; }

	public MarkdownParser MarkdownParser { get; }

	public ICrossLinkResolver CrossLinkResolver { get; }

	public FrozenDictionary<FilePath, DocumentationFile> Files { get; }

	public ConditionalWeakTable<MarkdownFile, INavigationItem> MarkdownNavigationLookup { get; }

	public IReadOnlyCollection<IDocsBuilderExtension> EnabledExtensions { get; }

	public ConcurrentDictionary<string, NavigationRenderResult> NavigationRenderResults { get; } = [];

	public DocumentationSet(
		BuildContext context,
		ILoggerFactory logFactory,
		ICrossLinkResolver linkResolver
	)
	{
		_logger = logFactory.CreateLogger<DocumentationSet>();
		Context = context;
		SourceDirectory = context.DocumentationSourceDirectory;
		OutputDirectory = context.OutputDirectory;
		CrossLinkResolver = linkResolver;
		Configuration = context.Configuration;
		EnabledExtensions = InstantiateExtensions();

		var resolver = new ParserResolvers
		{
			CrossLinkResolver = CrossLinkResolver,
			TryFindDocument = TryFindDocument,
			TryFindDocumentByRelativePath = TryFindDocumentByRelativePath,
			PositionalNavigation = this
		};
		MarkdownParser = new MarkdownParser(context, resolver);

		var fileFactory = new MarkdownFileFactory(context, MarkdownParser, EnabledExtensions);
		Navigation = new DocumentationSetNavigation<MarkdownFile>(context.ConfigurationYaml, context, fileFactory, pathPrefix: context.UrlPathPrefix);

		Name = Context.Git != GitCheckoutInformation.Unavailable
			? Context.Git.RepositoryName
			: Context.DocumentationCheckoutDirectory?.Name ?? $"unknown-{Context.DocumentationSourceDirectory.Name}";
		OutputStateFile = OutputDirectory.FileSystem.FileInfo.New(Path.Combine(OutputDirectory.FullName, ".doc.state"));
		LinkReferenceFile = OutputDirectory.FileSystem.FileInfo.New(Path.Combine(OutputDirectory.FullName, "links.json"));

		Files = fileFactory.Files;
		var files = Files.Values.ToArray();
		LastWrite = files.Max(f => f.SourceFile.LastWriteTimeUtc);

		var markdownFiles = files.OfType<MarkdownFile>().ToArray();
		MarkdownFiles = markdownFiles.ToFrozenSet();

		MarkdownNavigationLookup = [];
		var navigationFlatList = CreateNavigationLookup(Navigation);
		NavigationIndexedByOrder = navigationFlatList
			.ToDictionary(n => n.NavigationIndex, n => n)
			.ToFrozenDictionary();

		// Build cross-link dictionary including both:
		// 1. Direct leaf items (files without children)
		// 2. Index property of node items (files with children)
		var leafItems = navigationFlatList.OfType<ILeafNavigationItem<MarkdownFile>>();
		var nodeIndexes = navigationFlatList
			.OfType<INodeNavigationItem<MarkdownFile, INavigationItem>>()
			.Select(node => node.Index);

		NavigationIndexedByCrossLink = leafItems
			.Concat(nodeIndexes)
			.DistinctBy(n => n.Model.CrossLink)
			.ToDictionary(n => n.Model.CrossLink, n => n)
			.ToFrozenDictionary();

		ValidateRedirectsExists();
	}

	public FrozenDictionary<string, ILeafNavigationItem<MarkdownFile>> NavigationIndexedByCrossLink { get; }

	public DocumentationSetNavigation<MarkdownFile> Navigation { get; }

	public FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; }

	private IReadOnlyCollection<INavigationItem> CreateNavigationLookup(INavigationItem item)
	{
		// warning emit an error again
		switch (item)
		{
			case ILeafNavigationItem<MarkdownFile> markdownLeaf:
				var added = MarkdownNavigationLookup.TryAdd(markdownLeaf.Model, markdownLeaf);
				if (!added)
					Context.EmitWarning(Configuration.SourceFile, $"Duplicate navigation item {markdownLeaf.Model.CrossLink}");
				return [markdownLeaf];
			case ILeafNavigationItem<INavigationModel> leaf:
				return [leaf];
			case INodeNavigationItem<MarkdownFile, INavigationItem> node:
				var addedNode = MarkdownNavigationLookup.TryAdd(node.Index.Model, node);
				if (!addedNode)
					Context.EmitWarning(Configuration.SourceFile, $"Duplicate navigation item {node.Index.Model.CrossLink}");
				var nodeItems = node.NavigationItems.SelectMany(CreateNavigationLookup);
				return nodeItems.Concat([node]).ToArray();
			case INodeNavigationItem<INavigationModel, INavigationItem> node:
				var items = node.NavigationItems.SelectMany(CreateNavigationLookup);
				return items.Concat([node]).ToArray();
			default:
				return [];
		}
	}

	public static (string, INavigationItem)[] Pairs(INavigationItem item)
	{
		//TODO add crosslink to navigation if still necessary later
		switch (item)
		{
			case ILeafNavigationItem<IDocumentationFile> { IsCrossLink: true } f:
				return [(f.Url, item)];
			case ILeafNavigationItem<IDocumentationFile> f:
				return [(f.Url, item)];
			case INodeNavigationItem<INavigationModel, INavigationItem> g:
				var index = new List<(string, INavigationItem)>
				{
					(g.Url, g)
				};

				return index.Concat(g.NavigationItems.SelectMany(Pairs).ToArray())
					.DistinctBy(kv => kv.Item1)
					.ToArray();
			default:
				return [];
		}
	}

	private void ValidateRedirectsExists()
	{
		if (Configuration.Redirects is null || Configuration.Redirects.Count == 0)
			return;
		foreach (var redirect in Configuration.Redirects)
		{
			if (redirect.Value.To is not null)
				ValidateExists(redirect.Key, redirect.Value.To, redirect.Value.Anchors);
			else if (redirect.Value.Many is not null)
			{
				foreach (var r in redirect.Value.Many)
				{
					if (r.To is not null)
						ValidateExists(redirect.Key, r.To, r.Anchors);
				}
			}
		}

		void ValidateExists(string from, string to, IReadOnlyDictionary<string, string?>? valueAnchors)
		{
			if (to.Contains("://"))
			{
				if (!Uri.TryCreate(to, UriKind.Absolute, out _))
					Context.EmitError(Configuration.SourceFile, $"Redirect {from} points to {to} which is not a valid URI");

				return;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				to = to.Replace('/', Path.DirectorySeparatorChar);

			var fp = new FilePath(to, SourceDirectory);
			if (!Files.TryGetValue(fp, out var file))
			{
				Context.EmitError(Configuration.SourceFile, $"Redirect {from} points to {to} which does not exist");
				return;

			}

			if (file is not MarkdownFile markdownFile)
			{
				Context.EmitError(Configuration.SourceFile, $"Redirect {from} points to {to} which is not a markdown file");
				return;
			}

			if (valueAnchors is null or { Count: 0 })
				return;

			markdownFile.AnchorRemapping =
				markdownFile.AnchorRemapping?
					.Concat(valueAnchors)
					.DistinctBy(kv => kv.Key)
					.ToDictionary(kv => kv.Key, kv => kv.Value) ?? valueAnchors;
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
		if (MarkdownNavigationLookup.TryGetValue(markdown, out var navigation))
			return navigation;
		throw new Exception($"Could not find navigation item for {markdown.CrossLink}");
	}
	public INavigationItem FindNavigationByCrossLink(string crossLink)
	{
		if (NavigationIndexedByCrossLink.TryGetValue(crossLink, out var navigation))
			return navigation;
		throw new Exception($"Could not find navigation item for {crossLink}");
	}

	private bool _resolved;
	public async Task ResolveDirectoryTree(Cancel ctx)
	{
		if (_resolved)
			return;

		await Parallel.ForEachAsync(MarkdownFiles, ctx, async (file, token) => await file.MinimalParseAsync(TryFindDocumentByRelativePath, token));

		_resolved = true;
	}

	public RepositoryLinks CreateLinkReference()
	{
		var redirects = Configuration.Redirects;
		var crossLinks = Context.Collector.CrossLinks.ToHashSet().ToArray();
		var markdownInNavigation = NavigationIndexedByOrder.Values
			.OfType<ILeafNavigationItem<MarkdownFile>>()
			.Select(m => (Markdown: m.Model, Navigation: (INavigationItem)m))
			.Concat(NavigationIndexedByOrder.Values
				.OfType<INodeNavigationItem<MarkdownFile, INavigationItem>>()
				.Select(g => (Markdown: g.Index.Model, Navigation: (INavigationItem)g))
			)
			.ToList();

		var links = markdownInNavigation
			.Select(tuple =>
			{
				var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
					? tuple.Markdown.LinkReferenceRelativePath.Replace('\\', '/')
					: tuple.Markdown.LinkReferenceRelativePath;
				return (Path: path, tuple.Markdown, tuple.Navigation);
			})
			.DistinctBy(tuple => tuple.Path)
			.ToDictionary(
				tuple => tuple.Path,
				tuple =>
				{
					var anchors = tuple.Markdown.Anchors.Count == 0 ? null : tuple.Markdown.Anchors.ToArray();
					return new LinkMetadata
					{
						Anchors = anchors,
						Hidden = tuple.Navigation.Hidden
					};
				});

		return new RepositoryLinks
		{
			Redirects = redirects,
			UrlPathPrefix = Context.UrlPathPrefix,
			Origin = Context.Git,
			Links = links,
			CrossLinks = crossLinks
		};
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

		return list.AsReadOnly();
	}
}
