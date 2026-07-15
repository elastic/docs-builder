// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// <see cref="ChangelogRepoOwnerResolver"/> is the single source of truth for owner/repo resolution shared by
/// CDN entry sourcing (<see cref="ChangelogBundlingService"/>) and upload (<c>ChangelogCommands</c>). These
/// tests pin the precedence so a <c>bundle.repo: "owner/repo"</c> value without an explicit <c>bundle.owner</c>
/// always resolves to the same owner on both the produce (upload) and consume (CDN sourcing) sides.
/// </summary>
public class ChangelogRepoOwnerResolverTests
{
	[Fact]
	public void ResolveOwner_ExplicitOwnerSet_ReturnsExplicitOwner() =>
		ChangelogRepoOwnerResolver.ResolveOwner("acme-corp", "widget", "elastic").Should().Be("acme-corp");

	[Fact]
	public void ResolveOwner_ExplicitOwnerSet_IgnoresCombinedRepoOwner() =>
		ChangelogRepoOwnerResolver.ResolveOwner("acme-corp", "other-org/widget", "elastic").Should().Be("acme-corp");

	[Fact]
	public void ResolveOwner_NoExplicitOwner_CombinedRepo_SplitsOwnerFromRepoPrefix() =>
		ChangelogRepoOwnerResolver.ResolveOwner(owner: null, repo: "acme-corp/widget", fallback: "elastic").Should().Be("acme-corp");

	[Fact]
	public void ResolveOwner_NoExplicitOwner_BareRepo_ReturnsFallback() =>
		ChangelogRepoOwnerResolver.ResolveOwner(owner: null, repo: "widget", fallback: "elastic").Should().Be("elastic");

	[Fact]
	public void ResolveOwner_NoExplicitOwnerOrRepo_ReturnsFallback() =>
		ChangelogRepoOwnerResolver.ResolveOwner(owner: null, repo: null, fallback: "elastic").Should().Be("elastic");

	[Fact]
	public void ResolveOwner_NoExplicitOwnerOrFallback_CombinedRepo_SplitsOwnerFromRepoPrefix() =>
		ChangelogRepoOwnerResolver.ResolveOwner(owner: null, repo: "acme-corp/widget", fallback: null).Should().Be("acme-corp");

	[Fact]
	public void NormalizeRepo_CombinedRepo_StripsOwnerPrefix() =>
		ChangelogRepoOwnerResolver.NormalizeRepo("acme-corp/widget").Should().Be("widget");

	[Fact]
	public void NormalizeRepo_BareRepo_ReturnsUnchanged() =>
		ChangelogRepoOwnerResolver.NormalizeRepo("widget").Should().Be("widget");

	[Fact]
	public void NormalizeRepo_NullOrEmpty_ReturnsUnchanged()
	{
		ChangelogRepoOwnerResolver.NormalizeRepo(null).Should().BeNull();
		ChangelogRepoOwnerResolver.NormalizeRepo(string.Empty).Should().Be(string.Empty);
	}

	/// <summary>
	/// Regression for elastic/docs-eng-team#636: with <c>bundle.repo: "acme-corp/widget"</c> and no explicit
	/// <c>bundle.owner</c>, upload and CDN sourcing must resolve to the same owner even when a different
	/// fallback (git remote owner for upload, a fixed default for CDN sourcing) is available — the combined
	/// repo's owner prefix takes precedence over either fallback.
	/// </summary>
	[Fact]
	public void ResolveOwner_CombinedRepoWithoutExplicitOwner_AgreesAcrossDifferentFallbacks()
	{
		const string repo = "acme-corp/widget";

		var cdnSourcingOwner = ChangelogRepoOwnerResolver.ResolveOwner(owner: null, repo, fallback: "elastic");
		var uploadOwner = ChangelogRepoOwnerResolver.ResolveOwner(owner: null, repo, fallback: "some-other-git-remote-owner");

		cdnSourcingOwner.Should().Be("acme-corp");
		uploadOwner.Should().Be("acme-corp");
		uploadOwner.Should().Be(cdnSourcingOwner);
	}
}
