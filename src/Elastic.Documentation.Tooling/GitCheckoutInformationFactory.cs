// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;
using SoftCircuits.IniFileParser;

namespace Elastic.Documentation;

public static partial class GitCheckoutInformationFactory
{
	// manual read because libgit2sharp is not yet AOT ready
	public static GitCheckoutInformation Create(IDirectoryInfo? source, IFileSystem fileSystem, ILogger? logger = null)
	{
		if (source is null)
			return GitCheckoutInformation.Unavailable;

		var result = TryCreate(source, fileSystem, logger);

		// Fall back to canned test data only when the inner filesystem is a mock AND no .git entry
		// exists at all at the source. This preserves back-compat for tests that seed no git layout
		// (they get the well-known canned instance), while tests that seed a .git file or directory
		// — even one that fails to resolve — receive the real result (Unavailable).
		// Use ScopedFileSystem.InnerType (available since Nullean.ScopedFileSystem 0.4.0) to inspect
		// through the scope wrapper rather than relying on the outer type name.
		if (result == GitCheckoutInformation.Unavailable)
		{
			var fsType = fileSystem is ScopedFileSystem sf ? sf.InnerType : fileSystem.GetType();
			if (fsType.Name.Contains("Mock", StringComparison.OrdinalIgnoreCase) && IsLegacyTestWithoutGitLayout(fileSystem, source))
			{
				return new GitCheckoutInformation
				{
					Branch = "test-e35fcb27-5f60-4e",
					Remote = "elastic/docs-builder",
					Ref = "e35fcb27-5f60-4e",
					RepositoryName = "docs-builder"
				};
			}
		}

		return result;
	}

	private static GitCheckoutInformation TryCreate(IDirectoryInfo source, IFileSystem fileSystem, ILogger? logger)
	{
		// Resolve the actual .git directory. For regular repos this is source/.git/;
		// for worktrees source/.git is a file pointing to the real git dir.
		IDirectoryInfo gitDir;
		var gitDirPath = fileSystem.Path.Join(source.FullName, ".git");

		if (fileSystem.Directory.Exists(gitDirPath))
		{
			gitDir = fileSystem.DirectoryInfo.New(gitDirPath);
		}
		else
		{
			var gitFile = fileSystem.FileInfo.New(gitDirPath);
			if (!Paths.TryReadGitDirPointer(fileSystem, gitFile, out var resolvedGitDir)
				|| resolvedGitDir is null)
				return GitCheckoutInformation.Unavailable;

			gitDir = resolvedGitDir;
		}

		var gitConfigPath = fileSystem.Path.Join(gitDir.FullName, "config");
		if (!fileSystem.File.Exists(gitConfigPath))
		{
			logger?.LogInformation("Git checkout information not available.");
			return GitCheckoutInformation.Unavailable;
		}

		var headPath = fileSystem.Path.Join(gitDir.FullName, "HEAD");
		var headText = fileSystem.File.Exists(headPath)
			? fileSystem.File.ReadAllText(headPath).Trim()
			: null;

		if (headText is null)
			return GitCheckoutInformation.Unavailable;

		string gitRef;
		string branch;
		if (headText.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
		{
			var refPath = headText["ref:".Length..].Trim();
			branch = refPath.Replace("refs/heads/", string.Empty);
			var refFilePath = fileSystem.Path.Join(gitDir.FullName, refPath.Replace('/', fileSystem.Path.DirectorySeparatorChar));
			gitRef = fileSystem.File.Exists(refFilePath)
				? fileSystem.File.ReadAllText(refFilePath).Trim()
				: headText; // symbolic ref not yet written (new empty repo) — use the ref name itself
		}
		else
		{
			// Detached HEAD: raw SHA
			gitRef = headText;
			branch = Environment.GetEnvironmentVariable("GITHUB_PR_REF_NAME")
				?? Environment.GetEnvironmentVariable("GITHUB_REF_NAME")
				?? "detached/head";
		}

		var ini = new IniFile();
		using var stream = fileSystem.File.OpenRead(gitConfigPath);
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
	}

	/// <summary>
	/// Returns <see langword="true"/> for test setups that use a mock filesystem but do not seed a
	/// real git layout — either no <c>.git</c> entry at all, or a bare <c>.git</c> directory without
	/// a <c>config</c> file. Tests that only add <c>.git/</c> to make <c>FindGitRoot</c> succeed
	/// intentionally fall into the second case; they do not need real git metadata.
	/// <para>
	/// These test setups pre-date testable git resolution and continue to receive the canned test
	/// instance so they do not need to be updated. A <c>.git</c> <em>file</em> (worktree pointer) is
	/// excluded: tests that seed a worktree pointer are explicitly modelling a worktree layout and
	/// expect real resolution or <see cref="GitCheckoutInformation.Unavailable"/>.
	/// </para>
	/// </summary>
	private static bool IsLegacyTestWithoutGitLayout(IFileSystem fileSystem, IDirectoryInfo source)
	{
		var gitPath = fileSystem.Path.Join(source.FullName, ".git");
		var noGitEntry = !fileSystem.Directory.Exists(gitPath) && !fileSystem.File.Exists(gitPath);
		var gitDirWithoutConfig = fileSystem.Directory.Exists(gitPath)
			&& !fileSystem.File.Exists(fileSystem.Path.Join(gitPath, "config"));
		return noGitEntry || gitDirWithoutConfig;
	}

	private static string BranchTrackingRemote(string branch, IniFile config)
	{
		var sections = config.GetSections();
		var branchSection = $"branch \"{branch}\"";
		if (!sections.Contains(branchSection))
			return string.Empty;

		var remoteName = config.GetSetting(branchSection, "remote")?.Trim();
		if (string.IsNullOrEmpty(remoteName))
			return string.Empty;

		var remoteSection = $"remote \"{remoteName}\"";
		return config.GetSetting(remoteSection, "url")?.Trim() ?? string.Empty;
	}

	[GeneratedRegex(@"\.git$", RegexOptions.IgnoreCase)]
	private static partial Regex CutOffGitExtension();
}
