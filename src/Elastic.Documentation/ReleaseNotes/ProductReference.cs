// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Product reference with strongly typed lifecycle.
/// </summary>
public record ProductReference
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
	/// Populated from <see cref="ProductInfoDto.Versions"/> or, for backward compatibility,
	/// from a single-element list derived from <see cref="ProductInfoDto.Target"/> when <c>Versions</c>
	/// is absent on an already-published note.
	/// </summary>
	public IReadOnlyList<string> Versions { get; init; } = [];

	/// <summary>The lifecycle stage of the feature for this product.</summary>
	public Lifecycle? Lifecycle { get; init; }
}
