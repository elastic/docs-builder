// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Elastic.Changelog.Uploading;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

public class RegistryReconcilerTests
{
	private const string PublicBucket = "public-bucket";

	private static readonly DateTimeOffset FixedNow = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

	private readonly FakeS3 _s3 = new(PublicBucket);
	private readonly RegistryReconciler _reconciler;
	private readonly ReconcileMetrics _metrics = new();

	public RegistryReconcilerTests() =>
		_reconciler = new RegistryReconciler(
			NullLoggerFactory.Instance,
			_s3.Client,
			PublicBucket,
			new FakeTimeProvider(FixedNow),
			retryBaseDelay: TimeSpan.Zero,
			_metrics);

	private static ChangelogScope BundleScope(string product = "elasticsearch")
	{
		_ = ChangelogScope.TryCreateBundle(product, out var scope);
		return scope!;
	}

	private static ChangelogScope ChangelogScopeFor(string org, string repo, string branch)
	{
		_ = ChangelogScope.TryCreateChangelog(org, repo, branch, out var scope);
		return scope!;
	}

	// language=yaml
	private static string BundleYaml(string product, string target) => $"""
		products:
		  - product: {product}
		    target: {target}
		    repo: {product}
		    owner: elastic
		entries:
		  - file:
		      name: 1-feature.yaml
		      checksum: deadbeef
		    type: enhancement
		    title: Sample
		""";

	// language=yaml
	private const string AmendYaml = """
		exclude-entries:
		  - file:
		      name: 1-feature.yaml
		      checksum: deadbeef
		""";

	private string SeedBundle(ChangelogScope scope, string file, string target, string? product = null) =>
		_s3.Seed(PublicBucket, scope.Prefix + file, BundleYaml(product ?? scope.Group, target));

	private void SeedManifest(ChangelogScope scope, params RegistryBundle[] bundles) =>
		SeedManifest(scope, RegistryReconciler.Producer, Registry.CurrentSchemaVersion, bundles);

	private void SeedManifest(ChangelogScope scope, string? producer, int schemaVersion, params RegistryBundle[] bundles)
	{
		var manifest = new Registry
		{
			SchemaVersion = schemaVersion,
			Product = scope.Group,
			Producer = producer,
			GeneratedAt = FixedNow.AddDays(-1),
			Bundles = bundles
		};
		_ = _s3.Seed(PublicBucket, scope.RegistryKey, JsonSerializer.Serialize(manifest, RegistryJsonContext.Default.Registry));
	}

	private Registry WrittenManifest()
	{
		var content = _s3.ContentOf(PublicBucket, BundleScope().RegistryKey);
		return JsonSerializer.Deserialize(content, RegistryJsonContext.Default.Registry)!;
	}

	private Cancel Ctx => TestContext.Current.CancellationToken;

	[Fact]
	public async Task ReconcileGroup_HealsEntriesMissingFromManifest()
	{
		// Public bucket holds 1..4 but the manifest lists only 1 and 2 (3 was lost to a past gap;
		// 4 just landed): a single reconcile heals both, and only reads the YAMLs it cannot reuse.
		var scope = BundleScope();
		var etag1 = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		var etag2 = SeedBundle(scope, "es-9.2.0.yaml", "9.2.0");
		_ = SeedBundle(scope, "es-9.3.0.yaml", "9.3.0");
		_ = SeedBundle(scope, "es-9.4.0.yaml", "9.4.0");
		SeedManifest(scope,
			new RegistryBundle { File = "es-9.2.0.yaml", Target = "9.2.0", ETag = etag2 },
			new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag1 });

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		var manifest = WrittenManifest();
		manifest.Bundles.Select(b => b.File).Should().Equal(
			"es-9.4.0.yaml", "es-9.3.0.yaml", "es-9.2.0.yaml", "es-9.1.0.yaml");
		manifest.Bundles.Should().OnlyContain(b => b.Target != null);
		manifest.Producer.Should().Be(RegistryReconciler.Producer);

		// 1 and 2 were ETag-reused: only the manifest itself plus 3 and 4 were read.
		_s3.GetsFor(PublicBucket).Should().BeEquivalentTo([
			scope.RegistryKey, scope.Prefix + "es-9.3.0.yaml", scope.Prefix + "es-9.4.0.yaml"
		]);
	}

	[Fact]
	public async Task ReconcileGroup_DropsEntriesWhoseObjectIsGone()
	{
		var scope = BundleScope();
		var etag1 = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		SeedManifest(scope,
			new RegistryBundle { File = "es-9.2.0.yaml", Target = "9.2.0", ETag = "gone" },
			new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag1 });

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		WrittenManifest().Bundles.Select(b => b.File).Should().Equal("es-9.1.0.yaml");
	}

	[Fact]
	public async Task ReconcileGroup_ManifestAlreadyExact_SkipsWriteAndBundleReads()
	{
		var scope = BundleScope();
		var etag1 = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		var etag2 = SeedBundle(scope, "es-9.2.0.yaml", "9.2.0");
		SeedManifest(scope,
			new RegistryBundle { File = "es-9.2.0.yaml", Target = "9.2.0", ETag = etag2 },
			new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag1 });

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Unchanged);
		_s3.Puts.Should().BeEmpty();
		_s3.GetsFor(PublicBucket).Should().Equal(scope.RegistryKey);
		_metrics.RegistryUnchanged.Should().Be(1);
	}

	[Fact]
	public async Task ReconcileGroup_GeneratedAtAloneNeverCausesChurn()
	{
		// Same as above but the seeded generated_at differs from "now": still Unchanged.
		var scope = BundleScope();
		var etag = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		SeedManifest(scope, new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag });

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Unchanged);
		_s3.Puts.Should().BeEmpty();
	}

	[Fact]
	public async Task ReconcileGroup_AmendIsAlwaysRecomputed_EvenWhenItsETagMatches()
	{
		// The parent moved from 9.3.0 to 9.4.0 without the amend's own ETag changing; an
		// ETag-reuse of the amend entry would keep the stale inherited target forever.
		var scope = BundleScope();
		var parentETag = SeedBundle(scope, "es-9.3.0.yaml", "9.4.0");
		var amendETag = _s3.Seed(PublicBucket, scope.Prefix + "es-9.3.0.amend-1.yaml", AmendYaml);
		SeedManifest(scope,
			new RegistryBundle { File = "es-9.3.0.yaml", Target = "9.4.0", ETag = parentETag },
			new RegistryBundle { File = "es-9.3.0.amend-1.yaml", Target = "9.3.0", ETag = amendETag });

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		var amend = WrittenManifest().Bundles.Single(b => b.File == "es-9.3.0.amend-1.yaml");
		amend.Target.Should().Be("9.4.0", "the amend re-inherits the parent's current target on every reconcile");
	}

	[Fact]
	public async Task ReconcileGroup_AmendWithoutParent_RecordsNullTarget()
	{
		var scope = BundleScope();
		_ = _s3.Seed(PublicBucket, scope.Prefix + "es-9.3.0.amend-1.yaml", AmendYaml);

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		var entry = WrittenManifest().Bundles.Should().ContainSingle().Subject;
		entry.File.Should().Be("es-9.3.0.amend-1.yaml");
		entry.Target.Should().BeNull("the parent has not landed yet; a later reconcile self-corrects");
	}

	[Fact]
	public async Task ReconcileGroup_MultiProductBundle_MatchesTheGroupProduct()
	{
		// language=yaml
		const string multiProductYaml = """
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: elasticsearch
			    owner: elastic
			  - product: kibana
			    target: 9.4.0
			    repo: kibana
			    owner: elastic
			entries:
			  - file:
			      name: 1-feature.yaml
			      checksum: deadbeef
			    type: enhancement
			    title: Sample
			""";
		var scope = BundleScope("kibana");
		_ = _s3.Seed(PublicBucket, scope.Prefix + "multi.yaml", multiProductYaml);

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		var content = _s3.ContentOf(PublicBucket, scope.RegistryKey);
		var manifest = JsonSerializer.Deserialize(content, RegistryJsonContext.Default.Registry)!;
		manifest.Bundles.Single().Target.Should().Be("9.4.0", "kibana's own target must win, never blindly Products[0]");
	}

	[Fact]
	public async Task ReconcileGroup_ProducerMismatch_RecomputesEverythingAndWritesEvenWhenIdentical()
	{
		// A legacy (pass-through) manifest has no producer. Even when every entry would come out
		// identical, the write must happen — otherwise the producer version is never adopted and
		// every future reconcile keeps recomputing.
		var scope = BundleScope();
		var etag = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		SeedManifest(scope, producer: null, Registry.CurrentSchemaVersion,
			new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag });

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		_s3.GetsFor(PublicBucket).Should().Contain(scope.Prefix + "es-9.1.0.yaml", "producer mismatch disables ETag reuse");
		var manifest = WrittenManifest();
		manifest.Producer.Should().Be(RegistryReconciler.Producer);
		manifest.Bundles.Single().Should().BeEquivalentTo(new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag });
	}

	[Fact]
	public async Task ReconcileGroup_ListingPaginates()
	{
		var scope = BundleScope();
		for (var i = 1; i <= 5; i++)
			_ = SeedBundle(scope, $"es-9.{i}.0.yaml", $"9.{i}.0");
		_s3.PageSize = 2;

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		WrittenManifest().Bundles.Should().HaveCount(5);
		_s3.ListCalls.Should().BeGreaterThanOrEqualTo(3);
	}

	[Fact]
	public async Task ReconcileGroup_ChangelogScope_IsRejected()
	{
		// Pool manifests are not reconciled: they stay client-authored pass-through until Phase 3
		// retires them, so a changelog scope reaching the group reconciler is a programming error.
		var scope = ChangelogScopeFor("elastic", "repo", "main");
		_ = _s3.Seed(PublicBucket, scope.Prefix + "entry-a.yaml", "a: 1");

		var act = async () => await _reconciler.ReconcileGroupAsync(scope, Ctx);

		_ = await act.Should().ThrowAsync<ArgumentException>();
		_s3.Puts.Should().BeEmpty();
	}

	[Fact]
	public async Task ReconcileGroup_ExcludesTheManifestAndOtherNonYamlFromItsOwnListing()
	{
		var scope = BundleScope();
		_ = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		SeedManifest(scope);
		_ = _s3.Seed(PublicBucket, scope.Prefix + "notes.txt", "not yaml");
		_ = _s3.Seed(PublicBucket, scope.Prefix + "stray.json", "{}");

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		WrittenManifest().Bundles.Select(b => b.File).Should().Equal("es-9.1.0.yaml");
	}

	[Fact]
	public async Task ReconcileGroup_CorruptManifest_IsRebuiltWithItsLiveETagGuard()
	{
		var scope = BundleScope();
		_ = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		var corruptETag = _s3.Seed(PublicBucket, scope.RegistryKey, "{ not json ");

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		var put = _s3.Puts.Should().ContainSingle().Subject;
		put.IfMatch.Trim('"').Should().Be(corruptETag, "the conditional write must replace exactly the corrupt manifest that was read");
		WrittenManifest().Bundles.Should().ContainSingle();
	}

	[Fact]
	public async Task ReconcileGroup_EmptyListing_DeletesTheManifestConditionally()
	{
		var scope = BundleScope();
		SeedManifest(scope, new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = "aa" });
		var manifestETag = FakeS3.ETagOf(_s3.ContentOf(PublicBucket, scope.RegistryKey));

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Deleted);
		_s3.Exists(PublicBucket, scope.RegistryKey).Should().BeFalse();
		var delete = _s3.Deletes.Should().ContainSingle().Subject;
		delete.IfMatch.Trim('"').Should().Be(manifestETag);
	}

	[Fact]
	public async Task ReconcileGroup_EmptyListing_DeletesEvenAManifestWhoseBundlesAreAlreadyEmpty()
	{
		// Deletion must run before any equality short-circuit: absent ≠ empty for consumers.
		var scope = BundleScope();
		SeedManifest(scope);

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Deleted);
		_s3.Exists(PublicBucket, scope.RegistryKey).Should().BeFalse();
	}

	[Fact]
	public async Task ReconcileGroup_EmptyListingAndNoManifest_IsANoOp()
	{
		var outcome = await _reconciler.ReconcileGroupAsync(BundleScope(), Ctx);

		outcome.Should().Be(GroupReconcileOutcome.NoOp);
		_s3.Puts.Should().BeEmpty();
		_s3.Deletes.Should().BeEmpty();
	}

	[Fact]
	public async Task ReconcileGroup_DeleteLosingTheRace_RereadsAndRetries()
	{
		// A concurrent reconciler replaces the manifest between our read and our delete: the
		// conditional delete 412s, and the retry deletes the fresh manifest it re-reads.
		var scope = BundleScope();
		SeedManifest(scope);
		_s3.BeforeDelete = call =>
		{
			if (call == 1)
				SeedManifest(scope, new RegistryBundle { File = "late.yaml", Target = null, ETag = "bb" });
		};

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Deleted);
		_s3.Exists(PublicBucket, scope.RegistryKey).Should().BeFalse();
		_s3.Deletes.Should().HaveCount(2);
		_metrics.WriteConflicts.Should().Be(1);
	}

	[Fact]
	public async Task ReconcileGroup_PutLosingTheRace_RereadsAndRetries()
	{
		var scope = BundleScope();
		_ = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		SeedManifest(scope, producer: null, Registry.CurrentSchemaVersion);
		_s3.BeforePut = call =>
		{
			if (call == 1)
				SeedManifest(scope, producer: null, Registry.CurrentSchemaVersion,
					new RegistryBundle { File = "concurrent.yaml", Target = null, ETag = "cc" });
		};

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.Written);
		_s3.Puts.Should().HaveCount(2);
		WrittenManifest().Bundles.Select(b => b.File).Should().Equal("es-9.1.0.yaml");
	}

	[Fact]
	public async Task ReconcileGroup_ExhaustedConditionalRetries_Throws()
	{
		var scope = BundleScope();
		_ = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		SeedManifest(scope, producer: null, Registry.CurrentSchemaVersion);
		// Every attempt loses: a concurrent writer lands between every read and write.
		var counter = 0;
		_s3.BeforePut = _ => SeedManifest(scope, producer: null, Registry.CurrentSchemaVersion,
			new RegistryBundle { File = $"concurrent-{counter++}.yaml", Target = null, ETag = "cc" });

		var act = async () => await _reconciler.ReconcileGroupAsync(scope, Ctx);

		_ = await act.Should().ThrowAsync<ReconcileConflictException>();
		_s3.Puts.Should().HaveCount(5, "the retry loop is bounded");
	}

	[Fact]
	public async Task ReconcileGroup_NewerSchemaManifest_IsReportedAndLeftUntouched()
	{
		var scope = BundleScope();
		_ = SeedBundle(scope, "es-9.1.0.yaml", "9.1.0");
		SeedManifest(scope, RegistryReconciler.Producer, schemaVersion: Registry.CurrentSchemaVersion + 1);
		var before = _s3.ContentOf(PublicBucket, scope.RegistryKey);

		var outcome = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		outcome.Should().Be(GroupReconcileOutcome.RefusedNewerSchema);
		_s3.Puts.Should().BeEmpty();
		_s3.Deletes.Should().BeEmpty();
		_s3.ContentOf(PublicBucket, scope.RegistryKey).Should().Be(before);
	}

	[Fact]
	public async Task ReconcileGroup_SortsNewestTargetFirstWithFileNameTiebreak()
	{
		var scope = BundleScope();
		_ = SeedBundle(scope, "b.yaml", "9.1.0");
		_ = SeedBundle(scope, "a.yaml", "9.1.0");
		_ = SeedBundle(scope, "c.yaml", "9.4.0");

		_ = await _reconciler.ReconcileGroupAsync(scope, Ctx);

		WrittenManifest().Bundles.Select(b => b.File).Should().Equal("c.yaml", "a.yaml", "b.yaml");
	}

	private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
