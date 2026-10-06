// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Loads the <see cref="GlobalCatalog"/> aggregate: <c>versions.yml</c>, then <c>products.yml</c> (which resolves each
/// product's versioning system), then <c>legacy-url-mappings.yml</c> (which resolves each mapping's product).
/// The only place these files are read, and the place their constraints are checked: every violation of every part rides in
/// the result, and an error means no catalog. A parser failure becomes an error diagnostic carrying the message the parser
/// throws, so callers that require the catalog fail with the same message as before.
/// </summary>
public static class CatalogLoader
{
	private static readonly ConditionalWeakTable<ConfigurationFileProvider, Result<GlobalCatalog>> LoadedByProvider = [];

	/// <summary><see cref="Load"/>, once per provider, so the composition root and the findings report share one load.</summary>
	public static Result<GlobalCatalog> LoadOnce(ConfigurationFileProvider provider) => LoadedByProvider.GetValue(provider, Load);

	public static Result<GlobalCatalog> Load(ConfigurationFileProvider provider)
	{
		var diagnostics = new List<Diagnostic>();

		if (!TryLoad("versions.yml", provider.CreateVersionConfiguration, out var versions, diagnostics))
			return Result.Failure<GlobalCatalog>(diagnostics);
		diagnostics.AddRange(VersioningSystemConstraints.All.Report(versions).ToDiagnostics("versions.yml"));

		if (!TryLoad("products.yml", () => provider.CreateProducts(versions), out var products, diagnostics))
			return Result.Failure<GlobalCatalog>(diagnostics);
		diagnostics.AddRange(ProductCatalogConstraints.All.Report(products).ToDiagnostics("products.yml"));
		diagnostics.AddRange(
			ProductCatalogInVersioningSystemCatalogConstraints
				.All
				.Report(new ProductCatalogInVersioningSystemCatalog(products, versions))
				.ToDiagnostics("products.yml")
		);

		if (!TryLoad("legacy-url-mappings.yml", () => provider.CreateLegacyUrlMappings(products), out var legacy, diagnostics))
			return Result.Failure<GlobalCatalog>(diagnostics);
		diagnostics.AddRange(
			LegacyUrlMappingCatalogInProductCatalogConstraints
				.All
				.Report(new LegacyUrlMappingCatalogInProductCatalog(legacy, products))
				.ToDiagnostics("legacy-url-mappings.yml")
		);

		return diagnostics.Any(d => d.Severity == Severity.Error)
			? Result.Failure<GlobalCatalog>(diagnostics)
			: Result.Success(new GlobalCatalog(versions, products, legacy), diagnostics);
	}

	private static bool TryLoad<T>(string file, Func<T> load, out T value, List<Diagnostic> diagnostics)
	{
		try
		{
			value = load();
			return true;
		}
		catch (Exception e) when (e is not OutOfMemoryException)
		{
			value = default!;
			diagnostics.Add(new Diagnostic { Severity = Severity.Error, File = file, Message = e.Message });
			return false;
		}
	}
}

/// <summary>
/// Loads the <see cref="RelatedLearningCatalog"/> aggregate from <c>related-learning.yml</c>. Separate from the
/// <see cref="GlobalCatalog"/> because only documentation builds need it, and it has always been read when a build
/// starts rather than when the process starts.
/// </summary>
public static class RelatedLearningLoader
{
	public static Result<RelatedLearningCatalog> Load(ConfigurationFileProvider provider)
	{
		try
		{
			using var reader = provider.RelatedLearningFile.OpenText();
			var catalog = RelatedLearningConfigurationExtensions.ParseUnchecked(reader.ReadToEnd());
			var result = Result.From(RelatedLearningConstraints.All.Report(catalog), "related-learning.yml");
			return result.HasErrors ? Result.Failure<RelatedLearningCatalog>(result.Diagnostics) : result;
		}
		catch (Exception e) when (e is not OutOfMemoryException)
		{
			return Result.Failure<RelatedLearningCatalog>([
				new Diagnostic { Severity = Severity.Error, File = "related-learning.yml", Message = e.Message }
			]);
		}
	}
}

/// <summary>Loads the <see cref="SearchTuning"/> aggregate from <c>search.yml</c> (or the legacy <c>synonyms.yml</c>) and checks it.</summary>
public static class SearchTuningLoader
{
	private static readonly ConditionalWeakTable<ConfigurationFileProvider, Result<SearchTuning>> LoadedByProvider = [];

	/// <summary><see cref="Load"/>, once per provider, so the composition root and the findings report share one load.</summary>
	public static Result<SearchTuning> LoadOnce(ConfigurationFileProvider provider) => LoadedByProvider.GetValue(provider, Load);

	public static Result<SearchTuning> Load(ConfigurationFileProvider provider)
	{
		try
		{
			var tuning = provider.CreateSearchConfiguration();
			var result = Result.From(SearchTuningConstraints.All.Report(tuning), tuning.SourceFileName ?? "search.yml");
			return result.HasErrors ? Result.Failure<SearchTuning>(result.Diagnostics) : result;
		}
		catch (Exception e) when (e is not OutOfMemoryException)
		{
			return Result.Failure<SearchTuning>([new Diagnostic { Severity = Severity.Error, File = "search.yml", Message = e.Message }]);
		}
	}
}
