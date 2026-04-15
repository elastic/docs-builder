// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class GitRemoteConfigurationReaderTests
{
	[Fact]
	public void TryReadOriginUrl_DotGitDirectory_ReadsConfig()
	{
		var fs = new MockFileSystem();
		fs.AddFile(
			"/repo/.git/config",
			new("""
			    [remote "origin"]
			    	url = git@github.com:elastic/kibana.git
			    """));

		var ok = GitRemoteConfigurationReader.TryReadOriginUrl(fs, "/repo", out var url);

		ok.Should().BeTrue();
		url.Should().Be("git@github.com:elastic/kibana.git");
	}

	[Fact]
	public void TryReadOriginUrl_GitWorktreeFile_ResolvesGitDir()
	{
		var fs = new MockFileSystem();
		fs.AddFile(
			"/wt/.git",
			new("gitdir: /main/.git/worktrees/wt\n"));
		fs.AddFile(
			"/main/.git/worktrees/wt/commondir",
			new("../..\n"));
		fs.AddFile(
			"/main/.git/config",
			new("""
			    [remote "origin"]
			    	url = https://github.com/elastic/kibana.git
			    """));

		var ok = GitRemoteConfigurationReader.TryReadOriginUrl(fs, "/wt", out var url);

		ok.Should().BeTrue();
		url.Should().Be("https://github.com/elastic/kibana.git");
	}

	[Fact]
	public void TryReadOriginUrl_GitWorktreeFile_MissingCommondir_ReturnsFalse()
	{
		var fs = new MockFileSystem();
		fs.AddFile(
			"/wt/.git",
			new("gitdir: /main/.git/worktrees/wt\n"));

		var ok = GitRemoteConfigurationReader.TryReadOriginUrl(fs, "/wt", out var url);

		ok.Should().BeFalse();
		url.Should().BeNull();
	}
}
