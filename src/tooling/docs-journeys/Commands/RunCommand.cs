// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Nullean.Argh;

namespace Elastic.Documentation.Journeys.Cli.Commands;

internal sealed class RunCommand
{
	private static readonly string[] DefaultAllow =
	[
		"*.elastic.co",
		"*.googletagmanager.com",
		"elastic-global-nav-assets.s3.us-west-2.amazonaws.com"
	];

	/// <summary>
	/// Runs the journeys against a site and exits with 1 when any of them fails. Journeys only read: they validate
	/// the site and do not compare transcripts.
	/// </summary>
	/// <param name="url">-u, The origin to run against, such as <c>https://www.elastic.co</c>. Journeys <c>open</c> paths on it.</param>
	/// <param name="journeys">-j, The folder of journeys. Required: journeys are specific to the kind of site they check.</param>
	/// <param name="area">-a, Only these areas, comma separated.</param>
	/// <param name="parallel">-p, How many journeys run at once. Default: 4</param>
	/// <param name="allow">Extra host patterns the journeys may reach, comma separated, on top of the elastic.co defaults.</param>
	/// <param name="junit">Write a JUnit XML report here.</param>
	/// <param name="artifacts">Write screenshots and recordings here, one folder per journey.</param>
	public async Task<int> Run(
		string url,
		string journeys,
		string? area = null,
		int parallel = 4,
		string? allow = null,
		string? junit = null,
		string? artifacts = null,
		Cancel ct = default
	)
	{
		var baseUrl = Uri.TryCreate(url, UriKind.Absolute, out var parsed) && parsed.Scheme is "http" or "https"
			? parsed
			: throw new ArgumentException($"--url must be an http or https origin, not '{url}'.");
		var files = JourneyFile.Find(journeys, area);
		if (files.Count == 0)
			throw new ArgumentException($"No .journey files found in {journeys}; refusing to report an empty run as passed.");
		var hosts = DefaultAllow.Concat(
			(allow ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
		).ToList();

		var outcomes = await JourneyExecution.RunAsync(files, baseUrl, hosts, artifacts, parallel, ct).ConfigureAwait(false);

		foreach (var outcome in outcomes)
			Console.Out.WriteLine(
				outcome.Passed
					? $"ok      {outcome.File.Id} ({outcome.Elapsed.TotalSeconds:0.0}s)"
					: $"FAILED  {outcome.File.Id}\n{outcome.Describe()}"
			);

		var failed = outcomes.Count(o => !o.Passed);
		Console.Out.WriteLine($"{outcomes.Count - failed} passed, {failed} failed, against {baseUrl}");

		if (junit is not null)
			JUnitReport.Write(junit, baseUrl.Host, outcomes);
		WriteStepSummary(baseUrl, outcomes);
		return failed > 0 ? 1 : 0;
	}

	private static void WriteStepSummary(Uri baseUrl, IReadOnlyList<JourneyOutcome> outcomes)
	{
		if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is not { Length: > 0 } path)
			return;
		var text = new StringBuilder()
			.AppendLine($"### Journeys against {baseUrl}")
			.AppendLine()
			.AppendLine("| Journey | Result |")
			.AppendLine("|---|---|");
		foreach (var outcome in outcomes)
			_ = text.AppendLine($"| `{outcome.File.Id}` | {(outcome.Passed ? "passed" : "**failed**")} |");
		File.AppendAllText(path, text.ToString());
	}
}
