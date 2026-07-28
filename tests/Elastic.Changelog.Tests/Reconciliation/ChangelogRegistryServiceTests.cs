// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.SQS;
using Amazon.SQS.Model;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Reconciliation;

public class ChangelogRegistryServiceTests
{
	private const string PrivateBucket = "private-bucket";
	private const string PublicBucket = "public-bucket";

	private readonly FakeS3 _s3 = new(PrivateBucket, PublicBucket);
	private readonly IAmazonSQS _sqs = A.Fake<IAmazonSQS>();
	private readonly List<SendMessageRequest> _sent = [];
	private readonly TestDiagnosticsCollector _collector;

	public ChangelogRegistryServiceTests(ITestOutputHelper output)
	{
		_collector = new TestDiagnosticsCollector(output);
		_ = A.CallTo(() => _sqs.SendMessageAsync(A<SendMessageRequest>._, A<CancellationToken>._))
			.Invokes((SendMessageRequest r, CancellationToken _) => _sent.Add(r))
			.ReturnsLazily((SendMessageRequest _, CancellationToken _) =>
				Task.FromResult(new SendMessageResponse { MessageId = $"sqs-{_sent.Count}" }));
	}

	private ChangelogRegistryReconcileService ReconcileService(Func<string?>? confirm = null) =>
		new(NullLoggerFactory.Instance, _s3.Client, _sqs, confirm ?? (() => "y"));

	private static ChangelogRegistryReconcileArguments ReconcileArgs(
		bool dryRun = false, bool yes = false, string? product = null) => new()
		{
			S3BucketName = PrivateBucket,
			PublicS3BucketName = PublicBucket,
			QueueUrl = "https://sqs.example/queue",
			DryRun = dryRun,
			AssumeYes = yes,
			Product = product
		};

	private Cancel Ctx => TestContext.Current.CancellationToken;

	[Fact]
	public async Task Reconcile_DiscoversTheUnionOfBothBuckets_IncludingOrphanPublicGroups()
	{
		// Private: one bundle group and one pool. Public: an orphan group (nothing private backs
		// it) that only a union discovery would plan — its reconcile is what deletes the leftovers.
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "a");
		_ = _s3.Seed(PrivateBucket, "changelog/elastic/repo/main/entry.yaml", "b");
		_ = _s3.Seed(PublicBucket, "bundle/orphaned/old.yaml", "c");
		_ = _s3.Seed(PublicBucket, "bundle/manifest-only/registry.json", "{}");

		var ok = await ReconcileService().Reconcile(_collector, ReconcileArgs(yes: true), Ctx);

		ok.Should().BeTrue();
		var bodies = _sent.Select(r => r.MessageBody).ToList();
		bodies.Should().HaveCount(4);
		bodies.Should().Contain(b => b.Contains("\"scope\":\"bundle\"", StringComparison.Ordinal) && b.Contains("\"group\":\"elasticsearch\"", StringComparison.Ordinal));
		bodies.Should().Contain(b => b.Contains("\"scope\":\"changelog\"", StringComparison.Ordinal) && b.Contains("\"group\":\"elastic/repo/main\"", StringComparison.Ordinal));
		bodies.Should().Contain(b => b.Contains("\"group\":\"orphaned\"", StringComparison.Ordinal));
		bodies.Should().Contain(b => b.Contains("\"group\":\"manifest-only\"", StringComparison.Ordinal),
			"a group holding only an orphaned manifest still needs its reconcile");
		bodies.Should().OnlyContain(b => b.Contains("\"kind\":\"reconcile\"", StringComparison.Ordinal) && b.Contains("\"version\":1", StringComparison.Ordinal));
	}

	[Fact]
	public async Task Reconcile_EveryMessageValidatesAndSharesOneCorrelationId()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "a");
		_ = _s3.Seed(PrivateBucket, "bundle/kibana/kb-9.1.0.yaml", "b");

		var ok = await ReconcileService().Reconcile(_collector, ReconcileArgs(yes: true), Ctx);

		ok.Should().BeTrue();
		var correlationIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var request in _sent)
		{
			_ = ReconcileQueueMessage.TryRead(request.MessageBody, out var message);
			message.Should().NotBeNull();
			message!.TryResolveScope(out _, out _).Should().BeTrue("the CLI must only ever send messages the Lambda accepts");
			_ = correlationIds.Add(message.CorrelationId!);
		}
		correlationIds.Should().ContainSingle("one run stamps one correlation id on its whole ledger");
	}

	[Fact]
	public async Task Reconcile_DryRun_SendsNothing()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "a");

		var ok = await ReconcileService().Reconcile(_collector, ReconcileArgs(dryRun: true), Ctx);

		ok.Should().BeTrue();
		_sent.Should().BeEmpty();
	}

	[Fact]
	public async Task Reconcile_DeclinedConfirmation_Aborts()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "a");

		var ok = await ReconcileService(confirm: () => "n").Reconcile(_collector, ReconcileArgs(), Ctx);

		ok.Should().BeFalse();
		_sent.Should().BeEmpty();
	}

	[Fact]
	public async Task Reconcile_SingleScopeFilter_PlansOnlyThatGroup()
	{
		_ = _s3.Seed(PrivateBucket, "bundle/elasticsearch/es-9.1.0.yaml", "a");
		_ = _s3.Seed(PrivateBucket, "bundle/kibana/kb-9.1.0.yaml", "b");

		var ok = await ReconcileService().Reconcile(_collector, ReconcileArgs(yes: true, product: "kibana"), Ctx);

		ok.Should().BeTrue();
		_sent.Should().ContainSingle().Which.MessageBody.Should().Contain("\"group\":\"kibana\"");
	}

	[Fact]
	public async Task Reconcile_MixedScopeForms_IsRejected()
	{
		var args = ReconcileArgs(yes: true, product: "kibana") with { Owner = "elastic", Repo = "repo", Branch = "main" };

		var ok = await ReconcileService().Reconcile(_collector, args, Ctx);

		ok.Should().BeFalse();
		_sent.Should().BeEmpty();
	}

	[Fact]
	public async Task Verify_ReportsDivergenceAcrossThePlanAndFails()
	{
		// elasticsearch is converged; kibana has objects but no manifest.
		var yaml = "products:\n  - product: elasticsearch\n    target: 9.1.0\n    repo: elasticsearch\n    owner: elastic\n";
		var etag = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", yaml);
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/registry.json",
			"{\"schema_version\":1,\"product\":\"elasticsearch\",\"producer\":\"" + RegistryReconciler.Producer +
			"\",\"generated_at\":\"2026-07-01T00:00:00+00:00\",\"bundles\":[{\"file\":\"es-9.1.0.yaml\",\"target\":\"9.1.0\",\"etag\":\"" + etag + "\"}]}");
		_ = _s3.Seed(PublicBucket, "bundle/kibana/kb-9.1.0.yaml", "kb");

		var service = new ChangelogRegistryVerifyService(NullLoggerFactory.Instance, _s3.Client);
		var ok = await service.Verify(_collector, new ChangelogRegistryVerifyArguments
		{
			S3BucketName = PrivateBucket,
			PublicS3BucketName = PublicBucket
		}, Ctx);

		ok.Should().BeFalse("kibana diverges");
		_s3.Puts.Should().BeEmpty("verify is strictly read-only");
		_s3.Deletes.Should().BeEmpty();
	}

	[Fact]
	public async Task Verify_ConvergedPlan_Succeeds()
	{
		var yaml = "products:\n  - product: elasticsearch\n    target: 9.1.0\n    repo: elasticsearch\n    owner: elastic\n";
		var etag = _s3.Seed(PublicBucket, "bundle/elasticsearch/es-9.1.0.yaml", yaml);
		_ = _s3.Seed(PublicBucket, "bundle/elasticsearch/registry.json",
			"{\"schema_version\":1,\"product\":\"elasticsearch\",\"producer\":\"" + RegistryReconciler.Producer +
			"\",\"generated_at\":\"2026-07-01T00:00:00+00:00\",\"bundles\":[{\"file\":\"es-9.1.0.yaml\",\"target\":\"9.1.0\",\"etag\":\"" + etag + "\"}]}");

		var service = new ChangelogRegistryVerifyService(NullLoggerFactory.Instance, _s3.Client);
		var ok = await service.Verify(_collector, new ChangelogRegistryVerifyArguments
		{
			S3BucketName = PrivateBucket,
			PublicS3BucketName = PublicBucket
		}, Ctx);

		ok.Should().BeTrue();
	}
}
