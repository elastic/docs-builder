// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class GitHubRemoteParserTests
{
	[Theory]
	[InlineData("https://github.com/elastic/kibana.git", "elastic", "kibana")]
	[InlineData("https://github.com/elastic/kibana", "elastic", "kibana")]
	[InlineData("http://github.com/elastic/kibana", "elastic", "kibana")]
	[InlineData("https://github.com/elastic/some.repo.git", "elastic", "some.repo")]
	[InlineData("git@github.com:elastic/kibana.git", "elastic", "kibana")]
	[InlineData("ssh://git@github.com/elastic/kibana.git", "elastic", "kibana")]
	public void TryParseGitHubComOwnerRepo_ValidGitHubUrls_ReturnsOwnerRepo(string url, string expectedOwner, string expectedRepo)
	{
		var ok = GitHubRemoteParser.TryParseGitHubComOwnerRepo(url, out var owner, out var repo);

		ok.Should().BeTrue();
		owner.Should().Be(expectedOwner);
		repo.Should().Be(expectedRepo);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("https://gitlab.com/elastic/kibana")]
	[InlineData("https://github.com/elastic")]
	[InlineData("https://github.com/")]
	[InlineData("not-a-url")]
	public void TryParseGitHubComOwnerRepo_Invalid_ReturnsFalse(string? url)
	{
		var ok = GitHubRemoteParser.TryParseGitHubComOwnerRepo(url, out var owner, out var repo);

		ok.Should().BeFalse();
		owner.Should().BeNull();
		repo.Should().BeNull();
	}
}
