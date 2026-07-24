// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Elastic.Changelog.Uploading;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class RegistryRepairServiceTests(ITestOutputHelper output)
{
	private const string Bucket = "private-bucket";
	private const string RegistryKey = "bundle/elasticsearch/registry.json";

	private static readonly DateTimeOffset FixedNow = new(2026, 5, 6, 12, 0, 0, TimeSpan.Zero);

	private readonly FakeS3Bucket _bucket = new();
	private readonly TestDiagnosticsCollector _collector = new(output);

	private ChangelogRegistryRepairService Service =>
		new(NullLoggerFactory.Instance, _bucket.Client, new FakeTimeProvider(FixedNow));

	private static ChangelogRegistryRepairArguments Args(bool allowEmpty = false, bool dryRun = false) => new()
	{
		S3BucketName = Bucket,
		Product = "elasticsearch",
		AllowEmpty = allowEmpty,
		DryRun = dryRun
	};

	// language=yaml
	private static string BundleYaml(string target) => $"""
		products:
		  - product: elasticsearch
		    target: {target}
		    repo: elasticsearch
		    owner: elastic
		entries:
		  - file:
		      name: 1-feature.yaml
		      checksum: deadbeef
		    type: enhancement
		    title: Sample
		""";

	private string SeedBundle(string file, string target) =>
		_bucket.Seed($"bundle/elasticsearch/{file}", BundleYaml(target));

	private void SeedRegistry(params RegistryBundle[] entries) =>
		_ = _bucket.Seed(RegistryKey, JsonSerializer.Serialize(new Registry
		{
			Product = "elasticsearch",
			GeneratedAt = FixedNow,
			Bundles = entries
		}, RegistryJsonContext.Default.Registry));

	private Registry StoredRegistry() =>
		JsonSerializer.Deserialize(_bucket.ContentOf(RegistryKey), RegistryJsonContext.Default.Registry)!;

	[Fact]
	public async Task Repair_DivergedScope_ConvergesRegistryToActualObjects()
	{
		// One missing object, one stale entry, one divergent etag.
		var kept = SeedBundle("9.3.0.yaml", "9.3.0");
		_ = SeedBundle("9.4.0.yaml", "9.4.0");
		SeedRegistry(
			new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = "outdated" },
			new RegistryBundle { File = "9.2.0.yaml", Target = "9.2.0", ETag = "gone" });

		var result = await Service.Repair(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		var registry = StoredRegistry();
		registry.Bundles.Select(b => b.File).Should().Equal("9.4.0.yaml", "9.3.0.yaml");
		registry.Bundles.Single(b => b.File == "9.3.0.yaml").ETag.Should().Be(kept);
		registry.GeneratedAt.Should().Be(FixedNow);
		_bucket.Puts.Should().ContainSingle().Which.IfMatch.Should().NotBeNull();
	}

	[Fact]
	public async Task Repair_RunTwice_SecondRunWritesNothing()
	{
		_ = SeedBundle("9.3.0.yaml", "9.3.0");

		_ = await Service.Repair(_collector, Args(), TestContext.Current.CancellationToken);
		_bucket.Puts.Should().ContainSingle();

		var second = await Service.Repair(_collector, Args(), TestContext.Current.CancellationToken);

		second.Should().BeTrue();
		_bucket.Puts.Should().ContainSingle("a repaired scope must be clean; repairing again may not write");
	}

	[Fact]
	public async Task Repair_CleanScope_WritesNothing()
	{
		var etag = SeedBundle("9.3.0.yaml", "9.3.0");
		SeedRegistry(new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = etag });

		var result = await Service.Repair(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		_bucket.Puts.Should().BeEmpty();
	}

	[Fact]
	public async Task Repair_MissingRegistry_CreatesWithIfNoneMatch()
	{
		_ = SeedBundle("9.3.0.yaml", "9.3.0");

		var result = await Service.Repair(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		var put = _bucket.Puts.Should().ContainSingle().Subject;
		put.IfNoneMatch.Should().Be("*");
		put.IfMatch.Should().BeNull();
		StoredRegistry().Bundles.Should().ContainSingle().Which.Target.Should().Be("9.3.0");
	}

	[Fact]
	public async Task Repair_DryRun_WritesNothing()
	{
		_ = SeedBundle("9.3.0.yaml", "9.3.0");

		var result = await Service.Repair(_collector, Args(dryRun: true), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		_bucket.Puts.Should().BeEmpty();
		_bucket.Exists(RegistryKey).Should().BeFalse();
	}

	[Fact]
	public async Task Repair_EmptyScope_RefusesWithoutAllowEmpty()
	{
		SeedRegistry(new RegistryBundle { File = "9.2.0.yaml", Target = "9.2.0", ETag = "gone" });

		var result = await Service.Repair(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_bucket.Puts.Should().BeEmpty();
	}

	[Fact]
	public async Task Repair_EmptyScope_AllowEmptyWritesEmptyManifest()
	{
		SeedRegistry(new RegistryBundle { File = "9.2.0.yaml", Target = "9.2.0", ETag = "gone" });

		var result = await Service.Repair(_collector, Args(allowEmpty: true), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		StoredRegistry().Bundles.Should().BeEmpty();
	}

	[Fact]
	public async Task Repair_CorruptRegistry_RebuildsFromObjects()
	{
		_ = SeedBundle("9.3.0.yaml", "9.3.0");
		_ = _bucket.Seed(RegistryKey, "not json {{{");

		var result = await Service.Repair(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		// The corrupt object's live ETag guards the overwrite.
		_bucket.Puts.Should().ContainSingle().Which.IfMatch.Should().NotBeNull();
		StoredRegistry().Bundles.Should().ContainSingle().Which.File.Should().Be("9.3.0.yaml");
	}

	[Fact]
	public async Task Repair_NewerSchemaRegistry_RefusesToDowngrade()
	{
		_ = SeedBundle("9.3.0.yaml", "9.3.0");
		_ = _bucket.Seed(RegistryKey,
								 /*lang=json,strict*/
								 """{ "schema_version": 2, "product": "elasticsearch", "generated_at": "2026-05-06T12:00:00+00:00", "bundles": [] }""");

		var result = await Service.Repair(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_bucket.Puts.Should().BeEmpty();
	}

	[Fact]
	public async Task Repair_ConcurrentRegistryUpdate_ReInspectsAndKeepsConcurrentEntry()
	{
		_ = SeedBundle("9.3.0.yaml", "9.3.0");

		// Between the repair's read and its conditional PUT, a live upload publishes 9.4.0 and
		// refreshes the registry: the first PUT must fail its precondition and the retry must
		// re-list, so the concurrent object survives the repair.
		_bucket.BeforeFirstPut = () =>
		{
			var concurrentETag = SeedBundle("9.4.0.yaml", "9.4.0");
			SeedRegistry(new RegistryBundle { File = "9.4.0.yaml", Target = "9.4.0", ETag = concurrentETag });
		};

		var result = await Service.Repair(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		_bucket.Puts.Should().HaveCount(2, "the first write must lose the optimistic-concurrency race");
		StoredRegistry().Bundles.Select(b => b.File).Should().Equal("9.4.0.yaml", "9.3.0.yaml");
	}

	[Fact]
	public async Task Repair_InvalidScopeSelection_Errors()
	{
		var args = new ChangelogRegistryRepairArguments { S3BucketName = Bucket, Product = "elasticsearch", Owner = "elastic" };

		var result = await Service.Repair(_collector, args, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
	}

	private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}

}
