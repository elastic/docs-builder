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
/// Tests for <see cref="DocumentationPathsResolver.Resolve"/> and
/// <see cref="DocumentationFileSystem.Resolve"/>, covering the six-step bootstrap:
/// invocation → docset anchor → checkout → git directories → git info → output.
/// </summary>
public class DocumentationPathsResolverTests
{
	// -----------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------

	/// <summary>
	/// Normalises a Unix-style path through the mock filesystem so that assertions work on
	/// Windows, where MockFileSystem converts <c>/repo</c> → <c>C:\repo</c>.
	/// </summary>
	private static string P(MockFileSystem fs, string unixPath) => fs.DirectoryInfo.New(unixPath).FullName;

	/// <summary>
	/// Builds a minimal regular-repo filesystem:
	///   <c>/repo/.git/{HEAD,config,refs/...}</c> +
	///   <c>/repo/docs/docset.yml</c>
	/// </summary>
	private static MockFileSystem RegularRepo(
		string repoRoot = "/repo",
		string docsRelative = "docs",
		string branch = "main",
		string sha = "abc1234",
		string remote = "elastic/test-repo"
	)
	{
		var fs = new MockFileSystem();
		var docsPath = $"{repoRoot}/{docsRelative}";
		fs.AddDirectory($"{repoRoot}/.git");
		fs.AddFile($"{repoRoot}/.git/HEAD", new MockFileData($"ref: refs/heads/{branch}\n"));
		fs.AddFile($"{repoRoot}/.git/refs/heads/{branch}", new MockFileData($"{sha}\n"));
		fs.AddFile(
			$"{repoRoot}/.git/config",
			new MockFileData(
				$"""
			[remote "origin"]
				url = https://github.com/{remote}.git
			[branch "{branch}"]
				remote = origin
				merge = refs/heads/{branch}
			"""
			)
		);
		fs.AddFile($"{docsPath}/docset.yml", new MockFileData("toc: []\n"));
		return fs;
	}

	/// <summary>
	/// Builds a worktree filesystem:
	///   <c>/worktree/.git</c> (file) → <c>/main/.git/worktrees/wt</c> → commondir → <c>/main/.git</c>
	/// </summary>
	private static MockFileSystem WorktreeWithCommondir(
		string worktreeRoot = "/worktree",
		string mainRoot = "/main",
		string branch = "topic",
		string sha = "fedcba987654",
		string remote = "elastic/worktree-repo",
		string docsRelative = "docs"
	)
	{
		var fs = new MockFileSystem();
		var worktreeGitDir = $"{mainRoot}/.git/worktrees/wt";
		var mainGitDir = $"{mainRoot}/.git";
		var docsPath = $"{worktreeRoot}/{docsRelative}";

		// Worktree: .git is a pointer file
		fs.AddFile($"{worktreeRoot}/.git", new MockFileData($"gitdir: {worktreeGitDir}\n"));

		// Worktree-specific gitdir with commondir pointing back to the main .git
		fs.AddDirectory(worktreeGitDir);
		fs.AddFile($"{worktreeGitDir}/commondir", new MockFileData("../..\n")); // → /main/.git

		// Main .git has the shared objects, config, and HEAD
		fs.AddDirectory(mainGitDir);
		fs.AddFile($"{mainGitDir}/HEAD", new MockFileData($"ref: refs/heads/{branch}\n"));
		fs.AddFile($"{mainGitDir}/refs/heads/{branch}", new MockFileData($"{sha}\n"));
		fs.AddFile(
			$"{mainGitDir}/config",
			new MockFileData(
				$"""
			[remote "origin"]
				url = https://github.com/{remote}.git
			[branch "{branch}"]
				remote = origin
				merge = refs/heads/{branch}
			"""
			)
		);

		fs.AddFile($"{docsPath}/docset.yml", new MockFileData("toc: []\n"));
		return fs;
	}

	// -----------------------------------------------------------------------
	// Docset scan + checkout convergence
	// -----------------------------------------------------------------------

	[Fact]
	public void InvocationAtRepoRoot_ResolvesDocsetInDocsSubfolder()
	{
		var fs = RegularRepo();
		var invocation = fs.DirectoryInfo.New("/repo");

		var paths = DocumentationPathsResolver.Resolve(invocation, new DocumentationScopeOptions { Inner = fs }, fs);

		paths.SourceDirectory.FullName.Should().Be(P(fs, "/repo/docs"));
		paths.CheckoutDirectory.FullName.Should().Be(P(fs, "/repo"));
	}

	[Fact]
	public void InvocationAtDocsSubfolder_ResolvesDocsetAndCheckout()
	{
		var fs = RegularRepo();
		var invocation = fs.DirectoryInfo.New("/repo/docs");

		var paths = DocumentationPathsResolver.Resolve(invocation, new DocumentationScopeOptions { Inner = fs }, fs);

		paths.SourceDirectory.FullName.Should().Be(P(fs, "/repo/docs"));
		paths.CheckoutDirectory.FullName.Should().Be(P(fs, "/repo"));
	}

	[Fact]
	public void PathRepoRoot_And_PathDocsSubfolder_ResolveIdenticalCheckoutAndSource()
	{
		var fs = RegularRepo();

		var fromRoot = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/repo"), new DocumentationScopeOptions { Inner = fs }, fs);

		var fromDocs = DocumentationPathsResolver.Resolve(
			fs.DirectoryInfo.New("/repo/docs"),
			new DocumentationScopeOptions { Inner = fs },
			fs
		);

		fromRoot
			.CheckoutDirectory
			.FullName
			.Should()
			.Be(fromDocs.CheckoutDirectory.FullName, "--path /repo and --path /repo/docs must converge on the same checkout");
		fromRoot
			.SourceDirectory
			.FullName
			.Should()
			.Be(fromDocs.SourceDirectory.FullName, "--path /repo and --path /repo/docs must converge on the same source");
	}

	[Fact]
	public void AnchorTwoLevelsBelowInvocationRoot_StillResolvesCheckoutAtGitRoot()
	{
		// Mirrors the `elastic/infra` shape: no `--path` (invocation == repo root), and the only
		// docset sits two levels down (`docs/resilience-team/docset.yml`). The recursive scan in
		// step 2 finds it fine; without widening `MaxParents` by that same distance, step 3's
		// default maxParents=1 can't see the `.git` two levels above the anchor and would either
		// throw (real FS) or silently fall back to the wrong directory (mock FS leniency).
		var fs = RegularRepo(docsRelative: "docs/resilience-team");
		var invocation = fs.DirectoryInfo.New("/repo");

		var paths = DocumentationPathsResolver.Resolve(invocation, new DocumentationScopeOptions { Inner = fs }, fs);

		paths.SourceDirectory.FullName.Should().Be(P(fs, "/repo/docs/resilience-team"));
		paths
			.CheckoutDirectory
			.FullName
			.Should()
			.Be(P(fs, "/repo"), "the anchor's depth below the invocation root should widen the git-root search, not require --git-dir");
		paths.Git.IsAvailable.Should().BeTrue();
	}

	[Fact]
	public void AnchorAtInvocationRoot_UnrelatedAncestorGit_StillOutOfReach()
	{
		// The depth-widening must stay anchored to the invocation, not become unbounded: when the
		// anchor IS the invocation (depth 0), an unrelated repo's .git two levels up must remain
		// out of reach, exactly as before this change.
		var fs = new MockFileSystem();
		fs.AddDirectory("/parent-repo/.git");
		fs.AddDirectory("/parent-repo/checkout");
		fs.AddFile("/parent-repo/checkout/docs/docset.yml", new MockFileData("toc: []\n"));

		var invocation = fs.DirectoryInfo.New("/parent-repo/checkout/docs");
		var opts = new DocumentationScopeOptions { Inner = fs, Git = GitCheckoutInformation.Unavailable };

		var paths = DocumentationPathsResolver.Resolve(invocation, opts, fs);

		paths
			.CheckoutDirectory
			.FullName
			.Should()
			.Be(
				P(fs, "/parent-repo/checkout/docs"),
				"depth-widening is relative to the invocation, so an ancestor repo's .git outside the invocation must not be adopted"
			);
	}

	[Fact]
	public void InvocationPath_StoredVerbatim_IndependentOfCheckout()
	{
		var fs = RegularRepo();
		var invocationDir = fs.DirectoryInfo.New("/repo/docs");

		var paths = DocumentationPathsResolver.Resolve(invocationDir, new DocumentationScopeOptions { Inner = fs }, fs);

		paths.InvocationPath.FullName.Should().Be(P(fs, "/repo/docs"));
		paths.CheckoutDirectory.FullName.Should().Be(P(fs, "/repo"));
	}

	// -----------------------------------------------------------------------
	// Git directory resolution
	// -----------------------------------------------------------------------

	[Fact]
	public void RegularRepo_GitDirectories_ContainsOneEntry()
	{
		var fs = RegularRepo();

		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/repo"), new DocumentationScopeOptions { Inner = fs }, fs);

		paths.GitDirectories.Should().ContainSingle().Which.Should().Be(P(fs, "/repo/.git"));
	}

	[Fact]
	public void RegularRepo_GitInfo_IsResolved()
	{
		var fs = RegularRepo(branch: "my-branch", sha: "deadbeef1234", remote: "elastic/my-repo");

		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/repo"), new DocumentationScopeOptions { Inner = fs }, fs);

		paths.Git.IsAvailable.Should().BeTrue();
		paths.Git.Branch.Should().Be("my-branch");
		paths.Git.Ref.Should().Be("deadbeef1234");
		paths.Git.RepositoryName.Should().Be("my-repo");
	}

	// -----------------------------------------------------------------------
	// Git worktree
	// -----------------------------------------------------------------------

	[Fact]
	public void Worktree_CheckoutIsWorktreeRoot_NotMainRepo()
	{
		var fs = WorktreeWithCommondir();

		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/worktree"), new DocumentationScopeOptions { Inner = fs }, fs);

		paths.CheckoutDirectory.FullName.Should().Be(P(fs, "/worktree"));
	}

	[Fact]
	public void Worktree_GitDirectories_ContainsPointerAndMainGit()
	{
		var fs = WorktreeWithCommondir();

		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/worktree"), new DocumentationScopeOptions { Inner = fs }, fs);

		paths.GitDirectories.Should().HaveCount(2);
		paths.GitDirectories.Should().Contain(P(fs, "/worktree/.git"), "pointer file path must be in scope so the .git file is readable");
		paths
			.GitDirectories
			.Should()
			.Contain(P(fs, "/main/.git"), "resolved commondir target must be included so config/HEAD are readable");
	}

	[Fact]
	public void Worktree_GitInfo_ResolvedFromMainDotGit()
	{
		var fs = WorktreeWithCommondir(branch: "topic", sha: "fedcba987654", remote: "elastic/worktree-repo");

		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/worktree"), new DocumentationScopeOptions { Inner = fs }, fs);

		paths.Git.IsAvailable.Should().BeTrue();
		paths.Git.Branch.Should().Be("topic");
		paths.Git.Ref.Should().Be("fedcba987654");
		paths.Git.RepositoryName.Should().Be("worktree-repo");
	}

	[Fact]
	public void Worktree_InvocationAtDocsSubfolder_ResolvesIdenticallyToWorktreeRoot()
	{
		var fs = WorktreeWithCommondir();

		var fromWorktreeRoot = DocumentationPathsResolver.Resolve(
			fs.DirectoryInfo.New("/worktree"),
			new DocumentationScopeOptions { Inner = fs },
			fs
		);

		var fromDocs = DocumentationPathsResolver.Resolve(
			fs.DirectoryInfo.New("/worktree/docs"),
			new DocumentationScopeOptions { Inner = fs },
			fs
		);

		fromWorktreeRoot
			.CheckoutDirectory
			.FullName
			.Should()
			.Be(
				fromDocs.CheckoutDirectory.FullName,
				"worktree: --path /worktree and --path /worktree/docs must resolve to the same checkout"
			);
	}

	// -----------------------------------------------------------------------
	// Explicit --git-dir override
	// -----------------------------------------------------------------------

	[Fact]
	public void ExplicitGitDir_CheckoutIsGitDirParent()
	{
		// Layout: docset at /project/docs/, .git at /repo/.git (out-of-tree)
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/.git/HEAD", new MockFileData("ref: refs/heads/main\n"));
		fs.AddFile("/repo/.git/refs/heads/main", new MockFileData("cafe1234\n"));
		fs.AddFile(
			"/repo/.git/config",
			new MockFileData(
				"""
			[remote "origin"]
				url = https://github.com/elastic/override-test.git
			[branch "main"]
				remote = origin
				merge = refs/heads/main
			"""
			)
		);
		fs.AddFile("/project/docs/docset.yml", new MockFileData("toc: []\n"));

		var opts = new DocumentationScopeOptions { Inner = fs, GitDir = "/repo/.git" };

		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/project/docs"), opts, fs);

		paths.CheckoutDirectory.FullName.Should().Be(P(fs, "/repo"), "--git-dir /repo/.git → checkout = /repo/.git.Parent = /repo");
	}

	[Fact]
	public void ExplicitGitDir_GitInfo_ResolvedFromOverriddenGitDir()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/.git/HEAD", new MockFileData("ref: refs/heads/main\n"));
		fs.AddFile("/repo/.git/refs/heads/main", new MockFileData("aabbcc99\n"));
		fs.AddFile(
			"/repo/.git/config",
			new MockFileData(
				"""
			[remote "origin"]
				url = https://github.com/elastic/override-repo.git
			[branch "main"]
				remote = origin
				merge = refs/heads/main
			"""
			)
		);
		fs.AddFile("/project/docs/docset.yml", new MockFileData("toc: []\n"));

		var opts = new DocumentationScopeOptions { Inner = fs, GitDir = "/repo/.git" };

		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/project/docs"), opts, fs);

		paths.Git.IsAvailable.Should().BeTrue();
		paths.Git.Branch.Should().Be("main");
		paths.Git.Ref.Should().Be("aabbcc99");
		paths.Git.RepositoryName.Should().Be("override-repo");
	}

	// -----------------------------------------------------------------------
	// Mock filesystem — no git layout (graceful fallback)
	// -----------------------------------------------------------------------

	[Fact]
	public void MockFsWithoutGit_DoesNotThrow_CheckoutFallsBackToSource()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/repo/docs/docset.yml", new MockFileData("toc: []\n"));

		var opts = new DocumentationScopeOptions { Inner = fs, Git = GitCheckoutInformation.Unavailable };
		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/repo/docs"), opts, fs);

		paths.CheckoutDirectory.FullName.Should().Be(P(fs, "/repo/docs"), "mock FS fallback: no .git → checkout = source directory");
		paths.GitDirectories.Should().BeEmpty();
	}

	[Fact]
	public void MockFsWithoutGit_GitOverride_IsPreservedVerbatim()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/repo/docs/docset.yml", new MockFileData("toc: []\n"));

		var opts = new DocumentationScopeOptions { Inner = fs, Git = GitCheckoutInformation.Unavailable };
		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/repo/docs"), opts, fs);

		paths.Git.IsAvailable.Should().BeFalse();
		paths.Git.Should().Be(GitCheckoutInformation.Unavailable);
	}

	// -----------------------------------------------------------------------
	// Output directory default
	// -----------------------------------------------------------------------

	[Fact]
	public void Output_DefaultsToCheckoutArtifacts()
	{
		var fs = RegularRepo();
		var opts = new DocumentationScopeOptions { Inner = fs };

		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/repo"), opts, fs);

		// Default output is checkout/.artifacts/docs/html — NOT the invocation path.
		paths.OutputDirectory.FullName.Should().StartWith(P(fs, "/repo/.artifacts"));
	}

	[Fact]
	public void Output_InvocationAtDocsSubfolder_StillAnchorsToCheckout()
	{
		// Regression: before this fix, --path /repo/docs/ wrote to /repo/docs/.artifacts
		// instead of /repo/.artifacts. Checkout is /repo, so artifacts belong there.
		var fs = RegularRepo();
		var opts = new DocumentationScopeOptions { Inner = fs };

		var fromRoot = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/repo"), opts, fs);
		var fromDocs = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/repo/docs"), opts, fs);

		fromRoot
			.OutputDirectory
			.FullName
			.Should()
			.Be(fromDocs.OutputDirectory.FullName, "--path /repo and --path /repo/docs must produce the same default output directory");
	}

	[Fact]
	public void Output_ExplicitOverride_IsRespected()
	{
		var fs = RegularRepo();
		var opts = new DocumentationScopeOptions { Inner = fs, Output = "/custom/output" };

		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/repo"), opts, fs);

		paths.OutputDirectory.FullName.Should().Be(P(fs, "/custom/output"));
	}

	// -----------------------------------------------------------------------
	// ConfigurationFile option (pre-discovered docset)
	// -----------------------------------------------------------------------

	[Fact]
	public void PreDiscoveredConfigFile_SkipsDocsetScan()
	{
		// Docset is at /project/docs/docset.yml, but invocation is the project root
		var fs = RegularRepo(repoRoot: "/project");
		var docsetFile = fs.FileInfo.New("/project/docs/docset.yml");

		var opts = new DocumentationScopeOptions { Inner = fs, ConfigurationFile = docsetFile.FullName };
		var paths = DocumentationPathsResolver.Resolve(fs.DirectoryInfo.New("/project"), opts, fs);

		paths.SourceDirectory.FullName.Should().Be(P(fs, "/project/docs"));
		paths.ConfigurationPath.FullName.Should().Be(P(fs, "/project/docs/docset.yml"));
	}

	// -----------------------------------------------------------------------
	// No docset found → throws
	// -----------------------------------------------------------------------

	[Fact]
	public void NoDocset_Throws_DocumentationPathException()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/empty");

		var act =
			() => DocumentationPathsResolver.Resolve(
				fs.DirectoryInfo.New("/empty"),
				new DocumentationScopeOptions { Inner = fs, Git = GitCheckoutInformation.Unavailable },
				fs
			);

		act.Should().Throw<DocumentationPathException>().WithMessage("*docset.yml*");
	}

	// -----------------------------------------------------------------------
	// DocumentationFileSystem.Resolve — integration (same scenarios via the public API)
	// -----------------------------------------------------------------------

	[Fact]
	public void DocumentationFileSystem_Resolve_RegularRepo_ExposesResolvedPaths()
	{
		var fs = RegularRepo(branch: "feature", sha: "001122", remote: "elastic/docs-builder");
		var invocation = fs.DirectoryInfo.New("/repo");

		var docFs = DocumentationFileSystem.Resolve(invocation, new DocumentationScopeOptions { Inner = fs });

		docFs.Paths.CheckoutDirectory.FullName.Should().Be(P(fs, "/repo"));
		docFs.Paths.SourceDirectory.FullName.Should().Be(P(fs, "/repo/docs"));
		docFs.Paths.Git.Branch.Should().Be("feature");
		docFs.Paths.Git.RepositoryName.Should().Be("docs-builder");
	}

	[Fact]
	public void DocumentationFileSystem_Resolve_Worktree_ExposesMainGitInfo()
	{
		var fs = WorktreeWithCommondir(branch: "topic", sha: "99aabb", remote: "elastic/worktree-test");
		var invocation = fs.DirectoryInfo.New("/worktree");

		var docFs = DocumentationFileSystem.Resolve(invocation, new DocumentationScopeOptions { Inner = fs });

		docFs.Paths.CheckoutDirectory.FullName.Should().Be(P(fs, "/worktree"));
		docFs.Paths.Git.Branch.Should().Be("topic");
		docFs.Paths.Git.Ref.Should().Be("99aabb");
		docFs.Paths.Git.RepositoryName.Should().Be("worktree-test");
	}
}
