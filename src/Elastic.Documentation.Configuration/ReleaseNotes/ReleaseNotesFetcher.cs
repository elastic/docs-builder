// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// Prefetches CDN changelog bundles for every product declared under <c>release_notes</c> at build
/// startup, concurrently, mirroring how cross-links are fetched. Any failure to read a product's
/// registry is emitted as an error on the collector (strict fail-fast): a declared product whose
/// content cannot be sourced fails the build rather than silently rendering an empty changelog.
/// </summary>
public sealed class ReleaseNotesFetcher(ILoggerFactory logFactory, IFileSystem fileSystem, HttpMessageHandler? handler = null)
{
	private readonly ILoggerFactory _logFactory = logFactory;
	private readonly IFileSystem _fileSystem = fileSystem;
	private readonly HttpMessageHandler? _handler = handler;
	private readonly ILogger _logger = logFactory.CreateLogger<ReleaseNotesFetcher>();

	/// <summary>
	/// Prefetches release notes for the products declared in <paramref name="context"/>'s docset.yml and
	/// returns a ready resolver. Returns the no-op resolver when nothing is declared (no network is hit).
	/// </summary>
	public static async Task<IReleaseNotesResolver> PrefetchAsync(BuildContext context, ILoggerFactory logFactory, Cancel ctx)
	{
		var products = context.Configuration.ReleaseNotesProducts;
		if (products.Length == 0)
			return NoopReleaseNotesResolver.Instance;

		var fetcher = new ReleaseNotesFetcher(logFactory, context.ReadFileSystem);
		var fetched = await fetcher.FetchAsync(context.Collector, products, ctx).ConfigureAwait(false);
		return new ReleaseNotesResolver(fetched);
	}

	public async Task<FetchedReleaseNotes> FetchAsync(IDiagnosticsCollector collector, IReadOnlyCollection<string> products, Cancel ctx)
	{
		var declared = products
			.Where(p => !string.IsNullOrWhiteSpace(p))
			.Select(p => p.Trim())
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		if (declared.Length == 0)
			return FetchedReleaseNotes.Empty;

		var declaredSet = declared.ToFrozenSet(StringComparer.Ordinal);

		var baseUri = ChangelogCdn.ResolveBaseUri();
		if (baseUri is null)
		{
			collector.EmitError(string.Empty,
				$"No valid changelog CDN base URL is configured. Set the {ChangelogCdn.BaseUrlEnvironmentVariable} environment variable to an absolute http(s) URL.");
			return new FetchedReleaseNotes
			{
				BundlesByProduct = FrozenDictionary<string, IReadOnlyList<LoadedBundle>>.Empty,
				DeclaredProducts = declaredSet
			};
		}

		_logger.LogInformation("Fetching release notes for {Count} product(s) from {BaseUri}", declared.Length, baseUri);

		using var fetcher = new CdnChangelogFetcher(_logFactory, _fileSystem, _handler);
		var tasks = declared.Select(async product =>
		{
			// version: null — prefetch the full set; each directive applies its own :version: filter later.
			var bundles = await fetcher.FetchAsync(
				baseUri,
				product,
				version: null,
				msg => collector.EmitError(string.Empty, msg),
				msg => collector.EmitWarning(string.Empty, msg),
				ctx).ConfigureAwait(false);
			return (product, bundles);
		});

		var results = await Task.WhenAll(tasks).ConfigureAwait(false);

		return new FetchedReleaseNotes
		{
			BundlesByProduct = results.ToFrozenDictionary(r => r.product, r => r.bundles, StringComparer.Ordinal),
			DeclaredProducts = declaredSet
		};
	}
}
