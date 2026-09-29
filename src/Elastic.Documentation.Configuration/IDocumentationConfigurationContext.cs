// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Search;

namespace Elastic.Documentation.Configuration;

public interface IConfigurationContext
{
	/// <summary>Versioning systems, products and legacy URL mappings. Loaded once per process.</summary>
	GlobalCatalog Catalog { get; }

	/// <summary>Named learning destinations for the <c>{related-learning}</c> directive. Loaded on first use.</summary>
	RelatedLearningCatalog RelatedLearning { get; }
	ConfigurationFileProvider ConfigurationFileProvider { get; }
	DocumentationEndpoints Endpoints { get; }
	SearchConfiguration SearchConfiguration { get; }
}

/// Used only to seed <see cref="IConfigurationContext"/> in DI, you primarily want to depend on <see cref="IDocumentationConfigurationContext"/>
public class ConfigurationContext : IConfigurationContext
{
	/// <inheritdoc />
	public required GlobalCatalog Catalog { get; init; }

	/// <inheritdoc />
	/// <remarks>Read from the provider the first time it is needed, unless set explicitly.</remarks>
	public RelatedLearningCatalog RelatedLearning
	{
		get => field ??= RelatedLearningLoader.Load(ConfigurationFileProvider).OrThrow();
		init;
	}

	/// <inheritdoc />
	public required ConfigurationFileProvider ConfigurationFileProvider { get; init; }

	/// <inheritdoc />
	public required DocumentationEndpoints Endpoints { get; init; }

	/// <inheritdoc />
	public required SearchConfiguration SearchConfiguration { get; init; }
}

public interface IDocumentationConfigurationContext : IDocumentationContext, IConfigurationContext
{
	Uri? CanonicalBaseUrl { get; }
}
