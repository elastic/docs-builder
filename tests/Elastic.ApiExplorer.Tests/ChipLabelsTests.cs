// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;

namespace Elastic.ApiExplorer.Tests;

/// <summary>Chips drop the words every variant name in a list shares.</summary>
public class ChipLabelsTests
{
	[Test]
	public void Shorten_SharedSuffix_IsDropped() =>
		ChipLabels
			.Shorten(["bedrock_secrets", "crowdstrike_secrets", "d3security_secrets"])
			.Should()
			.Equal("bedrock", "crowdstrike", "d3security");

	[Test]
	public void Shorten_SharedPrefix_KeepsTheSeparatorsInside() =>
		ChipLabels
			.Shorten([
				"Kibana_HTTP_APIs_update_output_elasticsearch",
				"Kibana_HTTP_APIs_update_output_remote_elasticsearch",
				"Kibana_HTTP_APIs_update_output_logstash"
			])
			.Should()
			.Equal("elasticsearch", "remote_elasticsearch", "logstash");

	[Test]
	public void Shorten_CamelCaseSuffix_SplitsAtCapitals() =>
		ChipLabels
			.Shorten(["EqlRuleUpdateProps", "QueryRuleUpdateProps", "SavedQueryRuleUpdateProps", "ThreatMatchRuleUpdateProps"])
			.Should()
			.Equal("Eql", "Query", "SavedQuery", "ThreatMatch");

	[Test]
	public void Shorten_NameThatIsAllSharedWords_KeepsAWordOfItsOwn() =>
		ChipLabels.Shorten(["Aggregate", "TermsAggregate", "MaxAggregate"]).Should().Equal("Aggregate", "TermsAggregate", "MaxAggregate");

	[Test]
	public void Shorten_NothingShared_LeavesTheNames() =>
		ChipLabels.Shorten(["string", "LikeDocument", "QueryContainer"]).Should().Equal("string", "LikeDocument", "QueryContainer");

	[Test]
	public void Shorten_ArrayAndPlainVariantOfOneName_StayAsTheyAre() =>
		ChipLabels.Shorten(["Cat", "Cat", "Cat"]).Should().Equal("Cat", "Cat", "Cat");

	[Test]
	public void Shorten_AcronymBeforeAWord_SplitsAfterTheAcronym() =>
		ChipLabels.Shorten(["HTTPServerConfig", "HTTPClientConfig", "HTTPProxyConfig"]).Should().Equal("Server", "Client", "Proxy");
}
