// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Publishing;
using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace Elastic.Documentation.Assembler;

public class AssembleSources
{
	public AssembleContext AssembleContext { get; }

	public FrozenDictionary<string, AssemblerDocumentationSet> AssembleSets { get; }

	public FrozenDictionary<Uri, NavigationTocMapping> NavigationTocMappings { get; }

	public LegacyUrlMappingCatalog LegacyUrlMappings { get; }

	public PublishEnvironmentUriResolver UriResolver { get; }

	public ICrossLinkResolver CrossLinkResolver { get; }

	public static async Task<AssembleSources> AssembleAsync(
		ILoggerFactory logFactory,
		AssembleContext context,
		Checkout[] checkouts,
		IConfigurationContext configurationContext,
		IReadOnlySet<Exporter> availableExporters,
		Cancel ctx
	)
	{
		var logger = logFactory.CreateLogger<AssembleSources>();

		var linkIndexProvider = Aws3LinkIndexReader.CreateAnonymous();
		var navigationTocMappings = GetTocMappings(context);
		var uriResolver = new PublishEnvironmentUriResolver(navigationTocMappings, context.Environment);

		var sw = System.Diagnostics.Stopwatch.StartNew();
		var crossLinkFetcher = new AssemblerCrossLinkFetcher(logFactory, context.Configuration, context.Environment, linkIndexProvider);
		var crossLinks = await crossLinkFetcher.FetchCrossLinks(ctx);
		var crossLinkResolver = new CrossLinkResolver(crossLinks, uriResolver);
		logger.LogInformation("  AssembleAsync: FetchCrossLinks in {Elapsed:mm\\:ss\\.fff}", sw.Elapsed);

		// Shared, mutable resolver: every documentation set gets the same instance now and the prefetched
		// bundles are populated once below, after each set's docset.yml (and its `release_notes`) is parsed.
		var releaseNotesResolver = new ReleaseNotesResolver();

		var sources = new AssembleSources(
			logFactory,
			context,
			checkouts,
			configurationContext,
			navigationTocMappings,
			configurationContext.Catalog.LegacyUrlMappings,
			uriResolver,
			crossLinkResolver,
			releaseNotesResolver,
			availableExporters
		);

		var declaredProducts = sources
			.AssembleSets
			.Values
			.SelectMany(s => s.BuildContext.Configuration.ReleaseNotesProducts)
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		// Infer CDN products from each assembled repo's name — same logic as PrefetchAsync for single builds.
		// Repos without a products.yml entry fall back to the repo name itself (which may or may not be a
		// valid CDN product; IsValidCdnProductId filters out anything that could not be a real product path).
		var inferredProducts = sources
			.AssembleSets
			.Values
			.Select(
				s => configurationContext.Catalog.Products.GetProductByRepositoryName(s.Checkout.Repository.Name)?.Id ?? s
					.Checkout
					.Repository
					.Name
			)
			.Where(ReleaseNotesFetcher.IsValidCdnProductId)
			.Except(declaredProducts, StringComparer.Ordinal)
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		if (declaredProducts.Length > 0 || inferredProducts.Length > 0)
		{
			var releaseNotesFetcher = new ReleaseNotesFetcher(logFactory, context.ReadFileSystem);
			var fetched = await releaseNotesFetcher.FetchAsync(
				context.Collector,
				declaredProducts,
				inferredProducts.Length > 0 ? inferredProducts : null,
				ctx
			).ConfigureAwait(false);
			releaseNotesResolver.Populate(fetched);
			logger.LogInformation(
				"  AssembleAsync: Fetched release notes for {Declared} declared + {Inferred} inferred product(s)",
				declaredProducts.Length,
				inferredProducts.Length
			);
		}

		foreach (var (_, set) in sources.AssembleSets)
		{
			logger.LogInformation("Resolving directory tree for {RepositoryName}", set.Checkout.Repository.Name);
			await set.DocumentationSet.ResolveDirectoryTree(ctx);
		}

		return sources;
	}

	internal static AssembleSources ForTests(AssembleContext context, FrozenDictionary<string, AssemblerDocumentationSet> assembleSets) =>
		new(context, assembleSets);

	private AssembleSources(AssembleContext context, FrozenDictionary<string, AssemblerDocumentationSet> assembleSets)
	{
		AssembleContext = context;
		AssembleSets = assembleSets;
		NavigationTocMappings = FrozenDictionary<Uri, NavigationTocMapping>.Empty;
		LegacyUrlMappings = context.Catalog.LegacyUrlMappings;
		UriResolver = new PublishEnvironmentUriResolver(NavigationTocMappings, context.Environment);
		CrossLinkResolver = NoopCrossLinkResolver.Instance;
	}

	private AssembleSources(
		ILoggerFactory logFactory,
		AssembleContext assembleContext,
		Checkout[] checkouts,
		IConfigurationContext configurationContext,
		FrozenDictionary<Uri, NavigationTocMapping> navigationTocMappings,
		LegacyUrlMappingCatalog legacyUrlMappings,
		PublishEnvironmentUriResolver uriResolver,
		ICrossLinkResolver crossLinkResolver,
		IReleaseNotesResolver releaseNotesResolver,
		IReadOnlySet<Exporter> availableExporters
	)
	{
		NavigationTocMappings = navigationTocMappings;
		LegacyUrlMappings = legacyUrlMappings;
		UriResolver = uriResolver;
		CrossLinkResolver = crossLinkResolver;
		AssembleContext = assembleContext;
		AssembleSets = checkouts
			.Where(c => c.Repository is { Skip: false })
			.Select(
				c => new AssemblerDocumentationSet(
					logFactory,
					assembleContext,
					c,
					crossLinkResolver,
					releaseNotesResolver,
					configurationContext,
					availableExporters
				)
			)
			.ToDictionary(s => s.Checkout.Repository.Name, s => s)
			.ToFrozenDictionary();
	}

	public static FrozenDictionary<Uri, NavigationTocMapping> GetTocMappings(AssembleContext context) =>
		SiteNavigationLoader.GetTocMappings(context.Navigation, context.ConfigurationFileProvider.NavigationFile, context.Collector);
}
