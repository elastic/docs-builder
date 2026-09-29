// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
/// The only place these files are read. A failure becomes an error diagnostic carrying the message the parser throws,
/// so callers that require the catalog fail with the same message as before.
/// </summary>
public static class CatalogLoader
{
	public static Result<GlobalCatalog> Load(ConfigurationFileProvider provider)
	{
		if (!TryLoad("versions.yml", provider.CreateVersionConfiguration, out var versions, out var failure))
			return failure;
		if (!TryLoad("products.yml", () => provider.CreateProducts(versions), out var products, out failure))
			return failure;
		if (!TryLoad("legacy-url-mappings.yml", () => provider.CreateLegacyUrlMappings(products), out var legacy, out failure))
			return failure;
		return Result.Success(new GlobalCatalog(versions, products, legacy));
	}

	private static bool TryLoad<T>(string file, Func<T> load, out T value, out Result<GlobalCatalog> failure)
	{
		try
		{
			value = load();
			failure = null!;
			return true;
		}
		catch (Exception e) when (e is not OutOfMemoryException)
		{
			value = default!;
			failure = Result.Failure<GlobalCatalog>([new Diagnostic { Severity = Severity.Error, File = file, Message = e.Message }]);
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
			return Result.Success(RelatedLearningConfigurationExtensions.Parse(reader.ReadToEnd()));
		}
		catch (Exception e) when (e is not OutOfMemoryException)
		{
			return Result.Failure<RelatedLearningCatalog>([
				new Diagnostic { Severity = Severity.Error, File = "related-learning.yml", Message = e.Message }
			]);
		}
	}
}

/// <summary>Loads the <see cref="SearchTuning"/> aggregate from <c>search.yml</c> (or the legacy <c>synonyms.yml</c>).</summary>
public static class SearchTuningLoader
{
	public static Result<SearchTuning> Load(ConfigurationFileProvider provider)
	{
		try
		{
			return Result.Success(provider.CreateSearchConfiguration());
		}
		catch (Exception e) when (e is not OutOfMemoryException)
		{
			return Result.Failure<SearchTuning>([new Diagnostic { Severity = Severity.Error, File = "search.yml", Message = e.Message }]);
		}
	}
}
