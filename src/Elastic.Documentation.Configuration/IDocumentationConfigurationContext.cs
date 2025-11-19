// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Synonyms;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Documentation.Configuration;

public interface IConfigurationContext
{
	VersionsConfiguration VersionsConfiguration { get; }
	ConfigurationFileProvider ConfigurationFileProvider { get; }
	DocumentationEndpoints Endpoints { get; }
	ProductsConfiguration ProductsConfiguration { get; }
	LegacyUrlMappingConfiguration LegacyUrlMappings { get; }
	SynonymsConfiguration SynonymsConfiguration { get; }
}

/// Used only to seed <see cref="IConfigurationContext"/> in DI, you primarily want to depend on <see cref="IDocumentationConfigurationContext"/>
public class ConfigurationContext : IConfigurationContext
{
	/// <inheritdoc />
	public required VersionsConfiguration VersionsConfiguration { get; init; }

	/// <inheritdoc />
	public required ConfigurationFileProvider ConfigurationFileProvider { get; init; }

	/// <inheritdoc />
	public required DocumentationEndpoints Endpoints { get; init; }

	/// <inheritdoc />
	public required ProductsConfiguration ProductsConfiguration { get; init; }

	/// <inheritdoc />
	public required LegacyUrlMappingConfiguration LegacyUrlMappings { get; init; }

	/// <inheritdoc />
	public required SynonymsConfiguration SynonymsConfiguration { get; init; }
}

public interface IDocumentationConfigurationContext : IDocumentationContext, IConfigurationContext
{
	Uri? CanonicalBaseUrl { get; }
}
