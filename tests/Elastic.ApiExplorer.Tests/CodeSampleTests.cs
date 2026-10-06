// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class CodeSampleTests
{
	private static OpenApiOperation CreateOperationWithCodeSamples(JsonArray samplesArray)
	{
		var operation = new OpenApiOperation();
		operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
		operation.Extensions["x-codeSamples"] = new JsonNodeExtension(samplesArray);
		return operation;
	}

	[Test]
	public void CodeSamples_ReturnsEmptyList_WhenExtensionIsMissing()
	{
		var operation = new OpenApiOperation();

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result.Should().BeEmpty();
	}

	[Test]
	public void CodeSamples_ParsesValidSamples()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "curl", ["source"] = "curl -X GET \"$ELASTICSEARCH_URL/_search\"" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result.Should().HaveCount(2);
		result[0].Language.Should().Be("Console");
		result[0].Source.Should().Be("GET /_search");
		result[1].Language.Should().Be("curl");
		result[1].Source.Should().Be("curl -X GET \"$ELASTICSEARCH_URL/_search\"");
		result[1].HighlightClass.Should().Be("language-curl");
	}

	[Test]
	public void CodeSamples_OrdersConsoleFirstThenByLanguagePopularity()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "Ruby", ["source"] = "response = client.search" },
			new JsonObject { ["lang"] = "Python", ["source"] = "resp = client.search()" },
			new JsonObject { ["lang"] = "curl", ["source"] = "curl -X GET ..." },
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "Java", ["source"] = "client.search()" },
			new JsonObject { ["lang"] = "JavaScript", ["source"] = "await client.search()" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result.Select(s => s.Language).Should().Equal("Console", "JavaScript", "Python", "curl", "Java", "Ruby");
	}

	[Test]
	public void CodeSamples_NormalizesLanguageCasing()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "cURL", ["source"] = "curl -X GET ..." },
			new JsonObject { ["lang"] = "console", ["source"] = "GET /_search" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result.Select(s => s.Language).Should().Equal("Console", "curl");
		result[1].HighlightClass.Should().Be("language-curl");
		result[1].ClientLabel.Should().Be("Shell");
	}

	[Test]
	public void CodeSamples_SplitAnExampleSuffixOffTheLanguage()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "cURL_tag_names", ["source"] = "curl -X GET ...?tag_names=a" },
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /api/dashboards" },
			new JsonObject { ["lang"] = "Console_tag_names", ["source"] = "GET /api/dashboards?tag_names=a" },
			new JsonObject { ["lang"] = "my_language", ["source"] = "..." }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result
			.Select(s => (s.Language, s.Scenario))
			.Should()
			.Equal(("Console", null), ("Console", "tag_names"), ("curl", "tag_names"), ("my_language", null));
	}

	[Test]
	public void CodeSamples_UnrankedLanguagesKeepSpecOrderAfterRankedOnes()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "Haskell", ["source"] = "search client" },
			new JsonObject { ["lang"] = "Python", ["source"] = "resp = client.search()" },
			new JsonObject { ["lang"] = "Elixir", ["source"] = "Client.search()" },
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result.Select(s => s.Language).Should().Equal("Console", "Python", "Haskell", "Elixir");
	}

	[Test]
	public void CodeSamples_IncludesAllLanguagesWithConsoleFirst()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "Python", ["source"] = "resp = client.search()" },
			new JsonObject { ["lang"] = "curl", ["source"] = "curl -X GET ..." },
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "Ruby", ["source"] = "response = client.search" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result.Should().HaveCount(4);
		result[0].Language.Should().Be("Console");
		result.Skip(1).Select(s => s.Language).Should().BeEquivalentTo(["Python", "curl", "Ruby"]);
	}

	[Test]
	public void CodeSamples_SkipsEntriesWithMissingSource()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "Python" },
			new JsonObject { ["lang"] = "curl", ["source"] = "" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result.Should().HaveCount(1);
		result[0].Language.Should().Be("Console");
	}

	[Test]
	public void CodeSamples_SkipsEntriesWithMissingLang()
	{
		var samples = new JsonArray(
			new JsonObject { ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result.Should().HaveCount(1);
		result[0].Language.Should().Be("Console");
	}

	[Test]
	public void CodeSamples_HandlesEmptyArray()
	{
		var samples = new JsonArray();
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result.Should().BeEmpty();
	}

	[Test]
	[Arguments("Console", "language-console")]
	[Arguments("curl", "language-curl")]
	[Arguments("Python", "language-python")]
	[Arguments("JavaScript", "language-javascript")]
	[Arguments("Ruby", "language-ruby")]
	[Arguments("PHP", "language-php")]
	[Arguments("Java", "language-java")]
	[Arguments("Go", "language-go")]
	[Arguments("TypeScript", "language-typescript")]
	public void GetHighlightClass_MapsLanguagesCorrectly(string language, string expected) =>
		CodeSample.GetHighlightClass(language).Should().Be(expected);

	[Test]
	[Arguments(/*lang=json,strict*/ """{"ok":true}""", "language-json")]
	[Arguments(/*lang=json,strict*/ """[{"id":1}]""", "language-json")]
	[Arguments(/*lang=json*/ "  \n{ \"a\": 1 }", "language-json")]
	[Arguments("event: message\ndata: [DONE]", "language-plaintext")]
	[Arguments("", "language-plaintext")]
	[Arguments(null, "language-plaintext")]
	public void HighlightClassForExampleBody_DetectsJsonVsOther(string? source, string expected) =>
		CodeSample.HighlightClassForExampleBody(source).Should().Be(expected);

	[Test]
	public void CodeSamples_SetsCorrectHighlightClass()
	{
		var samples = new JsonArray(new JsonObject { ["lang"] = "curl", ["source"] = "curl -X GET \"$ELASTICSEARCH_URL/_search\"" });
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result[0].HighlightClass.Should().Be("language-curl");
	}

	[Test]
	public void CodeSamples_FormatsSingleLineCurl()
	{
		const string source =
			"curl -X PUT -H \"Authorization: ApiKey $ELASTIC_API_KEY\" -H \"Content-Type: application/json\" -d '{\"service\":\"cohere\"}' \"$ELASTICSEARCH_URL/_inference/rerank/my-rerank-model\"";
		var samples = new JsonArray(new JsonObject { ["lang"] = "curl", ["source"] = source });
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OpenApiExtensionReader.ParseCodeSamples(operation);

		result[0].Source.Should().StartWith("curl -X PUT \"$ELASTICSEARCH_URL/_inference/rerank/my-rerank-model\" \\");
		result[0].Source.Should().Contain("\n  -H \"Authorization: ApiKey $ELASTIC_API_KEY\" \\");
		result[0].Source.Should().Contain("\n  -H \"Content-Type: application/json\" \\");
		result[0].Source.Should().Contain("\n  -d ");
		result[0].Source.Should().Contain("\"service\"");
	}

	[Test]
	public void CurlSourceFormatter_LeavesMultilineUnchanged()
	{
		var source = "curl -X GET \\\n  \"$ELASTICSEARCH_URL/_search\"";
		CurlSourceFormatter.Format(source).Should().Be(source);
	}

	[Test]
	[Arguments("language-json", "highlight-json")]
	[Arguments("language-bash", "highlight-bash")]
	[Arguments("language-console", "highlight-console")]
	[Arguments("language-python", "highlight-python")]
	public void GetHighlightGroupClass_MapsLanguageClassToHighlightClass(string input, string expected) =>
		CodeSample.GetHighlightGroupClass(input).Should().Be(expected);

	[Test]
	public void GetHighlightGroupClass_HandlesNonLanguageClass() =>
		CodeSample.GetHighlightGroupClass("some-other-class").Should().Be("highlight-plaintext");

	[Test]
	public void GetHighlightGroupClass_HandlesEmptyInput() => CodeSample.GetHighlightGroupClass("").Should().Be("highlight-plaintext");

	[Test]
	public void GetHighlightGroupClass_HandlesLanguagePrefixOnly() =>
		CodeSample.GetHighlightGroupClass("language-").Should().Be("highlight-plaintext");

	[Test]
	[Arguments("Java", "elasticsearch-java")]
	[Arguments("Console", "Kibana Dev Tools")]
	[Arguments("C#", "Elastic.Clients.Elasticsearch")]
	[Arguments("Go", "")]
	public void ClientLabel_NamesTheLibraryThatRunsTheSample(string language, string expected) =>
		new CodeSample(language, "", CodeSample.GetHighlightClass(language)).ClientLabel.Should().Be(expected);

	[Test]
	public void GetHighlightClass_CSharp_UsesTheCsharpGrammar() => CodeSample.GetHighlightClass("C#").Should().Be("language-csharp");
}
