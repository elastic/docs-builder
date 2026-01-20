// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Services.Tests.Changelogs;

public abstract class ChangelogTestBase : IDisposable
{
	protected readonly MockFileSystem _fileSystem;
	protected readonly IConfigurationContext _configurationContext;
	protected readonly TestDiagnosticsCollector _collector;
	protected readonly ILoggerFactory _loggerFactory;
	protected readonly ITestOutputHelper _output;

	protected ChangelogTestBase(ITestOutputHelper output)
	{
		_output = output;
		_fileSystem = new MockFileSystem();
		_collector = new TestDiagnosticsCollector(output);
		_loggerFactory = new TestLoggerFactory(output);

		var versionsConfiguration = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack, new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Current = new SemVersion(9, 2, 0),
						Base = new SemVersion(9, 2, 0)
					}
				}
			},
		};

		var productsConfiguration = new ProductsConfiguration
		{
			Products = new Dictionary<string, Product>
			{
				{
					"elasticsearch", new Product
					{
						Id = "elasticsearch",
						DisplayName = "Elasticsearch",
						VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
					}
				},
				{
					"kibana", new Product
					{
						Id = "kibana",
						DisplayName = "Kibana",
						VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
					}
				},
				{
					"cloud-hosted", new Product
					{
						Id = "cloud-hosted",
						DisplayName = "Elastic Cloud Hosted",
						VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
					}
				},
				{
					"cloud-serverless", new Product
					{
						Id = "cloud-serverless",
						DisplayName = "Elastic Cloud Serverless",
						VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
					}
				}
			}.ToFrozenDictionary()
		};

		_configurationContext = new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints
			{
				Elasticsearch = ElasticsearchEndpoint.Default,
			},
			ConfigurationFileProvider = new ConfigurationFileProvider(NullLoggerFactory.Instance, _fileSystem),
			VersionsConfiguration = versionsConfiguration,
			ProductsConfiguration = productsConfiguration,
			SearchConfiguration = new SearchConfiguration { Synonyms = new Dictionary<string, string[]>(), Rules = [], DiminishTerms = [] },
			LegacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] },
		};
	}

	public void Dispose()
	{
		_loggerFactory.Dispose();
		GC.SuppressFinalize(this);
	}

	[SuppressMessage("Security", "CA5350:Do not use insecure cryptographic algorithm SHA1",
		Justification = "SHA1 is required for compatibility with existing changelog bundle format")]
	protected static string ComputeSha1(string content)
	{
		var bytes = System.Text.Encoding.UTF8.GetBytes(content);
		var hash = System.Security.Cryptography.SHA1.HashData(bytes);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}
}
