// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Scope selection shared by every registry reconciliation operation: a bundle scope is addressed
/// by <see cref="Product"/>, a changelog-pool scope by <see cref="Owner"/>/<see cref="Repo"/>/<see cref="Branch"/>.
/// Exactly one of the two forms must be provided.
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

	/// <summary>The private changelog bundles bucket the scope lives in.</summary>
	public required string S3BucketName { get; init; }

	/// <summary>
	/// Resolves the scope from the argument form used, emitting an error when neither or both
	/// forms are given or when a segment fails validation.
	/// </summary>
	public bool TryResolveScope(IDiagnosticsCollector collector, [NotNullWhen(true)] out ChangelogScope? scope)
	{
		scope = null;
		var hasProduct = !string.IsNullOrWhiteSpace(Product);
		var hasPool = !string.IsNullOrWhiteSpace(Owner) || !string.IsNullOrWhiteSpace(Repo) || !string.IsNullOrWhiteSpace(Branch);

		if (hasProduct == hasPool)
		{
			collector.EmitError(string.Empty,
				"Specify exactly one scope: --product for a bundle scope, or --owner, --repo, and --branch together for a changelog scope.");
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

/// <summary>Arguments for <see cref="ChangelogRegistryInspectionService.Inspect"/>.</summary>
public sealed record ChangelogRegistryInspectArguments : ChangelogRegistryScopeArguments
{
	/// <summary>Optional path to write the machine-readable <see cref="RegistryStateSnapshot"/> JSON to.</summary>
	public string? Out { get; init; }
}

/// <summary>Arguments for <see cref="ChangelogRegistryRepairService.Repair"/>.</summary>
public sealed record ChangelogRegistryRepairArguments : ChangelogRegistryScopeArguments
{
	/// <summary>Allow writing a manifest with zero entries when the scope holds no objects.</summary>
	public bool AllowEmpty { get; init; }

	/// <summary>Report what would change without writing.</summary>
	public bool DryRun { get; init; }
}

/// <summary>Arguments for <see cref="ChangelogPublicVerificationService.Verify"/>.</summary>
public sealed record ChangelogPublicVerifyArguments : ChangelogRegistryScopeArguments
{
	/// <summary>The scrubber-owned public bucket to check. Only ever read.</summary>
	public required string PublicS3BucketName { get; init; }

	/// <summary>Maximum number of comparison attempts before reporting divergence.</summary>
	public int MaxAttempts { get; init; } = 12;

	/// <summary>Delay between comparison attempts.</summary>
	public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(10);
}

/// <summary>Arguments for <see cref="ChangelogRegistryRepublishService.Republish"/>.</summary>
public sealed record ChangelogRegistryRepublishArguments : ChangelogRegistryScopeArguments
{
	/// <summary>Specific file names in the scope to republish. Mutually exclusive with <see cref="All"/>.</summary>
	public IReadOnlyList<string> Files { get; init; } = [];

	/// <summary>Republish every object in the scope, including its <c>registry.json</c>.</summary>
	public bool All { get; init; }
}
