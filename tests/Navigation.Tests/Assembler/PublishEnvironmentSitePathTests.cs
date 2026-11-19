// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class PublishEnvironmentSitePathTests
{
	private static PublishEnvironmentUriResolver Resolver(string? pathPrefix, bool apiExplorer) =>
		new(
			new Dictionary<Uri, NavigationTocMapping>().ToFrozenDictionary(),
			new PublishEnvironment
			{
				Uri = "https://www.elastic.co",
				PathPrefix = pathPrefix,
				FeatureFlags = new() { ["ASSEMBLER_API_EXPLORER"] = apiExplorer }
			}
		);

	[Test]
	[Arguments("docs", "https://www.elastic.co/docs/api/elasticsearch/operation/operation-search")]
	[Arguments("", "https://www.elastic.co/api/elasticsearch/operation/operation-search")]
	public void ResolveSitePath_AppliesOnlyTheEnvironmentPathPrefix(string pathPrefix, string expected)
	{
		var resolved = Resolver(pathPrefix, apiExplorer: true).ResolveSitePath(
			new Uri("elasticsearch://api/elasticsearch/operation/operation-search"),
			"api/elasticsearch/operation/operation-search"
		);

		resolved.ToString().Should().Be(expected, "a floating page is not under any repository path_prefix");
	}

	[Test]
	[Arguments(true, false)]
	[Arguments(false, true)]
	public void LegacyApiUrls_FollowsTheApiExplorerFlag(bool apiExplorer, bool legacy) =>
		// An environment without the API Explorer still proxies the legacy /api/doc/ URLs to bump.sh.
		Resolver("docs", apiExplorer).LegacyApiUrls.Should().Be(legacy);

	[Test]
	public void IsolatedResolver_LegacyApiUrls_MatchesWhatProdServes()
	{
		// Isolated builds link API pages to www.elastic.co, so their form must flip with prod's flag at the cutover.
		var config = AssemblyConfiguration.Deserialize(
			File.ReadAllText(Path.Join(Paths.WorkingDirectoryRoot.FullName, "config", "assembler.yml"))
		);
		var prodServesLegacy = !config.Environments["prod"].ToFeatureFlags().AssemblerApiExplorerEnabled;

		new IsolatedBuildEnvironmentUriResolver().LegacyApiUrls.Should().Be(prodServesLegacy);
	}
}
