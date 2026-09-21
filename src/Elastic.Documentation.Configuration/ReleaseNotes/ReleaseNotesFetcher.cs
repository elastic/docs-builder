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
/// startup, concurrently, mirroring how cross-links are fetched. Explicitly declared products are
/// strict on real errors, but a CDN 404 is a warning (the product is registered, no release cut yet).
/// Auto-inferred products are best-effort: a 404 is silently skipped and the directive emits a hint.
/// </summary>
public sealed class ReleaseNotesFetcher(ILoggerFactory logFactory, IFileSystem fileSystem, HttpMessageHandler? handler = null)
{
	private readonly ILoggerFactory _logFactory = logFactory;
	private readonly IFileSystem _fileSystem = fileSystem;
	private readonly HttpMessageHandler? _handler = handler;
	private readonly ILogger _logger = logFactory.CreateLogger<ReleaseNotesFetcher>();

	/// <summary>
	/// Prefetches release notes for all products and returns a ready resolver.
	/// Explicit products come from <c>release_notes</c> in docset.yml (required; 404 is a warning).
	/// The product is also inferred from the repository name via products.yml (best-effort, no error on 404).
	/// </summary>
	public static async Task<IReleaseNotesResolver> PrefetchAsync(BuildContext context, ILoggerFactory logFactory, Cancel ctx)
	{
		var explicitProducts = context.Configuration.ReleaseNotesProducts;

		// Infer the CDN product from the repository name via products.yml, same logic as the directive's
		// valueless :cdn: option. Only infer when the product isn't already explicitly declared.
		string? inferredProduct = null;
		if (context.Git.IsAvailable)
		{
			var repo = context.Git.RepositoryName;
			var candidate = context.ProductsConfiguration.GetProductByRepositoryName(repo)?.Id ?? repo;
			if (IsValidCdnProductId(candidate) && !explicitProducts.Contains(candidate, StringComparer.Ordinal))
				inferredProduct = candidate;
		}

		if (explicitProducts.Length == 0 && inferredProduct is null)
			return NoopReleaseNotesResolver.Instance;

		var fetcher = new ReleaseNotesFetcher(logFactory, context.ReadFileSystem);
		var fetched = await fetcher.FetchAsync(
			context.Collector,
			explicitProducts,
			inferredProduct is not null ? [inferredProduct] : null,
			ctx
		).ConfigureAwait(false);
		return new ReleaseNotesResolver(fetched);
	}

	/// <summary>
	/// Fetches bundles for <paramref name="requiredProducts"/> (error on real failure; 404 is a warning)
	/// and optionally for <paramref name="inferredProducts"/> (no error on 404 — best-effort only).
	/// </summary>
	public async Task<FetchedReleaseNotes> FetchAsync(
		IDiagnosticsCollector collector,
		IReadOnlyCollection<string> requiredProducts,
		IReadOnlyCollection<string>? inferredProducts = null,
		Cancel ctx = default
	)
	{
		var required = requiredProducts
			.Where(p => !string.IsNullOrWhiteSpace(p))
			.Select(p => p.Trim())
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		var inferredSet = inferredProducts is { Count: > 0 }
			? inferredProducts.Where(p => p is { Length: > 0 }).Except(required, StringComparer.Ordinal).ToFrozenSet(StringComparer.Ordinal)
			: [];

		if (required.Length == 0 && inferredSet.Count == 0)
			return FetchedReleaseNotes.Empty;

		var baseUri = ChangelogCdn.ResolveBaseUri();
		if (baseUri is null)
		{
			collector.EmitError(
				string.Empty,
				$"No valid changelog CDN base URL is configured. Set the {ChangelogCdn.BaseUrlEnvironmentVariable} environment variable to an absolute http(s) URL."
			);
			return new FetchedReleaseNotes
			{
				BundlesByProduct = [],
				DeclaredProducts = required.ToFrozenSet(StringComparer.Ordinal),
				NotFoundInferredProducts = []
			};
		}

		var allProducts = required.Concat(inferredSet).Distinct(StringComparer.Ordinal).ToArray();
		_logger.LogInformation("Fetching release notes for {Count} product(s) from {BaseUri}", allProducts.Length, baseUri);

		using var fetcher = new CdnChangelogFetcher(_logFactory, _fileSystem, _handler);
		var tasks = allProducts.Select(async product =>
		{
			var isRequired = required.Contains(product, StringComparer.Ordinal);
			var notFound = false;
			// version: null — prefetch the full set; each directive applies its own :version: filter later.
			var bundles = await fetcher.FetchAsync(baseUri, product, version: null, emitError: isRequired
				? msg => collector.EmitError(string.Empty, msg)
				: msg => collector.EmitWarning(string.Empty, msg), // inferred: real errors become warnings, not build failures
			 emitWarning: msg => collector.EmitWarning(string.Empty, msg), ctx,
			// 404 for products registered in products.yml is a warning, not an error — products.yml
			// membership is the authoritative gate; 404 means no release has been cut yet.
			// 404 for inferred products is silently tracked (hint emitted by the directive).
			emitNotFound: _ =>
			{
				notFound = true;
				if (isRequired)
					collector.EmitWarning(
						string.Empty,
						$"No CDN bundles published yet for '{product}' (registered in products.yml). The changelog will render empty until the first release is published."
					);
			}).ConfigureAwait(false);
			return (product, bundles, isRequired, notFound);
		});

		var results = await Task.WhenAll(tasks).ConfigureAwait(false);

		// Declared products: only those explicitly listed under release_notes — never inferred products.
		// Keeping inferred products out of DeclaredProducts prevents cross-repo contamination in assembler
		// runs where a single resolver is shared: one repo inferring 'foo' must not suppress the
		// undeclared-product error for an explicit :cdn: foo reference in a different repo.
		var declaredSet = required.ToFrozenSet(StringComparer.Ordinal);

		// BundlesByProduct includes all products that returned bundles (required or inferred).
		// The directive reaches inferred bundles via TryGetBundles when isAutoInferred is true.
		var bundleMap = results.Where(r => r.bundles.Count > 0).ToFrozenDictionary(r => r.product, r => r.bundles, StringComparer.Ordinal);

		// Track products that returned HTTP 404 so the directive can emit a "not yet published" hint.
		var notFoundInferred = results.Where(r => !r.isRequired && r.notFound).Select(r => r.product).ToFrozenSet(StringComparer.Ordinal);
		var notFoundDeclared = results.Where(r => r.isRequired && r.notFound).Select(r => r.product).ToFrozenSet(StringComparer.Ordinal);

		return new FetchedReleaseNotes
		{
			BundlesByProduct = bundleMap,
			DeclaredProducts = declaredSet,
			NotFoundInferredProducts = notFoundInferred,
			NotFoundDeclaredProducts = notFoundDeclared
		};
	}

	public static bool IsValidCdnProductId(string product) =>
		product.Length > 0 && product.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');
}
