// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Product reference with strongly typed lifecycle.
/// </summary>
[Equatable]
public partial record ProductReference
{
	/// <summary>The product identifier.</summary>
	public required string ProductId { get; init; }

	/// <summary>
	/// Obsolete — entries derive applicability from their origin branch; notes use <see cref="Versions"/>.
	/// Kept for backward compatibility when reading already-published pool objects that still carry target.
	/// </summary>
	[Obsolete("Entries derive applicability from their origin branch; notes use Versions.")]
	public string? Target { get; init; }

	/// <summary>
	/// The releases this note applies to (note-only). Empty for entries.
	/// Populated from the file's <c>versions</c> or, for backward compatibility,
	/// from a single-element list derived from the file's legacy <c>target</c> when <c>Versions</c>
	/// is absent on an already-published note.
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string> Versions { get; init; } = [];

	/// <summary>The <c>target</c> as written on the product, or null when absent; the loader reads it as <see cref="Versions"/>.</summary>
	[IgnoreEquality]
	public string? TargetRaw { get; init; }

	/// <summary>The lifecycle stage of the feature for this product.</summary>
	public Lifecycle? Lifecycle { get; init; }

	/// <summary>
	/// A product reference from the pieces a CLI or a label names: the product id, the releases it applies to (never the obsolete <c>target</c>), and a
	/// lifecycle's text (none when blank or not a lifecycle). A reference has no rule set of its own: its rules (<c>products-required</c>,
	/// <c>product-id-required</c>) are the entry's, so the entry that holds it is what is checked.
	/// </summary>
	public static ProductReference Parse(string? productId, IReadOnlyList<string>? versions = null, string? lifecycle = null) =>
		new() { ProductId = productId ?? "", Versions = versions ?? [], Lifecycle = ReleaseNotesOperation.ParseLifecycle(lifecycle) };

	/// <summary>A reference to a product alone, as a note scraped from a release notes page names it.</summary>
	public static ProductReference Of(string productId, IReadOnlyList<string>? versions = null) =>
		new() { ProductId = productId, Versions = versions ?? [] };
}
