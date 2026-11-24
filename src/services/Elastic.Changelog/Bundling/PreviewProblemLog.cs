// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Collects the pull requests of a preview run that lack a label, from every product and release, so the preview site can list
/// them. Handing it to a bundler also makes the bundler tolerate a missing product or area label.
/// </summary>
public sealed partial class PreviewProblemLog
{
	private readonly ConcurrentDictionary<(string Product, string Release, PreviewProblemKind Kind, string Url), (string Product, string Release, GitRangePrProblem Problem)> _problems = new();

	[GeneratedRegex(@"^https?://github\.com/(?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+)/(?:pull|issues)/\d+", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex PullRequestUrlRegex();

	/// <summary>The number of pull requests with a problem. One that lacks two labels, or is in two releases, counts once.</summary>
	public int PullRequestCount => _problems.Values.Select(v => v.Problem.Url).Distinct(StringComparer.Ordinal).Count();

	/// <summary>The number of records: a pull request counts once per kind of problem and release.</summary>
	public int Count => _problems.Count;

	/// <summary>Records a problem of <paramref name="product"/> at <paramref name="release"/>. A pull request is recorded once per kind and release.</summary>
	public void Add(string product, string release, GitRangePrProblem problem) =>
		_problems[(product, release, problem.Kind, problem.Url)] = (product, release, problem);

	/// <summary>
	/// The problems in a stable order. A pull request of a repository that is not in <paramref name="allowRepos"/>, or whose
	/// repository cannot be read from its URL, is named by number only: its link and title are dropped here, so they never reach a file.
	/// </summary>
	public IReadOnlyList<PreviewProblem> ToProblems(IReadOnlyList<string> allowRepos)
	{
		var allow = new HashSet<string>(allowRepos, StringComparer.OrdinalIgnoreCase);
		return [
			.. _problems
				.Values
				.Select(entry => ToProblem(entry, allow))
				.OrderBy(p => p.Product, StringComparer.Ordinal)
				.ThenBy(p => p.Release, StringComparer.Ordinal)
				.ThenBy(p => p.Kind)
				.ThenBy(p => p.Owner, StringComparer.Ordinal)
				.ThenBy(p => p.Repo, StringComparer.Ordinal)
				.ThenBy(p => p.Number)
		];
	}

	private static PreviewProblem ToProblem((string Product, string Release, GitRangePrProblem Problem) entry, HashSet<string> allow)
	{
		var (product, release, problem) = entry;
		var match = PullRequestUrlRegex().Match(problem.Url);
		var owner = match.Success ? match.Groups["owner"].Value : string.Empty;
		var repo = match.Success ? match.Groups["repo"].Value : string.Empty;
		var isPublic = match.Success && allow.Contains($"{owner}/{repo}");
		return new PreviewProblem
		{
			Product = product,
			Release = release,
			Kind = problem.Kind,
			Owner = owner,
			Repo = repo,
			Number = problem.Number,
			Url = isPublic ? problem.Url : null,
			Title = isPublic ? problem.Title : null,
			Labels = problem.Labels
		};
	}
}
