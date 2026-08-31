// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Tests for <see cref="GitCheckoutInformationFactory.Create"/> driven through
/// a <see cref="MockFileSystem"/> seeded with various git layouts.
/// Previously impossible because the mock short-circuit returned canned data for any MockFileSystem;
/// the inverted short-circuit now attempts real resolution first and only falls back to canned data
/// when resolution yields nothing.
/// </summary>
public class GitCheckoutResolutionTests
{
	private static MockFileSystem BuildFs(
		string root,
		string? branch = "main",
		string? sha = null,
		string? remote = null,
		bool worktree = false,
		string? worktreeGitDir = null
	)
	{
		var fs = new MockFileSystem();
		sha ??= "abc1234def5678";
		remote ??= "elastic/test-repo";

		if (!worktree)
		{
			fs.AddDirectory($"{root}/.git");
			// HEAD
			if (branch is not null)
				fs.AddFile($"{root}/.git/HEAD", new MockFileData($"ref: refs/heads/{branch}\n"));
			else
				fs.AddFile($"{root}/.git/HEAD", new MockFileData($"{sha}\n")); // detached
																			   // ref file
			if (branch is not null)
				fs.AddFile($"{root}/.git/refs/heads/{branch}", new MockFileData($"{sha}\n"));
			// config
			fs.AddFile(
				$"{root}/.git/config",
				new MockFileData(
					$"""
				[core]
					repositoryformatversion = 0
				[remote "origin"]
					url = https://github.com/{remote}.git
				[branch "{branch ?? "main"}"]
					remote = origin
					merge = refs/heads/{branch ?? "main"}
				"""
				)
			);
		}
		else
		{
			// Worktree: .git is a file pointing to the real git dir
			var realGitDir = worktreeGitDir ?? $"/main-repo/.git/worktrees/branch";
			fs.AddFile($"{root}/.git", new MockFileData($"gitdir: {realGitDir}\n"));
			// The real .git directory
			fs.AddDirectory(realGitDir);
			fs.AddFile($"{realGitDir}/HEAD", new MockFileData($"ref: refs/heads/{branch}\n"));
			fs.AddFile($"{realGitDir}/refs/heads/{branch}", new MockFileData($"{sha}\n"));
			fs.AddFile(
				$"{realGitDir}/config",
				new MockFileData(
					$"""
				[core]
					repositoryformatversion = 0
				[remote "origin"]
					url = https://github.com/{remote}.git
				[branch "{branch}"]
					remote = origin
					merge = refs/heads/{branch}
				"""
				)
			);
		}

		return fs;
	}

	[Fact]
	public void RegularRepo_ReturnsGitInfo()
	{
		var fs = BuildFs("/repo", branch: "feature/my-branch", sha: "deadbeef1234");

		var checkout = fs.DirectoryInfo.New("/repo");
		var scoped = new CheckoutsFileSystem(fs.DirectoryInfo.New("/repo"), inner: fs);

		var result = GitCheckoutInformationFactory.Create(checkout, scoped);

		result.IsAvailable.Should().BeTrue();
		result.Branch.Should().Be("feature/my-branch");
		result.Ref.Should().Be("deadbeef1234");
		// Remote is the full config url with .git suffix stripped; RepositoryName is the last segment
		result.Remote.Should().EndWith("/elastic/test-repo");
		result.RepositoryName.Should().Be("test-repo");
	}

	[Fact]
	public void RegularRepo_DetachedHead_NeverReturnsRandomGuid()
	{
		var fs = BuildFs("/repo", branch: null, sha: "cafebabe9876");

		var checkout = fs.DirectoryInfo.New("/repo");
		var scoped = new CheckoutsFileSystem(fs.DirectoryInfo.New("/repo"), inner: fs);

		var result = GitCheckoutInformationFactory.Create(checkout, scoped);

		// The production code reads GITHUB_PR_REF_NAME ?? GITHUB_REF_NAME ?? "detached/head".
		// Mirror that lookup so the test passes both locally and on CI (where GITHUB_REF_NAME=3789/merge).
		var expectedBranch = Environment.GetEnvironmentVariable("GITHUB_PR_REF_NAME")
			?? Environment.GetEnvironmentVariable("GITHUB_REF_NAME")
			?? "detached/head";
		result.IsAvailable.Should().BeTrue();
		result.Ref.Should().Be("cafebabe9876", "detached HEAD must use the actual SHA, never a random GUID");
		result.Branch.Should().Be(expectedBranch);
	}

	[Fact]
	public void WorktreeWithAbsoluteGitDir_ResolvesViaMainRepo()
	{
		var sha = "1a2b3c4d5e6f";
		var fs = BuildFs(
			"/worktree",
			branch: "my-feature",
			sha: sha,
			remote: "elastic/worktree-repo",
			worktree: true,
			worktreeGitDir: "/main-repo/.git/worktrees/my-feature"
		);

		// Scope must cover both the worktree dir and the main .git
		var scoped = new CheckoutsFileSystem(fs.DirectoryInfo.New("/worktree"), inner: fs);
		var extended = new Nullean.ScopedFileSystem.ScopedFileSystem(
			fs,
			new Nullean.ScopedFileSystem.ScopedFileSystemOptions(["/worktree", "/main-repo/.git/worktrees/my-feature"])
			{
				AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" },
				AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
			}
		);

		var checkout = fs.DirectoryInfo.New("/worktree");
		var result = GitCheckoutInformationFactory.Create(checkout, extended);

		result.IsAvailable.Should().BeTrue();
		result.Branch.Should().Be("my-feature");
		result.Ref.Should().Be(sha);
		result.RepositoryName.Should().Be("worktree-repo");
	}

	[Fact]
	public void WorktreeWithRelativeGitDir_ResolvesAgainstGitFileDirectory()
	{
		var fs = new MockFileSystem();
		// .git file with relative gitdir — resolved relative to /worktree, NOT process CWD
		fs.AddFile("/worktree/.git", new MockFileData("gitdir: ../.git/worktrees/branch\n"));
		fs.AddDirectory("/.git/worktrees/branch");
		fs.AddFile("/.git/worktrees/branch/HEAD", new MockFileData("ref: refs/heads/feature\n"));
		fs.AddFile("/.git/worktrees/branch/refs/heads/feature", new MockFileData("aabbccdd\n"));
		fs.AddFile(
			"/.git/worktrees/branch/config",
			new MockFileData(
				"""
			[remote "origin"]
				url = https://github.com/elastic/relative-test.git
			[branch "feature"]
				remote = origin
				merge = refs/heads/feature
			"""
			)
		);

		var checkout = fs.DirectoryInfo.New("/worktree");
		var scoped = new Nullean.ScopedFileSystem.ScopedFileSystem(
			fs,
			new Nullean.ScopedFileSystem.ScopedFileSystemOptions(["/worktree", "/.git"])
			{
				AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" },
				AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
			}
		);

		var result = GitCheckoutInformationFactory.Create(checkout, scoped);

		result.IsAvailable.Should().BeTrue();
		result.Branch.Should().Be("feature");
		result.Ref.Should().Be("aabbccdd");
		result.RepositoryName.Should().Be("relative-test");
	}

	[Fact]
	public void WorktreeWithCommondir_ResolvesViaCommondir()
	{
		var fs = new MockFileSystem();
		// .git file → worktree-specific dir → commondir → shared .git
		fs.AddFile("/worktree/.git", new MockFileData("gitdir: /main/.git/worktrees/wt\n"));
		fs.AddDirectory("/main/.git/worktrees/wt");
		fs.AddFile("/main/.git/worktrees/wt/commondir", new MockFileData("../..\n")); // points to /main/.git
																					  // The shared .git directory
		fs.AddDirectory("/main/.git");
		fs.AddFile("/main/.git/HEAD", new MockFileData("ref: refs/heads/topic\n"));
		fs.AddFile("/main/.git/refs/heads/topic", new MockFileData("fedcba987654\n"));
		fs.AddFile(
			"/main/.git/config",
			new MockFileData(
				"""
			[remote "origin"]
				url = https://github.com/elastic/commondir-test.git
			[branch "topic"]
				remote = origin
				merge = refs/heads/topic
			"""
			)
		);

		var checkout = fs.DirectoryInfo.New("/worktree");
		var scoped = new Nullean.ScopedFileSystem.ScopedFileSystem(
			fs,
			new Nullean.ScopedFileSystem.ScopedFileSystemOptions(["/worktree", "/main/.git"])
			{
				AllowedHiddenFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" },
				AllowedHiddenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" }
			}
		);

		var result = GitCheckoutInformationFactory.Create(checkout, scoped);

		result.IsAvailable.Should().BeTrue();
		result.Branch.Should().Be("topic");
		result.Ref.Should().Be("fedcba987654");
		result.RepositoryName.Should().Be("commondir-test");
	}

	[Fact]
	public void WorktreeMissingGitDir_ReturnsUnavailable()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/worktree/.git", new MockFileData("gitdir: /nonexistent/.git/worktrees/wt\n"));

		var checkout = fs.DirectoryInfo.New("/worktree");
		var scoped = new CheckoutsFileSystem(fs.DirectoryInfo.New("/worktree"), inner: fs);

		var result = GitCheckoutInformationFactory.Create(checkout, scoped);

		result.IsAvailable.Should().BeFalse("a worktree pointer to a missing gitdir must yield Unavailable");
	}

	[Fact]
	public void RegularRepo_PackedRefWithoutLooseRefFile_ResolvesShaFromPackedRefs()
	{
		// actions/checkout (and `git gc`) can leave HEAD pointing at a symbolic ref with no
		// corresponding loose file under refs/heads — the SHA only lives in packed-refs.
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/.git/HEAD", new MockFileData("ref: refs/heads/main\n"));
		fs.AddFile(
			"/repo/.git/packed-refs",
			new MockFileData(
				"""
			# pack-refs with: peeled fully-peeled sorted
			deadbeef1234567890deadbeef1234567890dead refs/heads/main
			cafebabe0000000000cafebabe0000000000cafe refs/remotes/origin/main
			"""
			)
		);
		fs.AddFile(
			"/repo/.git/config",
			new MockFileData(
				"""
			[core]
				repositoryformatversion = 0
			[remote "origin"]
				url = https://github.com/elastic/test-repo.git
			[branch "main"]
				remote = origin
				merge = refs/heads/main
			"""
			)
		);

		var checkout = fs.DirectoryInfo.New("/repo");
		var scoped = new CheckoutsFileSystem(fs.DirectoryInfo.New("/repo"), inner: fs);

		var result = GitCheckoutInformationFactory.Create(checkout, scoped);

		result.IsAvailable.Should().BeTrue();
		result.Branch.Should().Be("main");
		result
			.Ref
			.Should()
			.Be(
				"deadbeef1234567890deadbeef1234567890dead",
				"the SHA must be resolved from packed-refs, never the literal HEAD contents like 'ref: refs/heads/main'"
			);
	}

	[Fact]
	public void MockWithNoGitLayout_ReturnsCannedTestData()
	{
		// Back-compat: tests that seed no .git at all must continue to receive the canned
		// test instance rather than Unavailable, so existing test suites need no churn.
		var fs = new MockFileSystem();
		var checkout = fs.DirectoryInfo.New("/some/path");
		var scoped = new CheckoutsFileSystem(fs.DirectoryInfo.New("/some/path"), inner: fs);

		var result = GitCheckoutInformationFactory.Create(checkout, scoped);

		result.IsAvailable.Should().BeTrue("mock with no layout falls back to canned data");
		result.Branch.Should().Be("test-e35fcb27-5f60-4e");
		result.RepositoryName.Should().Be("docs-builder");
	}
}
