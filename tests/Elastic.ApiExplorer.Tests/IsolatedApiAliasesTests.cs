// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class IsolatedApiAliasesTests
{
	[Test]
	[Arguments("docs-builder-elasticsearch", true)]
	[Arguments("docs-builder-cloud-connect", true)]
	[Arguments("elasticsearch", false)]
	[Arguments("cloud-connect", false)]
	public void IsFixtureKey_MatchesPrefixedKeysOnly(string apiKey, bool expected) =>
		IsolatedApiAliases.IsFixtureKey(apiKey).Should().Be(expected);

	[Test]
	[Arguments("docs-builder-elasticsearch", "elasticsearch")]
	[Arguments("docs-builder-cloud-connect", "cloud-connect")]
	[Arguments("kibana", "kibana")]
	public void UrlKey_FixtureKey_DropsPrefix(string apiKey, string expected) => IsolatedApiAliases.UrlKey(apiKey).Should().Be(expected);
}
