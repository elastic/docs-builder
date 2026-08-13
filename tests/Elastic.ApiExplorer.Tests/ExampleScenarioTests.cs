// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class ExampleScenarioTests
{
	[Fact]
	public void BuildExampleScenarios_MergesRequestAndResponseByTitle()
	{
		var request = new ExampleDisplay("Multimodal", null, /*lang=json,strict*/ """{"input":[{"type":"image"}]}""", null);
		var response = new ExampleDisplay("Multimodal", null, /*lang=json,strict*/ """{"embeddings":[]}""", null);

		var scenarios = OperationPageModel.BuildExampleScenarios([request], [response], []);

		scenarios.Should().ContainSingle();
		scenarios[0].Title.Should().Be("Multimodal");
		scenarios[0].RequestJson.Should().Contain("image");
		scenarios[0].Responses.Should().ContainSingle();
		scenarios[0].Responses[0].JsonValue.Should().Contain("embeddings");
	}

	[Fact]
	public void BuildExampleScenarios_GroupsResponsesByStatusCode()
	{
		var ok = new ExampleDisplay("Create", null, /*lang=json,strict*/ """{"ok":true}""", null, "200");
		var bad = new ExampleDisplay("Create", null, /*lang=json,strict*/ """{"error":"bad"}""", null, "400");

		var scenarios = OperationPageModel.BuildExampleScenarios([], [ok, bad], []);

		scenarios.Should().ContainSingle();
		scenarios[0].Responses.Select(r => r.StatusCode).Should().Equal("200", "400");
		scenarios[0].Responses[0].JsonValue.Should().Contain("ok");
		scenarios[0].Responses[1].JsonValue.Should().Contain("error");
	}

	[Fact]
	public void BuildExampleScenarios_SharesUnmatchedErrorResponsesAcrossRequestScenarios()
	{
		var ipRequest = new ExampleDisplay("ip", null, /*lang=json,strict*/ """{"type":"ip"}""", null);
		var keywordRequest = new ExampleDisplay("keyword", null, /*lang=json,strict*/ """{"type":"keyword"}""", null);
		var ipOk = new ExampleDisplay("ip", null, /*lang=json,strict*/ """{"id":"1"}""", null, "200");
		var keywordOk = new ExampleDisplay("keyword", null, /*lang=json,strict*/ """{"id":"2"}""", null, "200");
		var badRequest = new ExampleDisplay("badRequest", null, /*lang=json,strict*/ """{"error":"bad"}""", null, "400");
		var unauthorized = new ExampleDisplay("unauthorized", null, /*lang=json,strict*/ """{"error":"auth"}""", null, "401");

		var scenarios = OperationPageModel.BuildExampleScenarios(
			[ipRequest, keywordRequest],
			[ipOk, keywordOk, badRequest, unauthorized],
			[]);

		scenarios.Should().HaveCount(2);
		scenarios.Select(s => s.Title).Should().Equal("ip", "keyword");
		scenarios[0].Responses.Select(r => r.StatusCode).Should().Equal("200", "400", "401");
		scenarios[0].Responses[0].JsonValue.Should().Contain("\"id\":\"1\"");
		scenarios[1].Responses.Select(r => r.StatusCode).Should().Equal("200", "400", "401");
		scenarios[1].Responses[0].JsonValue.Should().Contain("\"id\":\"2\"");
		scenarios[0].Responses[1].JsonValue.Should().Contain("bad");
		scenarios[1].Responses[1].JsonValue.Should().Contain("bad");
	}

	[Fact]
	public void BuildExampleScenarios_SharedResponseDoesNotOverwriteScenarioStatus()
	{
		var request = new ExampleDisplay("ip", null, /*lang=json,strict*/ """{"type":"ip"}""", null);
		var ok = new ExampleDisplay("ip", null, /*lang=json,strict*/ """{"id":"scenario"}""", null, "200");
		var sharedOk = new ExampleDisplay("genericOk", null, /*lang=json,strict*/ """{"id":"shared"}""", null, "200");

		var scenarios = OperationPageModel.BuildExampleScenarios([request], [ok, sharedOk], []);

		scenarios.Should().ContainSingle();
		scenarios[0].Responses.Should().ContainSingle();
		scenarios[0].Responses[0].JsonValue.Should().Contain("scenario");
	}

	[Fact]
	public void BuildExampleScenarios_CollapsesResponseOnlyNamedStatusesIntoOneScenario()
	{
		var bad = new ExampleDisplay("badRequest", null, /*lang=json,strict*/ """{"error":"bad"}""", null, "400");
		var unauthorized = new ExampleDisplay("unauthorized", null, /*lang=json,strict*/ """{"error":"auth"}""", null, "401");

		var scenarios = OperationPageModel.BuildExampleScenarios([], [bad, unauthorized], []);

		scenarios.Should().ContainSingle();
		scenarios[0].Responses.Select(r => r.StatusCode).Should().Equal("400", "401");
	}

	[Fact]
	public void BuildExampleScenarios_AttachesCodeSamplesToMatchingRequestBody()
	{
		var multimodal = new ExampleDisplay(
			"Multimodal embedding task",
			null,
								 /*lang=json,strict*/
								 """{"input":[{"content":{"type":"image"}}]}""",
			null);
		var textOnly = new ExampleDisplay(
			"Text-only embedding task",
			null,
								 /*lang=json,strict*/
								 """{"input":["The first text","The second text"]}""",
			null);
		var console = new CodeSample(
			"Console",
			"""
			POST _inference/embedding/my-endpoint
			{"input":[{"content":{"type":"image"}}]}
			""",
			"language-console");
		var python = new CodeSample("Python", "client.inference()", "language-python");

		var scenarios = OperationPageModel.BuildExampleScenarios(
			[multimodal, textOnly],
			[],
			[console, python]);

		scenarios.Should().HaveCount(2);
		scenarios[0].CodeSamples.Should().HaveCount(2);
		scenarios[1].CodeSamples.Should().BeEmpty();
		scenarios[0].ShowRequest.Should().BeFalse();
		scenarios[1].ShowRequest.Should().BeTrue();
	}

	[Fact]
	public void BuildExampleScenarios_CodeSamplesOnly_CreatesSingleScenario()
	{
		var samples = new[]
		{
			new CodeSample("Console", "GET /_search", "language-console")
		};

		var scenarios = OperationPageModel.BuildExampleScenarios([], [], samples);

		scenarios.Should().ContainSingle();
		scenarios[0].Title.Should().Be("Examples");
		scenarios[0].CodeSamples.Should().Equal(samples);
	}

	[Fact]
	public void EnsureResponseTabs_FillsStatusTabsWhenScenariosHaveNoResponses()
	{
		var samples = new[]
		{
			new CodeSample("Console", "DELETE /api/dashboards/{id}", "language-console")
		};
		var scenarios = OperationPageModel.BuildExampleScenarios([], [], samples);
		var responses = new OpenApiResponses
		{
			["200"] = new OpenApiResponse { Description = "deleted" },
			["404"] = new OpenApiResponse
			{
				Description = "not found",
				Content = new Dictionary<string, IOpenApiMediaType>
				{
					["application/json"] = new OpenApiMediaType
					{
						Schema = new OpenApiSchema { Type = JsonSchemaType.Object }
					}
				}
			}
		};

		var withTabs = OperationPageModel.EnsureResponseTabs(scenarios, responses);

		withTabs.Should().ContainSingle();
		withTabs[0].Responses.Select(r => r.StatusCode).Should().Equal("200", "404");
		withTabs[0].Responses[0].IsNoBody.Should().BeTrue();
		withTabs[0].Responses[0].HasExampleBody.Should().BeFalse();
		withTabs[0].Responses[1].IsNoBody.Should().BeFalse();
		withTabs[0].ShowResponse.Should().BeTrue();
	}

	[Fact]
	public void EnsureResponseTabs_DoesNotReplaceExistingResponseExamples()
	{
		var ok = new ExampleDisplay("Create", null, /*lang=json,strict*/ """{"ok":true}""", null, "200");
		var scenarios = OperationPageModel.BuildExampleScenarios([], [ok], []);
		var responses = new OpenApiResponses
		{
			["200"] = new OpenApiResponse { Description = "ok" },
			["400"] = new OpenApiResponse { Description = "bad" }
		};

		var withTabs = OperationPageModel.EnsureResponseTabs(scenarios, responses);

		withTabs.Should().ContainSingle();
		withTabs[0].Responses.Should().ContainSingle();
		withTabs[0].Responses[0].JsonValue.Should().Contain("ok");
	}

	[Fact]
	public void BuildExampleScenarios_PreservesRequestOrderAsScenarioTabs()
	{
		var a = new ExampleDisplay("Alpha", new HtmlString("a"), "{}", null);
		var b = new ExampleDisplay("Beta", null, "{}", null);

		var scenarios = OperationPageModel.BuildExampleScenarios([a, b], [], []);

		scenarios.Select(s => s.Title).Should().Equal("Alpha", "Beta");
		scenarios[0].DescriptionHtml.Should().NotBeNull();
	}

	[Fact]
	public void SanitizeExampleDescription_DropsRunCommandBoilerplate()
	{
		var onlyBoilerplate =
			"Run `PUT _inference/completion/azure_ai_studio_completion` to create an inference endpoint that performs a completion task.";
		var withNote =
			"Run `PUT _inference/text_embedding/azure_ai_studio_embeddings` to create an inference endpoint that performs a text_embedding task. Note that you do not specify a model here.";

		OperationPageModel.SanitizeExampleDescription(onlyBoilerplate).Should().BeNull();
		OperationPageModel.SanitizeExampleDescription(withNote).Should().Be(
			"Note that you do not specify a model here.");
		OperationPageModel.SanitizeExampleDescription("Useful context without a command.").Should().Be(
			"Useful context without a command.");
	}

	[Fact]
	public void SanitizeExampleDescription_DropsSuccessfulResponseFromPath()
	{
		var onlyBoilerplate =
			"A successful response from `POST _inference/completion/openai_completions`.";
		var withNote =
			"A successful response from `POST _inference/completion/openai_completions`. The response includes token usage.";

		OperationPageModel.SanitizeExampleDescription(onlyBoilerplate).Should().BeNull();
		OperationPageModel.SanitizeExampleDescription(withNote).Should().Be(
			"The response includes token usage.");
		OperationPageModel.SanitizeExampleDescription(
			"A successful response when performing a chat completion task with tools.")
			.Should().Be("A successful response when performing a chat completion task with tools.");
	}

	[Fact]
	public void SanitizeExampleDescription_DropsExampleBodyForRequest()
	{
		var onlyBoilerplate =
			"An example body for a `PUT _inference/rerank/my-rerank-model` request.";
		var withNote =
			"An example body for a `PUT _inference/rerank/my-rerank-model` request. Includes a custom `task_settings` block.";

		OperationPageModel.SanitizeExampleDescription(onlyBoilerplate).Should().BeNull();
		OperationPageModel.SanitizeExampleDescription(withNote).Should().Be(
			"Includes a custom `task_settings` block.");
	}

	[Fact]
	public void SanitizeExampleDescription_DropsAbbreviatedResponseFromPath()
	{
		var onlyBoilerplate =
			"An abbreviated response from `GET /my-index-000001/_search_shards`.";
		var withNote =
			"An abbreviated response from `GET /_segments`. Only the first shard is shown.";

		OperationPageModel.SanitizeExampleDescription(onlyBoilerplate).Should().BeNull();
		OperationPageModel.SanitizeExampleDescription(withNote).Should().Be(
			"Only the first shard is shown.");
		OperationPageModel.SanitizeExampleDescription(
			"An abbreviated response when requesting cluster nodes information.")
			.Should().Be("An abbreviated response when requesting cluster nodes information.");
	}
}
