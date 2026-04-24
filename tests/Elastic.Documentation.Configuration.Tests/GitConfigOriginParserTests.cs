// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class GitConfigOriginParserTests
{
	[Fact]
	public void TryGetRemoteOriginUrl_StandardConfig_ReturnsUrl()
	{
		var yaml = """
		           [core]
		           	repositoryformatversion = 0
		           [remote "origin"]
		           	url = https://github.com/elastic/kibana.git
		           	fetch = +refs/heads/*:refs/remotes/origin/*
		           """;

		var ok = GitConfigOriginParser.TryGetRemoteOriginUrl(yaml, out var url);

		ok.Should().BeTrue();
		url.Should().Be("https://github.com/elastic/kibana.git");
	}

	[Fact]
	public void TryGetRemoteOriginUrl_QuotedUrl_ReturnsUnquoted()
	{
		var yaml = """
		           [remote "origin"]
		           	url = "https://github.com/elastic/kibana.git"
		           """;

		var ok = GitConfigOriginParser.TryGetRemoteOriginUrl(yaml, out var url);

		ok.Should().BeTrue();
		url.Should().Be("https://github.com/elastic/kibana.git");
	}

	[Fact]
	public void TryGetRemoteOriginUrl_NoOrigin_ReturnsFalse()
	{
		var yaml = """
		           [remote "upstream"]
		           	url = https://github.com/elastic/kibana.git
		           """;

		var ok = GitConfigOriginParser.TryGetRemoteOriginUrl(yaml, out var url);

		ok.Should().BeFalse();
		url.Should().BeNull();
	}
}
