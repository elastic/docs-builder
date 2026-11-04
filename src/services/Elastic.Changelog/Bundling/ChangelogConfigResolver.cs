// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Bundling;

/// <summary>The outcome of looking up a product's <c>changelog.yml</c>.</summary>
/// <param name="Content">The YAML, or <see langword="null"/> when the repository has no file or the lookup failed.</param>
/// <param name="Failed">The lookup failed (outage, unexpected status). The error is already reported.</param>
public sealed record ChangelogConfigResult(string? Content, bool Failed);

/// <summary>
/// Finds the <c>changelog.yml</c> a product bundles with: a local prototype file first, then the file in the
/// product's repository at the manifest commit. A missing file (404) is told apart from a failed request,
/// so an outage is never reported as "no changelog.yml".
/// </summary>
public class ChangelogConfigResolver(HttpClient http, IFileSystem fileSystem, ILogger logger, IDiagnosticsCollector collector)
{
	private sealed record RepositoryFetch(string? Content, string? Url, string? Failure);

	public async Task<ChangelogConfigResult> ResolveAsync(
		string productId,
		string repoKey,
		string commitHash,
		string? prototypeDirectory,
		Cancel ctx
	)
	{
		var prototypePath = prototypeDirectory is null ? null : fileSystem.Path.Join(prototypeDirectory, $"{productId}.changelog.yml");
		if (prototypePath is not null && fileSystem.File.Exists(prototypePath))
			return await ResolvePrototypeAsync(productId, repoKey, commitHash, prototypePath, ctx);

		var repository = await FetchFromRepositoryAsync(repoKey, commitHash, ctx);
		if (repository.Failure is not null)
		{
			ReportFailure(productId, repoKey, commitHash, repository.Failure);
			return new ChangelogConfigResult(null, true);
		}

		return new ChangelogConfigResult(repository.Content, false);
	}

	private async Task<ChangelogConfigResult> ResolvePrototypeAsync(
		string productId,
		string repoKey,
		string commitHash,
		string prototypePath,
		Cancel ctx
	)
	{
		logger.LogInformation("Using prototype changelog config for '{Product}': {Path}", productId, prototypePath);
		var content = await fileSystem.File.ReadAllTextAsync(prototypePath, ctx);

		// A prototype wins over the repository file. Say so when both exist, because the repository
		// file is then ignored without a trace. A failed probe is not an error: the prototype is enough.
		var repository = await FetchFromRepositoryAsync(repoKey, commitHash, ctx);
		if (repository.Content is not null)
		{
			collector.EmitWarning(
				prototypePath,
				$"The prototype for '{productId}' shadows the changelog.yml in elastic/{repoKey}@{Short(commitHash)} ({repository.Url}). " +
					"Remove the prototype to bundle with the repository file."
			);
		}
		else if (repository.Failure is not null)
			logger.LogDebug(
				"Could not check for a repository changelog.yml next to the prototype for '{Product}': {Failure}",
				productId,
				repository.Failure
			);

		return new ChangelogConfigResult(content, false);
	}

	private async Task<RepositoryFetch> FetchFromRepositoryAsync(string repoKey, string commitHash, Cancel ctx)
	{
		string[] candidates =
		[
			$"https://raw.githubusercontent.com/elastic/{repoKey}/{commitHash}/docs/changelog.yml",
			$"https://raw.githubusercontent.com/elastic/{repoKey}/{commitHash}/changelog.yml"
		];

		foreach (var url in candidates)
		{
			try
			{
				using var response = await http.GetAsync(url, ctx);
				if (response.StatusCode == HttpStatusCode.NotFound)
					continue;
				if (!response.IsSuccessStatusCode)
					return new RepositoryFetch(null, url, $"Request to {url} returned {(int)response.StatusCode}.");
				return new RepositoryFetch(await response.Content.ReadAsStringAsync(ctx), url, null);
			}
			catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException && !ctx.IsCancellationRequested)
			{
				return new RepositoryFetch(null, url, $"Could not read {url}: {ex.Message}");
			}
		}

		return new RepositoryFetch(null, null, null);
	}

	private void ReportFailure(string productId, string repoKey, string commitHash, string failure)
	{
		var message = $"Could not fetch the changelog.yml for '{productId}' (repo: elastic/{repoKey}@{Short(commitHash)}). {failure}";
		logger.LogError("{Message}", message);
		collector.EmitError(string.Empty, message);
	}

	private static string Short(string commitHash) => commitHash.Length > 8 ? commitHash[..8] : commitHash;
}
