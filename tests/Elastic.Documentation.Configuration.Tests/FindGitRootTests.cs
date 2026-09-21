// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class FindGitRootTests
{
	[Fact]
	public void DocsAtRoot_FindsGitRoot()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/repo");

		var result = Paths.FindGitRoot(start);

		result.Should().NotBeNull();
		result.FullName.Should().Be(start.FullName);
	}

	[Fact]
	public void DocsInDocsFolder_FindsGitRoot()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/docs/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/repo/docs");
		var expected = fs.DirectoryInfo.New("/repo");

		// Default maxParents=1: docset is one level below checkout, so .git is found at depth 1
		var result = Paths.FindGitRoot(start);

		result.Should().NotBeNull();
		result.FullName.Should().Be(expected.FullName);
	}

	[Fact]
	public void DocsNestedTwoLevels_FindsGitRoot()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/docs/resilience-team/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/repo/docs/resilience-team");
		var expected = fs.DirectoryInfo.New("/repo");

		// maxParents=2: docset is two levels below checkout
		var result = Paths.FindGitRoot(start, maxParents: 2);

		result.Should().NotBeNull();
		result.FullName.Should().Be(expected.FullName);
	}

	[Fact]
	public void DocsNestedTwoLevels_WithDefaultMaxParents_ReturnsNull()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/docs/resilience-team/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/repo/docs/resilience-team");

		// Default maxParents=1 cannot reach .git at depth 2
		var result = Paths.FindGitRoot(start);

		result.Should().BeNull();
	}

	[Fact]
	public void BoundPreventsEscapingToParentRepo()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/parent-repo/.git");
		fs.AddDirectory("/parent-repo/checkout");
		fs.AddFile("/parent-repo/checkout/docs/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/parent-repo/checkout/docs");

		// maxParents=1: checks checkout/docs (depth 0) and checkout (depth 1), neither has .git
		var result = Paths.FindGitRoot(start);

		result.Should().BeNull("the .git is two levels above the anchor, beyond maxParents");
	}

	[Fact]
	public void BoundPreventsEscapingToParentRepo_DeeplyNested()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/workspace/projects/other-repo/.git");
		fs.AddDirectory("/workspace/projects/other-repo/subrepo/docs/team");
		fs.AddFile("/workspace/projects/other-repo/subrepo/docs/team/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/workspace/projects/other-repo/subrepo/docs/team");

		// Default maxParents=1: cannot reach .git three levels up
		var result = Paths.FindGitRoot(start);

		result.Should().BeNull("the .git belongs to a parent repo outside the allowed depth");
	}

	[Fact]
	public void GitRootDeepAboveAnchor_AcceptedWithLargeMaxParents()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/workspace/.git");
		fs.AddDirectory("/workspace/docs/a/b/c");
		fs.AddFile("/workspace/docs/a/b/c/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/workspace/docs/a/b/c");
		var expected = fs.DirectoryInfo.New("/workspace");

		// Must pass maxParents=4 since the anchor is 4 levels below the checkout
		var result = Paths.FindGitRoot(start, maxParents: 4);

		result.Should().NotBeNull();
		result.FullName.Should().Be(expected.FullName);
	}

	[Fact]
	public void NoGitDirectory_ReturnsNull()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/docs");
		fs.AddFile("/repo/docs/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/repo/docs");

		var result = Paths.FindGitRoot(start);

		result.Should().BeNull();
	}

	[Fact]
	public void WorktreeGitFile_FindsGitRoot()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/repo/.git", new("gitdir: /main/.git/worktrees/repo"));
		fs.AddFile("/repo/docs/team/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/repo/docs/team");
		var expected = fs.DirectoryInfo.New("/repo");

		// .git file (worktree pointer) counts the same as a .git dir; depth 2 needs maxParents=2
		var result = Paths.FindGitRoot(start, maxParents: 2);

		result.Should().NotBeNull();
		result.FullName.Should().Be(expected.FullName);
	}

	[Fact]
	public void WorktreeGitFile_OneLevel_FindsGitRoot()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/repo/.git", new("gitdir: /main/.git/worktrees/repo"));
		fs.AddFile("/repo/docs/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/repo/docs");
		var expected = fs.DirectoryInfo.New("/repo");

		// Docset one level below worktree root: found with default maxParents=1
		var result = Paths.FindGitRoot(start);

		result.Should().NotBeNull();
		result.FullName.Should().Be(expected.FullName);
	}
}
