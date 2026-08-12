// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

public class ShallowRegistryReconcilerTests
{
	private const string PublicBucket = "public-bucket";
	private const string BundleMapKey = "bundle/registry.json";
	private const string ChangelogMapKey = "changelog/registry.json";

	private readonly FakeS3 _s3 = new(PublicBucket);
	private readonly ReconcileMetrics _metrics = new();
	private readonly ShallowRegistryReconciler _reconciler;

	public ShallowRegistryReconcilerTests() =>
		_reconciler = new ShallowRegistryReconciler(
			NullLoggerFactory.Instance, _s3.Client, PublicBucket, retryBaseDelay: TimeSpan.Zero, metrics: _metrics);

	private Cancel Ctx => TestContext.Current.CancellationToken;

	private static ChangelogScope BundleScope(string product)
	{
		_ = ChangelogScope.TryCreateBundle(product, out var scope);
		return scope!;
	}

	private static ChangelogScope PoolScope(string org, string repo, string branch)
	{
		_ = ChangelogScope.TryCreateChangelog(org, repo, branch, out var scope);
		return scope!;
	}

	private SortedDictionary<string, string> Map(string key) =>
		JsonSerializer.Deserialize(_s3.ContentOf(PublicBucket, key), ShallowRegistryJsonContext.Default.SortedDictionaryStringString)!;

	private void SeedMap(string key, params (string Group, string Token)[] entries)
	{
		var map = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var (group, token) in entries)
			map[group] = token;
		_ = _s3.Seed(PublicBucket, key, JsonSerializer.Serialize(map, ShallowRegistryJsonContext.Default.SortedDictionaryStringString));
	}

	[Fact]
	public async Task Reconcile_AbsentMap_SeedsEveryFolderFromAFullTreeListing()
	{
		// First deploy: no map exists yet, so a single touched folder heals the whole tree.
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", "one");
		_ = _s3.Seed(PublicBucket, "bundle/kibana/kb-9.1.0.yaml", "two");
		_ = _s3.Seed(PublicBucket, "bundle/kibana/registry.json", "{}");

		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [BundleScope("elasticsearch")], Ctx);

		Map(BundleMapKey).Keys.Should().BeEquivalentTo("elasticsearch", "kibana");
	}

	[Fact]
	public async Task Reconcile_ExistingMap_PatchesOnlyTheTouchedFolders()
	{
		// Untouched folders keep their recorded value even when stale — their own events (or the
		// next absent-map rebuild) heal them. Patching avoids listing the whole tree per event.
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", "one");
		_ = _s3.Seed(PublicBucket, "bundle/kibana/kb-9.1.0.yaml", "two");
		SeedMap(BundleMapKey, ("kibana", "stale-token"));

		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [BundleScope("elasticsearch")], Ctx);

		var map = Map(BundleMapKey);
		map.Should().ContainKey("elasticsearch");
		map["kibana"].Should().Be("stale-token");
	}

	[Fact]
	public async Task Reconcile_TokenChangesWhenAnyFileChangesOrIsDeleted()
	{
		// The token digests the whole listing rather than echoing one object's ETag: deleting an
		// older file must change the value, or opt-out caches would never see the delete.
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", "old");
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.2.0.yaml", "new");
		var scope = BundleScope("elasticsearch");

		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [scope], Ctx);
		var before = Map(BundleMapKey)["elasticsearch"];

		_s3.Remove(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml");
		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [scope], Ctx);

		Map(BundleMapKey)["elasticsearch"].Should().NotBe(before);
	}

	[Fact]
	public async Task Reconcile_UnchangedFolder_SkipsTheWrite()
	{
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", "one");
		var scope = BundleScope("elasticsearch");
		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [scope], Ctx);
		var puts = _s3.Puts.Count;

		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [scope], Ctx);

		_s3.Puts.Count.Should().Be(puts, "an exact map must not be rewritten");
		_metrics.ShallowRegistryUnchanged.Should().Be(1);
	}

	[Fact]
	public async Task Reconcile_EmptiedFolder_IsRemovedFromTheMap()
	{
		_ = _s3.Seed(PublicBucket, "bundle/kibana/kb-9.1.0.yaml", "keep");
		SeedMap(BundleMapKey, ("elasticsearch", "token"), ("kibana", "token"));

		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [BundleScope("elasticsearch"), BundleScope("kibana")], Ctx);

		Map(BundleMapKey).Keys.Should().BeEquivalentTo("kibana");
	}

	[Fact]
	public async Task Reconcile_EmptiedTree_DeletesTheMapConditionally()
	{
		SeedMap(BundleMapKey, ("elasticsearch", "token"));

		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [BundleScope("elasticsearch")], Ctx);

		_s3.Exists(PublicBucket, BundleMapKey).Should().BeFalse("an empty tree's map is deleted: absent ≠ empty");
		_s3.Deletes.Should().ContainSingle().Which.IfMatch.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task Reconcile_PoolBranchWithSlash_DoesNotSweepNestedPools()
	{
		// Branches are stored verbatim, so the / delimiter is what keeps the "main" pool from
		// swallowing the "main/feature" pool's files into its token.
		_ = _s3.Seed(PublicBucket, "changelog/elastic/repo/main/entry-a.yaml", "a");
		_ = _s3.Seed(PublicBucket, "changelog/elastic/repo/main/feature/entry-b.yaml", "b");
		SeedMap(ChangelogMapKey, ("elastic/repo/main", "seed"), ("elastic/repo/main/feature", "seed"));

		await _reconciler.ReconcileAsync(ChangelogScopeKind.Changelog, [PoolScope("elastic", "repo", "main")], Ctx);
		var afterParent = Map(ChangelogMapKey)["elastic/repo/main"];
		await _reconciler.ReconcileAsync(ChangelogScopeKind.Changelog, [PoolScope("elastic", "repo", "main/feature")], Ctx);

		var map = Map(ChangelogMapKey);
		map["elastic/repo/main"].Should().Be(afterParent);
		map["elastic/repo/main/feature"].Should().NotBe("seed").And.NotBe(afterParent);
	}

	[Fact]
	public async Task Reconcile_UnparseableMap_IsRebuiltFromTheTreeListing()
	{
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", "one");
		_ = _s3.Seed(PublicBucket, "bundle/kibana/kb-9.1.0.yaml", "two");
		var corruptETag = _s3.Seed(PublicBucket, BundleMapKey, "{ not json ");

		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [BundleScope("elasticsearch")], Ctx);

		Map(BundleMapKey).Keys.Should().BeEquivalentTo("elasticsearch", "kibana");
		_s3.Puts.Should().ContainSingle().Which.IfMatch.Trim('"').Should().Be(corruptETag,
			"the conditional write must replace exactly the corrupt map that was read");
	}

	[Fact]
	public async Task Reconcile_PutLosingTheRace_RereadsAndRetries()
	{
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", "one");
		var raced = false;
		_s3.BeforePut = _ =>
		{
			if (raced)
				return;
			raced = true;
			SeedMap(BundleMapKey, ("kibana", "concurrent"));
		};

		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [BundleScope("elasticsearch")], Ctx);

		_metrics.WriteConflicts.Should().Be(1);
		var map = Map(BundleMapKey);
		map.Should().ContainKey("elasticsearch");
		map.Should().ContainKey("kibana", "the concurrent writer's entry was re-read and preserved");
	}

	[Fact]
	public async Task Reconcile_ExhaustedConditionalRetries_Throws()
	{
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", "one");
		var counter = 0;
		_s3.BeforePut = _ => SeedMap(BundleMapKey, ("racer", $"token-{counter++}"));

		var act = async () => await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [BundleScope("elasticsearch")], Ctx);

		_ = await act.Should().ThrowAsync<ReconcileConflictException>();
	}

	[Fact]
	public async Task Reconcile_NoTouchedFolders_IsANoOp()
	{
		await _reconciler.ReconcileAsync(ChangelogScopeKind.Bundle, [], Ctx);

		_s3.ListCalls.Should().Be(0);
		_s3.Puts.Should().BeEmpty();
	}

	[Fact]
	public async Task Reconcile_MismatchedScopeKind_IsRejected()
	{
		var act = async () => await _reconciler.ReconcileAsync(
			ChangelogScopeKind.Bundle, [PoolScope("elastic", "repo", "main")], Ctx);

		_ = await act.Should().ThrowAsync<ArgumentException>();
	}
}
