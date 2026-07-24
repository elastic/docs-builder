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

/// <summary>Inspect, repair, and verify per-scope changelog <c>registry.json</c> manifests against actual bucket state.</summary>
internal sealed class ChangelogRegistryCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector
)
{
	/// <summary>Compare a scope's private registry.json against the actual private-bucket objects and report every divergence.</summary>
	/// <remarks>
	/// <para>Registries merge additively and are not authoritative for removals or discovery, so they can drift
	/// from the objects actually in the bucket. This command detects and classifies that drift: <c>missing</c>
	/// (object exists, registry lacks it), <c>stale</c> (registry entry, object gone), <c>corrupt</c>
	/// (unparseable/invalid manifest), and <c>object-divergent</c> (registry metadata disagrees with the object).</para>
	/// <para>Strictly read-only — nothing is written to any bucket. Exits non-zero when the scope diverged.
	/// Use <c>changelog registry repair</c> to reconcile.</para>
	/// </remarks>
	/// <param name="s3BucketName">Private changelog bundles S3 bucket to inspect.</param>
	/// <param name="product">Product of a bundle scope (bundle/{product}/). Mutually exclusive with --owner/--repo/--branch.</param>
	/// <param name="owner">GitHub owner of a changelog scope (changelog/{org}/{repo}/{branch}/). Requires --repo and --branch.</param>
	/// <param name="repo">Repository of a changelog scope. Requires --owner and --branch.</param>
	/// <param name="branch">Branch of a changelog scope, stored verbatim (slashes become key segments). Requires --owner and --repo.</param>
	/// <param name="out">Path to write the machine-readable state snapshot JSON to.</param>
	/// <param name="ct">Cancellation token.</param>
	[RequiresAuth]
	[NoOptionsInjection]
	public async Task<int> Inspect(
		string s3BucketName,
		string? product = null,
		string? owner = null,
		string? repo = null,
		string? branch = null,
		[ExpandUserProfile, RejectSymbolicLinks] FileInfo? @out = null,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new ChangelogRegistryInspectionService(logFactory);
		var args = new ChangelogRegistryInspectArguments
		{
			S3BucketName = s3BucketName,
			Product = product,
			Owner = owner,
			Repo = repo,
			Branch = branch,
			Out = @out?.FullName
		};
		serviceInvoker.AddCommand(service, args,
			static async (s, c, state, ctx) => await s.Inspect(c, state, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Reconcile a scope's private registry.json from the actual private-bucket objects.</summary>
	/// <remarks>
	/// <para>Rebuilds the manifest from the objects that actually exist in the scope: missing entries are added,
	/// stale entries removed, and divergent metadata corrected. The write uses the same optimistic-concurrency
	/// conditional PUT as the live upload path (If-Match on update, If-None-Match: * on create), re-inspecting
	/// and retrying when a concurrent upload refreshes the manifest, so repair is safe to run alongside live
	/// uploads. Repair is idempotent: a clean scope writes nothing, and running it twice yields no further change.</para>
	/// <para>Only the <b>private</b> registry is written. The public copy is scrubber-owned and converges through
	/// the scrubber's pass-through of this write's own event. Every change is logged (before/after) for audit.</para>
	/// </remarks>
	/// <param name="s3BucketName">Private changelog bundles S3 bucket to repair the registry in.</param>
	/// <param name="product">Product of a bundle scope (bundle/{product}/). Mutually exclusive with --owner/--repo/--branch.</param>
	/// <param name="owner">GitHub owner of a changelog scope (changelog/{org}/{repo}/{branch}/). Requires --repo and --branch.</param>
	/// <param name="repo">Repository of a changelog scope. Requires --owner and --branch.</param>
	/// <param name="branch">Branch of a changelog scope, stored verbatim (slashes become key segments). Requires --owner and --repo.</param>
	/// <param name="allowEmpty">Allow writing a manifest with zero entries when the scope holds no objects. Without this flag an empty result aborts the repair.</param>
	/// <param name="dryRun">Report what would change without writing.</param>
	/// <param name="ct">Cancellation token.</param>
	[RequiresAuth]
	[CommandIntent(Intent.Idempotent)]
	[MutationScope(MutationScope.Global)]
	[NoOptionsInjection]
	public async Task<int> Repair(
		string s3BucketName,
		string? product = null,
		string? owner = null,
		string? repo = null,
		string? branch = null,
		bool allowEmpty = false,
		[DryRun] bool dryRun = false,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new ChangelogRegistryRepairService(logFactory);
		var args = new ChangelogRegistryRepairArguments
		{
			S3BucketName = s3BucketName,
			Product = product,
			Owner = owner,
			Repo = repo,
			Branch = branch,
			AllowEmpty = allowEmpty,
			DryRun = dryRun
		};
		serviceInvoker.AddCommand(service, args,
			static async (s, c, state, ctx) => await s.Repair(c, state, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Verify that the scrubber-owned public bucket converged to the state expected from the private bucket.</summary>
	/// <remarks>
	/// <para>Compares the public registry.json (a verbatim pass-through copy of the private one) and the public
	/// objects against the private scope, waiting under a bounded retry policy (--max-attempts x --poll-interval-seconds)
	/// because registry and YAML scrub events propagate independently and transient divergence is normal.</para>
	/// <para>Strictly read-only: the public bucket is never written — it is checked through a reader that has no
	/// write operations. If divergence persists (a lost or dead-lettered scrub event), recover with the explicit
	/// <c>changelog registry republish</c> operation on the private side.</para>
	/// </remarks>
	/// <param name="s3BucketName">Private changelog bundles S3 bucket the expected state derives from.</param>
	/// <param name="publicS3BucketName">Public (scrubbed) changelog bundles S3 bucket to check. Only ever read.</param>
	/// <param name="product">Product of a bundle scope (bundle/{product}/). Mutually exclusive with --owner/--repo/--branch.</param>
	/// <param name="owner">GitHub owner of a changelog scope (changelog/{org}/{repo}/{branch}/). Requires --repo and --branch.</param>
	/// <param name="repo">Repository of a changelog scope. Requires --owner and --branch.</param>
	/// <param name="branch">Branch of a changelog scope, stored verbatim (slashes become key segments). Requires --owner and --repo.</param>
	/// <param name="maxAttempts">Maximum comparison attempts before reporting divergence. Defaults to 12.</param>
	/// <param name="pollIntervalSeconds">Seconds to wait between attempts. Defaults to 10 (a two-minute budget with the default attempts).</param>
	/// <param name="ct">Cancellation token.</param>
	[RequiresAuth]
	[NoOptionsInjection]
	public async Task<int> VerifyPublic(
		string s3BucketName,
		string publicS3BucketName,
		string? product = null,
		string? owner = null,
		string? repo = null,
		string? branch = null,
		int maxAttempts = 12,
		int pollIntervalSeconds = 10,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new ChangelogPublicVerificationService(logFactory);
		var args = new ChangelogPublicVerifyArguments
		{
			S3BucketName = s3BucketName,
			PublicS3BucketName = publicS3BucketName,
			Product = product,
			Owner = owner,
			Repo = repo,
			Branch = branch,
			MaxAttempts = maxAttempts,
			PollInterval = TimeSpan.FromSeconds(pollIntervalSeconds)
		};
		serviceInvoker.AddCommand(service, args,
			static async (s, c, state, ctx) => await s.Verify(c, state, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Re-emit the private-bucket ObjectCreated event for scope objects so the scrubber re-processes them.</summary>
	/// <remarks>
	/// <para>The recovery path for a lost or dead-lettered scrub event: performs a metadata-preserving S3 self-copy
	/// of each selected private object (content and ETag unchanged), which produces the ObjectCreated notification
	/// the scrubber Lambda reacts to. The scrubber then re-scrubs and re-publishes the object to the public bucket
	/// itself — this command never writes to the public bucket.</para>
	/// <para>Republishing only happens through this explicit command; nothing triggers it implicitly.</para>
	/// </remarks>
	/// <param name="s3BucketName">Private changelog bundles S3 bucket holding the objects to republish.</param>
	/// <param name="product">Product of a bundle scope (bundle/{product}/). Mutually exclusive with --owner/--repo/--branch.</param>
	/// <param name="owner">GitHub owner of a changelog scope (changelog/{org}/{repo}/{branch}/). Requires --repo and --branch.</param>
	/// <param name="repo">Repository of a changelog scope. Requires --owner and --branch.</param>
	/// <param name="branch">Branch of a changelog scope, stored verbatim (slashes become key segments). Requires --owner and --repo.</param>
	/// <param name="files">File name(s) in the scope to republish (comma-separated or repeated). Mutually exclusive with --all.</param>
	/// <param name="all">Republish every object in the scope, including its registry.json.</param>
	/// <param name="ct">Cancellation token.</param>
	[RequiresAuth]
	[CommandIntent(Intent.Idempotent)]
	[MutationScope(MutationScope.Global)]
	[NoOptionsInjection]
	public async Task<int> Republish(
		string s3BucketName,
		string? product = null,
		string? owner = null,
		string? repo = null,
		string? branch = null,
		string[]? files = null,
		bool all = false,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new ChangelogRegistryRepublishService(logFactory);
		var args = new ChangelogRegistryRepublishArguments
		{
			S3BucketName = s3BucketName,
			Product = product,
			Owner = owner,
			Repo = repo,
			Branch = branch,
			Files = ExpandCommaSeparated(files),
			All = all
		};
		serviceInvoker.AddCommand(service, args,
			static async (s, c, state, ctx) => await s.Republish(c, state, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	private static List<string> ExpandCommaSeparated(string[]? values)
	{
		if (values is not { Length: > 0 })
			return [];

		var result = new List<string>();
		foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)))
		{
			if (value.Contains(','))
				result.AddRange(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
			else
				result.Add(value);
		}
		return result;
	}
}
