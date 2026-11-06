// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class PublishEnvironmentSitePathTests
{
	private static PublishEnvironmentUriResolver Resolver(string? pathPrefix) =>
		new(
			new Dictionary<Uri, NavigationTocMapping>().ToFrozenDictionary(),
			new PublishEnvironment { Uri = "https://www.elastic.co", PathPrefix = pathPrefix }
		);

	[Test]
	[Arguments("docs", "https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search")]
	[Arguments("", "https://www.elastic.co/api/doc/elasticsearch/operation/operation-search")]
	public void ResolveSitePath_AppliesOnlyTheEnvironmentPathPrefix(string pathPrefix, string expected)
	{
		var resolved = Resolver(pathPrefix).ResolveSitePath(
			new Uri("elasticsearch://api/elasticsearch/operation/operation-search"),
			"api/doc/elasticsearch/operation/operation-search"
		);

		resolved.ToString().Should().Be(expected, "a floating page is not under any repository path_prefix");
	}
}
