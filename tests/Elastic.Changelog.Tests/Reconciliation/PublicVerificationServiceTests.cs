// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Amazon.S3.Model;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Elastic.Changelog.Uploading;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class PublicVerificationServiceTests(ITestOutputHelper output)
{
	private const string PrivateBucket = "private-bucket";
	private const string PublicBucket = "public-bucket";
	private const string RegistryKey = "bundle/elasticsearch/registry.json";
	private const string BundleKey = "bundle/elasticsearch/9.3.0.yaml";

	private static readonly DateTimeOffset FixedNow = new(2026, 5, 6, 12, 0, 0, TimeSpan.Zero);

	private readonly FakeS3Bucket _private = new();
	private readonly FakeS3Bucket _public = new();
	private readonly TestDiagnosticsCollector _collector = new(output);

	private ChangelogPublicVerificationService Service =>
		new(NullLoggerFactory.Instance, _private.Client, _public.Client);

	private static ChangelogPublicVerifyArguments Args(int maxAttempts = 1) => new()
	{
		S3BucketName = PrivateBucket,
		PublicS3BucketName = PublicBucket,
		Product = "elasticsearch",
		MaxAttempts = maxAttempts,
		// Zero interval keeps the bounded-wait tests deterministic and instant.
		PollInterval = TimeSpan.Zero
	};

	// language=yaml
	private const string BundleYaml = """
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    repo: elasticsearch
		    owner: elastic
		entries: []
		""";

	private static string RegistryJson(params RegistryBundle[] entries) =>
		JsonSerializer.Serialize(new Registry
		{
			Product = "elasticsearch",
			GeneratedAt = FixedNow,
			Bundles = entries
		}, RegistryJsonContext.Default.Registry);

	private void SeedConvergedState()
	{
		var etag = _private.Seed(BundleKey, BundleYaml);
		var registry = RegistryJson(new RegistryBundle { File = "9.3.0.yaml", Target = "9.3.0", ETag = etag });
		_ = _private.Seed(RegistryKey, registry);
		// The scrubber copied the (unchanged) bundle and passed the registry through verbatim.
		_ = _public.Seed(BundleKey, BundleYaml);
		_ = _public.Seed(RegistryKey, registry);
	}

	private void AssertNoWrites(FakeS3Bucket bucket)
	{
		bucket.Puts.Should().BeEmpty();
		bucket.Copies.Should().BeEmpty();
		A.CallTo(() => bucket.Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._)).MustNotHaveHappened();
		A.CallTo(() => bucket.Client.CopyObjectAsync(A<CopyObjectRequest>._, A<CancellationToken>._)).MustNotHaveHappened();
		A.CallTo(() => bucket.Client.DeleteObjectAsync(A<DeleteObjectRequest>._, A<CancellationToken>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task Verify_ConvergedState_SucceedsWithoutWriting()
	{
		SeedConvergedState();

		var result = await Service.Verify(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		AssertNoWrites(_public);
		AssertNoWrites(_private);
	}

	[Fact]
	public async Task Verify_MissingPublicObject_ReportsWithoutWriting()
	{
		SeedConvergedState();
		_public.Remove(BundleKey);

		var result = await Service.Verify(_collector, Args(maxAttempts: 2), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("MissingPublicObject"));
		AssertNoWrites(_public);
		AssertNoWrites(_private);
	}

	[Fact]
	public async Task Verify_MissingPublicRegistry_Reports()
	{
		SeedConvergedState();
		_public.Remove(RegistryKey);

		var result = await Service.Verify(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("MissingPublicRegistry"));
	}

	[Fact]
	public async Task Verify_PublicRegistryDiffersFromPrivate_ReportsMismatch()
	{
		SeedConvergedState();
		_ = _public.Seed(RegistryKey, RegistryJson(new RegistryBundle { File = "9.2.0.yaml", Target = "9.2.0", ETag = "old" }));

		var result = await Service.Verify(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("RegistryMismatch"));
	}

	[Fact]
	public async Task Verify_StalePublicObject_Reports()
	{
		SeedConvergedState();
		_ = _public.Seed("bundle/elasticsearch/9.1.0.yaml", BundleYaml);

		var result = await Service.Verify(_collector, Args(), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("StalePublicObject"));
	}

	[Fact]
	public async Task Verify_PersistentDivergence_StopsAtMaxAttempts()
	{
		SeedConvergedState();
		_public.Remove(BundleKey);

		var result = await Service.Verify(_collector, Args(maxAttempts: 3), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		// One public listing per comparison attempt: the bounded policy must stop exactly at the limit.
		_public.ListCalls.Should().Be(3);
		AssertNoWrites(_public);
	}

	[Fact]
	public async Task Verify_ScrubberCatchesUpMidPoll_Converges()
	{
		SeedConvergedState();
		_public.Remove(BundleKey);
		// The scrubber "delivers" the object after the first divergent comparison.
		_public.OnList = call =>
		{
			if (call == 2)
				_ = _public.Seed(BundleKey, BundleYaml);
		};

		var result = await Service.Verify(_collector, Args(maxAttempts: 5), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		_public.ListCalls.Should().Be(2, "the wait loop must stop as soon as the state converges");
		AssertNoWrites(_public);
	}

	[Fact]
	public async Task Verify_InvalidMaxAttempts_Errors()
	{
		SeedConvergedState();

		var result = await Service.Verify(_collector, Args(maxAttempts: 0), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
	}

}
