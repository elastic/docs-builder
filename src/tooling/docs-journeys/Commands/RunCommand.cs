// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Nullean.Argh;
using Nullean.Cdp.Journeys;
using Nullean.Snapshots.Browser.Console;

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
	/// <param name="url">-u, The origin to run against, such as <c>https://www.elastic.co</c>, or <c>prod</c>, <c>edge</c>, <c>demo</c> or <c>local</c> for the site of that environment. Journeys <c>open</c> paths on it.</param>
	/// <param name="journeys">-j, A folder of journeys to run instead of the ones compiled into this binary. Needed for <c>--changed-only</c>.</param>
	/// <param name="area">-a, Only these areas, comma separated.</param>
	/// <param name="only">-o, Only these journeys by id (<c>area/name</c>), comma separated.</param>
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
	/// <param name="screenshotOnFailure">Take a screenshot when a step fails: drawn under the failure on a terminal that can show images, and written to <c>failure.png</c> in the journey's folder under <c>--artifacts</c>. A page can show private data, so it is off unless asked for.</param>
	/// <param name="verbose">-v, List every skipped journey instead of collapsing a reason that many share.</param>
	public async Task<int> Run(
		string url,
		string? journeys = null,
		string? area = null,
		string? only = null,
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
		bool verbose = false,
		bool screenshotOnFailure = false,
		Cancel ct = default
	)
	{
		var baseUrl = SiteContext.ParseUrl(url);
		if (changedOnly is not null && journeys is null)
			throw new ArgumentException(
				"--changed-only compares journey files with a git ref, so it needs --journeys <folder> in a checkout."
			);
		var folder = JourneySource.Folder(journeys);
		var files = JourneyFile.Find(folder, area);
		if (files.Count == 0)
			throw new ArgumentException($"No .journey files found in {folder}; refusing to report an empty run as passed.");
		if (only is not null)
			files = SelectOnly(files, only);
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
		var journeyBase = SiteContext.JourneyBase(baseUrl, site.BuildType);
		Console.Out.WriteLine(
			ConsoleJourneyReporter.UseInteractive
				? ConsoleJourneyReporter.ContextBlock(
					"docs-journeys",
					ContextRows(site, journeyBase, (env, buildType, feature)),
					site.ProbeNotes
				)
				: $"context: {site.Describe}"
		);

		var headers = new Dictionary<string, string>();
		if (identity.Synthetic)
			headers[RunIdentity.MonitorHeader] = "true";
		var options = new JourneyRunOptions
		{
			Files = [.. files.Select(f => new JourneyRunFile(f.Id, f.Path) { Display = f.DisplayPath(folder) })],
			BaseUrl = journeyBase,
			AllowedHosts = [.. DefaultAllow, .. SiteContext.Split(allow)],
			ArtifactsFor = artifacts is null ? null : f => Path.Combine(artifacts, f.Id),
			Features = site.Features,
			BuildType = site.BuildType,
			Env = site.Env,
			ReadOnly = identity.ReadOnly,
			AllowMutations = allowMutations,
			WritableUrls = identity.Writable,
			Viewport = new JourneyViewport(1280, 800),
			UserAgent = identity.Synthetic ? RunIdentity.UserAgent : null,
			Headers = headers,
			Tags = SiteContext.Split(tag),
			Parallel = parallel,
			AllowAllSkipped = allowAllSkipped && !site.ProbeFailed,
			Verbose = verbose,
			ScreenshotOnFailure = screenshotOnFailure,
			JUnitPath = junit,
			Rerun = RerunCommand(url, journeys, (env, buildType, feature)),
			ParseErrorExitCode = 1,
		};
		return await JourneyConsoleRunner.RunAsync(options, Console.Out, Console.Error, ct).ConfigureAwait(false);
	}

	private static List<ContextRow> ContextRows(
		SiteContext site,
		Uri journeyBase,
		(string? Env, string? BuildType, string? Feature) given
	) =>
		[
			new("Site", journeyBase.ToString()),
			new("Env", site.Env ?? "unknown", given.Env is null ? null : "--env"),
			new("Build", site.BuildType ?? "unknown", given.BuildType is null ? null : "--build-type"),
			new(
				"Features",
				site.Features.Count == 0 ? "none" : string.Join(' ', site.Features.Order(StringComparer.Ordinal)),
				given.Feature is null ? null : "--feature"
			),
		];

	/// <summary>The command that reruns one journey: same source and site context, with <c>{id}</c> standing for the journey.</summary>
	internal static string RerunCommand(string url, string? journeys, (string? Env, string? BuildType, string? Feature) context)
	{
		var text = new StringBuilder($"docs-journeys run --url {Quote(url)}");
		if (journeys is not null)
			_ = text.Append($" --journeys {Quote(journeys)}");
		if (context.Env is not null)
			_ = text.Append($" --env {Quote(context.Env)}");
		if (context.BuildType is not null)
			_ = text.Append($" --build-type {Quote(context.BuildType)}");
		if (context.Feature is not null)
			_ = text.Append($" --feature {Quote(context.Feature)}");
		return text.Append(" --only {id}").ToString();
	}

	private static string Quote(string value) =>
		value.Length > 0 && value.All(c => char.IsLetterOrDigit(c) || "/:.,_-@=+".Contains(c, StringComparison.Ordinal))
			? value
			: "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";

	private static IReadOnlyList<JourneyFile> SelectOnly(IReadOnlyList<JourneyFile> files, string only)
	{
		var ids = SiteContext.Split(only);
		var unknown = ids.Where(id => !files.Any(f => f.Id.Equals(id, StringComparison.OrdinalIgnoreCase))).ToList();
		return unknown.Count > 0
			? throw new ArgumentException($"No journey with id {string.Join(", ", unknown)}; run `docs-journeys list` for the ids.")
			: files.Where(f => ids.Contains(f.Id, StringComparer.OrdinalIgnoreCase)).ToList();
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
}
