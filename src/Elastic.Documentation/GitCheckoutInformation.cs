// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elastic.Documentation.Extensions;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;
using SoftCircuits.IniFileParser;

namespace Elastic.Documentation;

public partial record GitCheckoutInformation
{
	public static GitCheckoutInformation Unavailable { get; } = new()
	{
		Branch = "unavailable",
		Remote = "unavailable",
		Ref = "unavailable",
		RepositoryName = "unavailable"
	};

	[JsonPropertyName("branch")]
	public required string Branch { get; init; }

	[JsonPropertyName("remote")]
	public required string Remote { get; init; }

	[JsonPropertyName("ref")]
	public required string Ref { get; init; }

	[JsonPropertyName("name")]
	public string RepositoryName { get; init; } = "unavailable";

	/// <summary>
	/// The full git ref from GitHub Actions (e.g. refs/pull/123/merge). Set from GITHUB_REF when running in CI.
	/// </summary>
	[JsonPropertyName("github_ref")]
	public string? GitHubRef { get; init; }

	/// <summary>
	/// The GitHub repository in <c>org/repo</c> format, derived from the git remote URL.
	/// Falls back to <c>elastic/docs-builder</c> when either <see cref="Remote"/> or <see cref="RepositoryName"/> is unavailable,
	/// or when the remote does not resolve to a valid GitHub <c>org/repo</c> path,
	/// to avoid silently linking to an arbitrary repository.
	/// </summary>
	[JsonIgnore]
	public string GitHubRepository => ExtractGitHubOrgRepo(Remote) ?? "elastic/docs-builder";

	/// <summary>Extracts a validated <c>org/repo</c> path from a GitHub remote URL, or returns <c>null</c>.</summary>
	/// <remarks>
	/// Handles the common remote formats:
	/// <list type="bullet">
	///   <item><c>https://github.com/org/repo.git</c></item>
	///   <item><c>git@github.com:org/repo.git</c></item>
	///   <item><c>ssh://git@github.com/org/repo.git</c></item>
	///   <item><c>org/repo</c> (bare, e.g. from <c>GITHUB_REPOSITORY</c>)</item>
	/// </list>
	/// </remarks>
	private static string? ExtractGitHubOrgRepo(string? remote)
	{
		if (string.IsNullOrEmpty(remote) || remote == "unavailable")
			return null;

		var path = NormalizeToGitHubPath(remote);
		if (path is null)
			return null;

		// Strip trailing .git
		if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
			path = path[..^4];

		// Validate: must be exactly org/repo — two non-empty segments, no extra slashes
		var parts = path.Split('/');
		if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
			return null;

		return path;
	}

	/// <summary>Normalises the remote to the <c>org/repo[.git]</c> path portion, or <c>null</c> if not a GitHub remote.</summary>
	private static string? NormalizeToGitHubPath(string remote)
	{
		const string githubHost = "github.com";

		// git@github.com:org/repo.git  →  org/repo.git
		if (remote.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
			return remote["git@github.com:".Length..].TrimStart('/');

		// ssh://git@github.com/org/repo.git  →  org/repo.git
		if (remote.StartsWith("ssh://git@github.com/", StringComparison.OrdinalIgnoreCase))
			return remote["ssh://git@github.com/".Length..].TrimStart('/');

		// https://github.com/org/repo.git  or  http://github.com/org/repo.git  →  org/repo.git
		if (remote.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
			return remote["https://github.com/".Length..].TrimStart('/');
		if (remote.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
			return remote["http://github.com/".Length..].TrimStart('/');

		// Bare org/repo (e.g. GITHUB_REPOSITORY env var) — must not contain "://" or "@" (i.e. not a URL)
		if (!remote.Contains("://") && !remote.Contains('@') && !remote.Contains(githubHost, StringComparison.OrdinalIgnoreCase))
			return remote.TrimStart('/');

		return null;
	}

	// manual read because libgit2sharp is not yet AOT ready
	public static GitCheckoutInformation Create(IDirectoryInfo? source, IFileSystem fileSystem, ILogger? logger = null)
	{
		if (source is null)
			return Unavailable;

		// Return test data for in-memory (mock) file systems. Use ScopedFileSystem.InnerType
		// (available since Nullean.ScopedFileSystem 0.4.0) to inspect through the scope wrapper
		// rather than relying on the outer type name.
		var fsType = fileSystem is ScopedFileSystem sf ? sf.InnerType : fileSystem.GetType();
		if (fsType.Name.Contains("Mock", StringComparison.OrdinalIgnoreCase))
		{
			return new GitCheckoutInformation
			{
				Branch = $"test-e35fcb27-5f60-4e",
				Remote = "elastic/docs-builder",
				Ref = "e35fcb27-5f60-4e",
				RepositoryName = "docs-builder"
			};
		}
		var fakeRef = Guid.NewGuid().ToString()[..16];

		var gitDir = GitDir(source, ".git");
		if (!gitDir.Exists)
		{
			// try a worktree .git file
			var worktreeFile = Git(source, ".git");
			if (!worktreeFile.Exists)
				return Unavailable;
			var workTreePath = Read(source, ".git")?.Replace("gitdir: ", string.Empty);
			if (workTreePath is null)
				return Unavailable;
			//TODO read branch info from worktree do not fall through
			gitDir = fileSystem.DirectoryInfo.New(workTreePath).GetParent(".git");
			if (gitDir is null || !gitDir.Exists)
				return Unavailable;
		}

		var gitConfig = Git(gitDir, "config");
		if (!gitConfig.Exists)
		{
			logger?.LogInformation("Git checkout information not available.");
			return Unavailable;
		}

		var head = Read(gitDir, "HEAD") ?? fakeRef;
		var gitRef = head;
		var branch = head.Replace("refs/heads/", string.Empty);
		//not detached HEAD
		if (head.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
		{
			head = head.Replace("ref: ", string.Empty);
			gitRef = Read(gitDir, head) ?? fakeRef;
			branch = branch.Replace("ref: ", string.Empty);
		}
		else
			branch = Environment.GetEnvironmentVariable("GITHUB_PR_REF_NAME") ?? Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ?? "detached/head";

		var ini = new IniFile();
		using var stream = gitConfig.OpenRead();
		using var streamReader = new StreamReader(stream);
		ini.Load(streamReader);

		var remote = BranchTrackingRemote(branch, ini);
		logger?.LogInformation("Remote from branch: {GitRemote}", remote);
		if (string.IsNullOrEmpty(remote))
		{
			remote = BranchTrackingRemote("main", ini);
			logger?.LogInformation("Remote from main branch: {GitRemote}", remote);
		}

		if (string.IsNullOrEmpty(remote))
		{
			remote = BranchTrackingRemote("master", ini);
			logger?.LogInformation("Remote from master branch: {GitRemote}", remote);
		}

		if (string.IsNullOrEmpty(remote))
		{
			remote = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
			logger?.LogInformation("Remote from GITHUB_REPOSITORY: {GitRemote}", remote);
		}

		if (string.IsNullOrEmpty(remote))
		{
			remote = "elastic/docs-builder-unknown";
			logger?.LogInformation("Remote from fallback: {GitRemote}", remote);
		}
		remote = CutOffGitExtension().Replace(remote, string.Empty);

		var githubRef = Environment.GetEnvironmentVariable("GITHUB_REF");
		var info = new GitCheckoutInformation
		{
			Ref = gitRef,
			Branch = branch,
			Remote = remote,
			RepositoryName = remote.Split('/').Last(),
			GitHubRef = string.IsNullOrEmpty(githubRef) ? null : githubRef
		};

		logger?.LogInformation("-> Remote Name: {GitRemote}", info.Remote);
		logger?.LogInformation("-> Repository Name: {RepositoryName}", info.RepositoryName);
		return info;

		IFileInfo Git(IDirectoryInfo directoryInfo, string path) =>
			fileSystem.FileInfo.New(Path.Join(directoryInfo.FullName, path));

		IDirectoryInfo GitDir(IDirectoryInfo directoryInfo, string path) =>
			fileSystem.DirectoryInfo.New(Path.Join(directoryInfo.FullName, path));

		string? Read(IDirectoryInfo directoryInfo, string path)
		{
			var gitPath = Git(directoryInfo, path).FullName;
			return !fileSystem.File.Exists(gitPath)
				? null
				: fileSystem.File.ReadAllText(gitPath).Trim(Environment.NewLine.ToCharArray());
		}

		string BranchTrackingRemote(string b, IniFile c)
		{
			var sections = c.GetSections();
			var branchSection = $"branch \"{b}\"";
			if (!sections.Contains(branchSection))
				return string.Empty;

			var remoteName = ini.GetSetting(branchSection, "remote")?.Trim();

			var remoteSection = $"remote \"{remoteName}\"";

			remote = ini.GetSetting(remoteSection, "url");
			return remote ?? string.Empty;
		}
	}

	[GeneratedRegex(@"\.git$", RegexOptions.IgnoreCase)]
	private static partial Regex CutOffGitExtension();
}
