// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Loads the <see cref="GlobalCatalog"/> aggregate: <c>versions.yml</c>, then <c>products.yml</c> (which resolves each
/// product's versioning system), then <c>legacy-url-mappings.yml</c> (which resolves each mapping's product).
/// The only place these files are read. A file that cannot be parsed or mapped becomes an error finding carrying the message the parser
/// throws, so a caller that requires the catalog fails with the same message as before.
/// </summary>
public static class CatalogLoader
{
	private static readonly Loader<VersionsConfigDto, VersioningSystemCatalog> Versions = Loader.For(
		parse: (text, file) => ConfigurationFileProvider.Deserialize<VersionsConfigDto>(text, file),
		map: (dto, _) => VersionsConfigurationExtensions.ToCatalog(dto)
	);

	public static LoadResult<GlobalCatalog> Load(ConfigurationFileProvider provider)
	{
		var versionsFile = provider.VersionFile.FullName;
		var versions = Versions.Load(ReadText(provider.VersionFile), versionsFile);
		if (versions is not Loaded<VersioningSystemCatalog> loadedVersions)
			return new Unreadable<GlobalCatalog>(versions.Findings());

		var productsFile = provider.ProductsFile.FullName;
		var products = ProductsLoader(loadedVersions.Value).Load(ReadText(provider.ProductsFile), productsFile);
		if (products is not Loaded<ProductCatalog> loadedProducts)
			return new Unreadable<GlobalCatalog>([.. versions.Findings(), .. products.Findings()]);

		var legacyFile = provider.LegacyUrlMappingsFile.FullName;
		var legacy = LegacyLoader(loadedProducts.Value).Load(ReadText(provider.LegacyUrlMappingsFile), legacyFile);
		if (legacy is not Loaded<LegacyUrlMappingCatalog> loadedLegacy)
			return new Unreadable<GlobalCatalog>([.. versions.Findings(), .. products.Findings(), .. legacy.Findings()]);

		IReadOnlyList<Diagnostic> findings =
		[
			.. versions.Findings(),
			.. products.Findings(),
			.. new ProductCatalogInVersioningSystemCatalog(loadedProducts.Value, loadedVersions.Value).Check(productsFile).Findings,
			.. legacy.Findings(),
			.. new LegacyUrlMappingCatalogInProductCatalog(loadedLegacy.Value, loadedProducts.Value).Check(legacyFile).Findings
		];
		return findings.Any(d => d.Severity == Severity.Error)
			? new Unreadable<GlobalCatalog>(findings)
			: new Loaded<GlobalCatalog>(new GlobalCatalog(loadedVersions.Value, loadedProducts.Value, loadedLegacy.Value), findings);
	}

	// products.yml and legacy-url-mappings.yml are resolved against what was loaded before them, so their loaders close over it.
	private static Loader<ProductConfigDto, ProductCatalog> ProductsLoader(VersioningSystemCatalog versions) =>
		Loader.For(
			parse: (text, file) => ConfigurationFileProvider.Deserialize<ProductConfigDto>(text, file),
			map: (dto, _) => ProductExtensions.ToCatalog(dto, versions)
		);

	private static Loader<LegacyUrlMappingConfigDto, LegacyUrlMappingCatalog> LegacyLoader(ProductCatalog products) =>
		Loader.For(
			parse: (text, file) => ConfigurationFileProvider.Deserialize<LegacyUrlMappingConfigDto>(text, file),
			map: (dto, _) => LegacyUrlMappingExtensions.ToCatalog(dto, products)
		);

	private static string ReadText(System.IO.Abstractions.IFileInfo file)
	{
		using var reader = file.OpenText();
		return reader.ReadToEnd();
	}
}

/// <summary>
/// Loads the <see cref="RelatedLearningCatalog"/> aggregate from <c>related-learning.yml</c>. Separate from the
/// <see cref="GlobalCatalog"/> because only documentation builds need it, and it has always been read when a build
/// starts rather than when the process starts.
/// </summary>
public static class RelatedLearningLoader
{
	private static readonly Loader<RelatedLearningConfigDto, RelatedLearningCatalog> Yaml = Loader.For(
		parse: (text, file) => ConfigurationFileProvider.Deserialize<RelatedLearningConfigDto>(text, file),
		map: (dto, _) => RelatedLearningConfigurationExtensions.FromDto(dto)
	);

	public static LoadResult<RelatedLearningCatalog> Load(ConfigurationFileProvider provider)
	{
		using var reader = provider.RelatedLearningFile.OpenText();
		return Yaml.Load(reader.ReadToEnd(), provider.RelatedLearningFile.FullName);
	}
}
