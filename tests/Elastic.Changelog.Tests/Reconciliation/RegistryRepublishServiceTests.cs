// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class RegistryRepublishServiceTests(ITestOutputHelper output)
{
	private const string Bucket = "private-bucket";

	private readonly FakeS3Bucket _bucket = new();
	private readonly TestDiagnosticsCollector _collector = new(output);

	private ChangelogRegistryRepublishService Service => new(NullLoggerFactory.Instance, _bucket.Client);

	private static ChangelogRegistryRepublishArguments Args(IReadOnlyList<string>? files = null, bool all = false) => new()
	{
		S3BucketName = Bucket,
		Product = "elasticsearch",
		Files = files ?? [],
		All = all
	};

	[Fact]
	public async Task Republish_ExplicitFiles_SelfCopiesOnlyThoseKeys()
	{
		_ = _bucket.Seed("bundle/elasticsearch/9.3.0.yaml", "entries: []");
		_ = _bucket.Seed("bundle/elasticsearch/9.4.0.yaml", "entries: []");

		var result = await Service.Republish(_collector, Args(files: ["9.3.0.yaml"]), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		var copy = _bucket.Copies.Should().ContainSingle().Subject;
		copy.SourceBucket.Should().Be(Bucket);
		copy.DestinationBucket.Should().Be(Bucket, "republish only ever touches the private bucket");
		copy.SourceKey.Should().Be("bundle/elasticsearch/9.3.0.yaml");
		copy.DestinationKey.Should().Be(copy.SourceKey, "the re-emission is a self-copy");
		copy.MetadataDirective.Should().Be(S3MetadataDirective.REPLACE);
		copy.ContentType.Should().Be("application/yaml", "the rewrite must preserve the original content type");
		copy.Metadata["x-amz-meta-origin"].Should().Be("test", "the rewrite must preserve user metadata");
	}

	[Fact]
	public async Task Republish_SelfCopy_LeavesContentUntouched()
	{
		_ = _bucket.Seed("bundle/elasticsearch/9.3.0.yaml", "entries: []");

		_ = await Service.Republish(_collector, Args(files: ["9.3.0.yaml"]), TestContext.Current.CancellationToken);

		_bucket.ContentOf("bundle/elasticsearch/9.3.0.yaml").Should().Be("entries: []");
		_bucket.Puts.Should().BeEmpty("republish rewrites via CopyObject, never PutObject");
	}

	[Fact]
	public async Task Republish_All_IncludesEveryScopeObjectAndTheManifest()
	{
		_ = _bucket.Seed("bundle/elasticsearch/9.3.0.yaml", "entries: []");
		_ = _bucket.Seed("bundle/elasticsearch/registry.json", "{}");
		_ = _bucket.Seed("bundle/kibana/9.3.0.yaml", "entries: []");

		var result = await Service.Republish(_collector, Args(all: true), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		_bucket.Copies.Select(c => c.SourceKey).Should().BeEquivalentTo(
			"bundle/elasticsearch/9.3.0.yaml",
			"bundle/elasticsearch/registry.json");
	}

	[Fact]
	public async Task Republish_MissingObject_Errors()
	{
		var result = await Service.Republish(_collector, Args(files: ["9.9.9.yaml"]), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_bucket.Copies.Should().BeEmpty();
	}

	[Fact]
	public async Task Republish_NoSelection_Errors()
	{
		var result = await Service.Republish(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_bucket.Copies.Should().BeEmpty();
	}

	[Fact]
	public async Task Republish_BothSelections_Errors()
	{
		var result = await Service.Republish(_collector, Args(files: ["9.3.0.yaml"], all: true), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task Republish_UnsafeFileName_Rejected()
	{
		var result = await Service.Republish(_collector, Args(files: ["../evil.yaml"]), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		A.CallTo(() => _bucket.Client.CopyObjectAsync(A<CopyObjectRequest>._, A<CancellationToken>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task Republish_EmptyScopeWithAll_Succeeds()
	{
		var result = await Service.Republish(_collector, Args(all: true), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		_bucket.Copies.Should().BeEmpty();
	}

	[Fact]
	public async Task Republish_PartialFailure_ReportsAndContinues()
	{
		_ = _bucket.Seed("bundle/elasticsearch/9.3.0.yaml", "entries: []");

		var result = await Service.Republish(_collector,
			Args(files: ["9.3.0.yaml", "missing.yaml"]), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_bucket.Copies.Should().ContainSingle("the existing object must still be republished");
		_collector.Errors.Should().BeGreaterThan(0);
	}

}
