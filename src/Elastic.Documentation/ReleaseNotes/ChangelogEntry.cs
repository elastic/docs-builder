// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Domain type representing a changelog entry.
/// Uses strongly typed enums for Type, Subtype, and Lifecycle.
/// </summary>
public record ChangelogEntry
{
	/// <summary>Pull request URL or reference.</summary>
	public string? Pr { get; init; }

	/// <summary>Related issue URLs or references.</summary>
	public IReadOnlyList<string>? Issues { get; init; }

	/// <summary>The type of changelog entry (feature, bug-fix, etc.).</summary>
	public ChangelogEntryType Type { get; init; } = ChangelogEntryType.Invalid;

	/// <summary>The subtype of changelog entry (only applicable to breaking changes).</summary>
	public ChangelogEntrySubtype? Subtype { get; init; }

	/// <summary>Products affected by this changelog entry.</summary>
	public IReadOnlyList<ProductReference>? Products { get; init; }

	/// <summary>Areas affected by this changelog entry.</summary>
	public IReadOnlyList<string>? Areas { get; init; }

	/// <summary>The title of the changelog entry.</summary>
	public string Title { get; init; } = "";

	/// <summary>Optional description with more details.</summary>
	public string? Description { get; init; }

	/// <summary>Impact statement for breaking changes.</summary>
	public string? Impact { get; init; }

	/// <summary>Required action for breaking changes.</summary>
	public string? Action { get; init; }

	/// <summary>Optional feature ID for tracking.</summary>
	public string? FeatureId { get; init; }

	/// <summary>Whether this entry should be highlighted.</summary>
	public bool? Highlight { get; init; }

	/// <summary>
	/// Converts this ChangelogEntry to a BundledEntry for embedding in bundles.
	/// File property is set to null; set it separately using a 'with' expression.
	/// </summary>
	public BundledEntry ToBundledEntry() => new()
	{
		File = null,
		Type = Type != ChangelogEntryType.Invalid ? Type : null,
		Title = Title,
		Products = Products,
		Description = Description,
		Impact = Impact,
		Action = Action,
		FeatureId = FeatureId,
		Highlight = Highlight,
		Subtype = Subtype,
		Areas = Areas,
		Pr = Pr,
		Issues = Issues
	};
}
