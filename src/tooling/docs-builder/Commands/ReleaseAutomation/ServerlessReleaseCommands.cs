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
	/// service's <c>versions.yaml</c> history in <c>elastic/serverless-gitops</c>. The service's
	/// <c>docs/changelog.yml</c> is read at <paramref name="serviceVersion"/>, and the bundle is built
	/// with its <c>serverless-release</c> profile from the commit range.
	/// </para>
	/// <para>Requires <c>GITHUB_TOKEN</c> with read access to <c>elastic/serverless-gitops</c> and the service repository.</para>
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

		var version = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

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

		var changelogYaml = await GetRawAsync(
			http,
			$"repos/elastic/{svc.Repository}/contents/docs/changelog.yml?ref={serviceVersion}",
			ctx
		);
		if (changelogYaml is null)
		{
			collector.EmitError(string.Empty, $"Could not read docs/changelog.yml from elastic/{svc.Repository} at {serviceVersion}.");
			return 1;
		}

		var rootOutput = outputDir ?? new DirectoryInfo(Path.Join(Directory.GetCurrentDirectory(), "bundles"));
		if (!rootOutput.Exists)
			rootOutput.Create();

		var tempConfig = Path.Join(
			rootOutput.FullName,
			$"changelog-config-{svc.Repository}-{serviceVersion[..Math.Min(8, serviceVersion.Length)]}.yml"
		);
		await File.WriteAllTextAsync(tempConfig, changelogYaml, ctx);
		try
		{
			string? bundlePath = null;
			var arguments = new BundleChangelogsArguments
			{
				Profile = svc.Profile,
				ProfileArgument = version,
				OutputDirectory = rootOutput.FullName,
				Config = tempConfig,
				StartGitRef = startRef,
				EndGitRef = serviceVersion,
				DryRun = dryRun,
				OnBundlePathResolved = path => bundlePath = path
			};

			var bundleService = new ChangelogBundlingService(logFactory, ChangelogFileSystem.FromWorkingDirectory(), configurationContext);
			var success = await bundleService.BundleChangelogs(collector, arguments, ctx);
			if (bundlePath is not null)
				await githubActionsService.SetOutputAsync("bundle_path", bundlePath);
			return success ? 0 : 1;
		}
		finally
		{
			File.Delete(tempConfig);
		}
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
