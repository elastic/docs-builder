// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.RegularExpressions;
using Elastic.Documentation;

namespace Elastic.Changelog.GitHub;

/// <summary>The places a backport pull request can name the pull request it was backported from, before any is checked.</summary>
/// <param name="CherryPickShas">Commits named by <c>(cherry picked from commit &lt;sha&gt;)</c> trailers.</param>
/// <param name="SubjectRefs">Pull request numbers in the subjects of the pull request's commits.</param>
/// <param name="SubjectGroups">For each commit whose subject ends with <c>(#N)</c>, the numbers in that ending.</param>
/// <param name="TitleRefs">Pull request numbers in the pull request title.</param>
/// <param name="BodyRefs">Pull request numbers in links and markers in the pull request body.</param>
public sealed record BackportCandidates(
	IReadOnlyList<string> CherryPickShas,
	IReadOnlyList<int> SubjectRefs,
	IReadOnlyList<int> TitleRefs,
	IReadOnlyList<int> BodyRefs,
	IReadOnlyList<IReadOnlyList<int>> SubjectGroups
);

/// <summary>How the original pull request was found.</summary>
public enum BackportRootRoute
{
	CherryPickTrailer,
	CommitSubjectAndTitle,
	CommitSubject,
	Title,
	Body
}

/// <summary>The originals of one backport, how they were found, and whether the stronger routes disagreed.</summary>
public sealed record BackportRoots(IReadOnlyList<int> Roots, BackportRootRoute Route, bool RoutesDisagree);

/// <summary>
/// Finds the pull request a backport was made from. Each route is a different convention: Elastic repositories
/// write the original in the pull request title (<c>(#N)</c> or <c>(backport #N)</c>), in the subject of the
/// cherry-picked commits, in the body, or in a <c>cherry picked from commit</c> trailer. The functions here only
/// read text. A number counts as an original only after it is checked to be a merged pull request on the default branch.
/// </summary>
public static partial class BackportReferences
{
	// A pull request reference in the same repository. Not "owner/repo#N", not "word#N".
	[GeneratedRegex(@"(?<![\w/\-])#(\d+)", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex SameRepoReferenceRegex();

	// The "(#N)" that GitHub appends to a squashed commit. A cherry-picked squash can end with several, the original
	// first and the release-branch backports after it. A bare "#N" in a subject only mentions a pull request.
	[GeneratedRegex(@"(?:\(#\d+\)\s*)+$", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex TrailingReferenceRegex();

	[GeneratedRegex(@"cherry picked from commit ([0-9a-fA-F]{40})", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex CherryPickRegex();

	[GeneratedRegex(@"(?:backport(?:s|ed)?\s+of(?:\s+pull\s+request)?|automatic\s+backport\s+of\s+pull\s+request)\s*#(\d+)", RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex BodyMarkerRegex();

	/// <summary>Reads every route from the text of a pull request. Nothing here is checked against GitHub.</summary>
	public static BackportCandidates Extract(
		string owner,
		string repo,
		int number,
		string title,
		string? body,
		IEnumerable<string> commitMessages
	)
	{
		var messages = commitMessages.ToList();
		var shas = messages
			.SelectMany(m => CherryPickRegex().Matches(m).Select(x => x.Groups[1].Value.ToLowerInvariant()))
			.Distinct()
			.ToList();
		var subjects = messages.Select(m => m.Split('\n', 2)[0]);

		return new BackportCandidates(
			shas,
			Numbers(string.Join('\n', subjects), number),
			Numbers(title, number),
			BodyNumbers(owner, repo, number, body ?? string.Empty),
			[.. subjects.Select(subject => Numbers(TrailingReference(subject), number)).Where(group => group.Count > 0)]
		);
	}

	/// <summary>
	/// Chooses the originals from the numbers that passed the check. A trailer wins, because GitHub itself names
	/// the commit's pull request. Next comes what the commit subjects and the title agree on, then the subjects,
	/// the title, and the body. <see cref="BackportRoots.RoutesDisagree"/> is set when the trailer, the subjects and the
	/// title name different originals, so the run report can show it.
	/// </summary>
	public static BackportRoots? Choose(
		IReadOnlyCollection<int> trailer,
		IReadOnlyCollection<int> subject,
		IReadOnlyCollection<int> title,
		IReadOnlyCollection<int> body,
		IReadOnlyList<IReadOnlyList<int>>? subjectGroups = null
	)
	{
		var strong = new[] { trailer, subject, title }.Where(s => s.Count > 0).Select(s => s.Order().ToArray()).ToList();
		var disagree = strong.Count > 1 && strong.Skip(1).Any(s => !s.SequenceEqual(strong[0]));

		if (trailer.Count > 0)
			return new BackportRoots([.. trailer.Order()], BackportRootRoute.CherryPickTrailer, disagree);

		var both = subject.Intersect(title).Order().ToArray();
		if (both.Length > 0)
			return new BackportRoots(CoverEveryCommit(both, subjectGroups), BackportRootRoute.CommitSubjectAndTitle, disagree);
		if (subject.Count > 0)
			return new BackportRoots([.. subject.Order()], BackportRootRoute.CommitSubject, disagree);
		if (title.Count > 0)
			return new BackportRoots([.. title.Order()], BackportRootRoute.Title, disagree);
		if (body.Count > 0)
			return new BackportRoots([.. body.Order()], BackportRootRoute.Body, disagree);

		return null;
	}

	/// <summary>
	/// The title can name fewer pull requests than the backport holds. Every commit whose subject names none of the
	/// agreed originals still contributes the pull requests its subject ends with, so no cherry-picked change is dropped.
	/// A subject that only mentions another pull request ("Follow up on #10") is not a change of its own.
	/// </summary>
	private static int[] CoverEveryCommit(int[] agreed, IReadOnlyList<IReadOnlyList<int>>? subjectGroups) =>
		subjectGroups == null
			? agreed
			: [.. agreed.Concat(subjectGroups.Where(g => !g.Intersect(agreed).Any()).SelectMany(g => g)).Distinct().Order()];

	/// <summary>The part of a subject that names the pull request the commit was squashed from, or nothing.</summary>
	private static string TrailingReference(string subject) =>
		TrailingReferenceRegex().Match(subject) is { Success: true } m ? m.Value : string.Empty;

	/// <summary>A digit string too long for a pull request number is not a reference, so it is skipped.</summary>
	private static IEnumerable<int> ParsedNumbers(IEnumerable<Match> matches) =>
		matches.Select(m => int.TryParse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0).Where(
			n => n > 0
		);

	private static IReadOnlyList<int> Numbers(string text, int own) =>
		[.. ParsedNumbers(SameRepoReferenceRegex().Matches(text)).Where(n => n != own).Distinct()];

	private static IReadOnlyList<int> BodyNumbers(string owner, string repo, int own, string body)
	{
		var found = new List<int>();

		var link = new Regex(
			@$"github\.com/{Regex.Escape(owner)}/{Regex.Escape(repo)}/pull/(\d+)",
			RegexOptions.IgnoreCase | RegexOptions.NonBacktracking,
			RegexTimeouts.MatchTimeout
		);
		found.AddRange(ParsedNumbers(link.Matches(body)));
		found.AddRange(ParsedNumbers(BodyMarkerRegex().Matches(body)));

		// "Backports the following commits to 9.5: - title (#N)". List items only count when the body talks about a backport,
		// so an ordinary bullet list that mentions issue numbers is left alone.
		if (body.Contains("backport", StringComparison.OrdinalIgnoreCase))
		{
			foreach (var line in body.Split('\n').Where(l => l.TrimStart().StartsWith('-') || l.TrimStart().StartsWith('*')))
				found.AddRange(Numbers(line, own));
		}

		return [.. found.Where(n => n != own).Distinct()];
	}
}
