// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.S3;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// The read-only sibling of <c>changelog registry reconcile</c> and the standing drift-diagnosis
/// tool: for each planned group, compares the public manifest against what a reconcile of the
/// current public listing would write — same listing spec and entry rules by construction
/// (<see cref="RegistryReconciler.VerifyGroupAsync"/>) — and reports divergence. Zero divergence
/// across the plan is the cutover completion gate of elastic/docs-eng-team#688.
/// </summary>
public sealed class ChangelogRegistryVerifyService(
	ILoggerFactory logFactory,
	IAmazonS3? s3Client = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogRegistryVerifyService>();

	public async Task<bool> Verify(IDiagnosticsCollector collector, ChangelogRegistryVerifyArguments args, Cancel ctx)
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
			_logger.LogInformation("No registry groups found in {Private} or {Public}; nothing to verify", args.S3BucketName, args.PublicS3BucketName);
			return true;
		}

		var reconciler = new RegistryReconciler(logFactory, s3, args.PublicS3BucketName);
		var divergentGroups = 0;
		var findings = 0;

		foreach (var scope in plan)
		{
			ctx.ThrowIfCancellationRequested();

			var divergences = await reconciler.VerifyGroupAsync(scope, ctx);
			if (divergences.Count == 0)
			{
				_logger.LogInformation("{Scope}: converged", scope);
				continue;
			}

			divergentGroups++;
			findings += divergences.Count;
			foreach (var divergence in divergences)
				collector.EmitWarning(string.Empty, $"{scope.Prefix}{divergence.File}: [{divergence.Kind}] {divergence.Detail}");
		}

		if (divergentGroups > 0)
		{
			collector.EmitError(string.Empty,
				$"{divergentGroups} of {plan.Count} group(s) diverge from their public listing ({findings} finding(s)). " +
				"Run `changelog registry reconcile` to converge them, then verify again.");
			return false;
		}

		_logger.LogInformation("All {Count} group(s) converged: every public manifest matches its public listing", plan.Count);
		return true;
	}
}
