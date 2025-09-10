// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.ApiExplorer.Tests;

public static class TestHelpers
{
	public static IConfigurationContext CreateConfigurationContext(IFileSystem fileSystem, VersionsConfiguration? versionsConfiguration = null, ProductsConfiguration? productsConfiguration = null)
	{
		versionsConfiguration ??= new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack, new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Current = new SemVersion(8, 0, 0),
						Base = new SemVersion(8, 0, 0)
					}
				}
			},
		};
		productsConfiguration ??= new ProductsConfiguration
		{
			Products = new Dictionary<string, Product>()
			{
				{
					"elasticsearch", new Product
					{
						Id = "elasticsearch",
						DisplayName = "Elasticsearch",
						VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
					}
				}
			}.ToFrozenDictionary()
		};
		return new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints
			{
				Elasticsearch = ElasticsearchEndpoint.Default,
			},
			ConfigurationFileProvider = new ConfigurationFileProvider(fileSystem),
			VersionsConfiguration = versionsConfiguration,
			ProductsConfiguration = productsConfiguration,
			LegacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] },
		};
	}
}
