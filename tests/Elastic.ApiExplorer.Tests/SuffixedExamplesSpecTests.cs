// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// A spec that ties its code samples to a request example, by suffixing the language with the example title (<c>Python_A point in time search</c>) or by an example-level <c>x-codeSamples</c>,
/// must show each example once, with its body and its samples, instead of one example for the body and another for the samples.
/// </summary>
public class SuffixedExamplesSpecTests
{
	private static Cancel Ct => TestContext.Current!.Execution.CancellationToken;

	[Test]
	public async Task SearchOperation_SuffixedSamplesJoinTheirExamples()
	{
		var scenarios = await BuildScenarios();

		scenarios.Select(s => s.Title).Should().Equal("A simple term search", "A point in time search", "Search slicing");
		scenarios.Should().OnlyContain(s => s.RequestJson != null && s.CodeSamples.Count == 2);
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Contain("client.search(pit={\"id\":\"x\"})");
	}

	[Test]
	public async Task SearchOperation_RendersOneChipPerExample()
	{
		var scenarios = await BuildScenarios();

		var html = await _OperationExamplesPanel.Create(new OperationExamplesPanelModel { Scenarios = scenarios }).RenderAsync(
			cancellationToken: Ct
		);

		foreach (var title in new[] { "A simple term search", "A point in time search", "Search slicing" })
			html.Split($"<span class=\"api-example-chip-title\">{title}</span>").Should().HaveCount(2, $"'{title}' has one chip");
	}

	[Test]
	public async Task SearchOperation_ExampleLevelSamplesBelongToTheirExampleOnly()
	{
		var scenarios = await BuildScenarios("example-level-codesamples-openapi.json");

		scenarios.Select(s => s.Title).Should().Equal("A simple term search", "A point in time search", "Search slicing");
		scenarios.Should().OnlyContain(s => s.RequestJson != null && s.CodeSamples.Count == 3);
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Contain(c => c.Contains("keep_alive"));
		scenarios[0].CodeSamples.Select(c => c.Source).Should().NotContain(c => c.Contains("keep_alive"));
	}

	[Test]
	public async Task SearchOperation_ExampleLevelSamplesRenderOneChipPerExample()
	{
		var scenarios = await BuildScenarios("example-level-codesamples-openapi.json");

		var html = await _OperationExamplesPanel.Create(new OperationExamplesPanelModel { Scenarios = scenarios }).RenderAsync(
			cancellationToken: Ct
		);

		foreach (var title in new[] { "A simple term search", "A point in time search", "Search slicing" })
			html.Split($"<span class=\"api-example-chip-title\">{title}</span>").Should().HaveCount(2, $"'{title}' has one chip");
	}

	[Test]
	public async Task ExampleLevelSamples_OnAnotherContentType_StillAttach()
	{
		var scenarios = await BuildScenariosFromSpec(
			Spec(
				Json(Examples(("First", Example("First", """{"a":1}""", ("Console", "POST /_x\n{\"a\":1}"))))),
				Ndjson(
					Examples(
						("First", Example("First", "\"{}\"", ("Console", "duplicate()"))),
						("Second", Example("Second", "\"{}\"", ("Console", "POST /_x\n{}")))
					)
				)
			)
		);

		scenarios.Select(s => s.Title).Should().Equal("First", "Second");
		scenarios[0].CodeSamples.Select(c => c.Source).Should().NotContain("duplicate()");
		scenarios[1].CodeSamples.Should().ContainSingle();
	}

	[Test]
	public async Task ExampleLevelSamples_OfExamplesWithTheSameTitle_KeepTheFirstOnly()
	{
		var scenarios = await BuildScenariosFromSpec(
			Spec(
				Json(
					Examples(
						("searchOne", Example("Same title", """{"a":1}""", ("Python", "one()"))),
						("searchTwo", Example("Same title", """{"a":2}""", ("Python", "two()"))),
						("Search three", Example(null, """{"a":3}""", ("Python", "three()"))),
						("searchThree", Example("Search three", """{"a":4}""", ("Python", "four()")))
					)
				)
			)
		);

		scenarios.Select(s => s.Title).Should().Equal("Same title", "Search three");
		scenarios[0].CodeSamples.Select(c => c.Source).Should().Equal("one()");
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Equal("three()");
	}

	[Test]
	public async Task OperationSamples_OfAnExampleWithoutLocalSamples_AreKept()
	{
		var scenarios = await BuildScenariosFromSpec(Spec(
			Json(
				Examples(("First", Example("First", """{"a":1}""")), ("Second", Example("Second", """{"b":2}""", ("Python", "second()"))))
			),
			operationSamples: [("Console", "POST /_x\n{\"a\":1}"), ("Python", "first()")]
		));

		scenarios.Select(s => s.Title).Should().Equal("First", "Second");
		scenarios[0].CodeSamples.Select(c => c.Source).Should().Contain("first()");
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Equal("second()");
	}

	[Test]
	public async Task OperationSamples_OfAnExampleThatRepeatsOnlyTheConsoleSample_FollowThatExample()
	{
		var scenarios = await BuildScenariosFromSpec(Spec(
			Json(
				Examples(
					("First", Example("First", """{"a":1}""")),
					("Second", Example("Second", """{"b":2}""", ("Console", "POST /_x\n{\"b\":2}")))
				)
			),
			operationSamples: [("Console", "POST /_x\n{\"b\":2}"), ("Java", "java()")]
		));

		scenarios.Select(s => s.Title).Should().Equal("First", "Second");
		scenarios[0].CodeSamples.Should().BeEmpty();
		scenarios[1].CodeSamples.Select(c => c.Language).Should().Equal("Console", "Java");
	}

	[Test]
	public async Task OperationSamples_DifferingOnlyInWhitespace_AreStillTheExamplesOwn()
	{
		var scenarios = await BuildScenariosFromSpec(Spec(
			Json(
				Examples(
					("First", Example("First", """{"a":1}""", ("Console", "POST /_x\n{\"a\":1}"), ("Python", "first()"))),
					("Second", Example("Second", """{"b":2}""", ("Python", "second()")))
				)
			),
			operationSamples: [("Console", "POST /_x\r\n{\"a\":1}\r\n"), ("Python", "first()\n")]
		));

		scenarios.Select(s => s.Title).Should().Equal("First", "Second");
		scenarios[0].CodeSamples.Select(c => c.Language).Should().Equal("Console", "Python");
	}

	[Test]
	public async Task LocalSamples_ReplaceASuffixedOperationSampleOfTheSameLanguage()
	{
		var scenarios = await BuildScenariosFromSpec(Spec(
			Json(Examples(("Search slicing", Example("Search slicing", """{"a":1}""", ("Python", "local()"))))),
			operationSamples: [("Python_Search slicing", "suffixed()"), ("Java_Search slicing", "java()")]
		));

		scenarios.Should().ContainSingle();
		scenarios[0].CodeSamples.Select(c => (c.Language, c.Source)).Should().BeEquivalentTo([("Python", "local()"), ("Java", "java()")]);
	}

	[Test]
	public async Task SuffixedSamples_OfAnExampleTitledExample_SkipTheScenarioOfUnmatchedSamples()
	{
		var scenarios = await BuildScenariosFromSpec(Spec(
			Json(Examples(("Example", Example("Example", """{"a":1}""")))),
			operationSamples: [("Console", "POST /_x\n{\"other\":true}"), ("Python_Example", "named()")]
		));

		scenarios.Should().HaveCount(2);
		scenarios[0].FromUnmatchedSamples.Should().BeTrue();
		scenarios[0].CodeSamples.Select(c => c.Source).Should().NotContain("named()");
		scenarios[1].RequestJson.Should().Contain("\"a\"");
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Equal("named()");
	}

	[Test]
	public async Task ExampleLevelSamples_OfTheSameKeyOnAnotherContentType_AreKeptWhenTheTitlesDiffer()
	{
		var scenarios = await BuildScenariosFromSpec(
			Spec(
				Json(Examples(("example", Example("One", """{"a":1}""", ("Python", "one()"))))),
				Ndjson(Examples(("example", Example("Two", "\"{}\"", ("Python", "two()")))))
			)
		);

		scenarios.Select(s => s.Title).Should().Equal("One", "Two");
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Equal("two()");
	}

	[Test]
	public async Task RequestExamples_OfAnotherContentType_PairWithTheirOwnResponses()
	{
		var scenarios = await BuildScenariosFromSpec(
			Spec(
				Json(Examples(("First", Example("First", """{"a":1}""")))),
				Ndjson(Examples(("Second", Example("Second", "\"{}\"", ("Python", "two()")))))
			),
			[new ExampleDisplay("First", null, "{\"r\":1}", null, "200"), new ExampleDisplay("Second", null, "{\"r\":2}", null, "200")]
		);

		scenarios.Select(s => s.Title).Should().Equal("First", "Second");
		scenarios[1].Responses.Should().ContainSingle().Which.JsonValue.Should().Contain("\"r\":2");
	}

	[Test]
	public async Task ExampleLevelSamples_WithoutTheBody_KeepTheRequestBlock()
	{
		var external = Example("External", null, ("Python", "client.x()"));
		external["externalValue"] = "https://example.test/body.json";

		var scenarios = await BuildScenariosFromSpec(
			Spec(
				Json(
					Examples(
						("WithConsole", Example("With console", """{"a":1}""", ("Console", "POST /_x\n{\"a\":1}"))),
						("WithBody", Example("With body", """{"b":2}""", ("Python", "client.x(body={\"b\": 2})"))),
						("WithoutBody", Example("Without body", """{"c":3}""", ("Python", "client.x()"))),
						("External", external)
					)
				)
			)
		);

		scenarios.Select(s => s.CodeSamplesIncludeRequest).Should().Equal(true, true, false, false);
	}

	[Test]
	public async Task ExampleLevelSamples_OfAnExampleGivenByReference_Attach()
	{
		var components = new JsonObject
		{
			["examples"] = new JsonObject { ["Shared"] = Example("Shared example", """{"a":1}""", ("Python", "shared()")) }
		};

		var scenarios = await BuildScenariosFromSpec(
			Spec(Json(new JsonObject { ["Shared"] = new JsonObject { ["$ref"] = "#/components/examples/Shared" } }), components: components)
		);

		scenarios.Select(s => s.Title).Should().Equal("Shared example");
		scenarios[0].CodeSamples.Select(c => c.Source).Should().Equal("shared()");
	}

	[Test]
	public async Task ExampleLevelSamples_ForAPrefixedConsoleTarget_KeepThePrefix()
	{
		var scenarios = await BuildScenariosFromSpec(
			Spec(Json(Examples(("Kibana", Example("Kibana", """{"a":1}""", ("Console", "POST kbn:/api/x\n{\"a\":1}"))))))
		);

		scenarios[0].CodeSamples.Should().ContainSingle().Which.Source.Should().StartWith("POST kbn:/api/x");
	}

	private static async Task<IReadOnlyList<ExampleScenario>> BuildScenariosFromSpec(
		string json,
		IReadOnlyList<ExampleDisplay>? responses = null
	)
	{
		var document = await TestSpecs.LoadSpecAsync(json);
		var operation = document.Paths["/_x"].Operations![HttpMethod.Post];
		var contentTypes = OperationPageModel.RequestContentTypes(
			operation.RequestBody,
			OperationPageModel.PrimaryRequestContent(operation.RequestBody)
		);
		var examples = OperationPageModel.MapRequestExamples(contentTypes, static m => new HtmlString(m ?? ""));
		var samples = ExampleCodeSamples.Combine(
			OpenApiExtensionReader.ParseCodeSamples(operation),
			contentTypes.Select(static c => c.Examples)
		);
		return OperationPageModel.BuildExampleScenarios(examples, responses ?? [], samples);
	}

	private static (string MediaType, JsonObject Examples) Json(JsonObject examples) => ("application/json", examples);

	private static (string MediaType, JsonObject Examples) Ndjson(JsonObject examples) => ("application/x-ndjson", examples);

	private static JsonObject Examples(params (string Key, JsonObject Example)[] examples)
	{
		var result = new JsonObject();
		foreach (var (key, example) in examples)
			result[key] = example;
		return result;
	}

	/// <summary>An OpenAPI example with an optional summary and JSON value, and its own <c>x-codeSamples</c> when given.</summary>
	private static JsonObject Example(string? summary, string? valueJson, params (string Lang, string Source)[] samples)
	{
		var example = new JsonObject();
		if (summary is not null)
			example["summary"] = summary;
		if (valueJson is not null)
			example["value"] = JsonNode.Parse(valueJson);
		if (samples.Length > 0)
			example["x-codeSamples"] = CodeSamples(samples);
		return example;
	}

	private static JsonArray CodeSamples(IEnumerable<(string Lang, string Source)> samples) =>
		[.. samples.Select(static s => (JsonNode)new JsonObject { ["lang"] = s.Lang, ["source"] = s.Source })];

	/// <summary>A spec with one <c>POST</c> operation whose request body lists the examples under each content type.</summary>
	private static string Spec(
		(string MediaType, JsonObject Examples) primary,
		(string MediaType, JsonObject Examples)? secondary = null,
		(string Lang, string Source)[]? operationSamples = null,
		JsonObject? components = null
	)
	{
		var content = new JsonObject();
		foreach (var (mediaType, examples) in new[] { primary }.Concat(secondary is { } other ? [other] : []))
			content[mediaType] = new JsonObject { ["schema"] = new JsonObject { ["type"] = "object" }, ["examples"] = examples };

		var operation = new JsonObject
		{
			["operationId"] = "x",
			["requestBody"] = new JsonObject { ["content"] = content },
			["responses"] = new JsonObject { ["200"] = new JsonObject { ["description"] = "ok" } }
		};
		if (operationSamples is not null)
			operation["x-codeSamples"] = CodeSamples(operationSamples);

		var spec = new JsonObject
		{
			["openapi"] = "3.0.3",
			["info"] = new JsonObject { ["title"] = "t", ["version"] = "1" },
			["paths"] = new JsonObject { ["/_x"] = new JsonObject { ["post"] = operation } }
		};
		if (components is not null)
			spec["components"] = components;
		return spec.ToJsonString();
	}

	private static Task<IReadOnlyList<ExampleScenario>> BuildScenarios() => BuildScenarios("suffixed-examples-openapi.json");

	private static async Task<IReadOnlyList<ExampleScenario>> BuildScenarios(string fixture)
	{
		var file = new FileSystem().FileInfo.New(Path.Combine(AppContext.BaseDirectory, "TestData", fixture));
		var document = await OpenApiReader.Instance.ReadAsync(file)
			?? throw new InvalidOperationException($"Could not read {file.FullName}");
		var operation = document.Paths["/{index}/_search"].Operations![HttpMethod.Post];
		var requestExamples = operation.RequestBody?.Content?.FirstOrDefault().Value?.Examples;
		var examples = OperationPageModel.MapExamples(requestExamples, static m => new HtmlString(m ?? ""));
		var samples = ExampleCodeSamples.Combine(OpenApiExtensionReader.ParseCodeSamples(operation), [requestExamples]);
		return OperationPageModel.BuildExampleScenarios(examples, [], samples);
	}
}
