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
		var expected = start.FullName;

		var result = Paths.FindGitRoot(start, ceiling: start);

		result.Should().NotBeNull();
		result.FullName.Should().Be(expected);
	}

	[Fact]
	public void DocsInDocsFolder_FindsGitRoot()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/docs/docset.yml", new("toc: []"));

		var ceiling = fs.DirectoryInfo.New("/repo");
		var start = fs.DirectoryInfo.New("/repo/docs");

		var result = Paths.FindGitRoot(start, ceiling: ceiling);

		result.Should().NotBeNull();
		result.FullName.Should().Be(ceiling.FullName);
	}

	[Fact]
	public void DocsNestedTwoLevels_FindsGitRoot()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/docs/resilience-team/docset.yml", new("toc: []"));

		var ceiling = fs.DirectoryInfo.New("/repo");
		var start = fs.DirectoryInfo.New("/repo/docs/resilience-team");

		var result = Paths.FindGitRoot(start, ceiling: ceiling);

		result.Should().NotBeNull();
		result.FullName.Should().Be(ceiling.FullName);
	}

	[Fact]
	public void DocsNestedTwoLevels_WithoutCeiling_ReturnsNull()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/.git");
		fs.AddFile("/repo/docs/resilience-team/docset.yml", new("toc: []"));

		var start = fs.DirectoryInfo.New("/repo/docs/resilience-team");

		var result = Paths.FindGitRoot(start);

		result.Should().BeNull();
	}

	[Fact]
	public void CeilingPreventsEscapingToParentRepo()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/parent-repo/.git");
		fs.AddDirectory("/parent-repo/checkout");
		fs.AddFile("/parent-repo/checkout/docs/docset.yml", new("toc: []"));

		var ceiling = fs.DirectoryInfo.New("/parent-repo/checkout");
		var start = fs.DirectoryInfo.New("/parent-repo/checkout/docs");

		var result = Paths.FindGitRoot(start, ceiling: ceiling);

		result.Should().BeNull("the .git is above the ceiling and must not be reached");
	}

	[Fact]
	public void CeilingPreventsEscapingToParentRepo_DeeplyNested()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/workspace/projects/other-repo/.git");
		fs.AddDirectory("/workspace/projects/other-repo/subrepo/docs/team");
		fs.AddFile("/workspace/projects/other-repo/subrepo/docs/team/docset.yml", new("toc: []"));

		var ceiling = fs.DirectoryInfo.New("/workspace/projects/other-repo/subrepo");
		var start = fs.DirectoryInfo.New("/workspace/projects/other-repo/subrepo/docs/team");

		var result = Paths.FindGitRoot(start, ceiling: ceiling);

		result.Should().BeNull("the .git belongs to a parent repo outside the ceiling");
	}

	[Fact]
	public void GitRootInsideCeiling_IsAccepted()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/workspace/.git");
		fs.AddDirectory("/workspace/docs/a/b/c");
		fs.AddFile("/workspace/docs/a/b/c/docset.yml", new("toc: []"));

		var ceiling = fs.DirectoryInfo.New("/workspace");
		var start = fs.DirectoryInfo.New("/workspace/docs/a/b/c");

		var result = Paths.FindGitRoot(start, ceiling: ceiling);

		result.Should().NotBeNull();
		result.FullName.Should().Be(ceiling.FullName);
	}

	[Fact]
	public void NoGitDirectory_ReturnsNull()
	{
		var fs = new MockFileSystem();
		fs.AddDirectory("/repo/docs");
		fs.AddFile("/repo/docs/docset.yml", new("toc: []"));

		var ceiling = fs.DirectoryInfo.New("/repo");
		var start = fs.DirectoryInfo.New("/repo/docs");

		var result = Paths.FindGitRoot(start, ceiling: ceiling);

		result.Should().BeNull();
	}

	[Fact]
	public void WorktreeGitFile_InsideCeiling_IsAccepted()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/repo/.git", new("gitdir: /main/.git/worktrees/repo"));
		fs.AddFile("/repo/docs/team/docset.yml", new("toc: []"));

		var ceiling = fs.DirectoryInfo.New("/repo");
		var start = fs.DirectoryInfo.New("/repo/docs/team");

		var result = Paths.FindGitRoot(start, ceiling: ceiling);

		result.Should().NotBeNull();
		result.FullName.Should().Be(ceiling.FullName);
	}
}
