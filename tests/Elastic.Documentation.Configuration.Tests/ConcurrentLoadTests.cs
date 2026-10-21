// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Loaders are thread-safe (see <c>Constraints-Conventions.md</c>): the assembler builds many documentation sets at once, so the same loader runs on many
/// threads over different text. Every load below runs concurrently, over the shipped text and its bent variants, and must give what the same load gives alone.
/// </summary>
public class ConcurrentLoadTests
{
	private static string Describe(YamlOutcome o) =>
		o.Exception ?? $"{o.Value}|{string.Join('\n', o.Diagnostics.Select(d => d.ToString()))}";

	private sealed record YamlOutcome(object? Value, IReadOnlyList<Diagnostic> Diagnostics, string? Exception);

	private static YamlOutcome Run(LoaderProperties.YamlEntry entry, string text)
	{
		try
		{
			var (value, diagnostics) = entry.Load(text);
			return new YamlOutcome(value, diagnostics, null);
		}
		catch (Exception e)
		{
			return new YamlOutcome(null, [], $"{e.GetType().FullName}: {e.Message}");
		}
	}

	/// <summary>
	/// Law: loading many texts at once gives, for each text, what loading it alone gives.
	/// Consequence if broken: a loader shares mutable state (a converter's list, a cache, a static sink) and a build that loads documentation sets in
	/// parallel reads another set's findings, throws from a half-written collection, or spins in a corrupted dictionary.
	/// </summary>
	[Test]
	[Timeout(120_000)]
	public async Task LoadingManyTextsAtOnce_GivesWhatEachGivesAlone(CancellationToken cancellationToken)
	{
		var work = (from entry in LoaderProperties.YamlEntries()
					from text in Mutations.Corpus(LoaderProperties.BaselineConfig[entry.File])
					select (entry, text)).ToList();
		var alone = work.Select(w => Describe(Run(w.entry, w.text))).ToArray();

		// 6 rounds of everything at once, 32 threads wide
		var concurrent = new string[work.Count * 6];
		var run = Task.Run(
			() => Parallel.For(
				0,
				concurrent.Length,
				new ParallelOptions { MaxDegreeOfParallelism = 32, CancellationToken = cancellationToken },
				i => concurrent[i] = Describe(Run(work[i % work.Count].entry, work[i % work.Count].text))
			),
			cancellationToken
		);
		// a loader that spins forever fails the test instead of hanging it
		_ = await run.WaitAsync(TimeSpan.FromSeconds(100), cancellationToken);

		for (var i = 0; i < concurrent.Length; i++)
			concurrent[i]
				.Should()
				.Be(alone[i % work.Count], $"{work[i % work.Count].entry.Name} loaded concurrently over\n{work[i % work.Count].text}");
	}
}
