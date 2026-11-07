// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;

namespace Elastic.Changelog.GitHub;

/// <summary>Reads the issues that the body of a pull request says it fixes.</summary>
internal static class LinkedIssueExtractor
{
	/// <summary>
	/// Extracts linked issues from PR body.
	/// Matches patterns like "Fixes #123", "Closes #456", "Resolves #789", "Fixes owner/repo#123"
	/// </summary>
	public static IReadOnlyList<string> Extract(string body, string prOwner, string prRepo)
	{
		if (string.IsNullOrWhiteSpace(body))
			return [];

		var issues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Pattern for keywords followed by issue references
		// Matches: Fixes #123, Closes owner/repo#456, Resolves https://github.com/owner/repo/issues/789
		var keywordPattern = @"(?:fix(?:es|ed)?|close[sd]?|resolve[sd]?)\s+";

		// Pattern for cross-repo issue: owner/repo#123
		var crossRepoPattern = $@"{keywordPattern}([a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+)#(\d+)";
		foreach (Match match in Regex.Matches(body, crossRepoPattern, RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeout))
		{
			var repoPath = match.Groups[1].Value;
			var issueNum = match.Groups[2].Value;
			_ = issues.Add($"https://github.com/{repoPath}/issues/{issueNum}");
		}

		// Pattern for full GitHub issue URL
		var urlPattern = $@"{keywordPattern}(https://github\.com/([a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+)/issues/(\d+))";
		foreach (Match match in Regex.Matches(body, urlPattern, RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeout))
			_ = issues.Add(match.Groups[1].Value);

		// Pattern for same-repo issue: #123
		var sameRepoPattern = $@"{keywordPattern}#(\d+)";
		foreach (Match match in Regex.Matches(body, sameRepoPattern, RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeout))
		{
			// Skip if this is part of a cross-repo reference (already handled above)
			var issueNum = match.Groups[1].Value;
			var fullMatch = match.Value;
			if (!fullMatch.Contains('/'))
				_ = issues.Add($"https://github.com/{prOwner}/{prRepo}/issues/{issueNum}");
		}

		return [.. issues];
	}
}
