// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Bundling;

/// <summary>A backport that was replaced by its original pull requests.</summary>
public sealed record TracedBackport(int Backport, BackportRoots Roots);

/// <summary>The outcome of tracing the backports of a commit range.</summary>
/// <param name="Resolution">The range with every traced backport replaced by its originals.</param>
/// <param name="Traced">Backports that were replaced.</param>
/// <param name="Unresolved">Backports whose original was not found. They stay in the range as they are.</param>
public sealed record BackportTrace(CommitRangeResolution Resolution, IReadOnlyList<TracedBackport> Traced, IReadOnlyList<int> Unresolved)
{
	public string ToMarkdown()
	{
		if (Traced.Count == 0 && Unresolved.Count == 0)
			return string.Empty;

		var lines = new List<string>
		{
			"### Backports",
			"",
			$"{Traced.Count} backport(s) were replaced by their original pull request, {Unresolved.Count} could not be traced.",
			""
		};
		lines.AddRange(Traced.Select(t => $"- #{t.Backport} → {string.Join(", ", t.Roots.Roots.Select(r => $"#{r}"))} ({t.Roots.Route})"));
		lines.AddRange(
			Traced.Where(t => t.Roots.RoutesDisagree).Select(
				t => $"- #{t.Backport}: the routes that name the original disagree; used the strongest"
			)
		);
		lines.AddRange(Unresolved.Select(n => $"- #{n}: original not found, kept as it is"));
		return string.Join('\n', lines) + "\n";
	}
}

/// <summary>
/// Replaces the backports in a commit range by the pull requests they were made from, so release notes describe
/// a change once, where it was authored. Only pull requests with one of the configured labels are traced.
/// </summary>
public static class BackportTracer
{
	/// <summary>
	/// Traces the labeled pull requests of <paramref name="resolution"/>. Returns <c>null</c> after an error
	/// when GitHub cannot be queried.
	/// </summary>
	public static async Task<BackportTrace?> TraceAsync(
		IDiagnosticsCollector collector,
		IGitHubBackportService service,
		(string Owner, string Repo) repository,
		IReadOnlyCollection<string> labels,
		CommitRangeResolution resolution,
		Cancel ctx
	)
	{
		var backports = resolution.PullRequests.Where(pr => IsBackport(pr, labels)).Select(pr => pr.Number).ToList();
		if (backports.Count == 0)
			return new BackportTrace(resolution, [], []);

		var roots = await service.ResolveRootsAsync(collector, repository.Owner, repository.Repo, backports, ctx).ConfigureAwait(false);
		if (roots == null)
			return null;

		var traced = new List<TracedBackport>();
		var unresolved = new List<int>();
		var replaced = new List<CommitRangePullRequest>();
		foreach (var pr in resolution.PullRequests)
		{
			if (!backports.Contains(pr.Number))
				replaced.Add(pr);
			else if (roots.GetValueOrDefault(pr.Number) is { } found)
			{
				traced.Add(new TracedBackport(pr.Number, found));
				replaced.AddRange(
					found.Roots.Select(
						root => new CommitRangePullRequest
						{
							Number = root,
							Url = $"https://github.com/{repository.Owner}/{repository.Repo}/pull/{root}",
							CommitShas = pr.CommitShas
						}
					)
				);
			}
			else
			{
				unresolved.Add(pr.Number);
				replaced.Add(pr);
			}
		}

		return new BackportTrace(resolution with { PullRequests = Merge(replaced) }, traced, unresolved);
	}

	private static bool IsBackport(CommitRangePullRequest pr, IReadOnlyCollection<string> labels) =>
		pr.Labels.Any(l => labels.Contains(l, StringComparer.OrdinalIgnoreCase)) && !BackportReferences.IsRevert(pr.Title ?? string.Empty);

	/// <summary>Keeps the first appearance of each pull request and collects the commits of all its appearances.</summary>
	private static List<CommitRangePullRequest> Merge(IEnumerable<CommitRangePullRequest> pullRequests)
	{
		var merged = new List<CommitRangePullRequest>();
		var index = new Dictionary<int, int>();
		foreach (var pr in pullRequests)
		{
			if (!index.TryGetValue(pr.Number, out var at))
			{
				index[pr.Number] = merged.Count;
				merged.Add(pr);
			}
			else
				merged[at] = merged[at] with { CommitShas = [.. merged[at].CommitShas.Union(pr.CommitShas)] };
		}

		return merged;
	}
}
