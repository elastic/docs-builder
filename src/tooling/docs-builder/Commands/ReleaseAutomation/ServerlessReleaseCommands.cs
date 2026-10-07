// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Actions.Core.Services;
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

/// <summary>Serverless release automation commands.</summary>
internal sealed class ServerlessReleaseCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	private const string GitOpsRepository = "elastic/serverless-gitops";
	private const int MaxHistoryPages = 10;

	private readonly ILogger _logger = logFactory.CreateLogger<ServerlessReleaseCommands>();

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
	/// <para>Requires <c>GITHUB_TOKEN</c> with read access to <c>elastic/serverless-gitops</c> and the service repositories.</para>
	/// </remarks>
	/// <param name="service">Serverless service, as emitted by gpctl (for example <c>kibana</c>).</param>
	/// <param name="serviceVersion">Published endpoint ref of this promotion (<c>SERVICE_VERSION</c>). 12 characters or a full SHA.</param>
	/// <param name="date">Bundle version, a date (YYYY-MM-DD). Defaults to today's UTC date. Named <c>date</c> because <c>--version</c> is the CLI's own flag.</param>
	/// <param name="outputDir">Directory for the bundle file. Defaults to <c>./bundles</c>.</param>
	/// <param name="dryRun">Resolve the range and print the run report without writing a bundle.</param>
	[Hidden]
	[NoOptionsInjection]
	public async Task<int> Bundle(
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

		var startRef = await ResolvePreviousVersionAsync(http, svc, serviceVersion, ctx);
		if (startRef is null)
		{
			collector.EmitError(
				string.Empty,
				$"Could not resolve the previous {ServerlessPromotion.FinalSlice} version for '{svc.Id}' from {GitOpsRepository} history."
			);
			return 1;
		}
		_logger.LogInformation("Commit range for {Service}: {Start}..{End}", svc.Id, startRef, serviceVersion);
		await githubActionsService.SetOutputAsync("start_ref", startRef);

		// One bundle per repository: the service repository over the serverless range, then each
		// submodule over the range of commits the service repository pins at the two refs.
		var bundles = new List<string>();
		var primary = await BundleRangeAsync(
			http,
			svc.Repository,
			svc.Profile,
			startRef,
			serviceVersion,
			bundleVersion,
			rootOutput,
			dryRun,
			ctx
		);
		if (!primary.Success)
			return 1;
		bundles.AddRange(primary.Paths);

		foreach (var submodule in svc.Submodules)
		{
			var startSha = await GetSubmoduleShaAsync(http, svc.Repository, startRef, submodule, ctx);
			var endSha = await GetSubmoduleShaAsync(http, svc.Repository, serviceVersion, submodule, ctx);
			if (startSha is null || endSha is null)
			{
				collector.EmitError(
					string.Empty,
					$"Could not read the '{submodule.Path}' submodule commit of elastic/{svc.Repository} at {startRef} or {serviceVersion}."
				);
				return 1;
			}
			if (string.Equals(startSha, endSha, StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogInformation(
					"{Repository} did not change between {Start} and {End}; no bundle.",
					submodule.Repository,
					startRef,
					serviceVersion
				);
				continue;
			}
			_logger.LogInformation("Submodule {Repository}: {Start}..{End}", submodule.Repository, startSha, endSha);
			var result = await BundleRangeAsync(
				http,
				submodule.Repository,
				svc.Profile,
				startSha,
				endSha,
				bundleVersion,
				rootOutput,
				dryRun,
				ctx
			);
			if (!result.Success)
				return 1;
			bundles.AddRange(result.Paths);
		}

		if (bundles.Count > 0)
		{
			await githubActionsService.SetOutputAsync("bundle_path", bundles[0]);
			await githubActionsService.SetOutputAsync("bundle_paths", string.Join('\n', bundles));
			await githubActionsService.SetOutputAsync("bundle_directory", rootOutput.FullName);
		}
		return 0;
	}

	private async Task<(bool Success, IReadOnlyList<string> Paths)> BundleRangeAsync(
		HttpClient http,
		string repository,
		string profile,
		string startRef,
		string endRef,
		string bundleVersion,
		DirectoryInfo rootOutput,
		bool dryRun,
		CancellationToken ctx
	)
	{
		var changelogYaml = await GetRawAsync(http, $"repos/elastic/{repository}/contents/docs/changelog.yml?ref={endRef}", ctx);
		if (changelogYaml is null)
		{
			collector.EmitError(string.Empty, $"Could not read docs/changelog.yml from elastic/{repository} at {endRef}.");
			return (false, []);
		}

		var tempConfig = Path.Join(rootOutput.FullName, $"changelog-config-{repository}-{endRef[..Math.Min(8, endRef.Length)]}.yml");
		await File.WriteAllTextAsync(tempConfig, changelogYaml, ctx);
		try
		{
			var paths = new List<string>();
			var arguments = new BundleChangelogsArguments
			{
				Profile = profile,
				ProfileArgument = bundleVersion,
				OutputDirectory = rootOutput.FullName,
				Config = tempConfig,
				StartGitRef = startRef,
				EndGitRef = endRef,
				DryRun = dryRun,
				OnBundlePathResolved = path => paths.Add(path)
			};

			_logger.LogInformation(
				"Bundling elastic/{Repository} {Start}..{End} with profile {Profile}",
				repository,
				startRef,
				endRef,
				profile
			);
			var bundleService = new ChangelogBundlingService(logFactory, ChangelogFileSystem.FromWorkingDirectory(), configurationContext);
			var success = await bundleService.BundleChangelogs(collector, arguments, ctx);
			return (success, paths);
		}
		finally
		{
			File.Delete(tempConfig);
		}
	}

	private async Task<string?> GetSubmoduleShaAsync(
		HttpClient http,
		string repository,
		string gitRef,
		ServerlessSubmodule submodule,
		CancellationToken ctx
	)
	{
		var tree = await GetRawAsync(http, $"repos/elastic/{repository}/git/trees/{gitRef}", ctx);
		return tree is null ? null : ServerlessPromotion.FindSubmoduleSha(tree, submodule.Path);
	}

	private async Task<string?> ResolvePreviousVersionAsync(
		HttpClient http,
		ServerlessService svc,
		string serviceVersion,
		CancellationToken ctx
	)
	{
		var scanned = 0;
		for (var page = 1; page <= MaxHistoryPages; page++)
		{
			var json = await GetJsonAsync(http, $"repos/{GitOpsRepository}/commits?path={svc.VersionsPath}&per_page=100&page={page}", ctx);
			if (json is null)
				return null;
			using var doc = json;
			var commits = doc.RootElement;
			if (commits.GetArrayLength() == 0)
				break;
			scanned += commits.GetArrayLength();

			foreach (var commit in commits.EnumerateArray())
			{
				var message = commit.GetProperty("commit").GetProperty("message").GetString() ?? string.Empty;
				if (!ServerlessPromotion.IsFinalSlicePromotion(message))
					continue;
				var sha = commit.GetProperty("sha").GetString();
				var content = await GetRawAsync(http, $"repos/{GitOpsRepository}/contents/{svc.VersionsPath}?ref={sha}", ctx);
				var ds5 = content is null ? null : ServerlessPromotion.ParseFinalSliceVersion(content);
				var previous = ServerlessPromotion.FindPreviousVersion([ds5], serviceVersion);
				if (previous is not null)
				{
					_logger.LogInformation("Scanned {Scanned} gitops commits for {Path}", scanned, svc.VersionsPath);
					return previous;
				}
			}
		}
		_logger.LogWarning("Scanned {Scanned} gitops commits for {Path} without finding a previous version", scanned, svc.VersionsPath);
		return null;
	}

	private async Task<JsonDocument?> GetJsonAsync(HttpClient http, string path, CancellationToken ctx)
	{
		using var response = await http.GetAsync(path, ctx);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("GET {Path} returned {Status}", path, (int)response.StatusCode);
			return null;
		}
		await using var stream = await response.Content.ReadAsStreamAsync(ctx);
		return await JsonDocument.ParseAsync(stream, cancellationToken: ctx);
	}

	private async Task<string?> GetRawAsync(HttpClient http, string path, CancellationToken ctx)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw"));
		using var response = await http.SendAsync(request, ctx);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("GET {Path} returned {Status}", path, (int)response.StatusCode);
			return null;
		}
		return await response.Content.ReadAsStringAsync(ctx);
	}
}
