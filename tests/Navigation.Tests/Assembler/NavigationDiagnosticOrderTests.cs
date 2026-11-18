// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Search;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging.Abstractions;
using SharpYaml;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

/// <summary>
/// The order in which an assembler build reports what is wrong with <c>navigation.yml</c>: the toc mappings come first (a mount without a path prefix),
/// then the parse of the file reports its unknown keys, and the check that no two mounts share a path prefix comes last, after the docsets were assembled.
/// </summary>
public class NavigationDiagnosticOrderTests
{
	/// <summary>The steps of <c>AssemblerBuildService</c> on <paramref name="navigation"/>: the mappings, the parse, the prefix check. What each reported, then what threw.</summary>
	private static (IReadOnlyList<string> Messages, Exception? Thrown, bool? Valid) Steps(string navigation)
	{
		var configFileSystem = new MockFileSystem();
		var directory = ConfigurationFileProvider.AppDataConfigurationDirectory;
		foreach (var (file, content) in new[]
		{
			("assembler.yml", BaselineSite.ReadBaseline("assembler.yml")),
			("navigation.yml", navigation),
			("git-ref.txt", "baseline")
		})
			configFileSystem.AddFile(Path.Join(directory, file), new MockFileData(content));
		foreach (var file in new[] { "versions.yml", "products.yml", "legacy-url-mappings.yml", "search.yml", "related-learning.yml" })
			configFileSystem.AddFile(Path.Join(directory, file), new MockFileData(string.Empty));
		using var provider = new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(configFileSystem),
			skipPrivateRepositories: false,
			ConfigurationSource.Remote
		);
		var configurationContext = new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = new ElasticsearchEndpoint { Uri = new Uri("http://localhost:9200") } },
			ConfigurationFileProvider = provider,
			Catalog = new GlobalCatalog(
				new VersioningSystemCatalog { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() },
				new ProductCatalog { Products = FrozenDictionary<string, Product>.Empty },
				new LegacyUrlMappingCatalog { Mappings = [] }
			),
			SearchTuning = new SearchTuning { Synonyms = [], Rules = [], DiminishTerms = [] }
		};
		var collector = new TestDiagnosticsCollector();
		var checkouts = new CheckoutsFileSystem(new MockFileSystem().DirectoryInfo.New("/checkouts"), inner: new MockFileSystem());
		var context = new AssembleContext(
			AssemblyLoader.Create(provider),
			configurationContext,
			"prod",
			collector,
			checkouts,
			"/checkouts",
			"/output"
		);

		_ = AssembleSources.GetTocMappings(context);
		Exception? thrown = null;
		bool? valid = null;
		var navigationFile = provider.NavigationFile;
		try
		{
			var parsed = SiteNavigationLoader.Deserialize(
				configFileSystem.File.ReadAllText(navigationFile.FullName),
				collector,
				navigationFile.FullName
			);
			valid = SiteNavigationLoader.ValidatePathPrefixes(collector, parsed, navigationFile);
		}
		catch (Exception e)
		{
			thrown = e;
		}
		return ([.. collector.Diagnostics.Select(d => d.Message.Split(':')[0])], thrown, valid);
	}

	[Test]
	public void Build_NavigationWithAllThreeProblems_ReportsThemInTheOrderOfTheSteps()
	{
		// language=yaml
		const string navigation =
			"""
			toc:
			  - toc: repoa://a
			    path_prefix: shared
			  - toc: repob://b
			    path_prefix: shared/
			    pathprefx: typo
			  - toc: repoc://c
			""";
		var (messages, thrown, valid) = Steps(navigation);

		thrown.Should().BeNull();
		valid.Should().BeFalse();
		messages.Should().Equal(
			"Path prefix is not defined for",
			"Unknown key 'pathprefx' in $.toc[1]. Did you mean 'path_prefix'?",
			"Duplicate path prefix"
		);
	}

	// the mapping reader accepted entries the parser rejects, so what it reports comes first and the parse then throws
	[Test]
	public void Build_EntryWithARepoAndNoToc_MapsAndReportsBeforeTheParseThrows()
	{
		// language=yaml
		const string navigation = """
			toc:
			  - toc: repoc://c
			  - repo: other
			""";

		var (messages, thrown, valid) = Steps(navigation);

		messages.Should().Equal("Path prefix is not defined for", "Unknown key 'repo' in $.toc[1]. Valid keys");
		valid.Should().BeNull();
		thrown
			.Should()
			.BeOfType<YamlException>()
			.Which
			.Message
			.Should()
			.StartWith("toc entry has no 'toc:' key and will be ignored. Found keys: . Check for typos.");
	}
}
