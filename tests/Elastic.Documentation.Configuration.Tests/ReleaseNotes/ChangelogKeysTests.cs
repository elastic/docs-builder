// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

public class ChangelogKeysTests
{
	[Theory]
	[InlineData("elasticsearch")]
	[InlineData("elastic-agent")]
	[InlineData("cloud_hosted")]
	[InlineData("a")]
	[InlineData("Agent2")]
	public void IsValidProduct_ValidNames_ReturnsTrue(string product) =>
		ChangelogKeys.IsValidProduct(product).Should().BeTrue();

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	// Products never contain dots (unlike repos/branches).
	[InlineData("foo.bar")]
	[InlineData(".")]
	[InlineData("..")]
	[InlineData("foo bar")]
	[InlineData("foo/bar")]
	public void IsValidProduct_InvalidNames_ReturnsFalse(string? product) =>
		ChangelogKeys.IsValidProduct(product).Should().BeFalse();

	[Theory]
	[InlineData("elastic")]
	[InlineData("acme-corp")]
	[InlineData("ACME1")]
	public void IsValidOrg_ValidLogins_ReturnsTrue(string org) =>
		ChangelogKeys.IsValidOrg(org).Should().BeTrue();

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	// GitHub logins are ASCII alphanumerics and hyphens; dots and underscores are excluded.
	[InlineData("acme.corp")]
	[InlineData("acme_corp")]
	[InlineData(".")]
	[InlineData("..")]
	[InlineData("acme corp")]
	[InlineData("acme/corp")]
	public void IsValidOrg_InvalidLogins_ReturnsFalse(string? org) =>
		ChangelogKeys.IsValidOrg(org).Should().BeFalse();

	[Theory]
	[InlineData("elasticsearch")]
	// Repo names may contain dots (e.g. apm-agent-dotnet forks like apm.agent).
	[InlineData("apm.agent")]
	[InlineData("my_repo")]
	[InlineData("repo-1")]
	public void IsValidRepo_ValidNames_ReturnsTrue(string repo) =>
		ChangelogKeys.IsValidRepo(repo).Should().BeTrue();

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	// "." / ".." match the character class but are rejected to prevent traversal.
	[InlineData(".")]
	[InlineData("..")]
	[InlineData("a/b")]
	[InlineData("a b")]
	public void IsValidRepo_InvalidNames_ReturnsFalse(string? repo) =>
		ChangelogKeys.IsValidRepo(repo).Should().BeFalse();

	[Theory]
	[InlineData("main")]
	[InlineData("8.x")]
	[InlineData("9.0")]
	// Branches are stored verbatim: each '/'-delimited part is validated on its own.
	[InlineData("feature/foo")]
	[InlineData("release/8.x")]
	[InlineData("a_b")]
	public void IsValidBranch_ValidBranches_ReturnsTrue(string branch) =>
		ChangelogKeys.IsValidBranch(branch).Should().BeTrue();

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("/")]
	[InlineData("/main")]
	[InlineData("feature/")]
	[InlineData("feature//foo")]
	[InlineData(".")]
	[InlineData("..")]
	[InlineData("feature/..")]
	[InlineData("a b")]
	public void IsValidBranch_InvalidBranches_ReturnsFalse(string? branch) =>
		ChangelogKeys.IsValidBranch(branch).Should().BeFalse();

	[Theory]
	[InlineData("entry.yaml")]
	[InlineData("registry.json")]
	[InlineData("9.0.0.yaml")]
	public void IsSafeFileName_SingleSegments_ReturnsTrue(string fileName) =>
		ChangelogKeys.IsSafeFileName(fileName).Should().BeTrue();

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(".")]
	[InlineData("..")]
	[InlineData("a/b.yaml")]
	[InlineData(@"a\b.yaml")]
	public void IsSafeFileName_TraversalOrMultiSegment_ReturnsFalse(string? fileName) =>
		ChangelogKeys.IsSafeFileName(fileName).Should().BeFalse();

	[Fact]
	public void BundleFileKey_ComposesArtifactRootKey() =>
		ChangelogKeys.BundleFileKey("elasticsearch", "9.0.0.yaml")
			.Should().Be("bundle/elasticsearch/9.0.0.yaml");

	[Fact]
	public void ChangelogFileKey_ComposesArtifactRootKey() =>
		ChangelogKeys.ChangelogFileKey("elastic", "kibana", "main", "entry.yaml")
			.Should().Be("changelog/elastic/kibana/main/entry.yaml");

	[Fact]
	public void ChangelogFileKey_BranchSlashesBecomeKeySegments() =>
		ChangelogKeys.ChangelogFileKey("elastic", "kibana", "feature/foo", "entry.yaml")
			.Should().Be("changelog/elastic/kibana/feature/foo/entry.yaml");

	[Fact]
	public void BundleRegistryKey_ComposesManifestKey() =>
		ChangelogKeys.BundleRegistryKey("elasticsearch")
			.Should().Be("bundle/elasticsearch/registry.json");

	[Fact]
	public void ChangelogRegistryKey_ComposesManifestKeyFromGroup() =>
		ChangelogKeys.ChangelogRegistryKey("elastic/kibana/main")
			.Should().Be("changelog/elastic/kibana/main/registry.json");

	[Theory]
	[InlineData("bundle/elasticsearch/9.0.0.yaml", "elasticsearch")]
	[InlineData("bundle/elastic-agent/entry.yaml", "elastic-agent")]
	public void ExtractBundleGroup_BundleKeys_ReturnsProduct(string key, string expected) =>
		ChangelogKeys.ExtractBundleGroup(key).Should().Be(expected);

	[Theory]
	[InlineData("changelog/elastic/kibana/main/entry.yaml")]
	// No product segment ahead of the file name.
	[InlineData("bundle/entry.yaml")]
	[InlineData("bundle//entry.yaml")]
	[InlineData("other/elasticsearch/entry.yaml")]
	public void ExtractBundleGroup_NonBundleKeys_ReturnsNull(string key) =>
		ChangelogKeys.ExtractBundleGroup(key).Should().BeNull();

	[Theory]
	[InlineData("changelog/elastic/kibana/main/entry.yaml", "elastic/kibana/main")]
	// The branch's own '/' produce extra segments; the group is everything before the file name.
	[InlineData("changelog/elastic/kibana/feature/foo/entry.yaml", "elastic/kibana/feature/foo")]
	public void ExtractChangelogGroup_EntryKeys_ReturnsPool(string key, string expected) =>
		ChangelogKeys.ExtractChangelogGroup(key).Should().Be(expected);

	[Theory]
	[InlineData("bundle/elasticsearch/9.0.0.yaml")]
	// Shallower than org/repo/branch ahead of the file name.
	[InlineData("changelog/elastic/kibana/entry.yaml")]
	[InlineData("changelog/entry.yaml")]
	[InlineData("other/elastic/kibana/main/entry.yaml")]
	public void ExtractChangelogGroup_NonEntryKeys_ReturnsNull(string key) =>
		ChangelogKeys.ExtractChangelogGroup(key).Should().BeNull();

	[Fact]
	public void BundleSegments_ReturnsPrefixAndProduct() =>
		ChangelogKeys.BundleSegments("elasticsearch")
			.Should().Equal("bundle", "elasticsearch");

	[Fact]
	public void PoolSegments_ExpandsBranchSlashesIntoSegments() =>
		ChangelogKeys.PoolSegments("elastic", "kibana", "feature/foo")
			.Should().Equal("changelog", "elastic", "kibana", "feature", "foo");

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
	// Repo and branch segments may contain underscores; orgs may not.
	[InlineData("changelog/elastic/my_repo/main/registry.json")]
	[InlineData("changelog/elastic/kibana/my_branch/registry.json")]
	// Branch stored verbatim: a branch's own '/' become additional, valid key segments.
	[InlineData("changelog/elastic/kibana/feature/foo/registry.json")]
	[InlineData("changelog/elastic/kibana/release/8.x/registry.json")]
	public void IsRegistry_ValidArtifactRootKeys_ReturnsTrue(string key) =>
		ChangelogKeys.IsRegistry(key).Should().BeTrue();

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
	// Dots are allowed only for changelog repo/branch segments, never for bundle product segments
	// (producers validate products as [a-zA-Z0-9_-]+).
	[InlineData("bundle/foo.bar/registry.json")]
	// The org segment follows the producer's GitHub-login rule: no dots or underscores.
	[InlineData("changelog/acme.corp/widgets/main/registry.json")]
	[InlineData("changelog/acme_corp/widgets/main/registry.json")]
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
		ChangelogKeys.IsRegistry(key).Should().BeFalse();
}
