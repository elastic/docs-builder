// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Actions.Core.Services;
using Elastic.Changelog;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Serverless;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

namespace Documentation.Builder.Commands.ReleaseAutomation;

/// <summary>Serverless release automation command.</summary>
internal sealed class ServerlessReleaseCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	private const string GitOpsRepository = "elastic/serverless-gitops";

	private readonly ILogger _logger = logFactory.CreateLogger<ServerlessReleaseCommand>();

	/// <summary>
	/// Bundle the release notes of a serverless promotion, from the previously published endpoint ref
	/// to <paramref name="serviceVersion"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The previous endpoint ref is the last different <c>production-noncanary-ds-5</c> version in the
	/// service's <c>versions.yaml</c> history in <c>elastic/serverless-gitops</c>. Each repository's
	/// <c>docs/changelog.yml</c> is read at its end ref, and one bundle per repository is built with
	/// its <c>serverless-release</c> profile from the commit range. Kibana produces one bundle.
	/// Elasticsearch produces two: <c>elasticsearch-serverless</c> over the serverless range, and
	/// <c>elasticsearch</c>, a submodule of it, over the range of submodule commits pinned at the two refs.
	/// </para>
	/// <para>
	/// A range with no notes (for example only dependency bumps) is not an error: it writes a bundle with no entries, so
	/// the release stays on record, and the renderer skips it. A pull request whose metadata could not be read is still an error.
	/// Bundle files end with the short ref of the promotion (<c>{repo}-{product}-{date}-{short ref}.yaml</c>), so a later release on the
	/// same day, with or without notes, never has the name of an earlier one.
	/// </para>
	/// <para>Requires <c>GITHUB_TOKEN</c> with read access to <c>elastic/serverless-gitops</c> and the service repositories.</para>
	/// </remarks>
	/// <param name="service">Serverless service, as emitted by gpctl (for example <c>kibana</c>).</param>
	/// <param name="serviceVersion">Published endpoint ref of this promotion (<c>SERVICE_VERSION</c>). 12 characters or a full SHA.</param>
	/// <param name="date">Bundle version, a date (YYYY-MM-DD). Defaults to today's UTC date. Named <c>date</c> because <c>--version</c> is the CLI's own flag.</param>
	/// <param name="outputDir">Directory for the bundle file. Defaults to <c>./bundles</c>.</param>
	/// <param name="dryRun">Resolve the range and print the run report without writing a bundle.</param>
	[CommandName("serverless")]
	[Hidden(Schema = true)]
	[NoOptionsInjection]
	public async Task<int> Serverless(
		[Argument] string service,
		[Argument] string serviceVersion,
		string? date = null,
		[ExpandUserProfile] DirectoryInfo? outputDir = null,
		bool dryRun = false,
		CancellationToken ctx = default
	)
	{
		if (!ServerlessPromotion.Services.TryGetValue(service, out var svc))
		{
			collector.EmitError(
				string.Empty,
				$"Unknown serverless service '{service}'. Supported: {string.Join(", ", ServerlessPromotion.Services.Keys)}."
			);
			return 1;
		}

		var token = environmentVariables.GetEnvironmentVariable("GITHUB_TOKEN");
		if (string.IsNullOrWhiteSpace(token))
		{
			collector.EmitError(string.Empty, "GITHUB_TOKEN is required to read elastic/serverless-gitops and the service repository.");
			return 1;
		}

		var bundleVersion = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		var rootOutput = outputDir ?? new DirectoryInfo(Path.Join(Directory.GetCurrentDirectory(), "bundles"));

		// The bundling file system is scoped to the working directory and rejects hidden directories.
		var relativeOutput = Path.GetRelativePath(Directory.GetCurrentDirectory(), rootOutput.FullName);
		if (relativeOutput.Split(Path.DirectorySeparatorChar).Any(segment => segment.StartsWith('.') && segment != "." && segment != ".."))
		{
			collector.EmitError(string.Empty, $"--output-dir must not be or sit inside a hidden directory: {relativeOutput}");
			return 1;
		}
		if (!rootOutput.Exists)
			rootOutput.Create();

		using var http = new HttpClient { BaseAddress = new Uri("https://api.github.com/") };
		http.DefaultRequestHeaders.UserAgent.ParseAdd("docs-builder/1.0");
		http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var bundler = new ServerlessBundler(logFactory, collector, configurationContext, http);
		var startRef = await ResolvePreviousVersionAsync(bundler, svc, serviceVersion, ctx);
		if (startRef is null)
		{
			collector.EmitError(
				string.Empty,
				$"Could not resolve the previous {svc.FinalSlice} version for '{svc.Id}' from {GitOpsRepository} history."
			);
			return 1;
		}
		_logger.LogInformation("Commit range for {Service}: {Start}..{End}", svc.Id, startRef, serviceVersion);
		await githubActionsService.SetOutputAsync("start_ref", startRef);

		var result = await bundler.BundleAsync(
			new ServerlessBundleRequest(svc, startRef, serviceVersion, bundleVersion, rootOutput)
			{
				DryRun = dryRun,
				WriteEmptyRange = true,
				ShortRefOf = serviceVersion
			},
			ctx
		);
		if (!result.Success)
			return 1;
		var bundles = result.Paths;

		if (bundles.Count > 0)
		{
			await githubActionsService.SetOutputAsync("bundle_path", bundles[0]);
			await githubActionsService.SetOutputAsync("bundle_paths", string.Join('\n', bundles));
			await githubActionsService.SetOutputAsync("bundle_directory", rootOutput.FullName);
		}
		return 0;
	}

	private async Task<string?> ResolvePreviousVersionAsync(
		ServerlessBundler github,
		ServerlessService svc,
		string serviceVersion,
		CancellationToken ctx
	)
	{
		async Task<IReadOnlyList<GitopsCommit>?> GetCommitsPage(int page)
		{
			using var json = await github.GetJsonAsync(
				$"repos/{GitOpsRepository}/commits?path={svc.VersionsPath}&per_page=100&page={page}",
				ctx
			);
			if (json is null)
				return null;
			return [
				.. json
					.RootElement
					.EnumerateArray()
					.Select(
						commit => new GitopsCommit(
							commit.GetProperty("sha").GetString() ?? string.Empty,
							commit.GetProperty("commit").GetProperty("message").GetString() ?? string.Empty
						)
					)
			];
		}

		var result = await ServerlessPromotion.FindPreviousVersionAsync(
			GetCommitsPage,
			sha => github.GetRawAsync($"repos/{GitOpsRepository}/contents/{svc.VersionsPath}?ref={sha}", ctx),
			new PreviousVersionSearch(svc.FinalSlice, serviceVersion)
		);

		switch (result.Status)
		{
			case HistoryWalkStatus.Found:
				_logger.LogInformation("Scanned {Scanned} gitops commits for {Path}", result.ScannedCommits, svc.VersionsPath);
				return result.PreviousVersion;
			case HistoryWalkStatus.Failed:
				// The failed request is already logged. Do not guess: skipping a commit would pick an older version.
				collector.EmitError(
					string.Empty,
					$"Could not read the {GitOpsRepository} history of {svc.VersionsPath} after {result.ScannedCommits} commits, so the previous version is unknown. Run again."
				);
				return null;
			case HistoryWalkStatus.CapReached:
				collector.EmitError(
					string.Empty,
					$"Read {result.ScannedCommits} commits of {svc.VersionsPath} ({ServerlessPromotion.MaxHistoryPages} pages) without finding a previous {svc.FinalSlice} version."
				);
				return null;
			default:
				_logger.LogWarning(
					"Read all {Scanned} gitops commits for {Path} without finding a previous version",
					result.ScannedCommits,
					svc.VersionsPath
				);
				return null;
		}
	}
}
