// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class IsolatedApiAliasesTests
{
	[Theory]
	[InlineData("doc/elasticsearch", "doc/docs-builder-elasticsearch")]
	[InlineData("doc/elasticsearch/operation/operation-search", "doc/docs-builder-elasticsearch/operation/operation-search")]
	[InlineData("doc/kibana/operation/operation-post-agent-builder-conversations-conversation-id-attachments", "doc/docs-builder-kibana/operation/operation-post-agent-builder-conversations-conversation-id-attachments")]
	[InlineData("/doc/elasticsearch/", "doc/docs-builder-elasticsearch")]
	public void TryPrefixedDocSlug_ShortProductKey_MapsToFixture(string slug, string expected)
	{
		IsolatedApiAliases.TryPrefixedDocSlug(slug, out var prefixed).Should().BeTrue();
		prefixed.Should().Be(expected);
	}

	[Theory]
	[InlineData("doc/docs-builder-elasticsearch")]
	[InlineData("doc/docs-builder-elasticsearch/operation/operation-search")]
	[InlineData("api/doc/elasticsearch")]
	[InlineData("")]
	[InlineData("doc/")]
	public void TryPrefixedDocSlug_AlreadyPrefixedOrInvalid_ReturnsFalse(string slug)
	{
		IsolatedApiAliases.TryPrefixedDocSlug(slug, out var prefixed).Should().BeFalse();
		prefixed.Should().BeNull();
	}
}
