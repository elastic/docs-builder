// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Reads <c>remote.origin.url</c> from a local Git checkout using the file system (no subprocess).
/// </summary>
public static class GitRemoteConfigurationReader
{
	/// <summary>
	/// Reads <c>.git/config</c>, or the <c>config</c> file referenced by a <c>.git</c> worktree pointer file.
	/// </summary>
	public static bool TryReadOriginUrl(IFileSystem fileSystem, string repositoryRoot, [NotNullWhen(true)] out string? url)
	{
		url = null;
		var gitPath = fileSystem.Path.Combine(repositoryRoot, ".git");
		if (fileSystem.Directory.Exists(gitPath))
		{
			var configPath = fileSystem.Path.Combine(gitPath, "config");
			return TryReadOriginUrlFromConfigPath(fileSystem, configPath, out url);
		}

		if (!fileSystem.File.Exists(gitPath))
			return false;

		var gitFileText = fileSystem.File.ReadAllText(gitPath);
		var firstLineBreak = gitFileText.IndexOfAny(['\r', '\n']);
		var firstLine = firstLineBreak >= 0 ? gitFileText[..firstLineBreak] : gitFileText;
		firstLine = firstLine.Trim();
		if (!firstLine.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase))
			return false;

		var gitDir = firstLine["gitdir:".Length..].Trim();
		if (string.IsNullOrEmpty(gitDir))
			return false;

		var resolvedGitDir = fileSystem.Path.IsPathFullyQualified(gitDir)
			? gitDir
			: fileSystem.Path.GetFullPath(fileSystem.Path.Combine(repositoryRoot, gitDir));

		var worktreeConfigPath = fileSystem.Path.Combine(resolvedGitDir, "config");
		return TryReadOriginUrlFromConfigPath(fileSystem, worktreeConfigPath, out url);
	}

	private static bool TryReadOriginUrlFromConfigPath(IFileSystem fileSystem, string configPath, [NotNullWhen(true)] out string? url)
	{
		url = null;
		if (!fileSystem.File.Exists(configPath))
			return false;

		var content = fileSystem.File.ReadAllText(configPath);
		return GitConfigOriginParser.TryGetRemoteOriginUrl(content, out url);
	}
}
