// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class IsolatedApiAliasesTests
{
	[Test]
	[Arguments("doc/elasticsearch", "doc/docs-builder-elasticsearch")]
	[Arguments("doc/elasticsearch/operation/operation-search", "doc/docs-builder-elasticsearch/operation/operation-search")]
	[Arguments("doc/kibana/operation/operation-post-agent-builder-conversations-conversation-id-attachments", "doc/docs-builder-kibana/operation/operation-post-agent-builder-conversations-conversation-id-attachments")]
	[Arguments("/doc/elasticsearch/", "doc/docs-builder-elasticsearch")]
	public void TryPrefixedDocSlug_ShortProductKey_MapsToFixture(string slug, string expected)
	{
		IsolatedApiAliases.TryPrefixedDocSlug(slug, out var prefixed).Should().BeTrue();
		prefixed.Should().Be(expected);
	}

	[Test]
	[Arguments("doc/docs-builder-elasticsearch")]
	[Arguments("doc/docs-builder-elasticsearch/operation/operation-search")]
	[Arguments("api/doc/elasticsearch")]
	[Arguments("")]
	[Arguments("doc/")]
	public void TryPrefixedDocSlug_AlreadyPrefixedOrInvalid_ReturnsFalse(string slug)
	{
		IsolatedApiAliases.TryPrefixedDocSlug(slug, out var prefixed).Should().BeFalse();
		prefixed.Should().BeNull();
	}
}
