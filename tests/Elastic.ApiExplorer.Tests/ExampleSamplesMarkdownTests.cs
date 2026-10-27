// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>The markdown copy of an operation lists each example's code samples under that example's title.</summary>
[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ExampleSamplesMarkdownTests(ApiExplorerFixture fixture)
{
	[Test]
	public async Task Write_SeveralExamplesWithSamples_LabelsEachSet()
	{
		var markdown = await WriteMarkdownOfFixture("example-level-codesamples-openapi.json");

		markdown.Should().Contain("### A simple term search").And.Contain("### A point in time search").And.Contain("### Search slicing");
		markdown.Split("#### Console").Should().HaveCount(4, "each of the three examples has its own Console sample");
		markdown
			.IndexOf("### A point in time search", StringComparison.Ordinal)
			.Should()
			.BeGreaterThan(markdown.IndexOf("### A simple term search", StringComparison.Ordinal));
	}

	[Test]
	public async Task Write_OneExampleWithSamples_ListsTheSamplesWithoutExtraHeadings()
	{
		var markdown = await WriteMarkdown(
			/*lang=json,strict*/
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/_x":{"post":{"operationId":"x","requestBody":{"content":{
			  "application/json":{"schema":{"type":"object"},"examples":{
			    "Only":{"summary":"Only example","value":{"a":1},"x-codeSamples":[{"lang":"Console","source":"POST /_x\n{\"a\":1}"},{"lang":"Python","source":"client.x()"}]}}}}},
			  "responses":{"200":{"description":"ok"}}}}}}
			"""
		);

		markdown.Should().Contain("## Code examples").And.NotContain("### Only example");
		markdown.Split("#### Console").Should().HaveCount(2);
	}

	[Test]
	public async Task Write_GeneratedResponse_StatesItsSourceAndMediaType()
	{
		var markdown = await WriteMarkdown(
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},"paths":{"/subscription":{"get":{"operationId":"getSubscription","responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Subscription"}}}}}}}},"components":{"schemas":{"Subscription":{"type":"object","required":["state"],"properties":{"state":{"$ref":"#/components/schemas/SubscriptionState"}}},"SubscriptionState":{"type":"string","enum":["trial","active","inactive"]}}}}
			"""
		);

		markdown.Should().Contain("Generated from the API spec");
		markdown.Should().Contain("`application/json`");
		markdown.Should().Contain("\"state\": \"trial\"");
	}

	private async Task<string> WriteMarkdownOfFixture(string fixtureFile) =>
		await WriteMarkdown(
			await File.ReadAllTextAsync(
				Path.Combine(AppContext.BaseDirectory, "TestData", fixtureFile),
				TestContext.Current!.Execution.CancellationToken
			)
		);

	private async Task<string> WriteMarkdown(string json)
	{
		var document = await LoadSpecAsync(json);
		var navigation = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			fixture.Context,
			PassthroughMarkdownRenderer.Instance
		).CreateNavigation("fixture", document);
		var operation = ApiExplorerFixture.Walk(navigation).OfType<OperationNavigationItem>().Single();
		var context = new ApiRenderContext(
			fixture.Context,
			document,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
		)
		{ NavigationHtml = string.Empty, CurrentNavigation = operation, MarkdownRenderer = PassthroughMarkdownRenderer.Instance };
		return OperationCommonMark.Write(operation.Model, OperationPageModel.Create(operation.Model, context), context);
	}
}
