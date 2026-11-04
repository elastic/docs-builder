// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;

namespace Elastic.ApiExplorer.Tests;

public class SampleMethodsTests
{
	/// <summary>The search page: POST also GET on /{index}/_search and /_search.</summary>
	private static OperationEndpoint Search() =>
		OperationEndpoint.FromVariants(
			[
				new EndpointVariant("post", "/{index}/_search"),
				new EndpointVariant("get", "/{index}/_search"),
				new EndpointVariant("post", "/_search"),
				new EndpointVariant("get", "/_search")
			],
			"/{index}/_search"
		);

	[Test]
	public void AlignConsole_GetOnIndexSearch_BecomesPost() =>
		SampleMethods
			.AlignConsole(
				"GET /my-index-000001/_search?from=40&size=20\n{\n  \"query\": { \"term\": { \"user.id\": \"kimchy\" } }\n}",
				Search()
			)
			.Should()
			.Be("POST /my-index-000001/_search?from=40&size=20\n{\n  \"query\": { \"term\": { \"user.id\": \"kimchy\" } }\n}");

	[Test]
	public void AlignConsole_BareSlashlessPath_IsMatched() =>
		SampleMethods.AlignConsole("GET my-index-000001/_search", Search()).Should().Be("POST my-index-000001/_search");

	[Test]
	public void AlignConsole_MethodNotOnTheRow_IsLeftAlone() =>
		SampleMethods.AlignConsole("PUT /my-index/_search\n{}", Search()).Should().Be("PUT /my-index/_search\n{}");

	[Test]
	public void AlignConsole_PathOnNoRow_IsLeftAlone() =>
		SampleMethods.AlignConsole("GET /my-index/_count", Search()).Should().Be("GET /my-index/_count");

	[Test]
	public void AlignConsole_MultiRequestSample_RewritesOnlyTheLineWhoseRowAllowsIt() =>
		SampleMethods
			.AlignConsole("GET /my-index/_search\n{\"size\": 1}\nGET /my-index/_count\n{}", Search())
			.Should()
			.Be("POST /my-index/_search\n{\"size\": 1}\nGET /my-index/_count\n{}");

	[Test]
	public void AlignConsole_MethodWordInJsonBody_IsLeftAlone()
	{
		const string source = "POST /my-index/_search\n{\n  \"method\": \"GET\",\n  \"message\": \"GET /search HTTP/1.1 200 1070000\"\n}";

		SampleMethods.AlignConsole(source, Search()).Should().Be(source);
	}

	[Test]
	public void AlignConsole_MethodAloneOnALine_IsLeftAlone() =>
		SampleMethods.AlignConsole("GET\n/my-index/_search", Search()).Should().Be("GET\n/my-index/_search");

	[Test]
	public void AlignConsole_KbnPrefixedRequest_KeepsThePrefix()
	{
		var endpoint = OperationEndpoint.FromVariants(
			[
				new EndpointVariant("post", "/api/agent_builder/tools/_execute"),
				new EndpointVariant("get", "/api/agent_builder/tools/_execute")
			],
			"/api/agent_builder/tools/_execute"
		);

		SampleMethods
			.AlignConsole("GET kbn:/api/agent_builder/tools/_execute\n{}", endpoint)
			.Should()
			.Be("POST kbn:/api/agent_builder/tools/_execute\n{}");
	}

	[Test]
	public void AlignConsole_AlreadyDominant_IsUnchanged() =>
		SampleMethods.AlignConsole("POST /_search\n{}", Search()).Should().Be("POST /_search\n{}");

	private const string FormattedCurl =
		"curl -X GET \"$ELASTICSEARCH_URL/my-index-000001/_search?from=40&size=20\" \\\n  -H \"Authorization: ApiKey $ELASTIC_API_KEY\" \\\n  -H \"Content-Type: application/json\" \\\n  -d '{\"query\":{\"term\":{\"user.id\":\"GET\"}}}'";

	[Test]
	public void AlignCurl_DashXWithSpace_RewritesTheFlagOnly() =>
		SampleMethods
			.AlignCurl(FormattedCurl, Search())
			.Should()
			.Be(FormattedCurl.Replace("curl -X GET", "curl -X POST", StringComparison.Ordinal));

	[Test]
	[Arguments("curl -XGET \"$ELASTICSEARCH_URL/_search\"", "curl -XPOST \"$ELASTICSEARCH_URL/_search\"")]
	[Arguments("curl --request GET https://localhost:9200/_search", "curl --request POST https://localhost:9200/_search")]
	[Arguments("curl --request=GET \"$ELASTICSEARCH_URL/_search\"", "curl --request=POST \"$ELASTICSEARCH_URL/_search\"")]
	[Arguments("curl \\\n  --request GET \\\n  https://localhost:9200/_search", "curl \\\n  --request POST \\\n  https://localhost:9200/_search")]
	public void AlignCurl_OtherFlagForms_Rewrite(string source, string expected) =>
		SampleMethods.AlignCurl(source, Search()).Should().Be(expected);

	[Test]
	[Arguments("curl --head \"$ELASTICSEARCH_URL/my-index/_search\"")]
	[Arguments("curl -X GET \"$ELASTICSEARCH_URL/my-index/_count\"")]
	[Arguments("curl -X PUT \"$ELASTICSEARCH_URL/_search\"")]
	[Arguments("curl -X GET")]
	public void AlignCurl_NothingToAlign_IsLeftAlone(string source) => SampleMethods.AlignCurl(source, Search()).Should().Be(source);

	[Test]
	public void AlignDescription_RunLine_RewritesOnlyTheFirstCodeSpan()
	{
		var aligned = SampleMethods.AlignDescription(
			"Run `GET /my-index-000001/_search?from=40&size=20` to run a search. Compare `GET /_search`.",
			Search()
		);

		aligned.Should().Be("Run `POST /my-index-000001/_search?from=40&size=20` to run a search. Compare `GET /_search`.");
		GeneratedCodeSamples.ParseRequestLine(aligned).Should().Be(("POST", "/my-index-000001/_search?from=40&size=20"));
	}

	[Test]
	public void AlignDescription_NotARunLine_IsLeftAlone() =>
		SampleMethods.AlignDescription("Use `GET /_search` for a quick look.", Search()).Should().Be("Use `GET /_search` for a quick look.");

	[Test]
	public void Align_ClientSamples_AreUntouched()
	{
		var python = new CodeSample("Python", "client.perform_request(\"GET\", \"/my-index/_search\")", "language-python");
		var console = new CodeSample("Console", "GET /my-index/_search", "language-console");

		var aligned = SampleMethods.Align([python, console], Search());

		aligned[0].Should().Be(python);
		aligned[1].Source.Should().Be("POST /my-index/_search");
	}
}
