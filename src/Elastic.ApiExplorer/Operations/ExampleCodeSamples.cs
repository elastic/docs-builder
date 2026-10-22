// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>
/// Code samples that belong to a request example: the ones the example carries as its own <c>x-codeSamples</c>,
/// the ones the operation repeats or suffixes with the example's title, and how both join the example's scenario.
/// </summary>
internal static class ExampleCodeSamples
{
	/// <summary>
	/// Adds the <c>x-codeSamples</c> that request examples carry themselves, each tied to its example by title.
	/// Examples are read from every content type. Examples with the same title show as one example, so the first
	/// one keeps its samples. The operation's own unsuffixed samples stay, except the ones an example repeats verbatim:
	/// a spec may copy the first example's samples to the operation for tools that read only the operation.
	/// </summary>
	public static IReadOnlyList<CodeSample> Combine(
		IReadOnlyList<CodeSample> operationSamples,
		IEnumerable<IDictionary<string, IOpenApiExample>?> examplesPerContentType
	)
	{
		var local = Collect(examplesPerContentType);
		if (local.Count == 0)
			return operationSamples;

		var owner = OwnerOf(operationSamples, local);
		var carried = operationSamples.Where(s => s.Scenario is not null || !local.Exists(e => SameSample(s, e))).Select(
			s => s.Scenario is null && owner is not null ? s with { Scenario = owner } : s
		);

		// A local sample wins over an operation sample of the same example and language.
		var overridden = local.Select(ExampleLanguage).ToHashSet(StringComparer.OrdinalIgnoreCase);
		return [.. carried.Where(s => !overridden.Contains(ExampleLanguage(s))), .. local];
	}

	/// <summary>
	/// Each suffix names an example. Its samples join the example of the same title, replacing the sample of the
	/// same language that example already has; otherwise they become an example of their own, sharing the first
	/// example's responses, after the existing ones.
	/// </summary>
	public static List<ExampleScenario> Join(List<ExampleScenario> scenarios, IReadOnlyList<CodeSample> suffixed)
	{
		var responses = scenarios.Count > 0 ? scenarios[0].Responses : [];
		var result = new List<ExampleScenario>(scenarios);
		foreach (var group in suffixed.GroupBy(static c => c.Scenario!, StringComparer.OrdinalIgnoreCase))
		{
			var title = OperationPageModel.HumanizeExampleKey(group.Key);
			var existing = result.FindIndex(s => !s.FromUnmatchedSamples && IsNamed(s, title, group.Key));
			if (existing >= 0)
				result[existing] = MergeInto(result[existing], [.. group]);
			else
				result.Add(NewExample(title, [.. group], responses, result));
		}

		return result;
	}

	private static List<CodeSample> Collect(IEnumerable<IDictionary<string, IOpenApiExample>?> examplesPerContentType)
	{
		var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var samples = new List<CodeSample>();
		foreach (var (key, example) in examplesPerContentType.SelectMany(static e => e ?? new Dictionary<string, IOpenApiExample>()))
		{
			var title = OperationPageModel.ExampleTitle(key, example);
			if (titles.Add(title))
				samples.AddRange(OpenApiExtensionReader.ParseCodeSamples(example?.Extensions).Select(s => s with { Scenario = title }));
		}

		return samples;
	}

	/// <summary>
	/// The operation samples an example repeats verbatim are its own. The rest of that group belongs to the same
	/// example, so they follow it instead of attaching to whichever example the body check finds.
	/// </summary>
	private static string? OwnerOf(IReadOnlyList<CodeSample> operationSamples, IReadOnlyList<CodeSample> local) =>
		local
			.Select(l => (Local: l, Match: operationSamples.FirstOrDefault(o => o.Scenario is null && SameSample(o, l))))
			.Where(static pair => pair.Match is not null)
			.OrderByDescending(static pair => pair.Match!.IsConsole)
			.Select(static pair => pair.Local.Scenario)
			.FirstOrDefault();

	private static ExampleScenario MergeInto(ExampleScenario scenario, IReadOnlyList<CodeSample> samples)
	{
		var languages = samples.Select(static c => c.Language).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var merged = scenario.CodeSamples.Where(c => !languages.Contains(c.Language)).Concat(samples).OrderBy(static c => c.Rank).ToArray();
		return scenario with { CodeSamples = merged, CodeSamplesIncludeRequest = SamplesShowRequest(scenario, merged) };
	}

	private static ExampleScenario NewExample(
		string title,
		IReadOnlyList<CodeSample> samples,
		IReadOnlyList<ExampleResponse> responses,
		IReadOnlyList<ExampleScenario> existing
	) =>
		new()
		{
			Title = title,
			TabId = OperationPageModel.UniqueTabId(OperationPageModel.ToTabId(title, existing.Count), existing),
			CodeSamples = samples,
			CodeSamplesIncludeRequest = true,
			Responses = responses
		};

	private static bool IsNamed(ExampleScenario scenario, string humanizedTitle, string suffix) =>
		string.Equals(scenario.Title, humanizedTitle, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(scenario.Title, suffix, StringComparison.OrdinalIgnoreCase);

	/// <summary>Two samples of one language are the same when they differ only in line endings or surrounding whitespace.</summary>
	private static bool SameSample(CodeSample a, CodeSample b) =>
		string.Equals(a.Language, b.Language, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(a.Source.ReplaceLineEndings("\n").Trim(), b.Source.ReplaceLineEndings("\n").Trim(), StringComparison.Ordinal);

	/// <summary>
	/// The rail hides an example's own request block when its samples show the request. A Console sample is the request;
	/// other samples count when they contain the body. An example with no inline body has nothing to hide.
	/// </summary>
	private static bool SamplesShowRequest(ExampleScenario scenario, IEnumerable<CodeSample> samples)
	{
		if (string.IsNullOrWhiteSpace(scenario.RequestJson))
			return string.IsNullOrEmpty(scenario.RequestExternalValue);

		var body = OperationPageModel.Compact(scenario.RequestJson);
		return samples.Any(s => s.IsConsole || OperationPageModel.Compact(s.Source).Contains(body, StringComparison.Ordinal));
	}

	private static string ExampleLanguage(CodeSample sample) => $"{sample.Scenario}\u0001{sample.Language}";
}
