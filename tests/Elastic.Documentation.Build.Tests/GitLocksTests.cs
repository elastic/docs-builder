// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.ExternalCommands;

namespace Elastic.Documentation.Build.Tests;

public class GitLocksTests
{
	[Fact]
	public void ClearStale_RemovesAllLockFilesUnderGitDir()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/repo/.git/shallow.lock", new MockFileData(""));
		fs.AddFile("/repo/.git/refs/heads/main.lock", new MockFileData(""));
		fs.AddFile("/repo/.git/index.lock", new MockFileData(""));
		var cleared = new List<string>();

		GitLocks.ClearStale(fs, "/repo", cleared.Add);

		cleared.Should().HaveCount(3);
		fs.File.Exists("/repo/.git/shallow.lock").Should().BeFalse();
		fs.File.Exists("/repo/.git/refs/heads/main.lock").Should().BeFalse();
		fs.File.Exists("/repo/.git/index.lock").Should().BeFalse();
	}

	[Fact]
	public void ClearStale_DoesNotDeleteNonLockFiles()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/repo/.git/shallow.lock", new MockFileData(""));
		fs.AddFile("/repo/.git/config", new MockFileData("[core]"));
		var cleared = new List<string>();

		GitLocks.ClearStale(fs, "/repo", cleared.Add);

		cleared.Should().HaveCount(1);
		fs.File.Exists("/repo/.git/config").Should().BeTrue();
	}

	[Fact]
	public void ClearStale_WhenNoGitDir_DoesNothing()
	{
		var fs = new MockFileSystem();
		var cleared = new List<string>();

		var act = () => GitLocks.ClearStale(fs, "/repo", cleared.Add);

		act.Should().NotThrow();
		cleared.Should().BeEmpty();
	}
}
