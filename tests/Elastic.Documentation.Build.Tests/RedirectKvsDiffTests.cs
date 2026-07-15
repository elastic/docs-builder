// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Assembler.Deploying.Redirects;

namespace Elastic.Documentation.Build.Tests;

/*
 * RedirectKvsDiff tests
 * =====================
 *
 * These tests pin the diff semantics that decide which keys are written to and
 * deleted from the CloudFront KeyValueStore that backs documentation redirects.
 *
 * The regression that motivated this suite:
 * -----------------------------------------
 * A writer split `azure-native-isv-service.md` into three pages in elastic/docs-content#5818
 * but kept the parent page alive. The redirect for the parent page was kept in
 * redirects.yml without a top-level `to:`, which led the assembler to emit
 *   /docs/.../azure-native-isv-service -> /docs/.../azure-native-isv-service-troubleshooting
 * into the live KVS, breaking the still-valid parent page.
 *
 * Two follow-up PRs (#6667, #6716) tried to remove the stale redirect from
 * redirects.yml. Both failed because the diff helper had the operands of `Except`
 * reversed: it computed "keys in the new file but not in the live KVS" (which is
 * just the brand-new entries we are about to PUT) instead of "keys in the live
 * KVS that no longer appear in the new file" (the stale entries to DELETE).
 * Result: nothing was ever deleted from the KVS and the bad redirect "stuck".
 *
 * The `KeyRemovedFromSourced_AppearsInToDelete` test reproduces that exact
 * scenario. Don't loosen it.
 */

public class RedirectKvsDiffTests
{
	[Fact]
	public void ComputeBatchUpdates_KeyRemovedFromSourced_AppearsInToDelete()
	{
		// Reproduces elastic/docs-content#6716: a redirect that was previously pushed
		// to the KVS is dropped from redirects.json. The diff MUST flag it for deletion.
		const string staleKey = "/docs/deploy-manage/deploy/elastic-cloud/azure-native-isv-service";

		var sourcedRedirects = new Dictionary<string, string>
		{
			["/docs/some-other-page"] = "/docs/some-other-page-new"
		};
		var existingRedirects = new HashSet<string>
		{
			staleKey,
			"/docs/some-other-page"
		};

		var (toPut, toDelete) = RedirectKvsDiff.ComputeBatchUpdates(sourcedRedirects, existingRedirects);

		toDelete.Select(d => d.Key).Should().ContainSingle().Which.Should().Be(staleKey);
		toPut.Should().ContainSingle().Which.Key.Should().Be("/docs/some-other-page");
	}

	[Fact]
	public void ComputeBatchUpdates_KeyOnlyInSourced_IsPutAndNotDeleted()
	{
		var sourcedRedirects = new Dictionary<string, string>
		{
			["/docs/new-page"] = "/docs/new-page-target"
		};
		var existingRedirects = new HashSet<string>();

		var (toPut, toDelete) = RedirectKvsDiff.ComputeBatchUpdates(sourcedRedirects, existingRedirects);

		toPut.Should().ContainSingle().Which.Should().BeEquivalentTo(new
		{
			Key = "/docs/new-page",
			Value = "/docs/new-page-target"
		});
		toDelete.Should().BeEmpty();
	}

	[Fact]
	public void ComputeBatchUpdates_KeyInBoth_IsPutAndNotDeleted()
	{
		// The current implementation re-puts every sourced entry (even unchanged values).
		// This pins that behaviour explicitly so a future optimisation that skips
		// no-op puts has to update both the code and this test.
		var sourcedRedirects = new Dictionary<string, string>
		{
			["/docs/shared-key"] = "/docs/target"
		};
		var existingRedirects = new HashSet<string> { "/docs/shared-key" };

		var (toPut, toDelete) = RedirectKvsDiff.ComputeBatchUpdates(sourcedRedirects, existingRedirects);

		toPut.Select(p => p.Key).Should().Equal("/docs/shared-key");
		toDelete.Should().BeEmpty();
	}

	[Fact]
	public void ComputeBatchUpdates_BothEmpty_ProducesEmptyBatches()
	{
		var (toPut, toDelete) = RedirectKvsDiff.ComputeBatchUpdates(
			new Dictionary<string, string>(),
			new HashSet<string>());

		toPut.Should().BeEmpty();
		toDelete.Should().BeEmpty();
	}

	[Fact]
	public void ComputeBatchUpdates_OnlyExisting_AllAreDeleted()
	{
		// Mirrors a "remove every redirect" intent. The helper itself does not refuse
		// to compute this; the wipe guard (WouldWipeAllExisting) lives one layer up.
		var sourcedRedirects = new Dictionary<string, string>();
		var existingRedirects = new HashSet<string>
		{
			"/docs/a",
			"/docs/b",
			"/docs/c"
		};

		var (toPut, toDelete) = RedirectKvsDiff.ComputeBatchUpdates(sourcedRedirects, existingRedirects);

		toPut.Should().BeEmpty();
		toDelete.Select(d => d.Key).Should().BeEquivalentTo(existingRedirects);
	}

	[Fact]
	public void ComputeBatchUpdates_AzureIsvRegression_ScenarioEndToEnd()
	{
		// Simulates the post-#6716 state: redirects.yml no longer mentions the parent
		// page, but the KVS still has the stale entry from the original push.
		const string stalePath = "/docs/deploy-manage/deploy/elastic-cloud/azure-native-isv-service";

		var sourcedRedirects = new Dictionary<string, string>
		{
			// Many unrelated valid redirects exist in the same deploy.
			["/docs/some/other/legitimate-old"] = "/docs/some/other/legitimate-new",
			["/docs/another/page"] = "/docs/another/page-renamed"
		};
		var existingRedirects = new HashSet<string>
		{
			stalePath,
			"/docs/some/other/legitimate-old",
			"/docs/historical-entry-still-valid"
		};

		var (toPut, toDelete) = RedirectKvsDiff.ComputeBatchUpdates(sourcedRedirects, existingRedirects);

		toDelete.Select(d => d.Key).Should().Contain(stalePath,
			because: "the stale Azure ISV redirect must be removed from the KVS once it is dropped from redirects.yml");
		toDelete.Select(d => d.Key).Should().Contain("/docs/historical-entry-still-valid",
			because: "any KVS key absent from the sourced redirects is stale by definition");
		toDelete.Select(d => d.Key).Should().NotContain("/docs/another/page",
			because: "brand-new sourced entries belong in the PUT batch, not the DELETE batch");

		toPut.Select(p => p.Key).Should().Contain("/docs/another/page");
		toPut.Should().NotContain(p => p.Key == stalePath,
			because: "we never PUT a value for a key that the sourced file dropped");

		// Sanity: each toDelete key must come from the live KVS, never from the sourced set.
		foreach (var del in toDelete)
		{
			existingRedirects.Should().Contain(del.Key);
			sourcedRedirects.Keys.Should().NotContain(del.Key);
		}
	}

	[Fact]
	public void WouldWipeAllExisting_EmptySourcedAndPopulatedExisting_ReturnsTrue()
	{
		var sourced = new Dictionary<string, string>();
		var existing = new HashSet<string> { "/docs/a", "/docs/b" };

		RedirectKvsDiff.WouldWipeAllExisting(sourced, existing).Should().BeTrue();
	}

	[Fact]
	public void WouldWipeAllExisting_SourcedHasEntries_ReturnsFalse()
	{
		var sourced = new Dictionary<string, string> { ["/docs/a"] = "/docs/b" };
		var existing = new HashSet<string> { "/docs/a" };

		RedirectKvsDiff.WouldWipeAllExisting(sourced, existing).Should().BeFalse();
	}

	[Fact]
	public void WouldWipeAllExisting_BothEmpty_ReturnsFalse()
	{
		// Empty in, empty out is not a wipe — it's a no-op on a clean KVS.
		var sourced = new Dictionary<string, string>();
		var existing = new HashSet<string>();

		RedirectKvsDiff.WouldWipeAllExisting(sourced, existing).Should().BeFalse();
	}
}
