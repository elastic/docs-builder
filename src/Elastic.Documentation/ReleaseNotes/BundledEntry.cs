// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// A changelog entry within a bundle, with strongly typed enums.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> <c>ReleaseNotesLoader.LoadBundle</c> / <c>BundleLoader</c> in <c>Elastic.Documentation.Configuration</c> (a bundle YAML file) or the pure operations <see cref="Bundle.Assemble"/>, <see cref="Bundle.ForRelease"/> and <see cref="Bundle.AmendOf"/>, or <see cref="BundledEntry.Inline"/> and <see cref="BundledEntry.Exclusion"/>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with <c>BundleExamples.Entry</c> (tests), <c>BundledEntry.Inline</c> / <c>BundledEntry.Exclusion</c>, or an object initializer.</para>
/// </remarks>
[Equatable]
public partial record BundledEntry
{
	/// <summary>File information (name and checksum).</summary>
	public BundledFile? File { get; internal init; }

	/// <summary>The type of changelog entry (feature, bug-fix, etc.).</summary>
	public ChangelogEntryType? Type { get; internal init; }

	/// <summary>The title of the changelog entry.</summary>
	public string? Title { get; internal init; }

	/// <summary>Products affected by this changelog entry.</summary>
	[OrderedEquality]
	public IReadOnlyList<ProductReference>? Products { get; internal init; }

	/// <summary>Optional description with more details.</summary>
	public string? Description { get; internal init; }

	/// <summary>Impact statement for breaking changes.</summary>
	public string? Impact { get; internal init; }

	/// <summary>Required action for breaking changes.</summary>
	public string? Action { get; internal init; }

	/// <summary>Optional feature ID for tracking.</summary>
	public string? FeatureId { get; internal init; }

	/// <summary>Whether this entry should be highlighted.</summary>
	public bool? Highlight { get; internal init; }

	/// <summary>The subtype of changelog entry (only applicable to breaking changes).</summary>
	public ChangelogEntrySubtype? Subtype { get; internal init; }

	/// <summary>Areas affected by this changelog entry.</summary>
	[OrderedEquality]
	public IReadOnlyList<string>? Areas { get; internal init; }

	/// <summary>Pull request URLs or references.</summary>
	[OrderedEquality]
	public IReadOnlyList<string>? Prs { get; internal init; }

	/// <summary>Related issue URLs or references.</summary>
	[OrderedEquality]
	public IReadOnlyList<string>? Issues { get; internal init; }

	/// <summary>
	/// Marker reference: a bare PR number pointing to the authoritative entry in the same pool.
	/// Preserved through the scrub round-trip so the public pool can resolve markers end-to-end.
	/// </summary>
	public string? Link { get; internal init; }
}
