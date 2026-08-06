// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.AllowlistIdentity;

public record ResolveScrubberAllowlistArguments
{
	/// <summary>GitHub owner of the repository whose releases carry the identity asset.</summary>
	public string Owner { get; init; } = "elastic";

	/// <summary>GitHub repository whose releases carry the identity asset.</summary>
	public string Repo { get; init; } = "docs-builder";

	/// <summary>
	/// Release tag to resolve the identity from. When null, the newest release carrying the
	/// identity asset wins — that is the most recent deploy that passed the gated pipeline.
	/// </summary>
	public string? Tag { get; init; }

	/// <summary>Optional path to a local <c>assembler.yml</c> to compare against the deployed identity.</summary>
	public string? AssemblerPath { get; init; }
}

/// <summary>The deployed identity together with where it was found and how it compares to the local checkout.</summary>
public record ResolvedScrubberAllowlist
{
	/// <summary>The deployed allowlist identity document.</summary>
	public required ScrubberAllowlistIdentity Identity { get; init; }

	/// <summary>The release tag the identity asset was found on.</summary>
	public required string ReleaseTag { get; init; }

	/// <summary>Hash of the local <c>assembler.yml</c>, when a local path was given. Same format as the identity hash.</summary>
	public string? LocalSha256 { get; init; }

	/// <summary>Whether the local allowlist matches the deployed one; null when no local path was given.</summary>
	public bool? MatchesLocal => LocalSha256 is null ? null : string.Equals(LocalSha256, Identity.AllowlistSha256, StringComparison.Ordinal);
}

/// <summary>
/// Resolves which link allowlist the deployed changelog scrubber is actually running with, from the
/// identity asset the release pipeline attaches after each successful scrubber deploy. Backfill
/// planning pins this identity in every plan and ledger so "which links survive publication" is
/// always answered against the deployed allowlist, never against the local checkout.
/// </summary>
public class ScrubberAllowlistIdentityService(
	ILoggerFactory logFactory,
	IGitHubReleaseService releaseService,
	IFileSystem fileSystem
) : IService
{
	/// <summary>How many releases back to look for the identity asset when no tag is given.</summary>
	private const int ReleaseLookback = 20;

	private readonly ILogger _logger = logFactory.CreateLogger<ScrubberAllowlistIdentityService>();

	/// <summary>
	/// Resolves the deployed allowlist identity. Returns null after emitting errors when no
	/// identity can be resolved — an unresolvable identity must block plan approval, not degrade
	/// into a guess.
	/// </summary>
	public async Task<ResolvedScrubberAllowlist?> ResolveDeployedAsync(
		IDiagnosticsCollector collector,
		ResolveScrubberAllowlistArguments args,
		Cancel ctx = default)
	{
		var located = await LocateIdentityAssetAsync(collector, args, ctx);
		if (located is null)
			return null;

		var (release, asset) = located.Value;
		var json = await releaseService.DownloadAssetTextAsync(asset, ctx);
		if (json is null)
		{
			collector.EmitError(string.Empty,
				$"Failed to download release asset '{asset.Name}' from {args.Owner}/{args.Repo}@{release.TagName}.");
			return null;
		}

		if (!ScrubberAllowlistIdentity.TryParse(json, out var identity, out var problems))
		{
			foreach (var problem in problems)
				collector.EmitError(string.Empty, $"Invalid allowlist identity on {args.Owner}/{args.Repo}@{release.TagName}: {problem}");
			return null;
		}

		var localSha = ComputeLocalSha256(collector, args.AssemblerPath);
		var resolved = new ResolvedScrubberAllowlist
		{
			Identity = identity,
			ReleaseTag = release.TagName,
			LocalSha256 = localSha
		};

		_logger.LogInformation("Deployed scrubber allowlist: {Sha256} (commit {Commit}, release {Tag})",
			identity.AllowlistSha256, identity.DeploymentCommit, release.TagName);

		if (resolved.MatchesLocal == false)
		{
			collector.EmitWarning(string.Empty,
				$"Local assembler.yml ({localSha}) differs from the deployed scrubber allowlist ({identity.AllowlistSha256}, release {release.TagName}). " +
				"Links must be validated against the deployed allowlist, not the local checkout.");
		}
		else if (resolved.MatchesLocal == true)
		{
			_logger.LogInformation("Local assembler.yml matches the deployed allowlist");
		}

		return resolved;
	}

	private async Task<(GitHubReleaseInfo Release, GitHubReleaseAsset Asset)?> LocateIdentityAssetAsync(
		IDiagnosticsCollector collector,
		ResolveScrubberAllowlistArguments args,
		Cancel ctx)
	{
		if (!string.IsNullOrWhiteSpace(args.Tag))
		{
			var release = await releaseService.FetchReleaseAsync(args.Owner, args.Repo, args.Tag, ctx);
			if (release is null)
			{
				collector.EmitError(string.Empty,
					$"Release '{args.Tag}' was not found on {args.Owner}/{args.Repo}. Ensure the tag exists and credentials are set.");
				return null;
			}

			var asset = FindIdentityAsset(release);
			if (asset is null)
			{
				collector.EmitError(string.Empty,
					$"Release {args.Owner}/{args.Repo}@{release.TagName} does not carry the '{ScrubberAllowlistIdentity.AssetName}' asset: " +
					"either the release predates allowlist identity publication, or its scrubber deploy never completed.");
				return null;
			}

			return (release, asset);
		}

		var releases = await releaseService.FetchReleasesAsync(args.Owner, args.Repo, ReleaseLookback, ctx);
		foreach (var release in releases.Where(r => !r.Draft))
		{
			var asset = FindIdentityAsset(release);
			if (asset is not null)
				return (release, asset);
			_logger.LogDebug("Release {Tag} has no allowlist identity asset; looking further back", release.TagName);
		}

		collector.EmitError(string.Empty,
			$"No release among the latest {ReleaseLookback} on {args.Owner}/{args.Repo} carries the '{ScrubberAllowlistIdentity.AssetName}' asset, " +
			"so the deployed scrubber allowlist identity cannot be resolved. A backfill plan cannot be approved without it.");
		return null;
	}

	private static GitHubReleaseAsset? FindIdentityAsset(GitHubReleaseInfo release) =>
		release.Assets.FirstOrDefault(a => string.Equals(a.Name, ScrubberAllowlistIdentity.AssetName, StringComparison.Ordinal));

	private string? ComputeLocalSha256(IDiagnosticsCollector collector, string? assemblerPath)
	{
		if (string.IsNullOrWhiteSpace(assemblerPath))
			return null;

		if (!fileSystem.File.Exists(assemblerPath))
		{
			collector.EmitWarning(string.Empty, $"Local assembler.yml not found at '{assemblerPath}'; skipping the local comparison.");
			return null;
		}

		using var stream = fileSystem.File.OpenRead(assemblerPath);
		return ScrubberAllowlistIdentity.ComputeSha256(stream);
	}
}
