// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

public class NotesIndexReconcilerTests
{
	private const string PublicBucket = "public-bucket";

	/// <summary>New-format note using the <c>versions:</c> field.</summary>
	private const string NoteYaml = "title: Slow rollover known issue\n"
		+ "type: known-issue\n"
		+ "products:\n"
		+ "  - product: elasticsearch\n"
		+ "    versions: [9.0.0]\n";

	/// <summary>Legacy-format note using the obsolete <c>target:</c> field for backward-compat tests.</summary>
	private const string LegacyNoteYaml = "title: Legacy rollover known issue\n"
		+ "type: known-issue\n"
		+ "products:\n"
		+ "  - product: elasticsearch\n"
		+ "    target: 9.0.0\n";

	private const string NoteYamlTwoVersions = "title: Two-version known issue\n"
		+ "type: known-issue\n"
		+ "products:\n"
		+ "  - product: elasticsearch\n"
		+ "    versions: [9.0.0, 9.1.0]\n";

	private readonly FakeS3 _s3 = new(PublicBucket);
	private readonly NotesIndexReconciler _reconciler;

	public NotesIndexReconcilerTests() =>
		_reconciler = new NotesIndexReconciler(NullLoggerFactory.Instance, _s3.Client, PublicBucket, retryBaseDelay: TimeSpan.Zero);

	private static ChangelogScope NotesScope(string org = "elastic", string repo = "elasticsearch")
	{
		_ = ChangelogScope.TryCreateNotes(org, repo, out var scope);
		return scope!;
	}

	private void SeedNote(string branch, string fileName, string yaml) =>
		_s3.Seed(PublicBucket, $"changelog/elastic/elasticsearch/{branch}/{fileName}", yaml);

	private NotesIndex ReadIndex(string version) =>
		JsonSerializer.Deserialize(
			_s3.ContentOf(PublicBucket, ChangelogKeys.NotesIndexKey("elastic", "elasticsearch", version)),
			NotesIndexJsonContext.Default.NotesIndex
		)!;

	// Helper that projects entries to their paths for compact assertions.
	private static IEnumerable<string> Paths(NotesIndex index) => index.Notes.Select(e => e.Path);

	[Fact]
	public void DirectYamlParse_NoteYaml_HasVersions()
	{
		var dto = ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(NoteYaml);
		dto.Products.Should().NotBeNullOrEmpty("YAML has products");
		dto.Products?[0].Versions.Should().BeEquivalentTo(["9.0.0"]);
	}

	[Fact]
	public void DirectYamlParse_LegacyTargetField_FallsBackToVersions()
	{
		// Existing notes in pools still carry `target:` — the reconciler must still read them.
		var dto = ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(LegacyNoteYaml);
		dto.Products.Should().NotBeNullOrEmpty();
#pragma warning disable CS0618 // testing backward-compat read of obsolete Target
		dto.Products?[0].Target.Should().Be("9.0.0");
#pragma warning restore CS0618
	}

	[Fact]
	public async Task ReconcileRepo_SingleNote_WritesIndex()
	{
		SeedNote("main", "note-slow-rollover.yml", NoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		_s3.ListCalls.Should().BeGreaterThan(0, "reconciler should have listed the bucket");
		_s3
			.Gets
			.Count
			.Should()
			.BeGreaterThan(
				0,
				$"reconciler should have fetched the note; ListCalls={_s3.ListCalls} SeedExists={_s3.Exists(PublicBucket, "changelog/elastic/elasticsearch/main/note-slow-rollover.yml")}"
			);
		_s3
			.Puts
			.Count
			.Should()
			.BeGreaterThan(0, $"reconciler should have written the index; ListCalls={_s3.ListCalls} Gets={_s3.Gets.Count}");
		var index = ReadIndex("9.0.0");
		Paths(index).Should().BeEquivalentTo(["main/note-slow-rollover.yml"]);
	}

	[Fact]
	public async Task ReconcileRepo_SingleNote_DefaultsBundleSeqToZero()
	{
		SeedNote("main", "note-slow-rollover.yml", NoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		var index = ReadIndex("9.0.0");
		index.Notes.Should().ContainSingle().Which.BundleSeq.Should().Be(0);
	}

	[Fact]
	public async Task ReconcileRepo_LegacyTargetNote_IndexedViaFallback()
	{
		// A note that still uses the old `target:` field must still be indexed.
		SeedNote("main", "note-legacy.yml", LegacyNoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		Paths(ReadIndex("9.0.0")).Should().BeEquivalentTo(["main/note-legacy.yml"]);
	}

	[Fact]
	public async Task ReconcileRepo_NoteWithTwoVersions_AppearsInBothIndexes()
	{
		SeedNote("main", "note-two-versions.yml", NoteYamlTwoVersions);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		Paths(ReadIndex("9.0.0")).Should().BeEquivalentTo(["main/note-two-versions.yml"]);
		Paths(ReadIndex("9.1.0")).Should().BeEquivalentTo(["main/note-two-versions.yml"]);
	}

	[Fact]
	public async Task ReconcileRepo_SameNoteNameOnTwoBranches_BothInIndex()
	{
		SeedNote("main", "note-slow-rollover.yml", NoteYaml);
		SeedNote("9.0", "note-slow-rollover.yml", NoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		var index = ReadIndex("9.0.0");
		Paths(index).Should().BeEquivalentTo(["9.0/note-slow-rollover.yml", "main/note-slow-rollover.yml"]);
	}

	[Fact]
	public async Task ReconcileRepo_NoNotes_WritesNoIndexes()
	{
		// Seed a regular changelog entry that is not a note-*.yml
		_s3.Seed(PublicBucket, "changelog/elastic/elasticsearch/main/12345.yaml", "title: PR entry");

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		_s3.Puts.Should().BeEmpty();
	}

	[Fact]
	public async Task ReconcileRepo_NoteWithNoProducts_NotIncludedInAnyIndex()
	{
		SeedNote("main", "note-no-products.yml", "title: Note with no products\ntype: known-issue");

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		_s3.Puts.Should().BeEmpty();
	}

	[Fact]
	public async Task ReconcileRepo_IndexPathsAreSorted()
	{
		SeedNote("main", "note-b.yml", NoteYaml);
		SeedNote("9.0", "note-a.yml", NoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		var index = ReadIndex("9.0.0");
		Paths(index).Should().Equal(["9.0/note-a.yml", "main/note-b.yml"]);
	}

	[Fact]
	public async Task ReconcileRepo_BranchWithSlashInName_IsIncludedInIndex()
	{
		// Branch name contains '/' — e.g. "feature/my-fix"
		SeedNote("feature/my-fix", "note-slow-rollover.yml", NoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		var index = ReadIndex("9.0.0");
		Paths(index).Should().BeEquivalentTo(["feature/my-fix/note-slow-rollover.yml"]);
	}

	[Fact]
	public async Task ReconcileRepo_BranchWithSlashInName_FullPathInIndex()
	{
		// A feature branch with '/' in its name must index the full pool-relative path.
		// The branch is derivable from the path (everything before the last '/'), so it is not stored.
		SeedNote("feature/my-fix", "note-slow-rollover.yml", NoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		var index = ReadIndex("9.0.0");
		index.Notes.Should().ContainSingle().Which.Path.Should().Be("feature/my-fix/note-slow-rollover.yml");
	}

	[Fact]
	public async Task ReconcileRepo_StaleTargetRemoved_OldIndexDeleted()
	{
		// Pre-seed a stale notes-8.0.0.json index from a previous reconcile run.
		_s3.Seed(
			PublicBucket,
			ChangelogKeys.NotesIndexKey("elastic", "elasticsearch", "8.0.0"),
			/*lang=json,strict*/
			"""{"schema_version":1,"notes":[]}"""
		);

		// Only seed a note for 9.0.0.
		SeedNote("main", "note-slow-rollover.yml", NoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		// 9.0.0 index should be written.
		Paths(ReadIndex("9.0.0")).Should().BeEquivalentTo(["main/note-slow-rollover.yml"]);

		// 8.0.0 index should have been deleted.
		_s3.Deletes.Should().ContainSingle().Which.Key.Should().Be(ChangelogKeys.NotesIndexKey("elastic", "elasticsearch", "8.0.0"));
	}

	[Fact]
	public async Task ReconcileRepo_NoNotes_DeletesAllExistingIndexes()
	{
		// Pre-seed a stale notes index.
		_s3.Seed(
			PublicBucket,
			ChangelogKeys.NotesIndexKey("elastic", "elasticsearch", "9.0.0"),
			/*lang=json,strict*/
			"""{"schema_version":1,"notes":[]}"""
		);

		// No note files — just an unrelated changelog entry.
		_s3.Seed(PublicBucket, "changelog/elastic/elasticsearch/main/12345.yaml", "title: PR entry");

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		// No new indexes should be written.
		_s3.Puts.Should().BeEmpty();

		// The stale index should be deleted.
		_s3.Deletes.Should().ContainSingle().Which.Key.Should().Be(ChangelogKeys.NotesIndexKey("elastic", "elasticsearch", "9.0.0"));
	}
}
