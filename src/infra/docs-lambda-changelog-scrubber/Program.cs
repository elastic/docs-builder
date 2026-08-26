// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Reconciliation;
using Elastic.Changelog.Scrubbing;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Lambda.ChangelogScrubber;

var publicBucketName = Environment.GetEnvironmentVariable("PUBLIC_BUCKET_NAME")
	?? throw new InvalidOperationException("PUBLIC_BUCKET_NAME environment variable is required");

var allowRepos = BuildAllowlist();

await LambdaBootstrapBuilder
	.Create<SQSEvent, SQSBatchResponse>(Handler, new SourceGeneratorLambdaJsonSerializer<SerializerContext>())
	.Build()
	.RunAsync();

return;

IReadOnlyList<string> BuildAllowlist()
{
	using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("assembler.yml")
		?? throw new InvalidOperationException("Embedded assembler.yml not found");
	using var reader = new StreamReader(stream);
	var yaml = reader.ReadToEnd();
	var assembly = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
	return LinkAllowlistSanitizer.BuildAllowReposFromAssembler(assembly);
}

// Thin adapter over the testable processor in Elastic.Changelog: translate the SQS event in,
// run the state-driven reconcile, translate the failed message ids back out, emit metrics.
async Task<SQSBatchResponse> Handler(SQSEvent ev, ILambdaContext context)
{
	var region = Amazon.RegionEndpoint.GetBySystemName(
		Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1");
	var credentials = new Amazon.Runtime.EnvironmentVariablesAWSCredentials();

	using var s3Client = new AmazonS3Client(credentials, new AmazonS3Config
	{
		RegionEndpoint = region,
		Timeout = TimeSpan.FromSeconds(10),
		MaxErrorRetry = 2
	});

	using var logFactory = new LambdaLoggerFactory(context.Logger);
	var metrics = new ReconcileMetrics();
	var scrubber = new ChangelogContentScrubber(logFactory, allowRepos);
	var reconciler = new BundleRegistryReconciler(logFactory, s3Client, publicBucketName, metrics: metrics);
	var shallowReconciler = new ShallowRegistryReconciler(logFactory, s3Client, publicBucketName, metrics: metrics);
	var notesReconciler = new NotesIndexReconciler(logFactory, s3Client, publicBucketName, metrics: metrics);
	var processor = new ScrubberProcessor(logFactory, s3Client, publicBucketName, scrubber, reconciler, shallowReconciler, notesReconciler, metrics);

	var messages = ev.Records.Select(r => new ScrubberQueueMessage(r.MessageId, r.Body)).ToList();
	var failedIds = await processor.ProcessAsync(messages, CancellationToken.None);

	EmfMetricsEmitter.Emit(metrics);

	var response = new SQSBatchResponse(
		[.. failedIds.Select(id => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = id })]);
	if (failedIds.Count > 0)
		context.Logger.LogInformation("Failed {FailedCount} of {TotalCount} messages", failedIds.Count, ev.Records.Count);
	return response;
}
