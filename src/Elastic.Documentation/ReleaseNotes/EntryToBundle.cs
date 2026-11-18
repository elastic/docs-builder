// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// A <see cref="ChangelogEntry"/> a <c>changelog bundle</c> run is about to inline into a bundle: the stage that is checked for the fields a bundle needs.
/// An entry the pipeline reads for other purposes (a published pool, a validation run) is not held to these rules.
/// </summary>
public sealed record EntryToBundle(ChangelogEntry Data) : IHaveConstraints<EntryToBundle>
{
	public static ConstraintSet<EntryToBundle> Constraints => EntryToBundleConstraints.All;
}

/// <summary>Every rule an <see cref="EntryToBundle"/> must satisfy, in the order they are reported.</summary>
[Constraints<EntryToBundle>]
public static partial class EntryToBundleConstraints
{
	/// <summary>The first missing field is reported and the rest are not: the bundle command has always named one problem per file.</summary>
	public static readonly Constraint<EntryToBundle> EntryHasBundleFields = Constraints<EntryToBundle>.Check(
		e => FirstProblem(e.Data)
	).Describe("A changelog file has a title, a type and products, each with a product id, before it is inlined into a bundle.");

	private static List<Violation> FirstProblem(ChangelogEntry data)
	{
		if (string.IsNullOrWhiteSpace(data.Title))
			return [Violation.Error("Changelog file is missing required field: title")];
		if (data.Type == ChangelogEntryType.Invalid)
			return [Violation.Error("Changelog file is missing required field: type")];
		if (data.Products is not { Count: > 0 })
			return [Violation.Error("Changelog file is missing required field: products")];
		if (data.Products.Any(product => string.IsNullOrWhiteSpace(product.ProductId)))
			return [Violation.Error("Changelog file has product entry missing required field: product")];
		return [];
	}
}
