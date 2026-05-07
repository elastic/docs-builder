// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Parses GitHub.com remote URLs into owner and repository name (public API for changelog tooling).
/// </summary>
public static class GitHubRemoteParser
{
	/// <summary>
	/// Parses an HTTPS or SSH URL for github.com into <paramref name="owner"/> and <paramref name="repo"/>.
	/// Other hosts are rejected.
	/// </summary>
	public static bool TryParseGitHubComOwnerRepo(string? url, [NotNullWhen(true)] out string? owner, [NotNullWhen(true)] out string? repo)
	{
		owner = null;
		repo = null;
		if (string.IsNullOrWhiteSpace(url))
			return false;

		var trimmed = url.Trim();

		if (trimmed.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
		{
			var rest = trimmed["git@github.com:".Length..];
			return TrySplitOwnerRepoPath(rest, out owner, out repo);
		}

		if (trimmed.StartsWith("ssh://git@github.com/", StringComparison.OrdinalIgnoreCase))
		{
			var rest = trimmed["ssh://git@github.com/".Length..];
			return TrySplitOwnerRepoPath(rest, out owner, out repo);
		}

		if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
			return false;

		if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
			return false;

		var path = uri.AbsolutePath.Trim('/');
		return TrySplitOwnerRepoPath(path, out owner, out repo);
	}

	private static bool TrySplitOwnerRepoPath(string path, [NotNullWhen(true)] out string? owner, [NotNullWhen(true)] out string? repo)
	{
		owner = null;
		repo = null;
		if (string.IsNullOrWhiteSpace(path))
			return false;

		path = path.TrimEnd('/', ' ');
		if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
			path = path[..^4];

		var slash = path.IndexOf('/');
		if (slash <= 0 || slash >= path.Length - 1)
			return false;

		var o = path[..slash];
		var r = path[(slash + 1)..];
		if (string.IsNullOrEmpty(o) || string.IsNullOrEmpty(r) || r.Contains('/'))
			return false;

		owner = o;
		repo = r;
		return true;
	}
}
