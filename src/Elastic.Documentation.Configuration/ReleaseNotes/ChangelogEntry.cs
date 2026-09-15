// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// DTO for YAML deserialization of changelog entries.
/// Maps directly to the YAML file structure.
/// Used by bundling service for direct deserialization with error handling.
/// </summary>
public record ChangelogEntryDto
{
	public string? Pr { get; set; }
	public List<string>? Prs { get; set; }
	public List<string>? Issues { get; set; }
	public string? Type { get; set; }
	public string? Subtype { get; set; }
	public List<ProductInfoDto>? Products { get; set; }
	public List<string>? Areas { get; set; }
	public string? Title { get; set; }
	public string? Description { get; set; }
	public string? Impact { get; set; }
	public string? Action { get; set; }
	[YamlMember(Alias = "feature-id", ApplyNamingConventions = false)]
	public string? FeatureId { get; set; }
	public bool? Highlight { get; set; }

	/// <summary>
	/// Marker reference: a bare PR number pointing to the authoritative entry in the same pool.
	/// A marker carries <c>link:</c> and nothing else; any other field alongside it is invalid.
	/// Written by the pipeline for non-primary PRs in a multi-PR entry; never hand-authored.
	/// </summary>
	public string? Link { get; set; }

	/// <summary>
	/// When true, this public-bucket object is a scrubber-written source pointer that traces back
	/// to a canonical public key. Distinguishes source pointers from ordinary link-only PR markers
	/// so the delete path does not spuriously follow a regular marker to its canonical target.
	/// </summary>
	[YamlMember(Alias = "source-redirect", ApplyNamingConventions = false)]
	public bool? SourceRedirect { get; set; }
}

/// <summary>
/// DTO for product info in YAML.
/// Used by bundling service for direct deserialization with error handling.
/// </summary>
public record ProductInfoDto
{
	public string? Product { get; set; }

	/// <summary>
	/// Obsolete — entries derive applicability from their origin branch; notes use <see cref="Versions"/>.
	/// Still deserialized for backward compatibility with already-published pool objects.
	/// </summary>
	[Obsolete("Entries derive applicability from their origin branch; notes use Versions.")]
	public string? Target { get; set; }

	/// <summary>
	/// The releases this note applies to (note-only field). For entries this is always null or empty.
	/// Expressed in the YAML as a sequence:
	/// <code>
	/// versions: [9.3.0, 9.4.0, 9.5.0]
	/// </code>
	/// or as a pipe-separated string in the <c>--products</c> CLI flag:
	/// <code>
	/// --products 'elasticsearch 9.3.0|9.4.0|9.5.0 ga'
	/// </code>
	/// </summary>
	public List<string>? Versions { get; set; }

	public string? Lifecycle { get; set; }
}
