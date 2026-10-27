// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.GitHub;

/// <summary>Reads each pull request with one REST request, through <see cref="IGitHubPrService"/>.</summary>
public sealed class RestPullRequestSummaryProvider(IGitHubPrService prService) : IPullRequestSummaryProvider
{
	private const int MaxParallel = 6;

	public async Task<IReadOnlyDictionary<int, PullRequestSummary?>?> GetAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		IReadOnlyCollection<CommitRangePullRequest> pullRequests,
		Cancel ctx
	)
	{
		var distinct = pullRequests.Select(pr => pr.Number).Distinct().ToList();
		var summaries = new PullRequestSummary?[distinct.Count];
		using var gate = new SemaphoreSlim(MaxParallel);
		await Task.WhenAll(distinct.Select(async (number, index) =>
		{
			await gate.WaitAsync(ctx).ConfigureAwait(false);
			try
			{
				var url = string.Create(CultureInfo.InvariantCulture, $"https://github.com/{owner}/{repo}/pull/{number}");
				var info = await prService.FetchPrInfoAsync(url, owner, repo, ctx).ConfigureAwait(false);
				summaries[index] = info is null
					? null
					: new PullRequestSummary(
						number,
						info.Title,
						info.Labels,
						ReleaseNotesExtractor.ExtractReleaseNote(info.Body),
						info.LinkedIssues,
						ReleaseNotesExtractor.FindImpact(info.Body),
						ReleaseNotesExtractor.FindAction(info.Body)
					);
			}
			finally
			{
				_ = gate.Release();
			}
		})).ConfigureAwait(false);

		return distinct.Select((number, index) => (number, summary: summaries[index])).ToDictionary(x => x.number, x => x.summary);
	}
}
