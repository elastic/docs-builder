// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Headers;
using System.Text.Json;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Caching;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Serverless;

/// <summary>A commit range of a serverless service to bundle.</summary>
/// <param name="Service">The service whose repository, profile and submodules apply.</param>
/// <param name="StartRef">Start of the range, in the service repository.</param>
/// <param name="EndRef">End of the range, in the service repository. Each repository's <c>docs/changelog.yml</c> is read at this ref.</param>
/// <param name="BundleVersion">The version argument of the bundle profile: a date for a promotion, a label for a preview.</param>
/// <param name="OutputDirectory">Directory the bundle files are written to.</param>
public sealed record ServerlessBundleRequest(
	ServerlessService Service,
	string StartRef,
	string EndRef,
	string BundleVersion,
	DirectoryInfo OutputDirectory
)
{
	/// <summary>Resolve the range and report it without writing a bundle.</summary>
	public bool DryRun { get; init; }

	/// <summary>Write a bundle with no entries for a range with no notes, so the release stays on record.</summary>
	public bool WriteEmptyRange { get; init; }

	/// <summary>Called for a range with no notes when <see cref="WriteEmptyRange"/> is off. The range is then a success with no bundle, not an error.</summary>
	public Action? OnEmptyRange { get; init; }

	/// <summary>When set, a pull request without a product or area label is bundled anyway and recorded here, for the preview site to list.</summary>
	public PreviewProblemLog? Problems { get; init; }

	/// <summary>When set, the short form of this ref ends the bundle file names, so promotions on one day do not share a name.</summary>
	public string? ShortRefOf { get; init; }
}

/// <summary>The bundles one <see cref="ServerlessBundleRequest"/> wrote.</summary>
public sealed record ServerlessBundleResult(bool Success, IReadOnlyList<string> Paths);

/// <summary>
/// Bundles the release notes of a serverless service over a commit range: one bundle for the service repository, and one for
/// each submodule over the range of commits the service repository pins at the two refs. Shared by <c>release serverless</c>
/// and the release notes preview.
/// </summary>
public sealed class ServerlessBundler(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	HttpClient http,
	ChangelogCache? cache = null
)
{
	private readonly ILogger _logger = logFactory.CreateLogger<ServerlessBundler>();

	public async Task<ServerlessBundleResult> BundleAsync(ServerlessBundleRequest request, Cancel ctx)
	{
		var svc = request.Service;
		var primary = await BundleRangeAsync(request, svc.Repository, request.StartRef, request.EndRef, ctx);
		if (!primary.Success)
			return primary;

		var bundles = new List<string>(primary.Paths);
		foreach (var submodule in svc.Submodules)
		{
			var startSha = await GetSubmoduleShaAsync(svc.Repository, request.StartRef, submodule, ctx);
			var endSha = await GetSubmoduleShaAsync(svc.Repository, request.EndRef, submodule, ctx);
			if (startSha is null || endSha is null)
			{
				collector.EmitError(
					string.Empty,
					$"Could not read the '{submodule.Path}' submodule commit of elastic/{svc.Repository} at {request.StartRef} or {request.EndRef}."
				);
				return new ServerlessBundleResult(false, bundles);
			}
			if (string.Equals(startSha, endSha, StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogInformation(
					"{Repository} did not change between {Start} and {End}; no bundle.",
					submodule.Repository,
					request.StartRef,
					request.EndRef
				);
				continue;
			}
			_logger.LogInformation("Submodule {Repository}: {Start}..{End}", submodule.Repository, startSha, endSha);
			var result = await BundleRangeAsync(request, submodule.Repository, startSha, endSha, ctx);
			if (!result.Success)
				return new ServerlessBundleResult(false, bundles);
			bundles.AddRange(result.Paths);
		}

		return new ServerlessBundleResult(true, bundles);
	}

	public async Task<JsonDocument?> GetJsonAsync(string path, Cancel ctx)
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

	public async Task<string?> GetRawAsync(string path, Cancel ctx)
	{
		using var message = new HttpRequestMessage(HttpMethod.Get, path);
		message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw"));
		using var response = await http.SendAsync(message, ctx);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("GET {Path} returned {Status}", path, (int)response.StatusCode);
			return null;
		}
		return await response.Content.ReadAsStringAsync(ctx);
	}

	private async Task<ServerlessBundleResult> BundleRangeAsync(
		ServerlessBundleRequest request,
		string repository,
		string startRef,
		string endRef,
		Cancel ctx
	)
	{
		var svc = request.Service;
		var changelogYaml = await GetRawAsync($"repos/elastic/{repository}/contents/docs/changelog.yml?ref={endRef}", ctx);
		if (changelogYaml is null)
		{
			collector.EmitError(string.Empty, $"Could not read docs/changelog.yml from elastic/{repository} at {endRef}.");
			return new ServerlessBundleResult(false, []);
		}

		var output = request.OutputDirectory;
		var tempConfig = Path.Join(output.FullName, $"changelog-config-{repository}-{endRef[..Math.Min(8, endRef.Length)]}.yml");
		await File.WriteAllTextAsync(tempConfig, changelogYaml, ctx);
		try
		{
			var paths = new List<string>();
			var arguments = new BundleChangelogsArguments
			{
				Profile = svc.Profile,
				ProfileArgument = request.BundleVersion,
				OutputDirectory = output.FullName,
				Config = tempConfig,
				StartGitRef = startRef,
				EndGitRef = endRef,
				BaseRef = svc.BaseBranch,
				WriteEmptyRange = request.WriteEmptyRange,
				OnEmptyRange = request.OnEmptyRange,
				TolerateMissingLabels = request.Problems is not null,
				OnPullRequestProblem = request.Problems is null
					? null
					: problem => request.Problems.Add(svc.Product, request.BundleVersion, problem),
				DryRun = request.DryRun,
				OnBundlePathResolved = path => paths.Add(path)
			};

			_logger.LogInformation(
				"Bundling elastic/{Repository} {Start}..{End} with profile {Profile}",
				repository,
				startRef,
				endRef,
				svc.Profile
			);
			var bundleService = new ChangelogBundlingService(
				logFactory,
				ChangelogFileSystem.FromWorkingDirectory(extraRoots: [output.FullName]),
				configurationContext,
				cache: cache
			);
			var success = await bundleService.BundleChangelogs(collector, arguments, ctx);
			if (success && request.ShortRefOf is { } shortRefOf)
				return new ServerlessBundleResult(true, [.. paths.Select(path => AddShortRef(path, shortRefOf))]);
			return new ServerlessBundleResult(success, paths);
		}
		finally
		{
			File.Delete(tempConfig);
		}
	}

	// A bundle is named {repo}-{product}-{date}. The short ref of the promotion keeps two promotions on one day apart.
	private static string AddShortRef(string path, string serviceVersion)
	{
		var target = Path.Join(Path.GetDirectoryName(path), ServerlessPromotion.WithShortRef(Path.GetFileName(path), serviceVersion));
		if (target != path)
			File.Move(path, target, overwrite: true);
		return target;
	}

	private async Task<string?> GetSubmoduleShaAsync(string repository, string gitRef, ServerlessSubmodule submodule, Cancel ctx)
	{
		var tree = await GetRawAsync($"repos/elastic/{repository}/git/trees/{gitRef}", ctx);
		return tree is null ? null : ServerlessPromotion.FindSubmoduleSha(tree, submodule.Path);
	}
}
