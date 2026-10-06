// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Serverless;

namespace Elastic.Changelog.Tests.Serverless;

public class ServerlessPromotionTests
{
	private const string VersionsYaml =
		"""
		services:
		  kibana:
		    versions:
		      production-canary-ds-1: "883abb05af0b"
		      production-noncanary-ds-1: "883abb05af0b"
		      production-noncanary-ds-4: "7dd981dcd7d3"
		      production-noncanary-ds-5: "7dd981dcd7d3"
		      production-noncanary-ds-50: "aaaaaaaaaaaa"
		""";

	[Test]
	public void ParseFinalSliceVersion_ReadsDs5NotDs1() =>
		ServerlessPromotion.ParseFinalSliceVersion(VersionsYaml).Should().Be("7dd981dcd7d3");

	[Test]
	public void ParseFinalSliceVersion_ReturnsNullWhenMissing() =>
		ServerlessPromotion.ParseFinalSliceVersion("services: {}").Should().BeNull();

	[Test]
	[Arguments("gitops: production-noncanary-ds-4,production-noncanary-ds-5 Artifact promotion for kibana to 7dd981dcd7d3 (#199421)", true)]
	[Arguments("gitops: production-noncanary-ds-1 Artifact promotion for kibana to 883abb05af0b (#202658)", false)]
	[Arguments("gitops: dev Artifact promotion for kibana to 914daa00d40e (#202639)", false)]
	[Arguments("gitops: production-noncanary-ds-50 Artifact promotion", false)]
	public void IsFinalSlicePromotion(string message, bool expected) =>
		ServerlessPromotion.IsFinalSlicePromotion(message).Should().Be(expected);

	[Test]
	[Arguments("7dd981dcd7d3", "7dd981dcd7d3", true)]
	[Arguments("7dd981dcd7d3", "7DD981DCD7D3AAAAAAAAAAAAAAAAAAAAAAAAAAAA", true)]
	[Arguments("7dd981dcd7d3", "03c0df903631", false)]
	public void SameRef(string a, string b, bool expected) => ServerlessPromotion.SameRef(a, b).Should().Be(expected);

	[Test]
	public void FindPreviousVersion_SkipsCurrentAndReturnsFirstDifferent() =>
		ServerlessPromotion
			.FindPreviousVersion(["7dd981dcd7d3", null, "7dd981dcd7d3", "03c0df903631", "2fd663f3c363"], "7DD981DCD7D3ffff")
			.Should()
			.Be("03c0df903631");

	[Test]
	public void FindPreviousVersion_ReturnsNullWhenAllMatch() =>
		ServerlessPromotion.FindPreviousVersion(["7dd981dcd7d3"], "7dd981dcd7d3").Should().BeNull();
}
