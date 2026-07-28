// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Elastic.Changelog.Uploading;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

public class RegistryVerifyTests
{
	private const string PublicBucket = "public-bucket";

	private readonly FakeS3 _s3 = new(PublicBucket);
	private readonly RegistryReconciler _reconciler;

	public RegistryVerifyTests() =>
		_reconciler = new RegistryReconciler(NullLoggerFactory.Instance, _s3.Client, PublicBucket, retryBaseDelay: TimeSpan.Zero);

	private static ChangelogScope Scope
	{
		get
		{
			_ = ChangelogScope.TryCreateBundle("elasticsearch", out var scope);
			return scope!;
		}
	}

	// language=yaml
	private const string BundleYaml = """
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

	private void SeedManifest(int schemaVersion = Registry.CurrentSchemaVersion, string? producer = RegistryReconciler.Producer, params RegistryBundle[] bundles)
	{
		var manifest = new Registry
		{
			SchemaVersion = schemaVersion,
			Product = "elasticsearch",
			Producer = producer,
			GeneratedAt = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
			Bundles = bundles
		};
		_ = _s3.Seed(PublicBucket, Scope.RegistryKey, JsonSerializer.Serialize(manifest, RegistryJsonContext.Default.Registry));
	}

	private Cancel Ctx => TestContext.Current.CancellationToken;

	[Fact]
	public async Task Verify_ConvergedGroup_ReportsNothingAndWritesNothing()
	{
		var etag = _s3.Seed(PublicBucket, Scope.Prefix + "es-9.1.0.yaml", BundleYaml);
		SeedManifest(bundles: new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag });

		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		divergences.Should().BeEmpty();
		_s3.Puts.Should().BeEmpty();
		_s3.Deletes.Should().BeEmpty();
	}

	[Fact]
	public async Task Verify_ObjectsWithoutManifest_ReportsMissingManifest()
	{
		_ = _s3.Seed(PublicBucket, Scope.Prefix + "es-9.1.0.yaml", BundleYaml);

		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		var finding = divergences.Should().ContainSingle().Subject;
		finding.Kind.Should().Be(RegistryDivergenceKind.Missing);
		finding.File.Should().Be("registry.json");
	}

	[Fact]
	public async Task Verify_ManifestForEmptyGroup_ReportsStaleManifest()
	{
		SeedManifest();

		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		var finding = divergences.Should().ContainSingle().Subject;
		finding.Kind.Should().Be(RegistryDivergenceKind.Stale);
		finding.File.Should().Be("registry.json");
	}

	[Fact]
	public async Task Verify_UnlistedObject_ReportsMissingEntry()
	{
		var etag = _s3.Seed(PublicBucket, Scope.Prefix + "es-9.1.0.yaml", BundleYaml);
		_ = _s3.Seed(PublicBucket, Scope.Prefix + "es-9.2.0.yaml", BundleYaml);
		SeedManifest(bundles: new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag });

		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		var finding = divergences.Should().ContainSingle().Subject;
		finding.Kind.Should().Be(RegistryDivergenceKind.Missing);
		finding.File.Should().Be("es-9.2.0.yaml");
	}

	[Fact]
	public async Task Verify_EntryWhoseObjectIsGone_ReportsStaleEntry()
	{
		var etag = _s3.Seed(PublicBucket, Scope.Prefix + "es-9.1.0.yaml", BundleYaml);
		SeedManifest(bundles:
		[
			new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag },
			new RegistryBundle { File = "gone.yaml", Target = "9.0.0", ETag = "dead" }
		]);

		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		var finding = divergences.Should().ContainSingle().Subject;
		finding.Kind.Should().Be(RegistryDivergenceKind.Stale);
		finding.File.Should().Be("gone.yaml");
	}

	[Fact]
	public async Task Verify_EntryWithWrongETag_ReportsObjectDivergent()
	{
		_ = _s3.Seed(PublicBucket, Scope.Prefix + "es-9.1.0.yaml", BundleYaml);
		SeedManifest(bundles: new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = "outdated" });

		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		var finding = divergences.Should().ContainSingle().Subject;
		finding.Kind.Should().Be(RegistryDivergenceKind.ObjectDivergent);
		finding.File.Should().Be("es-9.1.0.yaml");
	}

	[Fact]
	public async Task Verify_CorruptManifest_ReportsCorrupt()
	{
		_ = _s3.Seed(PublicBucket, Scope.Prefix + "es-9.1.0.yaml", BundleYaml);
		_ = _s3.Seed(PublicBucket, Scope.RegistryKey, "{ not json ");

		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		divergences.Should().ContainSingle().Subject.Kind.Should().Be(RegistryDivergenceKind.Corrupt);
	}

	[Fact]
	public async Task Verify_NewerSchema_ReportsUnsupportedSchemaDistinctly()
	{
		_ = _s3.Seed(PublicBucket, Scope.Prefix + "es-9.1.0.yaml", BundleYaml);
		SeedManifest(schemaVersion: Registry.CurrentSchemaVersion + 1);

		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		divergences.Should().ContainSingle().Subject.Kind.Should().Be(RegistryDivergenceKind.UnsupportedSchema);
	}

	[Fact]
	public async Task Verify_LegacyProducerManifest_ReportsStaleMetadataEvenWhenEntriesMatch()
	{
		var etag = _s3.Seed(PublicBucket, Scope.Prefix + "es-9.1.0.yaml", BundleYaml);
		SeedManifest(producer: null, bundles: new RegistryBundle { File = "es-9.1.0.yaml", Target = "9.1.0", ETag = etag });

		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		var finding = divergences.Should().ContainSingle().Subject;
		finding.Kind.Should().Be(RegistryDivergenceKind.Stale);
		finding.File.Should().Be("registry.json");
	}

	[Fact]
	public async Task Verify_EmptyGroupWithoutManifest_ReportsNothing()
	{
		var divergences = await _reconciler.VerifyGroupAsync(Scope, Ctx);

		divergences.Should().BeEmpty();
	}
}
