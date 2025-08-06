// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Versions;

namespace Documentation.Assembler.Tests;

public static class TestHelpers
{
	public static IConfigurationContext CreateConfigurationContext(
		IFileSystem fileSystem,
		VersionsConfiguration? versionsConfiguration = null,
		ConfigurationFileProvider? configurationFileProvider = null
	)
	{
		configurationFileProvider ??= new ConfigurationFileProvider(fileSystem);
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
			}
		};
		return new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints
			{
				Elasticsearch = ElasticsearchEndpoint.Default,
			},
			ConfigurationFileProvider = configurationFileProvider,
			VersionsConfiguration = versionsConfiguration
		};
	}
}
