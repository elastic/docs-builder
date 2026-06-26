// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Uploading;

namespace Elastic.Changelog.Tests.Uploading;

public class RegistryKeyTests
{
	[Theory]
	// Bundle index (artifact-root): bundle/{product}/registry.json
	[InlineData("bundle/elasticsearch/registry.json")]
	[InlineData("bundle/kibana/registry.json")]
	[InlineData("bundle/elastic-agent/registry.json")]
	[InlineData("bundle/cloud_hosted/registry.json")]
	[InlineData("bundle/cloud-serverless/registry.json")]
	[InlineData("bundle/a/registry.json")]
	// Changelog-entry index (artifact-root): changelog/{repo}/registry.json
	[InlineData("changelog/elasticsearch/registry.json")]
	[InlineData("changelog/kibana/registry.json")]
	[InlineData("changelog/cloud/registry.json")]
	// Repo segments may contain dots (e.g. apm-agent-dotnet, elasticsearch-net).
	[InlineData("changelog/apm.agent/registry.json")]
	public void IsRegistry_ValidArtifactRootKeys_ReturnsTrue(string key) =>
		RegistryKey.IsRegistry(key).Should().BeTrue();

	[Theory]
	[InlineData("")]
	[InlineData("registry.json")]
	[InlineData("/registry.json")]
	// Old product-first layout is no longer a valid manifest key.
	[InlineData("elasticsearch/registry.json")]
	[InlineData("elasticsearch/changelog/registry.json")]
	// Missing/empty middle segment.
	[InlineData("bundle/registry.json")]
	[InlineData("changelog/registry.json")]
	[InlineData("bundle//registry.json")]
	// Unknown top-level prefix.
	[InlineData("entries/elasticsearch/registry.json")]
	[InlineData("elasticsearch/bundle/registry.json")]
	// Dots are allowed only for changelog repo segments, never for bundle product segments
	// (producers validate products as [a-zA-Z0-9_-]+).
	[InlineData("bundle/foo.bar/registry.json")]
	// Wrong extension.
	[InlineData("bundle/elasticsearch/registry.yaml")]
	[InlineData("changelog/elasticsearch/registry.yaml")]
	// Deeper nesting / traversal in the middle segment.
	[InlineData("bundle/elastic/search/registry.json")]
	[InlineData("changelog/elastic/search/registry.json")]
	[InlineData("bundle/../registry.json")]
	[InlineData("changelog/../registry.json")]
	[InlineData("bundle/elastic search/registry.json")]
	public void IsRegistry_InvalidKeys_ReturnsFalse(string key) =>
		RegistryKey.IsRegistry(key).Should().BeFalse();
}
