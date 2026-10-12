// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Configuration;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// The Elasticsearch spec writes each operation's multi-language samples from one of its request examples.
/// Whenever a named example sends the body the Console sample sends, the samples must sit on that example, so a
/// spec refresh that breaks the convention fails here rather than mislabelling samples on the site.
/// </summary>
public class CodeSamplePairingSpecTests
{
	[Test]
	public async Task ElasticsearchSpec_SamplesPairWithTheExampleWhoseBodyConsoleSends()
	{
		var document = await LoadElasticsearchSpec();
		var mismatches = new List<string>();
		var pairedByBody = 0;

		foreach (var (route, method, operation) in Operations(document))
		{
			var samples = OpenApiExtensionReader.ParseCodeSamples(operation).Where(static s => s.Scenario is null).ToArray();
			var examples = OperationPageModel.MapExamples(operation.RequestBody?.Content?.FirstOrDefault().Value?.Examples, NoMarkdown);
			if (samples.Length == 0 || examples.Count == 0 || samples.FirstOrDefault(static s => s.IsConsole) is not { } console)
				continue;

			var scenarios = OperationPageModel.BuildExampleScenarios(examples, [], samples);
			var carrying = scenarios.Where(static s => s.CodeSamples.Count > 0).ToArray();
			var consoleBody = OperationPageModel.Compact(GeneratedCodeSamples.SplitConsole(console.Source)?.Body ?? "");
			var sendingConsoleBody = scenarios.Where(
				s => s.RequestJson is { } json && OperationPageModel.Compact(json) == consoleBody
			).ToArray();

			if (carrying is not [var only])
				mismatches.Add($"{method} {route}: samples on {carrying.Length} examples");
			else if (sendingConsoleBody.Length > 0 && !sendingConsoleBody.Contains(only))
				mismatches.Add($"{method} {route}: samples on '{only.Title}' while '{sendingConsoleBody[0].Title}' sends the Console body");
			else if (sendingConsoleBody.Length > 0)
				pairedByBody++;
		}

		pairedByBody.Should().BeGreaterThan(100, "the spec ships samples alongside request examples for most body-carrying operations");
		mismatches.Should().BeEmpty(string.Join("\n", mismatches));
	}

	private static async Task<OpenApiDocument> LoadElasticsearchSpec()
	{
		var file = new FileSystem().FileInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "elasticsearch.json"));
		return await OpenApiReader.Instance.ReadAsync(file) ?? throw new InvalidOperationException($"Could not read {file.FullName}");
	}

	private static IEnumerable<(string Route, string Method, OpenApiOperation Operation)> Operations(OpenApiDocument document)
	{
		foreach (var (route, item) in document.Paths)
		{
			foreach (var (method, operation) in item.Operations ?? [])
				yield return (route, method.ToString().ToUpperInvariant(), operation);
		}
	}

	private static HtmlString NoMarkdown(string? markdown) => new(markdown ?? "");
}
