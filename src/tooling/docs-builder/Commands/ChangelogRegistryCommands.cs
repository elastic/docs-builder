// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Reconciliation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

namespace Documentation.Builder.Commands;

/// <summary>Operate on the scrubber-owned public changelog registries.</summary>
internal sealed class ChangelogRegistryCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector
)
{
	/// <summary>
	/// Send explicit reconcile messages to the scrubber queue so the Lambda performs a full group
	/// heal (object-level reconcile over the union of both buckets, then a registry rebuild from
	/// public state) for every planned group. This command never mutates S3 itself — the scrubber
	/// Lambda stays the public bucket's single writer. Convergent: re-running re-plans against
	/// current state. Enqueuing is not reconciling — gate on `changelog registry verify` after the
	/// queue drains and the DLQ is empty.
	/// </summary>
	/// <param name="s3BucketName">Private changelog bundles bucket to plan from.</param>
	/// <param name="publicS3BucketName">Public (CDN) changelog bundles bucket to plan from.</param>
	/// <param name="queueUrl">URL of the scrubber SQS queue to send reconcile messages to.</param>
	/// <param name="product">Only reconcile this bundle group (<c>bundle/{product}/</c>). Mutually exclusive with --owner/--repo/--branch.</param>
	/// <param name="owner">GitHub owner of a single changelog-pool group to reconcile.</param>
	/// <param name="repo">Repository of a single changelog-pool group to reconcile.</param>
	/// <param name="branch">Branch of a single changelog-pool group to reconcile (verbatim; slashes allowed).</param>
	/// <param name="dryRun">Print the group plan without sending anything.</param>
	/// <param name="yes">Skip the interactive confirmation (required when stdin is not a terminal).</param>
	[RequiresAuth]
	[CommandIntent(Intent.Destructive | Intent.Idempotent)]
	[MutationScope(MutationScope.Global)]
	[NoOptionsInjection]
	public async Task<int> Reconcile(
		string s3BucketName,
		string publicS3BucketName,
		string queueUrl,
		string? product = null,
		string? owner = null,
		string? repo = null,
		string? branch = null,
		[DryRun] bool dryRun = false,
		bool yes = false,
		Cancel ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new ChangelogRegistryReconcileService(logFactory);
		var args = new ChangelogRegistryReconcileArguments
		{
			S3BucketName = s3BucketName,
			PublicS3BucketName = publicS3BucketName,
			QueueUrl = queueUrl,
			Product = product,
			Owner = owner,
			Repo = repo,
			Branch = branch,
			DryRun = dryRun,
			AssumeYes = yes
		};
		serviceInvoker.AddCommand(service, args,
			static async (s, c, state, ct) => await s.Reconcile(c, state, ct)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>
	/// Compare each planned group's public <c>registry.json</c> against what a reconcile of the
	/// current public listing would write, and report divergence (missing, stale, corrupt,
	/// object-divergent; unsupported schema reported distinctly). Strictly read-only. Zero
	/// divergence across the plan is the cutover completion gate — and the standing
	/// drift-diagnosis tool afterwards.
	/// </summary>
	/// <param name="s3BucketName">Private changelog bundles bucket to plan from.</param>
	/// <param name="publicS3BucketName">Public (CDN) changelog bundles bucket to verify.</param>
	/// <param name="product">Only verify this bundle group (<c>bundle/{product}/</c>). Mutually exclusive with --owner/--repo/--branch.</param>
	/// <param name="owner">GitHub owner of a single changelog-pool group to verify.</param>
	/// <param name="repo">Repository of a single changelog-pool group to verify.</param>
	/// <param name="branch">Branch of a single changelog-pool group to verify (verbatim; slashes allowed).</param>
	[RequiresAuth]
	[NoOptionsInjection]
	public async Task<int> Verify(
		string s3BucketName,
		string publicS3BucketName,
		string? product = null,
		string? owner = null,
		string? repo = null,
		string? branch = null,
		Cancel ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new ChangelogRegistryVerifyService(logFactory);
		var args = new ChangelogRegistryVerifyArguments
		{
			S3BucketName = s3BucketName,
			PublicS3BucketName = publicS3BucketName,
			Product = product,
			Owner = owner,
			Repo = repo,
			Branch = branch
		};
		serviceInvoker.AddCommand(service, args,
			static async (s, c, state, ct) => await s.Verify(c, state, ct)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
