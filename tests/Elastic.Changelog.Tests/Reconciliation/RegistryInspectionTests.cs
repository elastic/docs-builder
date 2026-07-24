// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Elastic.Changelog.Uploading;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

public class RegistryInspectionTests
{
	private readonly FakeS3Bucket _bucket = new();
	private readonly RegistryScopeInspector _inspector;

	private static readonly DateTimeOffset FixedNow = new(2026, 5, 6, 12, 0, 0, TimeSpan.Zero);

	public RegistryInspectionTests() =>
		_inspector = new RegistryScopeInspector(NullLoggerFactory.Instance, new FakeTimeProvider(FixedNow));

	private IS3ScopeReader Reader => new S3ScopeReader(_bucket.Client, "private-bucket");

	private static ChangelogScope BundleScope(string product = "elasticsearch") =>
		ChangelogScope.TryCreateBundle(product, out var scope) ? scope : throw new InvalidOperationException();

	private static ChangelogScope PoolScope(string org = "elastic", string repo = "elasticsearch", string branch = "main") =>
		ChangelogScope.TryCreateChangelog(org, repo, branch, out var scope) ? scope : throw new InvalidOperationException();

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
	private const string LegacyAmendYaml = """
		exclude-entries:
		  - file:
		      name: 1-feature.yaml
		      checksum: deadbeef
		""";

	private string SeedBundle(string file, string target, string product = "elasticsearch") =>
		_bucket.Seed($"bundle/{product}/{file}", BundleYaml(product, target));

	private void SeedRegistry(string product = "elasticsearch", params RegistryBundle[] entries)
	{
		var registry = new Registry
		{
			Product = product,
			GeneratedAt = FixedNow,
			Bundles = entries
		};
		_ = _bucket.Seed($"bundle/{product}/registry.json", JsonSerializer.Serialize(registry, RegistryJsonContext.Default.Registry));
	}

	private Task<RegistryStateSnapshot> Inspect(ChangelogScope? scope = null) =>
		_inspector.InspectAsync(Reader, scope ?? BundleScope(), TestContext.Current.CancellationToken);

	[Fact]
	public async Task Inspect_CleanScope_ReportsClean()
	{
		var etag = SeedBundle("9.3.0.yaml", "9.3.0");
		SeedRegistry(entries: new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = etag });

		var snapshot = await Inspect();

		snapshot.IsClean.Should().BeTrue();
		snapshot.RegistryHealth.Should().Be(RegistryHealth.Valid);
		snapshot.Divergences.Should().BeEmpty();
		snapshot.Objects.Should().ContainSingle();
		snapshot.ExpectedEntries.Should().ContainSingle();
	}

	[Fact]
	public async Task Inspect_ObjectWithoutRegistryEntry_ReportsMissing()
	{
		var kept = SeedBundle("9.3.0.yaml", "9.3.0");
		_ = SeedBundle("9.4.0.yaml", "9.4.0");
		SeedRegistry(entries: new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = kept });

		var snapshot = await Inspect();

		snapshot.IsClean.Should().BeFalse();
		var divergence = snapshot.Divergences.Should().ContainSingle().Subject;
		divergence.Kind.Should().Be(RegistryDivergenceKind.Missing);
		divergence.File.Should().Be("9.4.0.yaml");
		divergence.ObjectTarget.Should().Be("9.4.0");
	}

	[Fact]
	public async Task Inspect_RegistryEntryWithoutObject_ReportsStale()
	{
		var etag = SeedBundle("9.3.0.yaml", "9.3.0");
		SeedRegistry(entries:
		[
			new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = etag },
			new RegistryBundle { File = "9.2.0.yaml", Target = "9.2.0", ETag = "gone" }
		]);

		var snapshot = await Inspect();

		var divergence = snapshot.Divergences.Should().ContainSingle().Subject;
		divergence.Kind.Should().Be(RegistryDivergenceKind.Stale);
		divergence.File.Should().Be("9.2.0.yaml");
		divergence.RegistryTarget.Should().Be("9.2.0");
	}

	[Fact]
	public async Task Inspect_UnparseableRegistry_ReportsCorrupt()
	{
		_ = SeedBundle("9.3.0.yaml", "9.3.0");
		_ = _bucket.Seed("bundle/elasticsearch/registry.json", "not json {{{");

		var snapshot = await Inspect();

		snapshot.RegistryHealth.Should().Be(RegistryHealth.Corrupt);
		snapshot.IsClean.Should().BeFalse();
		var divergence = snapshot.Divergences.Should().ContainSingle().Subject;
		divergence.Kind.Should().Be(RegistryDivergenceKind.Corrupt);
		divergence.File.Should().Be("registry.json");
		// Even with a corrupt manifest the snapshot still knows what the registry should contain.
		snapshot.ExpectedEntries.Should().ContainSingle().Which.Target.Should().Be("9.3.0");
	}

	[Fact]
	public async Task Inspect_RegistryWithUnsafeFileName_ReportsCorrupt()
	{
		SeedRegistry(entries: new RegistryBundle { File = "../evil.yaml", Target = "9.3.0", ETag = "etag" });

		var snapshot = await Inspect();

		snapshot.RegistryHealth.Should().Be(RegistryHealth.Corrupt);
		snapshot.Divergences.Should().ContainSingle().Which.Kind.Should().Be(RegistryDivergenceKind.Corrupt);
	}

	[Fact]
	public async Task Inspect_RegistryETagDisagreesWithObject_ReportsObjectDivergent()
	{
		_ = SeedBundle("9.3.0.yaml", "9.3.0");
		SeedRegistry(entries: new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = "outdated-etag" });

		var snapshot = await Inspect();

		var divergence = snapshot.Divergences.Should().ContainSingle().Subject;
		divergence.Kind.Should().Be(RegistryDivergenceKind.ObjectDivergent);
		divergence.RegistryETag.Should().Be("outdated-etag");
		divergence.ObjectETag.Should().Be(FakeS3Bucket.ETagOf(BundleYaml("elasticsearch", "9.3.0")));
	}

	[Fact]
	public async Task Inspect_RegistryTargetDisagreesWithObject_ReportsObjectDivergent()
	{
		// A legacy amend (no products of its own) whose registry entry recorded target: null,
		// while the parent bundle in the same scope resolves it to 9.3.0.
		var parentETag = SeedBundle("9.3.0.yaml", "9.3.0");
		var amendETag = _bucket.Seed("bundle/elasticsearch/9.3.0.amend-1.yaml", LegacyAmendYaml);
		SeedRegistry(entries:
		[
			new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = parentETag },
			new RegistryBundle { File = "9.3.0.amend-1.yaml", Target = null, ETag = amendETag }
		]);

		var snapshot = await Inspect();

		var divergence = snapshot.Divergences.Should().ContainSingle().Subject;
		divergence.Kind.Should().Be(RegistryDivergenceKind.ObjectDivergent);
		divergence.File.Should().Be("9.3.0.amend-1.yaml");
		divergence.RegistryTarget.Should().BeNull();
		divergence.ObjectTarget.Should().Be("9.3.0", "the amend inherits the parent bundle's target");
	}

	[Fact]
	public async Task Inspect_MissingRegistryWithObjects_ReportsEveryObjectMissing()
	{
		_ = SeedBundle("9.3.0.yaml", "9.3.0");
		_ = SeedBundle("9.4.0.yaml", "9.4.0");

		var snapshot = await Inspect();

		snapshot.RegistryHealth.Should().Be(RegistryHealth.Missing);
		snapshot.IsClean.Should().BeFalse();
		snapshot.Divergences.Should().HaveCount(2);
		snapshot.Divergences.Should().OnlyContain(d => d.Kind == RegistryDivergenceKind.Missing);
	}

	[Fact]
	public async Task Inspect_MissingRegistryOverEmptyScope_ReportsClean()
	{
		var snapshot = await Inspect();

		snapshot.RegistryHealth.Should().Be(RegistryHealth.Missing);
		snapshot.IsClean.Should().BeTrue("an empty scope with no manifest has nothing to reconcile");
	}

	[Fact]
	public async Task Inspect_NewerSchemaRegistry_ReportsUnsupportedWithoutDivergences()
	{
		_ = _bucket.Seed("bundle/elasticsearch/registry.json",
								 /*lang=json,strict*/
								 """{ "schema_version": 2, "product": "elasticsearch", "generated_at": "2026-05-06T12:00:00+00:00", "bundles": [] }""");

		var snapshot = await Inspect();

		snapshot.RegistryHealth.Should().Be(RegistryHealth.UnsupportedSchema);
		snapshot.IsClean.Should().BeFalse();
		snapshot.Divergences.Should().BeEmpty("entries of a newer schema cannot be judged");
	}

	[Fact]
	public async Task Inspect_UnparseableBundleObject_KeepsRegistryTargetAndDiagnoses()
	{
		var etag = _bucket.Seed("bundle/elasticsearch/9.3.0.yaml", "\tnot yaml: [");
		SeedRegistry(entries: new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = etag });

		var snapshot = await Inspect();

		// The object's target is unknown, so the recorded target is preserved instead of flagged.
		snapshot.Divergences.Should().BeEmpty();
		snapshot.ExpectedEntries.Should().ContainSingle().Which.Target.Should().Be("9.3.0");
		snapshot.Diagnostics.Should().ContainSingle(d => d.Contains("Could not parse bundle"));
	}

	[Fact]
	public async Task Inspect_ChangelogScope_EnumeratesEntriesWithoutTarget()
	{
		// language=yaml
		var etag = _bucket.Seed("changelog/elastic/elasticsearch/main/1-feature.yaml", """
			title: Sample
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.3.0
			""");
		// A nested pool (branch "main/foo") shares the key prefix but is a different scope.
		_ = _bucket.Seed("changelog/elastic/elasticsearch/main/foo/2-feature.yaml", "title: Nested");
		_ = _bucket.Seed("changelog/elastic/elasticsearch/main/registry.json", JsonSerializer.Serialize(new Registry
		{
			Product = "elastic/elasticsearch/main",
			GeneratedAt = FixedNow,
			Bundles = [new RegistryBundle { File = "1-feature.yaml", Target = null, ETag = etag }]
		}, RegistryJsonContext.Default.Registry));

		var snapshot = await Inspect(PoolScope());

		snapshot.IsClean.Should().BeTrue();
		snapshot.Objects.Should().ContainSingle().Which.File.Should().Be("1-feature.yaml");
		snapshot.ExpectedEntries.Should().ContainSingle().Which.Target.Should().BeNull();
	}

	[Fact]
	public async Task Inspect_NonYamlObject_IsIgnoredWithDiagnostic()
	{
		var etag = SeedBundle("9.3.0.yaml", "9.3.0");
		_ = _bucket.Seed("bundle/elasticsearch/notes.txt", "not yaml");
		SeedRegistry(entries: new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = etag });

		var snapshot = await Inspect();

		snapshot.IsClean.Should().BeTrue();
		snapshot.Diagnostics.Should().ContainSingle(d => d.Contains("notes.txt"));
	}

	[Fact]
	public async Task Inspect_Snapshot_SerializesMachineReadable()
	{
		var etag = SeedBundle("9.3.0.yaml", "9.3.0");
		SeedRegistry(entries: new RegistryBundle { File = "9.4.0.yaml", Target = "9.4.0", ETag = etag });

		var snapshot = await Inspect();
		var json = JsonSerializer.Serialize(snapshot, RegistryStateJsonContext.Default.RegistryStateSnapshot);

		json.Should().Contain("\"scope_kind\": \"Bundle\"");
		json.Should().Contain("\"registry_health\": \"Valid\"");
		json.Should().Contain("\"expected_entries\"");
		json.Should().Contain("\"is_clean\": false");
		json.Should().Contain("\"Missing\"").And.Contain("\"Stale\"");
	}

	private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
