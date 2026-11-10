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
		"*.elastic.dev",
		"*.cloudfront.net",
		"*.googletagmanager.com",
		"elastic-global-nav-assets.s3.us-west-2.amazonaws.com"
	];

	/// <summary>
	/// Runs the journeys against a site and exits with 1 when any of them fails, when one cannot be parsed, or when every
	/// selected journey was skipped. A run is read-only and identifies itself as a monitor unless told otherwise, and it
	/// validates the site without comparing transcripts.
	/// </summary>
	/// <param name="url">-u, The origin to run against, such as <c>https://www.elastic.co</c>. Journeys <c>open</c> paths on it.</param>
	/// <param name="journeys">-j, A folder of journeys to run instead of the ones compiled into this binary. Needed for <c>--changed-only</c>.</param>
	/// <param name="area">-a, Only these areas, comma separated.</param>
	/// <param name="tag">-t, Only journeys with one of these <c># @tags</c>, comma separated.</param>
	/// <param name="changedOnly">Only journeys that differ from this git ref (or are new), such as <c>origin/main</c>.</param>
	/// <param name="env">The environment's name (<c>prod</c>, <c>edge</c>, <c>demo</c>, <c>staging</c>), for <c>env:</c> conditions. Defaults to the one the URL's host belongs to, or unknown.</param>
	/// <param name="buildType">The site's build type, for <c>build-type:</c> conditions. Defaults to the site's <c>docs:build-type</c> tag, or unknown.</param>
	/// <param name="feature">The feature flags that are on, comma separated, for <c>@requires</c> and <c>feature</c> blocks. Defaults to the site's <c>docs:features</c> tag, or none.</param>
	/// <param name="probe">The page that is read for the site's <c>docs:*</c> tags. Default: /docs, falling back to / when it does not answer</param>
	/// <param name="parallel">-p, How many journeys run at once. Default: 4</param>
	/// <param name="allow">Extra host patterns the journeys may reach, comma separated, on top of the elastic.co defaults.</param>
	/// <param name="realUserAgent">Do not identify as a monitor: the site's telemetry then runs, and its exports are allowed as writes.</param>
	/// <param name="allowWrites">Do not refuse requests that are not GET, HEAD or OPTIONS.</param>
	/// <param name="writable">URL globs a read-only run may write to anyway, comma separated.</param>
	/// <param name="allowMutations">Run journeys marked <c># @mutates</c>, which a read-only run skips.</param>
	/// <param name="allowAllSkipped">Exit with 0 when every selected journey was skipped. For a check of the journeys a change touched, where none of them may apply to the site.</param>
	/// <param name="junit">Write a JUnit XML report here.</param>
	/// <param name="artifacts">Write screenshots and recordings here, one folder per journey.</param>
	public async Task<int> Run(
		string url,
		string? journeys = null,
		string? area = null,
		string? tag = null,
		string? changedOnly = null,
		string? env = null,
		string? buildType = null,
		string? feature = null,
		string probe = "/docs",
		int parallel = 4,
		string? allow = null,
		bool realUserAgent = false,
		bool allowWrites = false,
		string? writable = null,
		bool allowMutations = false,
		bool allowAllSkipped = false,
		string? junit = null,
		string? artifacts = null,
		Cancel ct = default
	)
	{
		var baseUrl = Uri.TryCreate(url, UriKind.Absolute, out var parsed) && parsed.Scheme is "http" or "https"
			? parsed
			: throw new ArgumentException($"--url must be an http or https origin, not '{url}'.");
		if (changedOnly is not null && journeys is null)
			throw new ArgumentException(
				"--changed-only compares journey files with a git ref, so it needs --journeys <folder> in a checkout."
			);
		var folder = JourneySource.Folder(journeys);
		var files = JourneyFile.Find(folder, area);
		if (files.Count == 0)
			throw new ArgumentException($"No .journey files found in {folder}; refusing to report an empty run as passed.");
		if (changedOnly is not null)
		{
			var changed = ChangedFiles.Since(folder, changedOnly);
			files = files.Where(f => changed.Contains(Path.GetFullPath(f.Path))).ToList();
			if (files.Count == 0)
			{
				Console.Out.WriteLine($"No journeys changed since {changedOnly}.");
				return 0;
			}
		}

		var identity = new RunIdentity(
			!realUserAgent,
			!allowWrites,
			SiteContext.Split(writable).Concat(realUserAgent ? RunIdentity.TelemetryWrites : []).ToList()
		);
		var site = await ResolveSiteAsync(
			baseUrl,
			new SiteContextOptions(env, buildType, feature is null ? null : SiteContext.Split(feature), probe),
			identity,
			ct
		).ConfigureAwait(false);
		Console.Out.WriteLine($"context: {site.Describe}");

		var selection = new Selection(SiteContext.Split(tag), site.ToJourneyContext(), allowMutations || allowWrites);
		var plan = files
			.Select(LoadedJourney.Load)
			.Select(l => (Loaded: l, Skip: l.Journey is null ? null : selection.SkipReason(l.Journey)))
			.ToList();
		var hosts = DefaultAllow.Concat(SiteContext.Split(allow)).ToList();
		var journeyBase = SiteContext.JourneyBase(baseUrl, site.BuildType);
		var settings = new RunSettings(journeyBase, hosts, selection.Context, identity) { Artifacts = artifacts, Parallel = parallel };
		var outcomes = await JourneyExecution.RunAsync(plan, settings, ct).ConfigureAwait(false);

		return Report(outcomes, journeyBase, junit, allowAllSkipped && !site.ProbeFailed);
	}

	internal static async Task<SiteContext> ResolveSiteAsync(
		Uri baseUrl,
		SiteContextOptions options,
		RunIdentity identity,
		CancellationToken ct
	)
	{
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
		if (identity.Synthetic)
		{
			_ = http.DefaultRequestHeaders.UserAgent.TryParseAdd(RunIdentity.UserAgent);
			_ = http.DefaultRequestHeaders.TryAddWithoutValidation(RunIdentity.MonitorHeader, "true");
		}
		return await SiteContext.ResolveAsync(baseUrl, options, http, ct).ConfigureAwait(false);
	}

	private static int Report(IReadOnlyList<JourneyOutcome> outcomes, Uri baseUrl, string? junit, bool allowAllSkipped)
	{
		foreach (var outcome in outcomes)
			Console.Out.WriteLine(
				outcome.Skipped
					? $"skipped {outcome.File.Id} ({outcome.SkipReason})"
					: outcome.Passed
						? $"ok      {outcome.File.Id} ({outcome.Elapsed.TotalSeconds:0.0}s)"
						: $"FAILED  {outcome.File.Id}\n{outcome.Describe()}"
			);

		var failed = outcomes.Count(o => o.Failed);
		var skipped = outcomes.Count(o => o.Skipped);
		var passed = outcomes.Count(o => o.Passed);
		Console.Out.WriteLine($"{passed} passed, {failed} failed, {skipped} skipped, against {baseUrl}");
		if (passed + failed == 0)
			Console.Out.WriteLine(
				allowAllSkipped
					? "Every selected journey was skipped; none applies to this site."
					: "Every selected journey was skipped, so nothing was checked."
			);

		if (junit is not null)
			JUnitReport.Write(junit, baseUrl.Host, outcomes);
		WriteStepSummary(baseUrl, outcomes);
		return failed > 0 || (passed + failed == 0 && !allowAllSkipped) ? 1 : 0;
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
			_ = text.AppendLine(
				$"| `{outcome.File.Id}` | {(outcome.Skipped ? $"skipped: {outcome.SkipReason}" : outcome.Passed ? "passed" : "**failed**")} |"
			);
		File.AppendAllText(path, text.ToString());
	}
}
