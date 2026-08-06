// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Scope selection shared by the registry commands: with no filter the plan covers every group
/// discovered in the union of both buckets; a bundle scope is addressed by <see cref="Product"/>,
/// a changelog-pool scope by <see cref="Owner"/>/<see cref="Repo"/>/<see cref="Branch"/>.
/// </summary>
public record ChangelogRegistryScopeArguments
{
	/// <summary>Product of a bundle scope (<c>bundle/{product}/</c>). Mutually exclusive with the owner/repo/branch form.</summary>
	public string? Product { get; init; }

	/// <summary>GitHub owner of a changelog-pool scope (<c>changelog/{org}/{repo}/{branch}/</c>).</summary>
	public string? Owner { get; init; }

	/// <summary>Repository of a changelog-pool scope.</summary>
	public string? Repo { get; init; }

	/// <summary>Branch of a changelog-pool scope (verbatim; slashes become key segments).</summary>
	public string? Branch { get; init; }

	/// <summary>The private changelog bundles bucket.</summary>
	public required string S3BucketName { get; init; }

	/// <summary>The scrubber-owned public bucket.</summary>
	public required string PublicS3BucketName { get; init; }

	/// <summary>
	/// Resolves the optional scope filter. True with a null <paramref name="scope"/> when no
	/// filter was given (plan every discovered group); false with an error when both forms are
	/// mixed or a segment fails validation.
	/// </summary>
	public bool TryResolveScopeFilter(IDiagnosticsCollector collector, out ChangelogScope? scope)
	{
		scope = null;
		var hasProduct = !string.IsNullOrWhiteSpace(Product);
		var hasPool = !string.IsNullOrWhiteSpace(Owner) || !string.IsNullOrWhiteSpace(Repo) || !string.IsNullOrWhiteSpace(Branch);

		if (!hasProduct && !hasPool)
			return true;

		if (hasProduct && hasPool)
		{
			collector.EmitError(string.Empty,
				"Specify at most one scope: --product for a bundle scope, or --owner, --repo, and --branch together for a changelog scope.");
			return false;
		}

		if (hasProduct)
		{
			if (ChangelogScope.TryCreateBundle(Product, out scope))
				return true;

			collector.EmitError(string.Empty, $"Invalid product \"{Product}\" (must match [a-zA-Z0-9_-]+).");
			return false;
		}

		if (ChangelogScope.TryCreateChangelog(Owner, Repo, Branch, out scope))
			return true;

		collector.EmitError(string.Empty,
			$"Invalid changelog scope \"{Owner ?? "<none>"}/{Repo ?? "<none>"}/{Branch ?? "<none>"}\": " +
			"--owner, --repo, and --branch are all required and each segment must be a valid key segment.");
		return false;
	}
}

/// <summary>Arguments for <see cref="ChangelogRegistryReconcileService.Reconcile"/>.</summary>
public sealed record ChangelogRegistryReconcileArguments : ChangelogRegistryScopeArguments
{
	/// <summary>URL of the scrubber SQS queue the reconcile messages are sent to.</summary>
	public required string QueueUrl { get; init; }

	/// <summary>Print the group plan without sending anything.</summary>
	public bool DryRun { get; init; }

	/// <summary>Skip the interactive confirmation (required in non-interactive contexts).</summary>
	public bool AssumeYes { get; init; }
}

/// <summary>Arguments for <see cref="ChangelogRegistryVerifyService.Verify"/>.</summary>
public sealed record ChangelogRegistryVerifyArguments : ChangelogRegistryScopeArguments;
