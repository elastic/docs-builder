// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// A <see cref="Bundle"/> as <c>changelog bundle</c> assembles it from changelog files: the stage that is checked for what the command chose
/// (which products the bundle lists). A bundle that was read is not an <see cref="AuthoredBundle"/>: it was written by an earlier run, and a rule of
/// the assembling command does not apply to it.
/// </summary>
public sealed record AuthoredBundle(Bundle Data) : IHaveConstraints<AuthoredBundle>
{
	public static ConstraintSet<AuthoredBundle> Constraints => AuthoredBundleConstraints.All;

	/// <summary>A bundle with nothing in it yet.</summary>
	public static AuthoredBundle Empty { get; } = new(new Bundle());

	/// <summary>This bundle listing <paramref name="products"/>.</summary>
	public AuthoredBundle WithProducts(IReadOnlyList<BundledProduct> products) => this with { Data = Data with { Products = products } };

	/// <summary>This bundle with <paramref name="entries"/> and the feature ids to hide.</summary>
	public AuthoredBundle WithEntries(IReadOnlyList<BundledEntry> entries, IReadOnlyList<string> hideFeatures) =>
		this with { Data = Data with { Entries = entries, HideFeatures = hideFeatures } };
}

/// <summary>Every rule an <see cref="AuthoredBundle"/> must satisfy, in the order they are reported.</summary>
[Constraints<AuthoredBundle>]
public static partial class AuthoredBundleConstraints
{
	public static readonly Constraint<AuthoredBundle> BundleProductSingleTarget = Constraints<AuthoredBundle>.Check(
		b =>
			[
				.. b
					.Data
					.Products
					.GroupBy(p => p.ProductId, StringComparer.OrdinalIgnoreCase)
					.Where(g => g.Count() > 1)
					.Select(
						g => Violation.Warning($"Product '{g.Key}' has multiple targets in bundle: {string.Join(", ", g.Select(Target))}")
					)
			]
	).Describe("A bundle lists a product with one target, not several.");

	private static string Target(BundledProduct p)
	{
		var target = string.IsNullOrWhiteSpace(p.Target) ? "(no target)" : p.Target;
		return p.Lifecycle is { } lifecycle ? $"{target} {lifecycle.ToStringFast(true)}" : target;
	}
}
