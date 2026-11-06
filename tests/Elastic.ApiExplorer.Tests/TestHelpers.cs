// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public static class TestHelpers
{
	public static IConfigurationContext CreateConfigurationContext(
		IFileSystem fileSystem,
		VersionsConfiguration? versionsConfiguration = null,
		ProductsConfiguration? productsConfiguration = null
	)
	{
		versionsConfiguration ??= CreateStackVersionsConfiguration(currentMajor: 9, currentMinor: 0);
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
			productsConfiguration = new ProductsConfiguration
			{
				Products = products.ToFrozenDictionary(),
				PublicReferenceProducts = products.ToFrozenDictionary(),
				ProductDisplayNames = products.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
			};
		}
		var search = new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] };
		return new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = ElasticsearchEndpoint.Default, },
			ConfigurationFileProvider = new ConfigurationFileProvider(NullLoggerFactory.Instance, new ConfigurationFileSystem(fileSystem)),
			VersionsConfiguration = versionsConfiguration,
			ProductsConfiguration = productsConfiguration,
			LegacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] },
			SearchConfiguration = search
		};
	}

	/// <summary>A build context rooted at the working directory with the default test configuration.</summary>
	public static BuildContext CreateBuildContext(
		DiagnosticsCollector? collector = null,
		BuildType buildType = BuildType.Isolated,
		string? urlPathPrefix = null
	) =>
		new(
			collector ?? new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			CreateConfigurationContext(new FileSystem())
		)
		{ BuildType = buildType, UrlPathPrefix = urlPathPrefix };

	/// <summary>A build context like <see cref="CreateBuildContext"/>, with the git scope and the versions and products configuration under test.</summary>
	public static BuildContext CreateScopedBuildContext(
		DiagnosticsCollector collector,
		VersionsConfiguration? versionsConfiguration = null,
		ProductsConfiguration? productsConfiguration = null,
		GitCheckoutInformation? git = null
	) =>
		new(
			collector,
			DocumentationFileSystem.Resolve(
				new FileSystem().DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
				new DocumentationScopeOptions { Git = git }
			),
			CreateConfigurationContext(new FileSystem(), versionsConfiguration, productsConfiguration)
		);

	/// <summary>An exporter whose Stack current version matches <c>config/versions.yml</c> (9.5.6).</summary>
	public static OpenApiDocumentExporter CurrentStackExporter() =>
		new(CreateStackVersionsConfiguration(currentMajor: 9, currentMinor: 5, patch: 6));

	/// <summary>One Elasticsearch version tree of <paramref name="spec"/>, for the exporter.</summary>
	public static IndexedTree EsTree(string moniker, OpenApiDocument spec) => new("elasticsearch", "elasticsearch", moniker, spec);

	public static VersionsConfiguration CreateStackVersionsConfiguration(int currentMajor, int currentMinor = 0, int patch = 0) =>
		new()
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack,
					new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Current = new SemVersion(currentMajor, currentMinor, patch),
						Base = new SemVersion(currentMajor, 0, 0)
					}
				}
			},
		};

	public static VersionsConfiguration CreateVersionlessConfiguration() =>
		new()
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Serverless,
					new VersioningSystem
					{
						Id = VersioningSystemId.Serverless,
						Current = new SemVersion(VersioningSystem.VersionlessSentinel, 0, 0),
						Base = new SemVersion(VersioningSystem.VersionlessSentinel, 0, 0)
					}
				}
			},
		};

	public static Product CreateProduct(string id, VersioningSystem versioningSystem, string? displayName = null) =>
		new() { Id = id, DisplayName = displayName ?? id, VersioningSystem = versioningSystem };
}
