// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class GitCommonRootTests
{
	[Fact]
	public void NormalRepo_ReturnsWorkingDirectoryRoot()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		var root = fs.DirectoryInfo.New("/repo");

		var result = Paths.ResolveGitCommonRoot(fs, root, isCI: false);

		result.FullName.Should().Be(root.FullName);
	}

	[Fact]
	public void Worktree_AbsoluteGitDir_ReturnsMainRepoRoot()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/worktree/.git", new MockFileData("gitdir: /main-repo/.git/worktrees/feature-branch"));
		fs.AddDirectory("/main-repo/.git/worktrees/feature-branch");
		var root = fs.DirectoryInfo.New("/worktree");

		var result = Paths.ResolveGitCommonRoot(fs, root, isCI: false);

		result.FullName.Should().Be(fs.DirectoryInfo.New("/main-repo").FullName);
	}

	[Fact]
	public void Worktree_RelativeGitDir_ReturnsMainRepoRoot()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/repos/worktree/.git", new MockFileData("gitdir: ../main-repo/.git/worktrees/feature-branch"));
		fs.AddDirectory("/repos/main-repo/.git/worktrees/feature-branch");
		var root = fs.DirectoryInfo.New("/repos/worktree");

		var result = Paths.ResolveGitCommonRoot(fs, root, isCI: false);

		result.FullName.Should().Be(fs.DirectoryInfo.New("/repos/main-repo").FullName);
	}

	[Fact]
	public void NoGitPresent_ReturnsWorkingDirectoryRoot()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo");
		var root = fs.DirectoryInfo.New("/repo");

		var result = Paths.ResolveGitCommonRoot(fs, root, isCI: false);

		result.FullName.Should().Be(root.FullName);
	}

	[Fact]
	public void MalformedGitFile_ReturnsWorkingDirectoryRoot()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/repo/.git", new MockFileData("not a valid gitdir reference"));
		var root = fs.DirectoryInfo.New("/repo");

		var result = Paths.ResolveGitCommonRoot(fs, root, isCI: false);

		result.FullName.Should().Be(root.FullName);
	}

	[Fact]
	public void Worktree_GitDirPathHasNoGitAncestor_ReturnsWorkingDirectoryRoot()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/worktree/.git", new MockFileData("gitdir: /some/path/without/git/ancestor"));
		var root = fs.DirectoryInfo.New("/worktree");

		var result = Paths.ResolveGitCommonRoot(fs, root, isCI: false);

		result.FullName.Should().Be(root.FullName);
	}

	[Fact]
	public void OnCI_Worktree_ReturnsWorkingDirectoryRoot()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/worktree/.git", new MockFileData("gitdir: /main-repo/.git/worktrees/feature-branch"));
		fs.AddDirectory("/main-repo/.git/worktrees/feature-branch");
		var root = fs.DirectoryInfo.New("/worktree");

		var result = Paths.ResolveGitCommonRoot(fs, root, isCI: true);

		result.FullName.Should().Be(root.FullName);
	}
}
