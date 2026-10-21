// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Search;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.IntegrationTests;

public static class TestHelpers
{
	public static IConfigurationContext CreateConfigurationContext(
		IFileSystem fileSystem,
		VersioningSystemCatalog? versionsConfiguration = null,
		ConfigurationFileProvider? configurationFileProvider = null,
		ProductCatalog? productsConfiguration = null
	)
	{
		configurationFileProvider ??= new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fileSystem),
			skipPrivateRepositories: true
		);
		versionsConfiguration ??= new VersioningSystemCatalog
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack,
					new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Current = new SemVersion(8, 0, 0),
						Base = new SemVersion(8, 0, 0)
					}
				}
			},
		};
		if (productsConfiguration is null)
		{
			var products = new Dictionary<string, Product>
			{
				{
					"elasticsearch",
					new Product
					{
						Id = "elasticsearch",
						DisplayName = "Elasticsearch",
						VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
					}
				}
			};
			productsConfiguration = ProductCatalog.FromPublicProducts(products);
		}
		var search = SearchTuning.None;
		return new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = ElasticsearchEndpoint.Default, },
			ConfigurationFileProvider = configurationFileProvider,
			Catalog = new GlobalCatalog(versionsConfiguration, productsConfiguration, LegacyUrlMappingCatalog.None),
			SearchTuning = search,
		};
	}
}
