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

	private static Task<IReadOnlyList<ExampleScenario>> BuildScenarios() => BuildScenarios("suffixed-examples-openapi.json");

	private static async Task<IReadOnlyList<ExampleScenario>> BuildScenarios(string fixture)
	{
		var file = new FileSystem().FileInfo.New(Path.Combine(AppContext.BaseDirectory, "TestData", fixture));
		var document = await OpenApiReader.Instance.ReadAsync(file)
			?? throw new InvalidOperationException($"Could not read {file.FullName}");
		var operation = document.Paths["/{index}/_search"].Operations![HttpMethod.Post];
		var requestExamples = operation.RequestBody?.Content?.FirstOrDefault().Value?.Examples;
		var examples = OperationPageModel.MapExamples(requestExamples, static m => new HtmlString(m ?? ""));
		var samples = OperationPageModel.WithExampleCodeSamples(OpenApiExtensionReader.ParseCodeSamples(operation), requestExamples);
		return OperationPageModel.BuildExampleScenarios(examples, [], samples);
	}
}
