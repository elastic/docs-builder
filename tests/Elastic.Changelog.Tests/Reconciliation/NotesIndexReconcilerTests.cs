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

	private const string NoteYaml =
		"title: Slow rollover known issue\n" +
		"type: known-issue\n" +
		"products:\n" +
		"  - product: elasticsearch\n" +
		"    target: 9.0.0\n";

	private const string NoteYamlTwoTargets =
		"title: Two-version known issue\n" +
		"type: known-issue\n" +
		"products:\n" +
		"  - product: elasticsearch\n" +
		"    target: 9.0.0\n" +
		"  - product: elasticsearch\n" +
		"    target: 9.1.0\n";

	private readonly FakeS3 _s3 = new(PublicBucket);
	private readonly NotesIndexReconciler _reconciler;

	public NotesIndexReconcilerTests() =>
		_reconciler = new NotesIndexReconciler(
			NullLoggerFactory.Instance, _s3.Client, PublicBucket, retryBaseDelay: TimeSpan.Zero);

	private static ChangelogScope NotesScope(string org = "elastic", string repo = "elasticsearch")
	{
		_ = ChangelogScope.TryCreateNotes(org, repo, out var scope);
		return scope!;
	}

	private void SeedNote(string branch, string fileName, string yaml) =>
		_s3.Seed(PublicBucket, $"changelog/elastic/elasticsearch/{branch}/{fileName}", yaml);

	private NotesIndex ReadIndex(string target) =>
		JsonSerializer.Deserialize(
			_s3.ContentOf(PublicBucket, ChangelogKeys.NotesIndexKey("elastic", "elasticsearch", target)),
			NotesIndexJsonContext.Default.NotesIndex)!;

	[Fact]
	public void DirectYamlParse_NoteYaml_HasProducts()
	{
		var dto = ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(NoteYaml);
		dto.Products.Should().NotBeNullOrEmpty("YAML has products");
		dto.Products![0].Target.Should().Be("9.0.0");
	}

	[Fact]
	public async Task ReconcileRepo_SingleNote_WritesIndex()
	{
		SeedNote("main", "note-slow-rollover.yml", NoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		_s3.ListCalls.Should().BeGreaterThan(0, "reconciler should have listed the bucket");
		_s3.Gets.Count.Should().BeGreaterThan(0, $"reconciler should have fetched the note; ListCalls={_s3.ListCalls} SeedExists={_s3.Exists(PublicBucket, "changelog/elastic/elasticsearch/main/note-slow-rollover.yml")}");
		_s3.Puts.Count.Should().BeGreaterThan(0, $"reconciler should have written the index; ListCalls={_s3.ListCalls} Gets={_s3.Gets.Count}");
		var index = ReadIndex("9.0.0");
		index.Notes.Should().BeEquivalentTo(["main/note-slow-rollover.yml"]);
	}

	[Fact]
	public async Task ReconcileRepo_NoteWithTwoTargets_AppearsInBothIndexes()
	{
		SeedNote("main", "note-two-targets.yml", NoteYamlTwoTargets);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		ReadIndex("9.0.0").Notes.Should().BeEquivalentTo(["main/note-two-targets.yml"]);
		ReadIndex("9.1.0").Notes.Should().BeEquivalentTo(["main/note-two-targets.yml"]);
	}

	[Fact]
	public async Task ReconcileRepo_SameNoteNameOnTwoBranches_BothInIndex()
	{
		SeedNote("main", "note-slow-rollover.yml", NoteYaml);
		SeedNote("9.0", "note-slow-rollover.yml", NoteYaml);

		await _reconciler.ReconcileRepoAsync(NotesScope(), TestContext.Current.CancellationToken);

		var index = ReadIndex("9.0.0");
		index.Notes.Should().BeEquivalentTo([
			"9.0/note-slow-rollover.yml",
			"main/note-slow-rollover.yml"
		]);
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
		index.Notes.Should().Equal(["9.0/note-a.yml", "main/note-b.yml"]);
	}
}
