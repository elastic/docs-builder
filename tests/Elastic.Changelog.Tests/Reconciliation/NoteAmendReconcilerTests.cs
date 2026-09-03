// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

/// <summary>
/// Tests for <see cref="NoteAmendReconciler"/>. Each scenario exercises a discrete path through
/// <c>ProcessVersionBundleAsync</c>: late note, already shipped, already in human amend, missing
/// file annotations, note removed (delete path), idempotent redelivery, no published bundle.
/// </summary>
public class NoteAmendReconcilerTests
{
	private const string PublicBucket = "public-bucket";
	private const string Org = "elastic";
	private const string Repo = "elasticsearch";
	private const string Product = "elasticsearch";
	private const string Version = "9.3.0";

	// Pool key prefix: changelog/elastic/elasticsearch/
	private static string NoteKey(string branch, string file) => $"changelog/{Org}/{Repo}/{branch}/{file}";

	private static string AmendNotesKey(string parentFile) =>
		$"bundle/{Product}/{Path.GetFileNameWithoutExtension(parentFile)}.amend-notes{Path.GetExtension(parentFile)}";

	private static string RegistryKey() => $"bundle/{Product}/registry.json";

	private static string BundleKey(string file) => $"bundle/{Product}/{file}";

	// language=yaml
	private const string NoteYaml = "title: CVE security fix\n"
		+ "type: security\n"
		+ "products:\n"
		+ "  - product: elasticsearch\n"
		+ "    versions: [9.3.0]\n"
		+ "    lifecycle: ga\n";

	private readonly FakeS3 _s3 = new(PublicBucket);
	private readonly NotesIndexReconciler _notesReconciler;
	private readonly NoteAmendReconciler _reconciler;

	public NoteAmendReconcilerTests()
	{
		_notesReconciler = new NotesIndexReconciler(NullLoggerFactory.Instance, _s3.Client, PublicBucket, retryBaseDelay: TimeSpan.Zero);
		_reconciler = new NoteAmendReconciler(
			NullLoggerFactory.Instance,
			_s3.Client,
			PublicBucket,
			_notesReconciler,
			retryBaseDelay: TimeSpan.Zero
		);
	}

	private static ChangelogScope NotesScope()
	{
		_ = ChangelogScope.TryCreateNotes(Org, Repo, out var scope);
		return scope!;
	}

	/// <summary>Builds a minimal parent bundle YAML with one PR entry that carries a file identity.</summary>
	private static string ParentBundleYaml(params string[] entryFileNames)
	{
		var bundle = new Bundle
		{
			Products = [new BundledProduct(Product, target: Version, lifecycle: Lifecycle.Ga)],
			Entries =
			[
				.. entryFileNames.Select(
					n => new BundledEntry
					{
						File = new BundledFile { Name = n, Checksum = "abc123" },
						Title = $"Entry for {n}",
						Type = ChangelogEntryType.BugFix
					}
				)
			]
		};
		return ReleaseNotesSerialization.SerializeBundle(bundle);
	}

	/// <summary>Registry JSON listing the given bundles for the test product, all at <see cref="Version"/>.</summary>
	private static string RegistryJson(params string[] files)
	{
		var bundles = files.Select(f => new ChangelogRegistryBundle { File = f, Target = Version }).ToList();
		var registry = new ChangelogRegistry { Product = Product, Bundles = bundles };
		return JsonSerializer.Serialize(registry, ChangelogRegistryJsonContext.Default.ChangelogRegistry);
	}

	/// <summary>Registry JSON listing the given bundles with an explicit target version (use when the bundle should NOT match <see cref="Version"/>).</summary>
	private static string RegistryJsonWithTarget(string target, params string[] files)
	{
		var bundles = files.Select(f => new ChangelogRegistryBundle { File = f, Target = target }).ToList();
		var registry = new ChangelogRegistry { Product = Product, Bundles = bundles };
		return JsonSerializer.Serialize(registry, ChangelogRegistryJsonContext.Default.ChangelogRegistry);
	}

	private static NotesIndex ReadNotesIndex(string json) => JsonSerializer.Deserialize(json, NotesIndexJsonContext.Default.NotesIndex)!;

	private static IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>> NotesByVersion(string version, params string[] paths) =>
		new Dictionary<string, IReadOnlyList<NoteIndexEntry>>
		{
			[version] = [.. paths.Select(p => new NoteIndexEntry { Path = p, BundleSeq = 0 })]
		};

	// -----------------------------------------------------------------------------------------

	[Fact]
	public async Task LateNote_NoBundleAmendYet_WritesAmendNotesSidecar()
	{
		const string parent = "elasticsearch-9.3.0.yaml";
		_s3.Seed(PublicBucket, RegistryKey(), RegistryJson(parent));
		_s3.Seed(PublicBucket, BundleKey(parent), ParentBundleYaml("main/pr-100.yaml"));
		_s3.Seed(PublicBucket, NoteKey("main", "note-cve.yml"), NoteYaml);

		var notesByVersion = NotesByVersion(Version, "main/note-cve.yml");
		await _reconciler.ReconcileAsync(NotesScope(), notesByVersion, TestContext.Current.CancellationToken);

		// Amend sidecar must have been written.
		_s3.Exists(PublicBucket, AmendNotesKey(parent)).Should().BeTrue("late note must produce an amend sidecar");

		// Notes index must be re-written with bundle_seq = 2.
		var indexKey = ChangelogKeys.NotesIndexKey(Org, Repo, Version);
		_s3.Exists(PublicBucket, indexKey).Should().BeTrue("notes index must be re-written with bundle_seq values");
		var index = ReadNotesIndex(_s3.ContentOf(PublicBucket, indexKey));
		index.Notes.Should().ContainSingle().Which.BundleSeq.Should().Be(2);
	}

	[Fact]
	public async Task NoteShippedInParent_NoAmendWritten_SeqIsOne()
	{
		const string parent = "elasticsearch-9.3.0.yaml";
		// Parent bundle already contains the note by leaf name.
		_s3.Seed(PublicBucket, RegistryKey(), RegistryJson(parent));
		_s3.Seed(PublicBucket, BundleKey(parent), ParentBundleYaml("main/note-cve.yml", "main/pr-100.yaml"));
		_s3.Seed(PublicBucket, NoteKey("main", "note-cve.yml"), NoteYaml);

		var notesByVersion = NotesByVersion(Version, "main/note-cve.yml");
		await _reconciler.ReconcileAsync(NotesScope(), notesByVersion, TestContext.Current.CancellationToken);

		// No amend sidecar should be written.
		_s3.Exists(PublicBucket, AmendNotesKey(parent)).Should().BeFalse("note already in parent → no amend needed");

		// bundle_seq must be 1 (shipped in original bundle).
		var indexKey = ChangelogKeys.NotesIndexKey(Org, Repo, Version);
		var index = ReadNotesIndex(_s3.ContentOf(PublicBucket, indexKey));
		index.Notes.Should().ContainSingle().Which.BundleSeq.Should().Be(1);
	}

	[Fact]
	public async Task NoteShippedInHumanAmend_NoAmendNotesWritten_SeqIsOne()
	{
		const string parent = "elasticsearch-9.3.0.yaml";
		const string humanAmend = "elasticsearch-9.3.0.amend-1.yaml";

		_s3.Seed(PublicBucket, RegistryKey(), RegistryJson(parent, humanAmend));
		_s3.Seed(PublicBucket, BundleKey(parent), ParentBundleYaml("main/pr-100.yaml"));

		// Human amend adds the note.
		var amendBundle = new Bundle
		{
			Products = [new BundledProduct(Product, target: Version, lifecycle: Lifecycle.Ga)],
			Entries =
			[
				new BundledEntry
				{
					File = new BundledFile { Name = "main/note-cve.yml", Checksum = "def456" },
					Title = "CVE",
					Type = ChangelogEntryType.Security
				}
			]
		};
		_s3.Seed(PublicBucket, BundleKey(humanAmend), ReleaseNotesSerialization.SerializeBundle(amendBundle));
		_s3.Seed(PublicBucket, NoteKey("main", "note-cve.yml"), NoteYaml);

		var notesByVersion = NotesByVersion(Version, "main/note-cve.yml");
		await _reconciler.ReconcileAsync(NotesScope(), notesByVersion, TestContext.Current.CancellationToken);

		_s3
			.Exists(PublicBucket, AmendNotesKey(parent))
			.Should()
			.BeFalse("note already in human amend → reconciler amend-notes must not be created");

		var indexKey = ChangelogKeys.NotesIndexKey(Org, Repo, Version);
		var index = ReadNotesIndex(_s3.ContentOf(PublicBucket, indexKey));
		index.Notes.Should().ContainSingle().Which.BundleSeq.Should().Be(1);
	}

	[Fact]
	public async Task ParentBundleHasNoFileAnnotations_Skipped_SeqRemainsZero()
	{
		const string parent = "elasticsearch-9.3.0.yaml";
		_s3.Seed(PublicBucket, RegistryKey(), RegistryJson(parent));

		// Parent bundle entries have no file blocks (hand-authored legacy format).
		var handAuthored = new Bundle
		{
			Products = [new BundledProduct(Product, target: Version, lifecycle: Lifecycle.Ga)],
			Entries = [new BundledEntry { Title = "Hand-authored entry, no file block", Type = ChangelogEntryType.BugFix }]
		};
		_s3.Seed(PublicBucket, BundleKey(parent), ReleaseNotesSerialization.SerializeBundle(handAuthored));
		_s3.Seed(PublicBucket, NoteKey("main", "note-cve.yml"), NoteYaml);

		var notesByVersion = NotesByVersion(Version, "main/note-cve.yml");
		await _reconciler.ReconcileAsync(NotesScope(), notesByVersion, TestContext.Current.CancellationToken);

		// No amend written; shipped state is unknown.
		_s3.Exists(PublicBucket, AmendNotesKey(parent)).Should().BeFalse("unknown shipped state → skip, no amend");

		// Notes index is still re-written, but bundle_seq stays 0.
		var indexKey = ChangelogKeys.NotesIndexKey(Org, Repo, Version);
		var index = ReadNotesIndex(_s3.ContentOf(PublicBucket, indexKey));
		index.Notes.Should().ContainSingle().Which.BundleSeq.Should().Be(0);
	}

	[Fact]
	public async Task NoteRemovedFromIndex_ExistingAmendSidecarDeleted()
	{
		const string parent = "elasticsearch-9.3.0.yaml";
		_s3.Seed(PublicBucket, RegistryKey(), RegistryJson(parent));
		_s3.Seed(PublicBucket, BundleKey(parent), ParentBundleYaml("main/pr-100.yaml"));

		// Pre-existing amend-notes sidecar from a previous reconcile.
		var staleAmend = new Bundle
		{
			Products = [new BundledProduct(Product, target: Version, lifecycle: Lifecycle.Ga)],
			Entries =
			[
				new BundledEntry
				{
					File = new BundledFile { Name = "main/note-cve.yml", Checksum = "old" },
					Title = "CVE",
					Type = ChangelogEntryType.Security
				}
			]
		};
		_s3.Seed(PublicBucket, AmendNotesKey(parent), ReleaseNotesSerialization.SerializeBundle(staleAmend));

		// No notes for this version (note was deleted from the pool).
		var notesByVersion = NotesByVersion(Version);
		await _reconciler.ReconcileAsync(NotesScope(), notesByVersion, TestContext.Current.CancellationToken);

		_s3.Exists(PublicBucket, AmendNotesKey(parent)).Should().BeFalse("stale amend sidecar must be deleted when no notes remain");
		_s3.Deletes.Should().ContainSingle().Which.Key.Should().Be(AmendNotesKey(parent));
	}

	[Fact]
	public async Task Idempotent_SameStateRedelivered_NoSecondPut()
	{
		const string parent = "elasticsearch-9.3.0.yaml";
		_s3.Seed(PublicBucket, RegistryKey(), RegistryJson(parent));
		_s3.Seed(PublicBucket, BundleKey(parent), ParentBundleYaml("main/pr-100.yaml"));
		_s3.Seed(PublicBucket, NoteKey("main", "note-cve.yml"), NoteYaml);

		var notesByVersion = NotesByVersion(Version, "main/note-cve.yml");

		// First reconcile → amend sidecar written.
		await _reconciler.ReconcileAsync(NotesScope(), notesByVersion, TestContext.Current.CancellationToken);
		var putsAfterFirst = _s3.Puts.Count;
		putsAfterFirst.Should().BeGreaterThan(0, "first pass must write the amend sidecar and the notes index");

		// Second reconcile with the same state → content is identical → no additional PUTs.
		await _reconciler.ReconcileAsync(NotesScope(), notesByVersion, TestContext.Current.CancellationToken);
		var putsAfterSecond = _s3.Puts.Count;

		// The notes index re-write is idempotent too (same content, conditional PUT is a no-op).
		// At most 1 extra put for the index (if the reconciler always writes it), and 0 for the amend sidecar.
		var amendSidecarPuts = _s3.Puts.Count(p => p.Key == AmendNotesKey(parent));
		amendSidecarPuts.Should().Be(1, "amend sidecar must be written exactly once across both passes");
	}

	[Fact]
	public async Task NoBundleForVersion_NoAmend_SeqRemainsZero()
	{
		// Registry exists for the product but contains no bundle that matches the version.
		_s3.Seed(PublicBucket, RegistryKey(), RegistryJsonWithTarget("8.0.0", "elasticsearch-8.0.0.yaml"));
		_s3.Seed(PublicBucket, BundleKey("elasticsearch-8.0.0.yaml"), ParentBundleYaml("main/pr-100.yaml"));
		_s3.Seed(PublicBucket, NoteKey("main", "note-cve.yml"), NoteYaml);

		var notesByVersion = NotesByVersion(Version, "main/note-cve.yml");
		await _reconciler.ReconcileAsync(NotesScope(), notesByVersion, TestContext.Current.CancellationToken);

		// No amend sidecar: no matching bundle.
		_s3.Puts.Should().NotContain(p => p.Key.Contains("amend-notes"), "no matching bundle → no amend possible");

		// Notes index re-written with bundle_seq = 0.
		var indexKey = ChangelogKeys.NotesIndexKey(Org, Repo, Version);
		var index = ReadNotesIndex(_s3.ContentOf(PublicBucket, indexKey));
		index.Notes.Should().ContainSingle().Which.BundleSeq.Should().Be(0);
	}

	[Fact]
	public async Task NoProductsInBundleTree_NoAmend()
	{
		// Bundle tree is empty (no products listed under bundle/).
		// No registry.json objects exist, so ListObjectsV2 returns no common prefixes.
		var notesByVersion = NotesByVersion(Version, "main/note-cve.yml");
		await _reconciler.ReconcileAsync(NotesScope(), notesByVersion, TestContext.Current.CancellationToken);

		_s3.Puts.Should().NotContain(p => p.Key.Contains("amend-notes"));
	}
}
