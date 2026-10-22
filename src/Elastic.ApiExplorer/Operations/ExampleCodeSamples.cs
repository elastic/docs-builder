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
	public static IReadOnlyList<CodeSample> Combine(IReadOnlyList<CodeSample> operationSamples, IEnumerable<RequestContent> requestContent)
	{
		var local = Collect(requestContent);
		if (local.Count == 0)
			return operationSamples;

		var owner = OwnerOf(operationSamples, local);
		var carried = operationSamples.Where(s => s.IsTied || !local.Exists(e => SameSample(s, e))).Select(
			s => s.IsTied || owner is null ? s : s with { ExampleTitle = owner }
		);

		// A local sample wins over an operation sample of the same example and language.
		return [.. carried.Where(s => !local.Exists(e => Overrides(e, s))), .. local];
	}

	/// <summary>
	/// Tied samples name an example, by its title or by the suffix of their language. They join the example they name,
	/// replacing the sample of the same language that example already has; otherwise they become an example of their
	/// own, sharing the first example's responses, after the existing ones.
	/// </summary>
	public static List<ExampleScenario> Join(List<ExampleScenario> scenarios, IReadOnlyList<CodeSample> tied)
	{
		var responses = scenarios.Count > 0 ? scenarios[0].Responses : [];
		var result = new List<ExampleScenario>(scenarios);
		var resolved = tied.Select(sample => (Sample: sample, Index: result.FindIndex(s => Names(s, sample)))).ToArray();

		foreach (var group in resolved.Where(static r => r.Index >= 0).GroupBy(static r => r.Index))
			result[group.Key] = MergeInto(result[group.Key], [.. group.Select(static r => r.Sample)]);

		foreach (var group in resolved.Where(static r => r.Index < 0).GroupBy(
			static r => NewTitleOf(r.Sample),
			StringComparer.OrdinalIgnoreCase
		))
			result.Add(NewExample(group.Key, [.. group.Select(static r => r.Sample)], responses, result));

		return result;
	}

	private static List<CodeSample> Collect(IEnumerable<RequestContent> requestContent)
	{
		var titles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var samples = new List<CodeSample>();
		foreach (var (key, example) in requestContent.SelectMany(static c => c.Examples ?? new Dictionary<string, IOpenApiExample>()))
		{
			var title = OperationPageModel.ExampleTitle(key, example);
			if (titles.Add(title))
				samples.AddRange(OpenApiExtensionReader.ParseCodeSamples(example?.Extensions).Select(s => s with { ExampleTitle = title }));
		}

		return samples;
	}

	/// <summary>
	/// The operation samples an example repeats verbatim are its own. The rest of that group belongs to the same
	/// example, so they follow it instead of attaching to whichever example the body check finds.
	/// </summary>
	private static string? OwnerOf(IReadOnlyList<CodeSample> operationSamples, IReadOnlyList<CodeSample> local) =>
		local
			.Select(l => (Local: l, Match: operationSamples.FirstOrDefault(o => !o.IsTied && SameSample(o, l))))
			.Where(static pair => pair.Match is not null)
			.OrderByDescending(static pair => pair.Match!.IsConsole)
			.Select(static pair => pair.Local.ExampleTitle)
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

	/// <summary>The title of the example a sample names: its own title, or the suffix of its language read as an example key.</summary>
	private static string NewTitleOf(CodeSample sample) => sample.ExampleTitle ?? OperationPageModel.HumanizeExampleKey(sample.Scenario!);

	/// <summary>
	/// An example made only to hold unmatched samples is never named. A sample's title must match the scenario's
	/// title; a language suffix may also match the humanized key, as the example's own title is made from a key.
	/// </summary>
	private static bool Names(ExampleScenario scenario, CodeSample sample) =>
		!scenario.FromUnmatchedSamples
			&& (sample.ExampleTitle is { } title
				? Same(scenario.Title, title)
				: Same(scenario.Title, sample.Scenario!) || Same(scenario.Title, NewTitleOf(sample)));

	/// <summary>A local sample overrides a sample of the same language that names the same example.</summary>
	private static bool Overrides(CodeSample local, CodeSample other) =>
		Same(local.Language, other.Language)
			&& (other.ExampleTitle is { } title
				? Same(local.ExampleTitle!, title)
				: other.Scenario is { } suffix && (Same(local.ExampleTitle!, suffix) || Same(local.ExampleTitle!, NewTitleOf(other))));

	/// <summary>Two samples of one language are the same when they differ only in line endings or surrounding whitespace.</summary>
	private static bool SameSample(CodeSample a, CodeSample b) =>
		Same(a.Language, b.Language)
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

	private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
