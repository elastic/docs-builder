// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Uploading;

namespace Elastic.Changelog.Tests.Uploading;

public class RegistryKeyTests
{
	[Theory]
	// Bundle index (artifact-root): bundle/{product}/registry.json — exactly one product segment.
	[InlineData("bundle/elasticsearch/registry.json")]
	[InlineData("bundle/kibana/registry.json")]
	[InlineData("bundle/elastic-agent/registry.json")]
	[InlineData("bundle/cloud_hosted/registry.json")]
	[InlineData("bundle/cloud-serverless/registry.json")]
	[InlineData("bundle/a/registry.json")]
	// Changelog-entry index (artifact-root): changelog/{org}/{repo}/{branch}/registry.json.
	[InlineData("changelog/elastic/elasticsearch/main/registry.json")]
	[InlineData("changelog/elastic/kibana/master/registry.json")]
	// External org (e.g. an acquired company keeping its own GitHub org).
	[InlineData("changelog/acme-corp/widgets/main/registry.json")]
	// Repo and branch segments may contain dots (e.g. apm-agent-dotnet, branch 8.x).
	[InlineData("changelog/elastic/apm.agent/main/registry.json")]
	[InlineData("changelog/elastic/elasticsearch/8.x/registry.json")]
	[InlineData("changelog/elastic/kibana/9.0/registry.json")]
	// Branch stored verbatim: a branch's own '/' become additional, valid key segments.
	[InlineData("changelog/elastic/kibana/feature/foo/registry.json")]
	[InlineData("changelog/elastic/kibana/release/8.x/registry.json")]
	public void IsRegistry_ValidArtifactRootKeys_ReturnsTrue(string key) =>
		RegistryKey.IsRegistry(key).Should().BeTrue();

	[Theory]
	[InlineData("")]
	[InlineData("registry.json")]
	[InlineData("/registry.json")]
	// Old product-first and single-segment changelog layouts are no longer valid manifest keys.
	[InlineData("elasticsearch/registry.json")]
	[InlineData("elasticsearch/changelog/registry.json")]
	[InlineData("changelog/elasticsearch/registry.json")]
	// Changelog manifests shallower than org/repo/branch (3 segments) are rejected.
	[InlineData("changelog/elastic/elasticsearch/registry.json")]
	// Missing/empty middle segment.
	[InlineData("bundle/registry.json")]
	[InlineData("changelog/registry.json")]
	[InlineData("bundle//registry.json")]
	[InlineData("changelog/elastic//main/registry.json")]
	// Unknown top-level prefix.
	[InlineData("entries/elastic/elasticsearch/main/registry.json")]
	[InlineData("elasticsearch/bundle/registry.json")]
	// Dots are allowed only for changelog segments, never for bundle product segments
	// (producers validate products as [a-zA-Z0-9_-]+).
	[InlineData("bundle/foo.bar/registry.json")]
	// Wrong extension.
	[InlineData("bundle/elasticsearch/registry.yaml")]
	[InlineData("changelog/elastic/elasticsearch/main/registry.yaml")]
	// Deeper nesting is rejected for bundles (must stay single-segment).
	[InlineData("bundle/elastic/search/registry.json")]
	// Traversal anywhere in the middle segments.
	[InlineData("bundle/../registry.json")]
	[InlineData("changelog/../registry.json")]
	[InlineData("changelog/elastic/../main/registry.json")]
	[InlineData("changelog/elastic/elasticsearch/../registry.json")]
	// Spaces (and other out-of-class characters) are rejected.
	[InlineData("bundle/elastic search/registry.json")]
	[InlineData("changelog/elastic/elastic search/main/registry.json")]
	public void IsRegistry_InvalidKeys_ReturnsFalse(string key) =>
		RegistryKey.IsRegistry(key).Should().BeFalse();
}
