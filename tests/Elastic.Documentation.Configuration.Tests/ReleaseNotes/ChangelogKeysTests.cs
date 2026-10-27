// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

public class ChangelogKeysTests
{
	[Test]
	[Arguments("elasticsearch")]
	[Arguments("elastic-agent")]
	[Arguments("cloud_hosted")]
	[Arguments("a")]
	[Arguments("Agent2")]
	public void IsValidProduct_ValidNames_ReturnsTrue(string product) => ChangelogKeys.IsValidProduct(product).Should().BeTrue();

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments(" ")]
	// Products never contain dots (unlike repos/branches).
	[Arguments("foo.bar")]
	[Arguments(".")]
	[Arguments("..")]
	[Arguments("foo bar")]
	[Arguments("foo/bar")]
	public void IsValidProduct_InvalidNames_ReturnsFalse(string? product) => ChangelogKeys.IsValidProduct(product).Should().BeFalse();

	[Test]
	[Arguments("elastic")]
	[Arguments("acme-corp")]
	[Arguments("ACME1")]
	public void IsValidOrg_ValidLogins_ReturnsTrue(string org) => ChangelogKeys.IsValidOrg(org).Should().BeTrue();

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments(" ")]
	// GitHub logins are ASCII alphanumerics and hyphens; dots and underscores are excluded.
	[Arguments("acme.corp")]
	[Arguments("acme_corp")]
	[Arguments(".")]
	[Arguments("..")]
	[Arguments("acme corp")]
	[Arguments("acme/corp")]
	public void IsValidOrg_InvalidLogins_ReturnsFalse(string? org) => ChangelogKeys.IsValidOrg(org).Should().BeFalse();

	[Test]
	[Arguments("elasticsearch")]
	// Repo names may contain dots (e.g. apm-agent-dotnet forks like apm.agent).
	[Arguments("apm.agent")]
	[Arguments("my_repo")]
	[Arguments("repo-1")]
	public void IsValidRepo_ValidNames_ReturnsTrue(string repo) => ChangelogKeys.IsValidRepo(repo).Should().BeTrue();

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments(" ")]
	// "." / ".." match the character class but are rejected to prevent traversal.
	[Arguments(".")]
	[Arguments("..")]
	[Arguments("a/b")]
	[Arguments("a b")]
	public void IsValidRepo_InvalidNames_ReturnsFalse(string? repo) => ChangelogKeys.IsValidRepo(repo).Should().BeFalse();

	[Test]
	[Arguments("main")]
	[Arguments("8.x")]
	[Arguments("9.0")]
	// Branches are stored verbatim: each '/'-delimited part is validated on its own.
	[Arguments("feature/foo")]
	[Arguments("release/8.x")]
	[Arguments("a_b")]
	public void IsValidBranch_ValidBranches_ReturnsTrue(string branch) => ChangelogKeys.IsValidBranch(branch).Should().BeTrue();

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments(" ")]
	[Arguments("/")]
	[Arguments("/main")]
	[Arguments("feature/")]
	[Arguments("feature//foo")]
	[Arguments(".")]
	[Arguments("..")]
	[Arguments("feature/..")]
	[Arguments("a b")]
	public void IsValidBranch_InvalidBranches_ReturnsFalse(string? branch) => ChangelogKeys.IsValidBranch(branch).Should().BeFalse();

	[Test]
	[Arguments("entry.yaml")]
	[Arguments("registry.json")]
	[Arguments("9.0.0.yaml")]
	public void IsSafeFileName_SingleSegments_ReturnsTrue(string fileName) => ChangelogKeys.IsSafeFileName(fileName).Should().BeTrue();

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments(" ")]
	[Arguments(".")]
	[Arguments("..")]
	[Arguments("a/b.yaml")]
	[Arguments(@"a\b.yaml")]
	public void IsSafeFileName_TraversalOrMultiSegment_ReturnsFalse(string? fileName) =>
		ChangelogKeys.IsSafeFileName(fileName).Should().BeFalse();

	[Test]
	public void BundleFileKey_ComposesArtifactRootKey() =>
		ChangelogKeys.BundleFileKey("elasticsearch", "9.0.0.yaml").Should().Be("bundle/elasticsearch/9.0.0.yaml");

	[Test]
	public void ChangelogFileKey_ComposesArtifactRootKey() =>
		ChangelogKeys.ChangelogFileKey("elastic", "kibana", "main", "entry.yaml").Should().Be("changelog/elastic/kibana/main/entry.yaml");

	[Test]
	public void ChangelogFileKey_BranchSlashesBecomeKeySegments() =>
		ChangelogKeys
			.ChangelogFileKey("elastic", "kibana", "feature/foo", "entry.yaml")
			.Should()
			.Be("changelog/elastic/kibana/feature/foo/entry.yaml");

	[Test]
	public void BundleRegistryKey_ComposesManifestKey() =>
		ChangelogKeys.BundleRegistryKey("elasticsearch").Should().Be("bundle/elasticsearch/registry.json");

	[Test]
	public void ChangelogRegistryKey_ComposesManifestKeyFromGroup() =>
		ChangelogKeys.ChangelogRegistryKey("elastic/kibana/main").Should().Be("changelog/elastic/kibana/main/registry.json");

	[Test]
	[Arguments("bundle/elasticsearch/9.0.0.yaml", "elasticsearch")]
	[Arguments("bundle/elastic-agent/entry.yaml", "elastic-agent")]
	public void ExtractBundleGroup_BundleKeys_ReturnsProduct(string key, string expected) =>
		ChangelogKeys.ExtractBundleGroup(key).Should().Be(expected);

	[Test]
	[Arguments("changelog/elastic/kibana/main/entry.yaml")]
	// No product segment ahead of the file name.
	[Arguments("bundle/entry.yaml")]
	[Arguments("bundle//entry.yaml")]
	[Arguments("other/elasticsearch/entry.yaml")]
	// The product segment is validated on extraction so an out-of-class group can never be
	// re-composed into a registry key or URI (e.g. via BundleRegistryKey).
	[Arguments("bundle/../entry.yaml")]
	[Arguments("bundle/foo.bar/entry.yaml")]
	[Arguments("bundle/elastic search/entry.yaml")]
	public void ExtractBundleGroup_NonBundleKeys_ReturnsNull(string key) => ChangelogKeys.ExtractBundleGroup(key).Should().BeNull();

	[Test]
	[Arguments("changelog/elastic/kibana/main/entry.yaml", "elastic/kibana/main")]
	// The branch's own '/' produce extra segments; the group is everything before the file name.
	[Arguments("changelog/elastic/kibana/feature/foo/entry.yaml", "elastic/kibana/feature/foo")]
	public void ExtractChangelogGroup_EntryKeys_ReturnsPool(string key, string expected) =>
		ChangelogKeys.ExtractChangelogGroup(key).Should().Be(expected);

	[Test]
	[Arguments("bundle/elasticsearch/9.0.0.yaml")]
	// Shallower than org/repo/branch ahead of the file name.
	[Arguments("changelog/elastic/kibana/entry.yaml")]
	[Arguments("changelog/entry.yaml")]
	[Arguments("other/elastic/kibana/main/entry.yaml")]
	// Each group segment is validated per position on extraction (same rules as IsRegistry),
	// so traversal, empty, or out-of-class segments can never be re-composed into keys or URIs.
	[Arguments("changelog/elastic//main/entry.yaml")]
	[Arguments("changelog/../kibana/main/entry.yaml")]
	[Arguments("changelog/elastic/../main/entry.yaml")]
	[Arguments("changelog/acme.corp/widgets/main/entry.yaml")]
	[Arguments("changelog/elastic/elastic search/main/entry.yaml")]
	public void ExtractChangelogGroup_NonEntryKeys_ReturnsNull(string key) => ChangelogKeys.ExtractChangelogGroup(key).Should().BeNull();

	[Test]
	public void BundleSegments_ReturnsPrefixAndProduct() =>
		ChangelogKeys.BundleSegments("elasticsearch").Should().Equal("bundle", "elasticsearch");

	[Test]
	public void PoolSegments_ExpandsBranchSlashesIntoSegments() =>
		ChangelogKeys.PoolSegments("elastic", "kibana", "feature/foo").Should().Equal("changelog", "elastic", "kibana", "feature", "foo");

	[Test]
	// Bundle index (artifact-root): bundle/{product}/registry.json — exactly one product segment.
	[Arguments("bundle/elasticsearch/registry.json")]
	[Arguments("bundle/kibana/registry.json")]
	[Arguments("bundle/elastic-agent/registry.json")]
	[Arguments("bundle/cloud_hosted/registry.json")]
	[Arguments("bundle/cloud-serverless/registry.json")]
	[Arguments("bundle/a/registry.json")]
	// Changelog-entry index (artifact-root): changelog/{org}/{repo}/{branch}/registry.json.
	[Arguments("changelog/elastic/elasticsearch/main/registry.json")]
	[Arguments("changelog/elastic/kibana/master/registry.json")]
	// External org (e.g. an acquired company keeping its own GitHub org).
	[Arguments("changelog/acme-corp/widgets/main/registry.json")]
	// Repo and branch segments may contain dots (e.g. apm-agent-dotnet, branch 8.x).
	[Arguments("changelog/elastic/apm.agent/main/registry.json")]
	[Arguments("changelog/elastic/elasticsearch/8.x/registry.json")]
	[Arguments("changelog/elastic/kibana/9.0/registry.json")]
	// Repo and branch segments may contain underscores; orgs may not.
	[Arguments("changelog/elastic/my_repo/main/registry.json")]
	[Arguments("changelog/elastic/kibana/my_branch/registry.json")]
	// Branch stored verbatim: a branch's own '/' become additional, valid key segments.
	[Arguments("changelog/elastic/kibana/feature/foo/registry.json")]
	[Arguments("changelog/elastic/kibana/release/8.x/registry.json")]
	public void IsRegistry_ValidArtifactRootKeys_ReturnsTrue(string key) => ChangelogKeys.IsRegistry(key).Should().BeTrue();

	[Test]
	[Arguments("")]
	[Arguments("registry.json")]
	[Arguments("/registry.json")]
	// Old product-first and single-segment changelog layouts are no longer valid manifest keys.
	[Arguments("elasticsearch/registry.json")]
	[Arguments("elasticsearch/changelog/registry.json")]
	[Arguments("changelog/elasticsearch/registry.json")]
	// Changelog manifests shallower than org/repo/branch (3 segments) are rejected.
	[Arguments("changelog/elastic/elasticsearch/registry.json")]
	// Missing/empty middle segment.
	[Arguments("bundle/registry.json")]
	[Arguments("changelog/registry.json")]
	[Arguments("bundle//registry.json")]
	[Arguments("changelog/elastic//main/registry.json")]
	// Unknown top-level prefix.
	[Arguments("entries/elastic/elasticsearch/main/registry.json")]
	[Arguments("elasticsearch/bundle/registry.json")]
	// Dots are allowed only for changelog repo/branch segments, never for bundle product segments
	// (producers validate products as [a-zA-Z0-9_-]+).
	[Arguments("bundle/foo.bar/registry.json")]
	// The org segment follows the producer's GitHub-login rule: no dots or underscores.
	[Arguments("changelog/acme.corp/widgets/main/registry.json")]
	[Arguments("changelog/acme_corp/widgets/main/registry.json")]
	// Wrong extension.
	[Arguments("bundle/elasticsearch/registry.yaml")]
	[Arguments("changelog/elastic/elasticsearch/main/registry.yaml")]
	// Deeper nesting is rejected for bundles (must stay single-segment).
	[Arguments("bundle/elastic/search/registry.json")]
	// Traversal anywhere in the middle segments.
	[Arguments("bundle/../registry.json")]
	[Arguments("changelog/../registry.json")]
	[Arguments("changelog/elastic/../main/registry.json")]
	[Arguments("changelog/elastic/elasticsearch/../registry.json")]
	// Spaces (and other out-of-class characters) are rejected.
	[Arguments("bundle/elastic search/registry.json")]
	[Arguments("changelog/elastic/elastic search/main/registry.json")]
	public void IsRegistry_InvalidKeys_ReturnsFalse(string key) => ChangelogKeys.IsRegistry(key).Should().BeFalse();

	[Test]
	[Arguments("/bundle/elasticsearch/9.3.0.yaml", "elasticsearch", "9.3.0.yaml")]
	[Arguments("bundle/elasticsearch/9.3.0.yaml", "elasticsearch", "9.3.0.yaml")]
	[Arguments("https://cdn.example/bundle/elasticsearch/9.3.0.yaml", "elasticsearch", "9.3.0.yaml")]
	[Arguments("https://cdn.example/prefix/bundle/kibana/9.3.0.yaml", "kibana", "9.3.0.yaml")]
	[Arguments("/bundle/elasticsearch/9.3.0.amend-1.yaml", "elasticsearch", "9.3.0.amend-1.yaml")]
	public void TryParseBundleLocator_BundlePaths_ReturnsProductAndFile(string input, string product, string fileName)
	{
		var parsed = ChangelogKeys.TryParseBundleLocator(input, out var parsedProduct, out var parsedFile);
		parsed.Should().BeTrue();
		parsedProduct.Should().Be(product);
		parsedFile.Should().Be(fileName);
	}

	[Test]
	[Arguments("/changelog/elastic/kibana/main/entry.yaml")]
	[Arguments("https://cdn.example/changelog/elastic/kibana/main/entry.yaml")]
	[Arguments("/bundle/elasticsearch")]
	[Arguments("not-a-path")]
	[Arguments("bundle/foo.bar/9.3.0.yaml")]
	[Arguments("/bundle/elasticsearch/a/b.yaml")]
	[Arguments("https://cdn.example/notbundle/kibana/9.3.0.yaml")]
	[Arguments("")]
	[Arguments(null)]
	public void TryParseBundleLocator_NonBundlePaths_ReturnsFalse(string? input)
	{
		ChangelogKeys.TryParseBundleLocator(input, out var product, out var fileName).Should().BeFalse();
		product.Should().BeNull();
		fileName.Should().BeNull();
	}
}
