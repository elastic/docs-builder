// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Authoring.Tests.Framework;

namespace Elastic.Authoring.Tests.Inline.CrossLinkRedirectAnchors;

public class Scenario1ComplexRedirectMappingWithAnchorDropping
{
	[Test, DisplayName("No anchor redirects to new-anchorless page")]
	public void NoAnchorRedirects() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-1-old.md",
			"/testing/redirects/multi-topic-page-1-new-anchorless"
		);

	[Test, DisplayName("Unmatched anchor for '!' rule redirects to new-anchorless page and drops anchor")]
	public void UnmatchedAnchorDropsAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-1-old.md#unmatched-anchor",
			"/testing/redirects/multi-topic-page-1-new-anchorless"
		);

	[Test, DisplayName("topic-a-intro redirects to topic-a-subpage and drops anchor (null target)")]
	public void TopicAIntroDropsAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-1-old.md#topic-a-intro",
			"/testing/redirects/multi-topic-page-1-new-topic-a-subpage"
		);

	[Test, DisplayName("topic-a-details redirects to topic-a-subpage with new anchor")]
	public void TopicADetailsRedirectsWithNewAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-1-old.md#topic-a-details",
			"/testing/redirects/multi-topic-page-1-new-topic-a-subpage#details-anchor"
		);

	[Test, DisplayName("topic-b-main redirects to topic-b-subpage with new anchor")]
	public void TopicBMainRedirectsWithNewAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-1-old.md#topic-b-main",
			"/testing/redirects/multi-topic-page-1-new-topic-b-subpage#main-anchor"
		);

	[Test, DisplayName("topic-c-main redirects to old page and keeps anchor")]
	public void TopicCMainKeepsAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-1-old.md#topic-c-main",
			"/testing/redirects/multi-topic-page-1-old#topic-c-main"
		);
}

public class Scenario2ComplexRedirectMappingWithAnchorPassing
{
	[Test, DisplayName("No anchor redirects to old page (self)")]
	public void NoAnchorRedirectsToSelf() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-2-old.md",
			"/testing/redirects/multi-topic-page-2-old"
		);

	[Test, DisplayName("Unmatched anchor for '{}' rule redirects to old page (self) and keeps anchor")]
	public void UnmatchedAnchorKeepsAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-2-old.md#unmatched-anchor",
			"/testing/redirects/multi-topic-page-2-old#unmatched-anchor"
		);

	[Test, DisplayName("topic-a-intro redirects to topic-a-subpage with new anchor")]
	public void TopicAIntroRedirectsWithNewAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-2-old.md#topic-a-intro",
			"/testing/redirects/multi-topic-page-2-new-topic-a-subpage#introduction"
		);

	[Test, DisplayName("topic-a-details redirects to topic-a-subpage and drops anchor (null target)")]
	public void TopicADetailsDropsAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-2-old.md#topic-a-details",
			"/testing/redirects/multi-topic-page-2-new-topic-a-subpage"
		);

	[Test, DisplayName("topic-b-main redirects to topic-b-subpage with new anchor")]
	public void TopicBMainRedirectsWithNewAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-2-old.md#topic-b-main",
			"/testing/redirects/multi-topic-page-2-new-topic-b-subpage#summary"
		);

	[Test, DisplayName("topic-b-config redirects to topic-b-subpage and drops anchor (null target)")]
	public void TopicBConfigDropsAnchor() =>
		CrossLinkResolverAssertions.ResolvesTo(
			"docs-content://testing/redirects/multi-topic-page-2-old.md#topic-b-config",
			"/testing/redirects/multi-topic-page-2-new-topic-b-subpage"
		);
}
