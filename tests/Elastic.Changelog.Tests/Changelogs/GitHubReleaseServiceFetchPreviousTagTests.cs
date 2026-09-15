// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.GitHub;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Tests for <see cref="GitHubReleaseService.FetchPreviousTagAsync"/>: semver-aware, prefix-aware
/// previous-release lookup via the paginated releases API.
/// Covers sequential histories, interleaved multi-version lines, prefixed tags, and pagination.
/// </summary>
public class GitHubReleaseServiceFetchPreviousTagTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private const string Owner = "elastic";
	private const string Repo = "elasticsearch";

	private GitHubReleaseService Service(StubHandler handler) =>
		new(new TestLoggerFactory(Output), new GitHubApiTransport(handler, "test-token"));

	/// <summary>Builds a JSON releases array string (newest-first order).</summary>
	private static string ReleasesJson(params string[] tagNames)
	{
		var items = tagNames.Select(
			t =>
				$$$"""{"tag_name":"{{{t}}}","name":"{{{t}}}","body":"","prerelease":false,"draft":false,"html_url":"https://github.com/elastic/elasticsearch/releases/tag/{{{t}}}","published_at":null,"assets":[]}"""
		);
		return $"[{string.Join(",", items)}]";
	}

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	// ─────────────────────────────────────────────────────────────────────────
	// Basic sequential history
	// ─────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task FetchPreviousTag_SequentialHistory_ReturnsPredecessor()
	{
		var handler = new StubHandler(_ => Json(ReleasesJson("v1.2.0", "v1.1.0", "v1.0.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0");
		result.Should().Be("v1.1.0");
	}

	[Fact]
	public async Task FetchPreviousTag_SingleRelease_ReturnsNull()
	{
		var handler = new StubHandler(_ => Json(ReleasesJson("v1.0.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.0.0");
		result.Should().BeNull();
	}

	[Fact]
	public async Task FetchPreviousTag_TagNotInList_StillFindsHighestBelowBySemver()
	{
		// v1.5.0 is not in the releases list, but the semver ordering still finds the highest
		// version below it (v1.1.0) without requiring the current tag to be present.
		var handler = new StubHandler(req =>
		{
			if (req.RequestUri!.PathAndQuery.Contains("/tags"))
				return Json("[]");
			return Json(ReleasesJson("v1.1.0", "v1.0.0"));
		});
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.5.0");
		result.Should().Be("v1.1.0");
	}

	[Fact]
	public async Task FetchPreviousTag_EmptyReleaseList_ReturnsNull()
	{
		var handler = new StubHandler(_ => Json("[]"));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.0.0");
		result.Should().BeNull();
	}

	[Fact]
	public async Task FetchPreviousTag_HttpFailure_ReturnsNull()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.0.0");
		result.Should().BeNull();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Multi-version (interleaved) histories — must only look within same major
	// ─────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task FetchPreviousTag_InterleavedV1V2_ReturnsCorrectV2Predecessor()
	{
		// Newest-first: v2.1 comes right after v1.5, but the previous v2.x is v2.0.
		var handler = new StubHandler(_ => Json(ReleasesJson("v2.1.0", "v1.5.0", "v2.0.0", "v1.4.0", "v1.3.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v2.1.0");
		result.Should().Be("v2.0.0");
	}

	[Fact]
	public async Task FetchPreviousTag_InterleavedV1V2_ReturnsCorrectV1Predecessor()
	{
		var handler = new StubHandler(_ => Json(ReleasesJson("v2.1.0", "v1.5.0", "v2.0.0", "v1.4.0", "v1.3.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.5.0");
		result.Should().Be("v1.4.0");
	}

	[Fact]
	public async Task FetchPreviousTag_InterleavedMajorVersions_NoPreviousInSameMajor_ReturnsNull()
	{
		// v2.0.0 is the first v2 release; v1.9.0 should NOT be returned.
		var handler = new StubHandler(_ => Json(ReleasesJson("v2.1.0", "v2.0.0", "v1.9.0", "v1.8.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v2.0.0");
		result.Should().BeNull();
	}

	[Fact]
	public async Task FetchPreviousTag_ThreeMajorVersionsInterleaved_CorrectPredecessorForEach()
	{
		var handler = new StubHandler(
			_ => Json(ReleasesJson("v3.1.0", "v2.3.0", "v1.9.0", "v3.0.0", "v2.2.0", "v1.8.0", "v2.1.0", "v1.7.0"))
		);

		(await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v3.1.0")).Should().Be("v3.0.0");
		(await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v2.3.0")).Should().Be("v2.2.0");
		(await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.9.0")).Should().Be("v1.8.0");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Prefixed tags — must only look within same prefix
	// ─────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task FetchPreviousTag_PrefixedTag_OnlyMatchesSamePrefix()
	{
		// agent-v1.2.0 must not pick v1.1.0 (different prefix)
		var handler = new StubHandler(_ => Json(ReleasesJson("agent-v1.2.0", "v1.1.0", "agent-v1.1.0", "v1.0.0", "agent-v1.0.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "agent-v1.2.0");
		result.Should().Be("agent-v1.1.0");
	}

	[Fact]
	public async Task FetchPreviousTag_UnprefixedTag_DoesNotMatchPrefixedReleases()
	{
		var handler = new StubHandler(_ => Json(ReleasesJson("v1.2.0", "agent-v1.1.0", "v1.1.0", "agent-v1.0.0", "v1.0.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0");
		result.Should().Be("v1.1.0");
	}

	[Fact]
	public async Task FetchPreviousTag_PrefixWithDash_MajorVersionIsolation()
	{
		// agent-v2.0.0 first in v2 line; agent-v1.x should NOT be returned.
		var handler = new StubHandler(_ => Json(ReleasesJson("agent-v2.1.0", "agent-v2.0.0", "agent-v1.9.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "agent-v2.0.0");
		result.Should().BeNull();
	}

	[Fact]
	public async Task FetchPreviousTag_PrefixWithoutV_Matched()
	{
		var handler = new StubHandler(_ => Json(ReleasesJson("agent-1.2.0", "agent-1.1.0", "agent-1.0.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "agent-1.2.0");
		result.Should().Be("agent-1.1.0");
	}

	[Fact]
	public async Task FetchPreviousTag_NoPrefixNoV_BareVersion()
	{
		var handler = new StubHandler(_ => Json(ReleasesJson("2.3.1", "2.3.0", "2.2.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "2.3.1");
		result.Should().Be("2.3.0");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Pre-release tags (semver with pre-release suffix)
	// ─────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task FetchPreviousTag_FirstPreRelease_AnchorsAtPreviousStable()
	{
		// v1.2.0-beta.1 is the first pre-release in the v1.2.x series — no prior prerelease exists.
		// Its predecessor is the previous stable: v1.1.0.
		var handler = new StubHandler(_ => Json(ReleasesJson("v1.2.0-beta.1", "v1.1.0", "v1.0.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0-beta.1");
		result.Should().Be("v1.1.0");
	}

	[Fact]
	public async Task FetchPreviousTag_NonFirstPreRelease_ReturnsPreviousPreRelease()
	{
		// v1.2.0-beta.2 has a prior prerelease v1.2.0-beta.1 — that is returned, not v1.1.0.
		var handler = new StubHandler(_ => Json(ReleasesJson("v1.2.0-beta.2", "v1.2.0-beta.1", "v1.1.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0-beta.2");
		result.Should().Be("v1.2.0-beta.1");
	}

	[Fact]
	public async Task FetchPreviousTag_StableSkipsAllPreReleasesOfSameVersion()
	{
		// v1.2.1 is a stable release. Its predecessors v1.2.1-beta.2 and v1.2.1-beta.1 must be skipped.
		var handler = new StubHandler(_ => Json(ReleasesJson("v1.2.1", "v1.2.1-beta.2", "v1.2.1-beta.1", "v1.2.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.1");
		result.Should().Be("v1.2.0");
	}

	[Fact]
	public async Task FetchPreviousTag_StableSkipsInterleavedPreReleases()
	{
		// Mixed interleaved releases: v2.1.0 (stable) must skip v2.1.0-rc.1 and v2.0.0-beta.1
		// and land on v2.0.0 (the previous stable in the v2.x line).
		var handler = new StubHandler(_ => Json(ReleasesJson("v2.1.0", "v2.1.0-rc.1", "v2.0.0-beta.1", "v2.0.0", "v1.9.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v2.1.0");
		result.Should().Be("v2.0.0");
	}

	[Fact]
	public async Task FetchPreviousTag_StableWithNoStablePredecessor_ReturnsNull()
	{
		// v1.0.0 is the first stable; its only candidates are prereleases — all skipped.
		var handler = new StubHandler(_ => Json(ReleasesJson("v1.0.0", "v1.0.0-rc.2", "v1.0.0-rc.1")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.0.0");
		result.Should().BeNull();
	}

	[Fact]
	public async Task FetchPreviousTag_PreRelease_AcceptsStableAsFallback()
	{
		// v1.2.0-alpha.1 is the first pre-release; the only older same-line tag is the
		// stable v1.1.0 — a stable candidate is accepted when no prerelease predecessor exists.
		var handler = new StubHandler(_ => Json(ReleasesJson("v1.2.0-alpha.1", "v1.1.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0-alpha.1");
		result.Should().Be("v1.1.0");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Pagination — current tag on page 1, predecessor on page 2
	// ─────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task FetchPreviousTag_Pagination_FindsPredecessorAcrossPages()
	{
		// Page 1: current tag v2.1.0 at index 0; the rest are v1.x (wrong major, skipped).
		// Page 2: v2.0.0 — same prefix and major, non-prerelease → returned.
		var page1Tags = Enumerable.Range(0, 100).Select(i => i == 0 ? "v2.1.0" : $"v1.{99 - i}.0").ToArray();
		var page2Tags = new[] { "v2.0.0", "v1.0.0" };

		var handler = new StubHandler(req =>
		{
			var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri!.Query);
			var page = query["page"];
			return page == "2" ? Json(ReleasesJson(page2Tags)) : Json(ReleasesJson(page1Tags));
		});

		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v2.1.0");
		result.Should().Be("v2.0.0");
	}

	[Fact]
	public async Task FetchPreviousTag_Pagination_StopsWhenPageIsNotFull()
	{
		// Page 1: full 100 items; page 2: 5 items (partial) → algorithm stops after page 2.
		// Must not fetch page 3 or the tags API (a match was found in releases).
		var page1Tags = Enumerable.Range(0, 100).Select(i => $"v1.{100 - i}.0").ToArray();
		var page2Tags = new[] { "v1.0.4", "v1.0.3", "v1.0.2", "v1.0.1", "v1.0.0" };

		var requestCount = 0;
		var handler = new StubHandler(req =>
		{
			requestCount++;
			var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri!.Query);
			return query["page"] == "2" ? Json(ReleasesJson(page2Tags)) : Json(ReleasesJson(page1Tags));
		});

		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.100.0");
		result.Should().Be("v1.99.0");
		requestCount.Should().Be(2, "stops after the first partial page — never fetches page 3 or the tags fallback");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Case-insensitive tag matching
	// ─────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task FetchPreviousTag_TagMatchIsCaseInsensitive()
	{
		var handler = new StubHandler(_ => Json(ReleasesJson("V1.2.0", "V1.1.0", "V1.0.0")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0");
		result.Should().Be("V1.1.0");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Non-semver tags — prefix extracted from text before first digit
	// ─────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task FetchPreviousTag_DateSuffixTag_ReturnsPredecessorWithSamePrefix()
	{
		// "release-" is the prefix; major = -1 (no X.Y.Z), so no major-version filter applies.
		var handler = new StubHandler(_ => Json(ReleasesJson("release-20260901", "release-20260801", "release-20260701")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "release-20260901");
		result.Should().Be("release-20260801");
	}

	[Fact]
	public async Task FetchPreviousTag_DateSuffixTag_DoesNotMatchDifferentPrefix()
	{
		// "agent-release-" does not match "release-" prefix.
		var handler = new StubHandler(_ => Json(ReleasesJson("release-20260901", "agent-release-20260801", "release-20260801")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "release-20260901");
		result.Should().Be("release-20260801");
	}

	[Fact]
	public async Task FetchPreviousTag_VPrefixedDateTag_MatchesVPrefixOnly()
	{
		// "v20260901" → prefix = "v", major = -1; should match other "v*" non-semver tags.
		var handler = new StubHandler(_ => Json(ReleasesJson("v20260901", "20260801", "v20260801")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v20260901");
		result.Should().Be("v20260801");
	}

	[Fact]
	public async Task FetchPreviousTag_BareIntTag_MatchesOtherBareIntTags()
	{
		// No prefix (empty string); all bare-digit tags share the empty prefix.
		var handler = new StubHandler(_ => Json(ReleasesJson("20260901", "20260801", "20260701")));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "20260901");
		result.Should().Be("20260801");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Out-of-order creation dates — must pick highest semver below current,
	// not just the first candidate encountered in API order
	// ─────────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task FetchPreviousTag_OutOfOrder_ReturnsHighestBelowNotFirstBelow()
	{
		// API order (creation-date newest-first): v4.2.0, v3.8.5, v4.1.0, v4.1.1
		// A naïve "first after found" would return v4.1.0, but v4.1.1 is the correct predecessor.
		var handler = new StubHandler(req =>
		{
			if (req.RequestUri!.PathAndQuery.Contains("/tags"))
				return Json("[]");
			return Json(ReleasesJson("v4.2.0", "v3.8.5", "v4.1.0", "v4.1.1"));
		});
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v4.2.0");
		result.Should().Be("v4.1.1");
	}

	[Fact]
	public async Task FetchPreviousTag_OutOfOrder_MaintenanceBranchInterleaved()
	{
		// Maintenance branch (v4.1.x) releases created after v4.2.0 are listed first by creation date.
		// Correct predecessor for v4.2.0 is v4.1.1, the highest v4.x below v4.2.0.
		var handler = new StubHandler(req =>
		{
			if (req.RequestUri!.PathAndQuery.Contains("/tags"))
				return Json("[]");
			return Json(ReleasesJson("v4.1.1", "v4.2.0-BC_3", "v4.2.0-BC_2", "v4.2.0", "v3.8.5", "v4.2.0-BC_1", "v4.1.0", "v3.8.4"));
		});
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v4.2.0");
		result.Should().Be("v4.1.1");
	}

	[Fact]
	public async Task FetchPreviousTag_OutOfOrder_AcrossPages()
	{
		// Page 1 has v4.1.0 (a plausible but not the best match); page 2 has v4.1.1 (the correct one).
		// Both pages must be read to ensure the highest candidate wins.
		var page1Tags = Enumerable.Range(0, 100).Select(i => i == 0 ? "v4.2.0" : i == 1 ? "v4.1.0" : $"v3.{99 - i}.0").ToArray();
		var page2Tags = new[] { "v4.1.1", "v3.0.0" };

		var handler = new StubHandler(req =>
		{
			if (req.RequestUri!.PathAndQuery.Contains("/tags"))
				return Json("[]");
			var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri!.Query);
			return query["page"] == "2" ? Json(ReleasesJson(page2Tags)) : Json(ReleasesJson(page1Tags));
		});

		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v4.2.0");
		result.Should().Be("v4.1.1");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Tags API fallback — used when releases API yields no predecessor
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>Builds a JSON tags array string (each item has only a "name" field).</summary>
	private static string TagsJson(params string[] tagNames)
	{
		var items = tagNames.Select(t => $$$"""{"name":"{{{t}}}"}""");
		return $"[{string.Join(",", items)}]";
	}

	[Fact]
	public async Task FetchPreviousTag_TagsApiFallback_FindsPredecessorNotInReleases()
	{
		// Releases list has only the current tag; the predecessor exists only as a git tag.
		var handler = new StubHandler(
			req => req.RequestUri!.PathAndQuery.Contains("/tags")
				? Json(TagsJson("v1.2.0", "v1.1.0", "v1.0.0"))
				: Json(ReleasesJson("v1.2.0"))
		);
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0");
		result.Should().Be("v1.1.0");
	}

	[Fact]
	public async Task FetchPreviousTag_TagsApiFallback_EmptyReleases_FindsInTags()
	{
		// No GitHub Releases at all; falls back to tags.
		var handler = new StubHandler(
			req => req.RequestUri!.PathAndQuery.Contains("/tags") ? Json(TagsJson("v1.2.0", "v1.1.0", "v1.0.0")) : Json("[]")
		);
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0");
		result.Should().Be("v1.1.0");
	}

	[Fact]
	public async Task FetchPreviousTag_TagsApiFallback_HttpFailure_ReturnsNull()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0");
		result.Should().BeNull();
	}

	[Fact]
	public async Task FetchPreviousTag_TagsApiFallback_EmptyTagsList_ReturnsNull()
	{
		var handler = new StubHandler(req => req.RequestUri!.PathAndQuery.Contains("/tags") ? Json("[]") : Json("[]"));
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0");
		result.Should().BeNull();
	}

	[Fact]
	public async Task FetchPreviousTag_TagsApiFallback_OutOfOrder_ReturnsHighestBelow()
	{
		// Tags API (like releases) may return tags in creation-date order, not semver order.
		// Correct answer: v1.1.1, not v1.1.0 (v1.1.1 appears later but is higher semver).
		var handler = new StubHandler(
			req => req.RequestUri!.PathAndQuery.Contains("/tags") ? Json(TagsJson("v1.2.0", "v1.1.0", "v1.1.1")) : Json("[]")
		);
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.2.0");
		result.Should().Be("v1.1.1");
	}

	[Fact]
	public async Task FetchPreviousTag_TagsApiFallback_Pagination_FindsPredecessorOnPage2()
	{
		// Page 1 of tags: 100 items (v1.100.0 current + v3.x wrong-major).
		// Page 2 of tags: v1.99.0 (correct predecessor).
		var page1Tags = Enumerable.Range(0, 100).Select(i => i == 0 ? "v1.100.0" : $"v3.{99 - i}.0").ToArray();
		var page2Tags = new[] { "v1.99.0", "v1.98.0" };

		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.PathAndQuery;
			if (!path.Contains("/tags"))
				return Json("[]"); // no releases
			var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri.Query);
			return query["page"] == "2" ? Json(TagsJson(page2Tags)) : Json(TagsJson(page1Tags));
		});

		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v1.100.0");
		result.Should().Be("v1.99.0");
	}

	[Fact]
	public async Task FetchPreviousTag_TagsApiFallback_PrefixIsolation()
	{
		// Tags API fallback also respects prefix isolation.
		var handler = new StubHandler(
			req => req.RequestUri!.PathAndQuery.Contains("/tags") ? Json(TagsJson("agent-v1.2.0", "v1.1.0", "agent-v1.1.0")) : Json("[]")
		);
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "agent-v1.2.0");
		result.Should().Be("agent-v1.1.0");
	}

	[Fact]
	public async Task FetchPreviousTag_TagsApiFallback_MajorIsolation()
	{
		// Tags API fallback also respects major-version isolation.
		var handler = new StubHandler(
			req => req.RequestUri!.PathAndQuery.Contains("/tags") ? Json(TagsJson("v2.0.0", "v1.9.0", "v1.8.0")) : Json("[]")
		);
		// v2.0.0 is the first v2 release; no prior v2.x → null even though v1.9.0 exists.
		var result = await Service(handler).FetchPreviousTagAsync(Owner, Repo, "v2.0.0");
		result.Should().BeNull();
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
