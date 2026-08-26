// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Elastic.Changelog.Scrubbing;
using Elastic.Changelog.Tests.Reconciliation;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Scrubbing;

public class ScrubberProcessorTests
{
	private const string PrivateBucket = "private-bucket";
	private const string PublicBucket = "public-bucket";

	private readonly FakeS3 _s3 = new(PrivateBucket, PublicBucket);
	private readonly IChangelogContentScrubber _scrubber = A.Fake<IChangelogContentScrubber>();
	private readonly ReconcileMetrics _metrics = new();
	private readonly ScrubberProcessor _processor;

	public ScrubberProcessorTests()
	{
		// The real scrub pass has its own tests; here it just marks content so assertions can
		// tell a scrubbed write from a raw copy.
		_ = A.CallTo(() => _scrubber.ScrubAsync(A<string>._, A<string>._, A<Cancel>._))
			.ReturnsLazily((string _, string content, Cancel _) =>
				Task.FromResult(new ScrubResult { Content = "scrubbed: " + content }));

		var reconciler = new BundleRegistryReconciler(
			NullLoggerFactory.Instance, _s3.Client, PublicBucket, retryBaseDelay: TimeSpan.Zero, metrics: _metrics);
		var shallowReconciler = new ShallowRegistryReconciler(
			NullLoggerFactory.Instance, _s3.Client, PublicBucket, retryBaseDelay: TimeSpan.Zero, metrics: _metrics);
		var notesReconciler = new NotesIndexReconciler(
			NullLoggerFactory.Instance, _s3.Client, PublicBucket, sourceBucketName: PrivateBucket, retryBaseDelay: TimeSpan.Zero, metrics: _metrics);
		_processor = new ScrubberProcessor(
			NullLoggerFactory.Instance, _s3.Client, PublicBucket, _scrubber, reconciler, shallowReconciler, notesReconciler, _metrics);
	}

	private Cancel Ctx => TestContext.Current.CancellationToken;

	private static int MessageCounter;

	private static ScrubberQueueMessage Message(string eventName, string key, string bucket = PrivateBucket)
	{
		var id = $"msg-{Interlocked.Increment(ref MessageCounter)}";
		// The shape S3 bucket notifications deliver to SQS (fields the processor reads).
		var body =
			"{\"Records\":[{\"eventName\":\"" + eventName + "\",\"s3\":{\"bucket\":{\"name\":\"" + bucket +
			"\"},\"object\":{\"key\":\"" + key + "\"}}}]}";
		return new ScrubberQueueMessage(id, body);
	}

	private Registry PublicManifest(string registryKey) =>
		JsonSerializer.Deserialize(_s3.ContentOf(PublicBucket, registryKey), RegistryJsonContext.Default.Registry)!;

	private SortedDictionary<string, string> ShallowMap(string mapKey) =>
		JsonSerializer.Deserialize(_s3.ContentOf(PublicBucket, mapKey), ShallowRegistryJsonContext.Default.SortedDictionaryStringString)!;

	[Fact]
	public async Task Process_CreatedEvent_ScrubsCopiesAndWritesTheGroupManifest()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "content-1");

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.1.0.yaml")], Ctx);

		failed.Should().BeEmpty();
		_s3.ContentOf(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml").Should().Be("scrubbed: content-1");
		PublicManifest("bundle/elasticsearch/registry.json").Bundles.Select(b => b.File).Should().Equal("es-9.1.0.yaml");
	}

	[Fact]
	public async Task Process_StaleRemovedEventAfterRecreate_RecopiesInsteadOfDeleting()
	{
		// The event type is advisory: the private object exists again, so a late ObjectRemoved
		// must re-copy the live object, not delete the public one.
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "recreated");
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", "old-public");

		var failed = await _processor.ProcessAsync([Message("ObjectRemoved:Delete", "bundle/elasticsearch/es-9.1.0.yaml")], Ctx);

		failed.Should().BeEmpty();
		_s3.ContentOf(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml").Should().Be("scrubbed: recreated");
		_s3.Deletes.Should().NotContain(d => d.Key == "bundle/elasticsearch/es-9.1.0.yaml");
	}

	[Fact]
	public async Task Process_StaleCreatedEventAfterDelete_RemovesThePublicCopyAndManifest()
	{
		// Private object is gone; a late ObjectCreated must converge on deletion — and the group
		// reconcile then removes the now-empty group's manifest.
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", "stale-public");
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/registry.json",
			/*lang=json,strict*/ """{"schema_version":1,"product":"elasticsearch","generated_at":"2026-01-01T00:00:00+00:00","bundles":[]}""");

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.1.0.yaml")], Ctx);

		failed.Should().BeEmpty();
		_s3.Exists(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml").Should().BeFalse();
		_s3.Exists(PublicBucket, "bundle/elasticsearch/registry.json").Should().BeFalse("an empty group's manifest is deleted: absent ≠ empty");
	}

	[Fact]
	public async Task Process_BundleRegistryKeyEvents_NeverCopyOrDelete_OnlyTriggerAGroupReconcile()
	{
		// The bundle manifest is reconciler-owned. Old CLI versions still write private bundle
		// manifests (and Phase 3's cleanup will delete them) — those events may never touch the
		// public registry object directly, only schedule a reconcile that derives the public
		// manifest from public state.
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/registry.json", /*lang=json,strict*/ """{"private":"manifest"}""");
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", BundleYaml());

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", "bundle/elasticsearch/registry.json")], Ctx);

		failed.Should().BeEmpty();
		// The public manifest was reconciled from the listing — not copied from the private one.
		var manifest = PublicManifest("bundle/elasticsearch/registry.json");
		manifest.Producer.Should().Be(BundleRegistryReconciler.Producer);
		manifest.Bundles.Select(b => b.File).Should().Equal("es-9.1.0.yaml");
		_s3.GetsFor(PrivateBucket).Should().BeEmpty("the private bundle registry content must never be read for pass-through");
	}

	[Fact]
	public async Task Process_PoolRegistryKeyEvents_AreIgnored()
	{
		// Pool registry keys (changelog/{org}/{repo}/{branch}/registry.json) are retired — no client
		// writes them since #3760. A stale event from an old client is silently dropped.
		const string poolRegistry = "changelog/elastic/kibana/main/registry.json";
		const string content = /*lang=json,strict*/ """{"schema_version":1,"bundles":[{"file":"100.yaml"}]}""";
		_ = _s3.Seed(PrivateBucket, poolRegistry, content);

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", poolRegistry)], Ctx);

		failed.Should().BeEmpty();
		_s3.Exists(PublicBucket, poolRegistry).Should().BeFalse("retired pool registry keys are not mirrored");
		_metrics.GroupReconciles.Should().Be(0, "pool manifests are not reconciled");
		_s3.Puts.Should().BeEmpty("no S3 writes for a retired pool registry event");
	}

	[Fact]
	public async Task Process_PoolRegistryKeyDeleteEvents_AreAlsoIgnored()
	{
		// Delete events for the retired pool registry are dropped the same way as creates —
		// the key is no longer managed, so no public-bucket delete is needed.
		const string poolRegistry = "changelog/elastic/kibana/main/registry.json";
		_ = _s3.Seed(PublicBucket, poolRegistry, "{}");

		var failed = await _processor.ProcessAsync([Message("ObjectRemoved:Delete", poolRegistry)], Ctx);

		failed.Should().BeEmpty();
		_s3.Exists(PublicBucket, poolRegistry).Should().BeTrue("the event was ignored; no delete happened");
	}

	[Fact]
	public async Task Process_PoolYamlEvents_ScrubAndUpdateTheShallowMap_ButWriteNoPoolManifest()
	{
		_ = _s3.Seed(PrivateBucket, "changelog/elastic/kibana/main/100.yaml", "entry");

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", "changelog/elastic/kibana/main/100.yaml")], Ctx);

		failed.Should().BeEmpty();
		_s3.ContentOf(PublicBucket, "changelog/elastic/kibana/main/100.yaml").Should().Be("scrubbed: entry");
		_s3.Exists(PublicBucket, "changelog/elastic/kibana/main/registry.json")
			.Should().BeFalse("the reconciler no longer produces pool manifests");
		_metrics.GroupReconciles.Should().Be(0);

		var map = ShallowMap("changelog/registry.json");
		map.Should().ContainKey("elastic/kibana/main");
	}

	[Fact]
	public async Task Process_BundleYamlEvents_UpdateTheShallowMapForTheProduct()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "content");

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.1.0.yaml")], Ctx);

		failed.Should().BeEmpty();
		var map = ShallowMap("bundle/registry.json");
		map.Should().ContainKey("elasticsearch");
	}

	[Fact]
	public async Task Process_MultiplePoolsInOneBatch_CoalesceIntoASingleShallowMapWrite()
	{
		_ = _s3.Seed(PrivateBucket, "changelog/elastic/kibana/main/100.yaml", "one");
		_ = _s3.Seed(PrivateBucket, "changelog/elastic/elasticsearch/main/200.yaml", "two");

		var failed = await _processor.ProcessAsync(
		[
			Message("ObjectCreated:Put", "changelog/elastic/kibana/main/100.yaml"),
			Message("ObjectCreated:Put", "changelog/elastic/elasticsearch/main/200.yaml")
		], Ctx);

		failed.Should().BeEmpty();
		_s3.Puts.Where(p => p.Key == "changelog/registry.json").Should().ContainSingle("one tree gets one map write per batch");
		ShallowMap("changelog/registry.json").Keys.Should().BeEquivalentTo("elastic/kibana/main", "elastic/elasticsearch/main");
	}

	[Fact]
	public async Task Process_OtherJsonAndNonYamlKeys_AreSkipped()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/stray.json", "{}");
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/notes.txt", "text");

		var failed = await _processor.ProcessAsync(
		[
			Message("ObjectCreated:Put", "bundle/elasticsearch/stray.json"),
			Message("ObjectCreated:Put", "bundle/elasticsearch/notes.txt")
		], Ctx);

		failed.Should().BeEmpty();
		_s3.Puts.Should().BeEmpty();
		_s3.Deletes.Should().BeEmpty();
	}

	[Fact]
	public async Task Process_MultipleEventsForOneKey_CoalesceIntoASingleObjectReconcile()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "content");

		var failed = await _processor.ProcessAsync(
		[
			Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.1.0.yaml"),
			Message("ObjectRemoved:Delete", "bundle/elasticsearch/es-9.1.0.yaml"),
			Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.1.0.yaml")
		], Ctx);

		failed.Should().BeEmpty();
		_metrics.ObjectReconciles.Should().Be(1, "the event type is ignored, so one key needs one look");
		_s3.Puts.Where(p => p.Key == "bundle/elasticsearch/es-9.1.0.yaml").Should().ContainSingle();
	}

	[Fact]
	public async Task Process_MultipleKeysInOneGroup_CoalesceIntoASingleGroupReconcile()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "one");
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.2.0.yaml", "two");
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.3.0.yaml", "three");

		var failed = await _processor.ProcessAsync(
		[
			Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.1.0.yaml"),
			Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.2.0.yaml"),
			Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.3.0.yaml")
		], Ctx);

		failed.Should().BeEmpty();
		_metrics.ObjectReconciles.Should().Be(3);
		_metrics.GroupReconciles.Should().Be(1, "all three keys share one group");
		PublicManifest("bundle/elasticsearch/registry.json").Bundles.Should().HaveCount(3);
	}

	[Fact]
	public async Task Process_SourceChangingMidFlight_IsDetectedByPostWriteValidationAndRedone()
	{
		// Older-read-writes-last: v2 lands right after our read of v1. The post-write HEAD sees
		// the mismatch and redoes the reconcile from current state, so v1 never wins.
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "v1");
		_s3.AfterGet = (key, call) =>
		{
			if (key == "bundle/elasticsearch/es-9.1.0.yaml" && _s3.ContentOf(PrivateBucket, key) == "v1")
				_ = _s3.Seed(PrivateBucket, key, "v2");
		};

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.1.0.yaml")], Ctx);

		failed.Should().BeEmpty();
		_s3.ContentOf(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml").Should().Be("scrubbed: v2");
		_metrics.ObjectReconcileRetries.Should().Be(1);
	}

	[Fact]
	public async Task Process_CanonicalKeyEntry_WritesToCanonicalPublicKeyAndSourcePointer()
	{
		// Scrubber says the private key 12345-fix.yaml should be written to public as 12345.yaml.
		const string privateKey = "changelog/elastic/elasticsearch/main/12345-fix.yaml";
		const string canonicalKey = "changelog/elastic/elasticsearch/main/12345.yaml";
		_ = _s3.Seed(PrivateBucket, privateKey, "entry-content");
		_ = A.CallTo(() => _scrubber.ScrubAsync(privateKey, A<string>._, A<Cancel>._))
			.ReturnsLazily((string _, string content, Cancel _) =>
				Task.FromResult(new ScrubResult { Content = "scrubbed: " + content, CanonicalKey = canonicalKey }));

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", privateKey)], Ctx);

		failed.Should().BeEmpty();
		_s3.ContentOf(PublicBucket, canonicalKey).Should().Be("scrubbed: entry-content",
			"canonical key must receive the scrubbed content");
		// A source pointer is written at the source key so the delete path can trace back to the
		// canonical key when the private object is eventually removed.
		_s3.Exists(PublicBucket, privateKey).Should().BeTrue(
			"source pointer must exist so delete events can trace to the canonical key");
		_s3.ContentOf(PublicBucket, privateKey).Should().Contain("link:",
			"source pointer must be a link marker pointing to the canonical PR number");
	}

	[Fact]
	public async Task Process_MultiPrEntry_WritesMarkersForNonPrimaryPrs()
	{
		// Scrubber returns markers for PRs 200 and 300 pointing to the primary PR 100.
		const string privateKey = "changelog/elastic/elasticsearch/main/100.yaml";
		_ = _s3.Seed(PrivateBucket, privateKey, "multi-pr-content");
		_ = A.CallTo(() => _scrubber.ScrubAsync(privateKey, A<string>._, A<Cancel>._))
			.ReturnsLazily((string _, string content, Cancel _) =>
				Task.FromResult(new ScrubResult
				{
					Content = "scrubbed: " + content,
					Markers =
					[
						("changelog/elastic/elasticsearch/main/200.yaml", "link: \"100\"\n"),
						("changelog/elastic/elasticsearch/main/300.yaml", "link: \"100\"\n")
					]
				}));

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", privateKey)], Ctx);

		failed.Should().BeEmpty();
		_s3.ContentOf(PublicBucket, privateKey).Should().Be("scrubbed: multi-pr-content");
		_s3.ContentOf(PublicBucket, "changelog/elastic/elasticsearch/main/200.yaml").Should().Be("link: \"100\"\n",
			"marker for PR 200 must be written to public bucket");
		_s3.ContentOf(PublicBucket, "changelog/elastic/elasticsearch/main/300.yaml").Should().Be("link: \"100\"\n",
			"marker for PR 300 must be written to public bucket");
	}

	[Fact]
	public async Task Process_FailedObjectReconcile_FailsOnlyItsOwnMessages()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/bad.yaml", "bad");
		_ = _s3.Seed(PrivateBucket, "bundle/kibana/good.yaml", "good");
		_ = A.CallTo(() => _scrubber.ScrubAsync("bundle/elasticsearch/bad.yaml", A<string>._, A<Cancel>._))
			.Throws(new InvalidOperationException("cannot scrub"));

		var badMessage = Message("ObjectCreated:Put", "bundle/elasticsearch/bad.yaml");
		var goodMessage = Message("ObjectCreated:Put", "bundle/kibana/good.yaml");
		var failed = await _processor.ProcessAsync([badMessage, goodMessage], Ctx);

		failed.Should().ContainSingle().Which.Should().Be(badMessage.MessageId);
		_s3.Exists(PublicBucket, "bundle/kibana/good.yaml").Should().BeTrue();
		PublicManifest("bundle/kibana/registry.json").Bundles.Should().ContainSingle();
	}

	[Fact]
	public async Task Process_FailedGroupReconcile_FailsEveryContributingMessage()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "one");
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.2.0.yaml", "two");
		_ = _s3.Seed(PrivateBucket, "bundle/kibana/kb-9.1.0.yaml", "three");
		// Make every conditional manifest write for the elasticsearch group lose its race.
		var counter = 0;
		_s3.BeforePut = call =>
		{
			_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/registry.json", $"{{\"race\":{counter++}}}");
		};

		var first = Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.1.0.yaml");
		var second = Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.2.0.yaml");
		var other = Message("ObjectCreated:Put", "bundle/kibana/kb-9.1.0.yaml");

		var failed = await _processor.ProcessAsync([first, second, other], Ctx);

		failed.Should().BeEquivalentTo([first.MessageId, second.MessageId],
			"every record that contributed to the failed group must redeliver");
	}

	[Fact]
	public async Task Process_UnparseableMessageBody_FailsThatMessage()
	{
		var garbage = new ScrubberQueueMessage("msg-garbage", "not json at all {");

		var failed = await _processor.ProcessAsync([garbage], Ctx);

		failed.Should().ContainSingle().Which.Should().Be("msg-garbage");
	}

	[Fact]
	public async Task Process_KeyOutsideAnyGroupLayout_IsCopiedButTriggersNoGroupReconcile()
	{
		// changelog/{org}/{file} has too few segments for a pool; the object itself still syncs.
		_ = _s3.Seed(PrivateBucket, "changelog/elastic/stray.yaml", "stray");

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", "changelog/elastic/stray.yaml")], Ctx);

		failed.Should().BeEmpty();
		_s3.Exists(PublicBucket, "changelog/elastic/stray.yaml").Should().BeTrue();
		_metrics.GroupReconciles.Should().Be(0);
	}

	[Fact]
	public async Task Process_BatchMixingObjectAndRegistryEvents_MarksGroupContributionsAcrossBoth()
	{
		// A YAML event and a bundle-registry event for the same group coalesce into one group
		// reconcile fed by both messages.
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "one");
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/registry.json", "{}");

		var failed = await _processor.ProcessAsync(
		[
			Message("ObjectCreated:Put", "bundle/elasticsearch/es-9.1.0.yaml"),
			Message("ObjectCreated:Put", "bundle/elasticsearch/registry.json")
		], Ctx);

		failed.Should().BeEmpty();
		_metrics.GroupReconciles.Should().Be(1);
	}

	[Fact]
	public async Task Process_ClientUploadedNotesIndex_IsRejectedWithNoPublicWrite()
	{
		// The notes index is reconciler-owned; a client that uploads notes-*.json must be blocked.
		_s3.Seed(PrivateBucket, "changelog/elastic/kibana/notes-9.0.0.json", /*lang=json,strict*/ """{"notes":[]}""");

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", "changelog/elastic/kibana/notes-9.0.0.json")], Ctx);

		failed.Should().BeEmpty();
		_s3.Exists(PublicBucket, "changelog/elastic/kibana/notes-9.0.0.json").Should().BeFalse();
		// No reconcile triggered — only logging
		_metrics.GroupReconciles.Should().Be(0);
	}

	[Fact]
	public async Task Process_NoteFile_ScrubbedAndNotesReconcileTriggered()
	{
		// language=yaml
		var noteYaml = """
			title: Known rollover issue
			type: known-issue
			products:
			  - product: elasticsearch
			    target: 9.0.0
			""";
		_s3.Seed(PrivateBucket, "changelog/elastic/elasticsearch/main/note-rollover.yml", noteYaml);

		var failed = await _processor.ProcessAsync(
			[Message("ObjectCreated:Put", "changelog/elastic/elasticsearch/main/note-rollover.yml")], Ctx);

		failed.Should().BeEmpty();
		// The note was scrubbed and copied to the public bucket
		_s3.ContentOf(PublicBucket, "changelog/elastic/elasticsearch/main/note-rollover.yml")
			.Should().StartWith("scrubbed:");
		// The notes index was written (reconciler read the note and produced notes-9.0.0.json)
		_s3.Exists(PublicBucket, "changelog/elastic/elasticsearch/notes-9.0.0.json").Should().BeTrue();
	}

	[Fact]
	public async Task Process_PassThroughMarker_DoesNotOverwriteExistingCanonicalContent()
	{
		// Issue 1: a private marker derived from raw (pre-allowlist) PRs can arrive after the
		// canonical public entry has already been written. The marker write must be skipped so
		// it cannot overwrite canonical content.
		const string markerKey = "changelog/elastic/elasticsearch/main/20.yaml";
		_ = _s3.Seed(PrivateBucket, markerKey, "link: \"10\"\n");
		_ = _s3.Seed(PublicBucket, markerKey, "scrubbed canonical content at 20");
		_ = A.CallTo(() => _scrubber.ScrubAsync(markerKey, A<string>._, A<Cancel>._))
			.ReturnsLazily((string _, string content, Cancel _) =>
				Task.FromResult(new ScrubResult { Content = content, IsMarker = true }));

		var failed = await _processor.ProcessAsync([Message("ObjectCreated:Put", markerKey)], Ctx);

		failed.Should().BeEmpty();
		_s3.ContentOf(PublicBucket, markerKey).Should().Be(
			"scrubbed canonical content at 20",
			"pass-through marker must not overwrite existing canonical content");
	}

	[Fact]
	public async Task Process_DeleteOfNonCanonicalSource_DeletesCanonicalAndMarkersThroughSourcePointer()
	{
		// Issue 2: when the private source key is non-canonical (e.g. 12345-fix.yaml), the
		// scrubber writes canonical content to a different public key (12345.yaml) and leaves a
		// source pointer at the source key. On delete, the processor must trace that pointer and
		// remove the canonical and all its markers.
		const string privateKey = "changelog/elastic/elasticsearch/main/12345-fix.yaml";
		const string canonicalKey = "changelog/elastic/elasticsearch/main/12345.yaml";
		const string markerKey = "changelog/elastic/elasticsearch/main/67890.yaml";
		// Simulate state left by the prior write: source pointer at privateKey, canonical at
		// canonicalKey, and a secondary-PR marker at markerKey.
		// The source pointer must carry source-redirect: true to be distinguishable from a plain marker.
		_ = _s3.Seed(PublicBucket, privateKey,
			ReleaseNotesSerialization.SerializeEntry(new ChangelogEntry { Link = "12345", SourceRedirect = true }));
		_ = _s3.Seed(PublicBucket, canonicalKey,
			// language=yaml
			"""
			type: enhancement
			title: "Example"
			prs:
			  - https://github.com/elastic/elasticsearch/pull/12345
			  - https://github.com/elastic/elasticsearch/pull/67890
			""");
		_ = _s3.Seed(PublicBucket, markerKey, "link: \"12345\"\n");

		var failed = await _processor.ProcessAsync([Message("ObjectRemoved:Delete", privateKey)], Ctx);

		failed.Should().BeEmpty();
		_s3.Exists(PublicBucket, privateKey).Should().BeFalse("source pointer must be deleted");
		_s3.Exists(PublicBucket, canonicalKey).Should().BeFalse("canonical entry must be deleted");
		_s3.Exists(PublicBucket, markerKey).Should().BeFalse("secondary-PR marker must be deleted via DeleteStaleMarkersAsync");
	}

	[Fact]
	public async Task Process_DeleteOfCanonicalEntryWithMarkers_DeletesMarkersBeforeCanonical()
	{
		// When the canonical entry's source key is itself the canonical key (numeric filename),
		// the delete path must also clean up its secondary-PR markers in the public bucket.
		const string canonicalKey = "changelog/elastic/elasticsearch/main/100.yaml";
		const string marker200 = "changelog/elastic/elasticsearch/main/200.yaml";
		const string marker300 = "changelog/elastic/elasticsearch/main/300.yaml";
		_ = _s3.Seed(PublicBucket, canonicalKey,
			// language=yaml
			"""
			type: enhancement
			title: "Multi-PR entry"
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			  - https://github.com/elastic/elasticsearch/pull/200
			  - https://github.com/elastic/elasticsearch/pull/300
			""");
		_ = _s3.Seed(PublicBucket, marker200, "link: \"100\"\n");
		_ = _s3.Seed(PublicBucket, marker300, "link: \"100\"\n");

		var failed = await _processor.ProcessAsync([Message("ObjectRemoved:Delete", canonicalKey)], Ctx);

		failed.Should().BeEmpty();
		_s3.Exists(PublicBucket, canonicalKey).Should().BeFalse("canonical entry must be deleted");
		_s3.Exists(PublicBucket, marker200).Should().BeFalse("PR-200 marker must be deleted");
		_s3.Exists(PublicBucket, marker300).Should().BeFalse("PR-300 marker must be deleted");
	}

	[Fact]
	public async Task Process_DeleteOfNonCanonicalSourceWithPlainMarker_DoesNotDeleteCanonical()
	{
		// Regression guard for source-pointer ambiguity: a plain link: marker at a non-numeric key
		// must NOT be treated as a source pointer. Only objects with source-redirect: true trigger
		// canonical deletion; otherwise any migrated marker could accidentally nuke live content.
		const string privateKey = "changelog/elastic/elasticsearch/main/12345-fix.yaml";
		const string canonicalKey = "changelog/elastic/elasticsearch/main/12345.yaml";
		// Public object has link: but no source-redirect: true — it's a plain marker, not a pointer.
		_ = _s3.Seed(PublicBucket, privateKey, "link: \"12345\"\n");
		_ = _s3.Seed(PublicBucket, canonicalKey, "type: enhancement\ntitle: Real\n");

		var failed = await _processor.ProcessAsync([Message("ObjectRemoved:Delete", privateKey)], Ctx);

		failed.Should().BeEmpty();
		_s3.Exists(PublicBucket, privateKey).Should().BeFalse("the source-key public object is deleted");
		_s3.Exists(PublicBucket, canonicalKey).Should().BeTrue(
			"a plain link: marker must not trigger canonical deletion — only source-redirect: true does");
	}

	[Fact]
	public async Task Process_DeleteOfYmlSourceKey_TracesSourcePointerToCanonical()
	{
		// .yml files are never canonical PR keys; IsNumericYamlKey must return false for them
		// even when the stem is numeric, so source-pointer tracing runs correctly.
		const string privateKey = "changelog/elastic/elasticsearch/main/12345.yml";
		const string canonicalKey = "changelog/elastic/elasticsearch/main/12345.yaml";
		_ = _s3.Seed(PublicBucket, privateKey,
			ReleaseNotesSerialization.SerializeEntry(new ChangelogEntry { Link = "12345", SourceRedirect = true }));
		_ = _s3.Seed(PublicBucket, canonicalKey, "type: enhancement\ntitle: Real\n");

		var failed = await _processor.ProcessAsync([Message("ObjectRemoved:Delete", privateKey)], Ctx);

		failed.Should().BeEmpty();
		_s3.Exists(PublicBucket, privateKey).Should().BeFalse("source pointer is cleaned up");
		_s3.Exists(PublicBucket, canonicalKey).Should().BeFalse(
			"canonical must be deleted via pointer tracing — .yml stem being numeric must not block this");
	}

	// language=yaml
	private static string BundleYaml() => """
		products:
		  - product: elasticsearch
		    target: 9.1.0
		    repo: elasticsearch
		    owner: elastic
		entries:
		  - file:
		      name: 1-feature.yaml
		      checksum: deadbeef
		    type: enhancement
		    title: Sample
		""";
}
