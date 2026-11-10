// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Nullean.Cdp.Journeys;

namespace Elastic.Documentation.Journeys.Cli;

/// <summary>What a run knows about the site it runs against, so journeys can say what they need.</summary>
internal sealed partial class SiteContext
{
	/// <summary>
	/// The documentation environments by the host their site answers on (<c>config/assembler.yml</c> and the
	/// deploy workflows name them). A host not listed is an unknown environment.
	/// </summary>
	private static readonly Dictionary<string, string> KnownHosts = new(StringComparer.OrdinalIgnoreCase)
	{
		["www.elastic.co"] = "prod",
		["staging-website.elastic.co"] = "staging",
		["dwnz7p9ulv07a.cloudfront.net"] = "staging",
		["www-qa.elastic.co"] = "edge",
		["docs-edge.elastic.dev"] = "edge",
		["d34ipnu52o64md.cloudfront.net"] = "edge",
		["docs-demo.elastic.dev"] = "demo",
		["d3fvim9nkr2bzu.cloudfront.net"] = "demo",
		["docs-v3-preview.elastic.dev"] = "preview",
		["localhost"] = "local",
		["127.0.0.1"] = "local"
	};

	public string? Env { get; init; }
	public string? BuildType { get; init; }
	public IReadOnlySet<string> Features { get; init; } = new HashSet<string>(StringComparer.Ordinal);

	/// <summary>Where each part of the context came from, for the run's output.</summary>
	public string Describe { get; init; } = "";

	/// <summary>True when the site's tags were needed and the probe could not read them.</summary>
	public bool ProbeFailed { get; init; }

	public JourneyContext ToJourneyContext() => new(Features, BuildType, Env);

	public static string? EnvFromHost(Uri url) => KnownHosts.TryGetValue(url.Host, out var env) ? env : null;

	/// <summary>
	/// Builds the context: an explicit <paramref name="env"/>, <paramref name="buildType"/> or <paramref name="features"/>
	/// wins; the rest comes from the site's own <c>docs:build-type</c> and <c>docs:features</c> meta tags on
	/// <paramref name="probePath"/>. A page that does not answer leaves them unknown, and unknown conditions are not matched. A page
	/// that answers with neither <c>docs:build-type</c> nor a <c>build-type-*</c> body class is an assembler site that predates the tag.
	/// </summary>
	public static async Task<SiteContext> ResolveAsync(Uri baseUrl, SiteContextOptions options, HttpClient http, CancellationToken ct)
	{
		var env = options.Env ?? EnvFromHost(baseUrl);
		var buildType = options.BuildType;
		var features = options.Features is { } given ? new HashSet<string>(given, StringComparer.Ordinal) : null;
		var notes = new List<string> { $"env={env ?? "unknown"}{(options.Env is null ? "" : " (--env)")}" };
		var probeFailed = false;

		if (buildType is null || features is null)
		{
			var (html, probed, error) = await ProbeAsync(http, baseUrl, options.ProbePath, ct).ConfigureAwait(false);
			if (html is null)
			{
				probeFailed = true;
				notes.Add($"probe {options.ProbePath} failed: {error}");
			}
			else
			{
				var metaBuildType = BuildTypeMeta().Match(html) is { Success: true } b ? b.Groups["content"].Value : null;
				var classBuildType = BuildTypeClass().Match(html) is { Success: true } c ? c.Groups["type"].Value : null;
				var metaFeatures = FeaturesMeta().Match(html) is { Success: true } f ? Split(f.Groups["content"].Value) : null;
				if (metaBuildType is null && metaFeatures is null)
					notes.Add($"{probed} has no docs:build-type or docs:features tags");
				// Isolated and Codex sites render the build-type body class, so only a deployed assembler site that predates
				// both can lack it.
				var siteBuildType = metaBuildType ?? classBuildType;
				if (siteBuildType is null && buildType is null)
					notes.Add($"{probed} predates docs:build-type: assuming an assembler site");
				buildType ??= siteBuildType ?? "assembler";
				features ??= metaFeatures;
			}
		}

		notes.Add($"build-type={buildType ?? "unknown"}{(options.BuildType is null ? "" : " (--build-type)")}");
		notes.Add($"features=[{string.Join(' ', (features ?? []).Order(StringComparer.Ordinal))}]");
		return new SiteContext
		{
			Env = env,
			BuildType = buildType,
			Features = features ?? [],
			Describe = string.Join(", ", notes),
			ProbeFailed = probeFailed
		};
	}

	public static HashSet<string> Split(string? list) =>
		(list ?? "").Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(
			StringComparer.Ordinal
		);

	// An isolated docs-builder site serves from /, so a probe that 404s falls back to the root page.
	private static async Task<(string? Html, string Probed, string? Error)> ProbeAsync(
		HttpClient http,
		Uri baseUrl,
		string probePath,
		CancellationToken ct
	)
	{
		var (html, error) = await ReadAsync(http, new Uri(baseUrl, probePath), ct).ConfigureAwait(false);
		if (html is not null || probePath == "/")
			return (html, probePath, error);
		var (rootHtml, rootError) = await ReadAsync(http, new Uri(baseUrl, "/"), ct).ConfigureAwait(false);
		return rootHtml is null ? (null, probePath, $"{error}; / failed: {rootError}") : (rootHtml, "/", null);
	}

	private static async Task<(string? Html, string? Error)> ReadAsync(HttpClient http, Uri url, CancellationToken ct)
	{
		try
		{
			using var response = await http.GetAsync(url, ct).ConfigureAwait(false);
			return response.IsSuccessStatusCode
				? (await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false), null)
				: (null, $"HTTP {(int)response.StatusCode}");
		}
		catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
		{
			return (null, e.Message);
		}
	}

	// An empty docs:features renders as <meta name="docs:features"> with no content attribute.
	[GeneratedRegex("""<meta name="docs:features"(?: content="(?<content>[^"]*)")?""")]
	private static partial Regex FeaturesMeta();

	[GeneratedRegex("""<meta name="docs:build-type" content="(?<content>[^"]*)""")]
	private static partial Regex BuildTypeMeta();

	// Every layout renders the build type as a body class, which is what isolated and Codex sites answer with.
	[GeneratedRegex("""build-type-(?<type>isolated|assembler|codex)""")]
	private static partial Regex BuildTypeClass();
}

internal sealed record SiteContextOptions(string? Env, string? BuildType, IReadOnlySet<string>? Features, string ProbePath);
