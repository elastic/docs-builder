// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.S3;
using Amazon.SQS;
using Amazon.SQS.Model;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// The cutover/heal entry point of elastic/docs-eng-team#688 Phase 2: plans the affected groups
/// (one scope, or the union of both buckets' groups so orphan public groups are covered) and
/// sends one explicit, versioned reconcile message per group to the scrubber queue. The Lambda
/// does the actual work — this command never mutates S3 itself, which keeps the single-writer
/// invariant intact. Convergent by design: re-running re-plans against current state.
/// </summary>
public sealed class ChangelogRegistryReconcileService(
	ILoggerFactory logFactory,
	IAmazonS3? s3Client = null,
	IAmazonSQS? sqsClient = null,
	Func<string?>? confirmationReader = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogRegistryReconcileService>();

	public async Task<bool> Reconcile(IDiagnosticsCollector collector, ChangelogRegistryReconcileArguments args, Cancel ctx)
	{
		if (!args.TryResolveScopeFilter(collector, out var filter))
			return false;

		using var defaultS3 = s3Client is null ? new AmazonS3Client() : null;
		var s3 = s3Client ?? defaultS3!;

		var plan = filter is not null
			? [filter]
			: await ChangelogGroupDiscovery.DiscoverUnionAsync(s3, args.S3BucketName, args.PublicS3BucketName, ctx);

		if (plan.Count == 0)
		{
			_logger.LogInformation("No registry groups found in {Private} or {Public}; nothing to reconcile", args.S3BucketName, args.PublicS3BucketName);
			return true;
		}

		_logger.LogInformation("Reconcile plan: {Count} group(s)", plan.Count);
		foreach (var scope in plan)
			_logger.LogInformation("  {Kind,-9} {Group}", scope.Kind == ChangelogScopeKind.Bundle ? "bundle" : "changelog", scope.Group);

		if (args.DryRun)
		{
			_logger.LogInformation("[dry-run] Would send {Count} reconcile message(s) to {QueueUrl}", plan.Count, args.QueueUrl);
			return true;
		}

		if (!args.AssumeYes && !Confirm(plan.Count, collector))
			return false;

		// One correlation id per run ties every ledger line to this invocation; the cutover
		// checkpoint replays this ledger through `registry verify`.
		var correlationId = Guid.NewGuid().ToString("N");
		using var defaultSqs = sqsClient is null ? new AmazonSQSClient() : null;
		var sqs = sqsClient ?? defaultSqs!;

		var sent = 0;
		foreach (var scope in plan)
		{
			ctx.ThrowIfCancellationRequested();

			var message = ReconcileQueueMessage.For(scope, correlationId);
			var response = await sqs.SendMessageAsync(new SendMessageRequest
			{
				QueueUrl = args.QueueUrl,
				MessageBody = message.ToJson()
			}, ctx);
			sent++;
			_logger.LogInformation(
				"ledger: group={Group} scope={Scope} message-id={MessageId} correlation-id={CorrelationId}",
				scope.Group, message.Scope, response.MessageId, correlationId);
		}

		_logger.LogInformation(
			"Sent {Sent} reconcile message(s) (correlation-id {CorrelationId}). Enqueuing is not reconciling: " +
			"watch the queue drain, triage any DLQ entry, then gate on `changelog registry verify`.",
			sent, correlationId);
		return true;
	}

	private bool Confirm(int groupCount, IDiagnosticsCollector collector)
	{
		if (confirmationReader is null && Console.IsInputRedirected)
		{
			collector.EmitError(string.Empty,
				"Refusing to send reconcile messages without confirmation in a non-interactive session; re-run with --yes (or --dry-run to preview).");
			return false;
		}

		Console.Write($"Send reconcile messages for {groupCount} group(s)? Every public registry in the plan will be rewritten end-to-end. [y/N] ");
		var answer = confirmationReader is not null ? confirmationReader() : Console.ReadLine();
		if (string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
			return true;

		collector.EmitError(string.Empty, "Aborted: confirmation declined.");
		return false;
	}
}
