// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Codex;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Navigation.Tests.Codex;

public class CodexStaticResourcesTests
{
	[Test]
	public async Task ExtractEmbeddedStaticResources_SkipsViteManifestButCopiesTheEntryBundle()
	{
		var outputFs = new MockFileSystem();
		var output = outputFs.DirectoryInfo.New("/codex-output");
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			CreateConfigurationContext()
		);

		await new CodexGenerator(NullLoggerFactory.Instance, context, output).ExtractEmbeddedStaticResources(CancellationToken.None);

		var copied = outputFs.AllFiles.Select(f => outputFs.Path.GetFileName(f)).ToArray();
		copied.Should().NotContain(f => f.StartsWith('.'));
		copied.Should().Contain(f => f.StartsWith("main-") && f.EndsWith(".js"));
	}

	private static ConfigurationContext CreateConfigurationContext() =>
		new()
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = ElasticsearchEndpoint.Default },
			ConfigurationFileProvider = new ConfigurationFileProvider(
				NullLoggerFactory.Instance,
				new ConfigurationFileSystem(new FileSystem())
			),
			VersionsConfiguration = new VersionsConfiguration
			{
				VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>()
			},
			ProductsConfiguration = new ProductsConfiguration
			{
				Products = FrozenDictionary<string, Product>.Empty,
				PublicReferenceProducts = FrozenDictionary<string, Product>.Empty,
				ProductDisplayNames = FrozenDictionary<string, string>.Empty
			},
			LegacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] },
			SearchConfiguration = new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
		};
}
