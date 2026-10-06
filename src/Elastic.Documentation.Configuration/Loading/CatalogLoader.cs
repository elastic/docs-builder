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
/// The only place these files are read, and the place their constraints are checked: every violation of every part rides in
/// the result, and an error means no catalog. A parser failure becomes an error diagnostic carrying the message the parser
/// throws, so callers that require the catalog fail with the same message as before.
/// </summary>
public static class CatalogLoader
{
	public static Result<GlobalCatalog> Load(ConfigurationFileProvider provider)
	{
		var diagnostics = new List<Diagnostic>();

		// Each stage is Parse + Map (the provider's Create* helper, internal) + Rules through the pipeline; every stage's
		// violations are kept, and a stage that could not be built stops the load.
		if (!Stage("versions.yml", provider.CreateVersionConfiguration, VersioningSystemConstraints.All, diagnostics, out var versions))
			return Result.Failure<GlobalCatalog>(diagnostics);

		if (!Stage("products.yml", () => provider.CreateProducts(versions), ProductCatalogConstraints.All, diagnostics, out var products))
			return Result.Failure<GlobalCatalog>(diagnostics);
		diagnostics.AddRange(
			ProductCatalogInVersioningSystemCatalogConstraints
				.All
				.Report(new ProductCatalogInVersioningSystemCatalog(products, versions))
				.ToDiagnostics("products.yml")
		);

		if (!Stage("legacy-url-mappings.yml", () => provider.CreateLegacyUrlMappings(products), null, diagnostics, out var legacy))
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

	private static bool Stage<T>(
		string file,
		Func<T> build,
		Nullean.Entities.ConstraintSet<T>? rules,
		List<Diagnostic> diagnostics,
		out T value
	) where T : class
	{
		// A stage with no set of its own (legacy-url-mappings) is only built; the pair set that relates it to the products checks it.
		var result = rules is null ? LoaderPipeline.Build(file, build) : LoaderPipeline.Run(file, build, rules, errorsAreFatal: false);
		diagnostics.AddRange(result.Diagnostics);
		value = result.Value!;
		return result.Value is not null;
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
		const string file = "related-learning.yml";
		return LoaderPipeline.Run(
			file,
			parse: () =>
			{
				using var reader = provider.RelatedLearningFile.OpenText();
				return reader.ReadToEnd();
			},
			map: RelatedLearningConfigurationExtensions.ParseUnchecked,
			rules: RelatedLearningConstraints.All
		);
	}
}

/// <summary>Loads the <see cref="SearchTuning"/> aggregate from <c>search.yml</c> (or the legacy <c>synonyms.yml</c>) and checks it.</summary>
public static class SearchTuningLoader
{
	public static Result<SearchTuning> Load(ConfigurationFileProvider provider) =>
		LoaderPipeline.Run(
			"search.yml",
			provider.CreateSearchConfiguration,
			SearchTuningConstraints.All,
			fileOf: tuning => tuning.SourceFileName
		);
}
