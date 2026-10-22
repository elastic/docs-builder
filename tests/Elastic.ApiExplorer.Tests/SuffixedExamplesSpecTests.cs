// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
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
			/*lang=json,strict*/
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/_x":{"post":{"operationId":"x","requestBody":{"content":{
			  "application/json":{"schema":{"type":"object"},"examples":{
			    "First":{"summary":"First","value":{"a":1},"x-codeSamples":[{"lang":"Console","source":"POST /_x\n{\"a\":1}"}]}}},
			  "application/x-ndjson":{"schema":{"type":"string"},"examples":{
			    "First":{"summary":"First","value":"{}","x-codeSamples":[{"lang":"Console","source":"duplicate()"}]},
			    "Second":{"summary":"Second","value":"{}","x-codeSamples":[{"lang":"Console","source":"POST /_x\n{}"}]}}}}},
			  "responses":{"200":{"description":"ok"}}}}}}
			"""
		);

		scenarios.Select(s => s.Title).Should().Equal("First", "Second");
		scenarios[0].CodeSamples.Select(c => c.Source).Should().NotContain("duplicate()");
		scenarios[1].CodeSamples.Should().ContainSingle();
	}

	[Test]
	public async Task ExampleLevelSamples_OfExamplesWithTheSameTitle_KeepTheFirstOnly()
	{
		var scenarios = await BuildScenariosFromSpec(
			/*lang=json,strict*/
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/_x":{"post":{"operationId":"x","requestBody":{"content":{
			  "application/json":{"schema":{"type":"object"},"examples":{
			    "searchOne":{"summary":"Same title","value":{"a":1},"x-codeSamples":[{"lang":"Python","source":"one()"}]},
			    "searchTwo":{"summary":"Same title","value":{"a":2},"x-codeSamples":[{"lang":"Python","source":"two()"}]},
			    "Search three":{"value":{"a":3},"x-codeSamples":[{"lang":"Python","source":"three()"}]},
			    "searchThree":{"summary":"Search three","value":{"a":4},"x-codeSamples":[{"lang":"Python","source":"four()"}]}}}}},
			  "responses":{"200":{"description":"ok"}}}}}}
			"""
		);

		scenarios.Select(s => s.Title).Should().Equal("Same title", "Search three");
		scenarios[0].CodeSamples.Select(c => c.Source).Should().Equal("one()");
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Equal("three()");
	}

	[Test]
	public async Task OperationSamples_OfAnExampleWithoutLocalSamples_AreKept()
	{
		var scenarios = await BuildScenariosFromSpec(
			/*lang=json,strict*/
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/_x":{"post":{"operationId":"x",
			  "x-codeSamples":[{"lang":"Console","source":"POST /_x\n{\"a\":1}"},{"lang":"Python","source":"first()"}],
			  "requestBody":{"content":{"application/json":{"schema":{"type":"object"},"examples":{
			    "First":{"summary":"First","value":{"a":1}},
			    "Second":{"summary":"Second","value":{"b":2},"x-codeSamples":[{"lang":"Python","source":"second()"}]}}}}},
			  "responses":{"200":{"description":"ok"}}}}}}
			"""
		);

		scenarios.Select(s => s.Title).Should().Equal("First", "Second");
		scenarios[0].CodeSamples.Select(c => c.Source).Should().Contain("first()");
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Equal("second()");
	}

	[Test]
	public async Task LocalSamples_ReplaceASuffixedOperationSampleOfTheSameLanguage()
	{
		var scenarios = await BuildScenariosFromSpec(
			/*lang=json,strict*/
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/_x":{"post":{"operationId":"x",
			  "x-codeSamples":[{"lang":"Python_Search slicing","source":"suffixed()"},{"lang":"Java_Search slicing","source":"java()"}],
			  "requestBody":{"content":{"application/json":{"schema":{"type":"object"},"examples":{
			    "Search slicing":{"summary":"Search slicing","value":{"a":1},"x-codeSamples":[{"lang":"Python","source":"local()"}]}}}}},
			  "responses":{"200":{"description":"ok"}}}}}}
			"""
		);

		scenarios.Should().ContainSingle();
		scenarios[0].CodeSamples.Select(c => (c.Language, c.Source)).Should().BeEquivalentTo([("Python", "local()"), ("Java", "java()")]);
	}

	[Test]
	public async Task ExampleLevelSamples_OfTheSameKeyOnAnotherContentType_AreKeptWhenTheTitlesDiffer()
	{
		var scenarios = await BuildScenariosFromSpec(
			/*lang=json,strict*/
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/_x":{"post":{"operationId":"x","requestBody":{"content":{
			  "application/json":{"schema":{"type":"object"},"examples":{
			    "example":{"summary":"One","value":{"a":1},"x-codeSamples":[{"lang":"Python","source":"one()"}]}}},
			  "application/x-ndjson":{"schema":{"type":"string"},"examples":{
			    "example":{"summary":"Two","value":"{}","x-codeSamples":[{"lang":"Python","source":"two()"}]}}}}},
			  "responses":{"200":{"description":"ok"}}}}}}
			"""
		);

		scenarios.Select(s => s.Title).Should().Equal("One", "Two");
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Equal("two()");
	}

	[Test]
	public async Task RequestExamples_OfAnotherContentType_PairWithTheirOwnResponses()
	{
		var scenarios = await BuildScenariosFromSpec(
			/*lang=json,strict*/
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/_x":{"post":{"operationId":"x","requestBody":{"content":{
			  "application/json":{"schema":{"type":"object"},"examples":{"First":{"summary":"First","value":{"a":1}}}},
			  "application/x-ndjson":{"schema":{"type":"string"},"examples":{"Second":{"summary":"Second","value":"{}","x-codeSamples":[{"lang":"Python","source":"two()"}]}}}}},
			  "responses":{"200":{"description":"ok"}}}}}}
			""",
			[new ExampleDisplay("First", null, "{\"r\":1}", null, "200"), new ExampleDisplay("Second", null, "{\"r\":2}", null, "200")]
		);

		scenarios.Select(s => s.Title).Should().Equal("First", "Second");
		scenarios[1].Responses.Should().ContainSingle().Which.JsonValue.Should().Contain("\"r\":2");
	}

	[Test]
	public async Task OperationSamples_OfAnExampleThatRepeatsOnlyTheConsoleSample_FollowThatExample()
	{
		var scenarios = await BuildScenariosFromSpec(
			/*lang=json,strict*/
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/_x":{"post":{"operationId":"x",
			  "x-codeSamples":[{"lang":"Console","source":"POST /_x\n{\"b\":2}"},{"lang":"Java","source":"java()"}],
			  "requestBody":{"content":{"application/json":{"schema":{"type":"object"},"examples":{
			    "First":{"summary":"First","value":{"a":1}},
			    "Second":{"summary":"Second","value":{"b":2},"x-codeSamples":[{"lang":"Console","source":"POST /_x\n{\"b\":2}"}]}}}}},
			  "responses":{"200":{"description":"ok"}}}}}}
			"""
		);

		scenarios.Select(s => s.Title).Should().Equal("First", "Second");
		scenarios[0].CodeSamples.Should().BeEmpty();
		scenarios[1].CodeSamples.Select(c => c.Language).Should().Equal("Console", "Java");
	}

	[Test]
	public async Task SuffixedSamples_OfAnExampleTitledExample_SkipTheScenarioOfUnmatchedSamples()
	{
		var scenarios = await BuildScenariosFromSpec(
			/*lang=json,strict*/
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/_x":{"post":{"operationId":"x",
			  "x-codeSamples":[{"lang":"Console","source":"POST /_x\n{\"other\":true}"},{"lang":"Python_Example","source":"named()"}],
			  "requestBody":{"content":{"application/json":{"schema":{"type":"object"},"examples":{
			    "Example":{"summary":"Example","value":{"a":1}}}}}},
			  "responses":{"200":{"description":"ok"}}}}}}
			"""
		);

		scenarios.Should().HaveCount(2);
		scenarios[0].FromUnmatchedSamples.Should().BeTrue();
		scenarios[0].CodeSamples.Select(c => c.Source).Should().NotContain("named()");
		scenarios[1].RequestJson.Should().Contain("\"a\"");
		scenarios[1].CodeSamples.Select(c => c.Source).Should().Equal("named()");
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
		var samples = OperationPageModel.WithExampleCodeSamples(
			OpenApiExtensionReader.ParseCodeSamples(operation),
			contentTypes.Select(static c => c.Examples)
		);
		return OperationPageModel.BuildExampleScenarios(examples, responses ?? [], samples);
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
		var samples = OperationPageModel.WithExampleCodeSamples(OpenApiExtensionReader.ParseCodeSamples(operation), [requestExamples]);
		return OperationPageModel.BuildExampleScenarios(examples, [], samples);
	}
}
