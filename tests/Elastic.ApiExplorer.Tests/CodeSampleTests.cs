// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
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

	[Fact]
	public void CodeSamples_ReturnsEmptyList_WhenExtensionIsMissing()
	{
		var operation = new OpenApiOperation();

		var result = OperationViewModel.ParseCodeSamples(operation);

		result.Should().BeEmpty();
	}

	[Fact]
	public void CodeSamples_ParsesValidSamples()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "curl", ["source"] = "curl -X GET ..." }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OperationViewModel.ParseCodeSamples(operation);

		result.Should().HaveCount(2);
		result[0].Language.Should().Be("Console");
		result[0].Source.Should().Be("GET /_search");
		result[1].Language.Should().Be("curl");
		result[1].Source.Should().Be("curl -X GET ...");
	}

	[Fact]
	public void CodeSamples_OrdersConsoleFirst()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "Python", ["source"] = "resp = client.search()" },
			new JsonObject { ["lang"] = "curl", ["source"] = "curl -X GET ..." },
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "Java", ["source"] = "client.search()" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OperationViewModel.ParseCodeSamples(operation);

		result[0].Language.Should().Be("Console");
	}

	[Fact]
	public void CodeSamples_PreservesOrderForNonConsole()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "Python", ["source"] = "resp = client.search()" },
			new JsonObject { ["lang"] = "curl", ["source"] = "curl -X GET ..." },
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "Ruby", ["source"] = "response = client.search" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OperationViewModel.ParseCodeSamples(operation);

		result[0].Language.Should().Be("Console");
		result[1].Language.Should().Be("Python");
		result[2].Language.Should().Be("curl");
		result[3].Language.Should().Be("Ruby");
	}

	[Fact]
	public void CodeSamples_SkipsEntriesWithMissingSource()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "Python" },
			new JsonObject { ["lang"] = "curl", ["source"] = "" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OperationViewModel.ParseCodeSamples(operation);

		result.Should().HaveCount(1);
		result[0].Language.Should().Be("Console");
	}

	[Fact]
	public void CodeSamples_SkipsEntriesWithMissingLang()
	{
		var samples = new JsonArray(
			new JsonObject { ["source"] = "GET /_search" },
			new JsonObject { ["lang"] = "Console", ["source"] = "GET /_search" }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OperationViewModel.ParseCodeSamples(operation);

		result.Should().HaveCount(1);
		result[0].Language.Should().Be("Console");
	}

	[Fact]
	public void CodeSamples_HandlesEmptyArray()
	{
		var samples = new JsonArray();
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OperationViewModel.ParseCodeSamples(operation);

		result.Should().BeEmpty();
	}

	[Theory]
	[InlineData("Console", "language-console")]
	[InlineData("curl", "language-bash")]
	[InlineData("Python", "language-python")]
	[InlineData("JavaScript", "language-javascript")]
	[InlineData("Ruby", "language-ruby")]
	[InlineData("PHP", "language-php")]
	[InlineData("Java", "language-java")]
	[InlineData("Go", "language-go")]
	[InlineData("TypeScript", "language-typescript")]
	public void GetHighlightClass_MapsLanguagesCorrectly(string language, string expected)
	{
		CodeSample.GetHighlightClass(language).Should().Be(expected);
	}

	[Fact]
	public void CodeSamples_SetsCorrectHighlightClass()
	{
		var samples = new JsonArray(
			new JsonObject { ["lang"] = "curl", ["source"] = "curl -X GET ..." }
		);
		var operation = CreateOperationWithCodeSamples(samples);

		var result = OperationViewModel.ParseCodeSamples(operation);

		result[0].HighlightClass.Should().Be("language-bash");
	}

	[Theory]
	[InlineData("language-json", "highlight-json")]
	[InlineData("language-bash", "highlight-bash")]
	[InlineData("language-console", "highlight-console")]
	[InlineData("language-python", "highlight-python")]
	public void GetHighlightGroupClass_MapsLanguageClassToHighlightClass(string input, string expected) =>
		CodeSample.GetHighlightGroupClass(input).Should().Be(expected);

	[Fact]
	public void GetHighlightGroupClass_HandlesNonLanguageClass() =>
		CodeSample.GetHighlightGroupClass("some-other-class").Should().Be("highlight-plaintext");

	[Fact]
	public void GetHighlightGroupClass_HandlesEmptyInput() =>
		CodeSample.GetHighlightGroupClass("").Should().Be("highlight-plaintext");

	[Fact]
	public void GetHighlightGroupClass_HandlesLanguagePrefixOnly() =>
		CodeSample.GetHighlightGroupClass("language-").Should().Be("highlight-plaintext");
}
