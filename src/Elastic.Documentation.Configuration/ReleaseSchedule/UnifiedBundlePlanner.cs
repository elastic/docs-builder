// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.ReleaseSchedule;

/// <summary>The commits that bound the release notes of one product, from its commit in the previous manifest to its commit in the target manifest.</summary>
public sealed record ProductBundleRange(string ProductId, string RepoKey, string Version, string StartCommit, string EndCommit);

public enum PlanProblemKind
{
	/// <summary>The target manifest has no commit for the product, so its end of the range is unknown.</summary>
	MissingFromManifest,

	/// <summary>The previous manifest has no commit for the product, so its start of the range is unknown.</summary>
	MissingFromPreviousManifest
}

/// <summary>A product that blocks the plan, with the manifest key that was looked up.</summary>
public sealed record PlanProblem(string ProductId, string ArtifactKey, PlanProblemKind Kind);

/// <summary>What <c>release unified bundle</c> does for one target: the ranges to bundle, the products to skip, and what blocks the plan.</summary>
public sealed record UnifiedBundlePlan(
	IReadOnlyList<ProductBundleRange> Ranges,
	IReadOnlyList<string> Unchanged,
	IReadOnlyList<PlanProblem> Problems
)
{
	public bool IsComplete => Problems.Count == 0;
}

/// <summary>
/// The decisions of <c>release unified bundle</c> without any I/O: which products take part, and for each
/// one the range of commits between two manifests. Kept apart from the command so they can be tested.
/// </summary>
public static class UnifiedBundlePlanner
{
	/// <summary>The products that publish through the DRA pipeline.</summary>
	public static IReadOnlyList<Product> SelectDraProducts(IEnumerable<Product> products) =>
		[.. products.Where(p => p.Features.ReleaseNotes == ReleaseNotesPath.DailyReleasableArtifacts)];

	/// <summary>Limits <paramref name="draProducts"/> to the requested ids, and names the ids that match no product.</summary>
	public static (IReadOnlyList<Product> Selected, IReadOnlyList<string> Unknown) FilterProducts(
		IReadOnlyList<Product> draProducts,
		IReadOnlyCollection<string>? requested
	)
	{
		if (requested is not { Count: > 0 })
			return (draProducts, []);

		var unknown = requested.Where(id => draProducts.All(p => !p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))).ToArray();
		var selected = draProducts.Where(p => requested.Contains(p.Id, StringComparer.OrdinalIgnoreCase)).ToArray();
		return (selected, unknown);
	}

	/// <summary>The key of the product in a build manifest: the DRA artifact, else the repository, else the id.</summary>
	public static string ArtifactKey(Product product) => (product.DraArtifact ?? product.Repository ?? product.Id).ToLowerInvariant();

	/// <summary>The GitHub repository of the product.</summary>
	public static string RepoKey(Product product) => (product.Repository ?? product.Id).ToLowerInvariant();

	/// <summary>
	/// Plans the bundles of <paramref name="version"/>. A product whose commit is the same in both manifests has no
	/// changes and gets no range. A product that lacks a commit in either manifest blocks the plan: the start of a
	/// range is never guessed, and a silently incomplete set of bundles must not look like success.
	/// </summary>
	public static UnifiedBundlePlan Plan(
		IReadOnlyList<Product> products,
		string version,
		ElasticBuildManifest manifest,
		ElasticBuildManifest previousManifest
	)
	{
		var ranges = new List<ProductBundleRange>();
		var unchanged = new List<string>();
		var problems = new List<PlanProblem>();

		foreach (var product in products)
		{
			var artifactKey = ArtifactKey(product);

			if (!TryGetCommit(manifest, artifactKey, out var endCommit))
			{
				problems.Add(new PlanProblem(product.Id, artifactKey, PlanProblemKind.MissingFromManifest));
				continue;
			}

			if (!TryGetCommit(previousManifest, artifactKey, out var startCommit))
			{
				problems.Add(new PlanProblem(product.Id, artifactKey, PlanProblemKind.MissingFromPreviousManifest));
				continue;
			}

			if (string.Equals(startCommit, endCommit, StringComparison.OrdinalIgnoreCase))
				unchanged.Add(product.Id);
			else
				ranges.Add(new ProductBundleRange(product.Id, RepoKey(product), version, startCommit, endCommit));
		}

		return new UnifiedBundlePlan(ranges, unchanged, problems);
	}

	private static bool TryGetCommit(ElasticBuildManifest manifest, string artifactKey, out string commit)
	{
		if (manifest.Projects.TryGetValue(artifactKey, out var project) && !string.IsNullOrEmpty(project.CommitHash))
		{
			commit = project.CommitHash;
			return true;
		}

		commit = string.Empty;
		return false;
	}
}
