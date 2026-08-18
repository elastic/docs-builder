// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiUrlBuilderTests
{
	[Theory]
	[InlineData("elasticsearch", "main", "elasticsearch")]
	[InlineData("elasticsearch", "9", "elasticsearch/v9")]
	[InlineData("elasticsearch", "8", "elasticsearch/v8")]
	[InlineData("kibana", "10", "kibana/v10")]
	public void ProductSuffix_MapsVersionMonikersToPathSuffixes(string apiKey, string versionMoniker, string expected)
	{
		ApiUrlBuilder.ProductSuffix(apiKey, versionMoniker).Should().Be(expected);
	}

	[Theory]
	[InlineData("", "elasticsearch", "/api/doc/elasticsearch")]
	[InlineData("", "elasticsearch/v9", "/api/doc/elasticsearch/v9")]
	public void ProductRoot_UsesVersionAwareSuffix(string urlPathPrefix, string apiUrlSuffix, string expected)
	{
		ApiUrlBuilder.ProductRoot(urlPathPrefix, apiUrlSuffix).Should().Be(expected);
	}
}
