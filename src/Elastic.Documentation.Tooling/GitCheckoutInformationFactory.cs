// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
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
			var worktreeFile = Git(source, ".git");
			if (!worktreeFile.Exists)
				return GitCheckoutInformation.Unavailable;
			var workTreePath = Read(source, ".git")?.Replace("gitdir: ", string.Empty);
			if (workTreePath is null)
				return GitCheckoutInformation.Unavailable;
			gitDir = fileSystem.DirectoryInfo.New(workTreePath).GetParent(".git");
			if (gitDir is null || !gitDir.Exists)
				return GitCheckoutInformation.Unavailable;
		}

		var gitConfig = Git(gitDir, "config");
		if (!gitConfig.Exists)
		{
			logger?.LogInformation("Git checkout information not available.");
			return GitCheckoutInformation.Unavailable;
		}

		var head = Read(gitDir, "HEAD") ?? fakeRef;
		var gitRef = head;
		var branch = head.Replace("refs/heads/", string.Empty);
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

			remote = ini.GetSetting(remoteSection, "url")?.Trim();
			return remote ?? string.Empty;
		}
	}

	[GeneratedRegex(@"\.git$", RegexOptions.IgnoreCase)]
	private static partial Regex CutOffGitExtension();
}
