// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// The one way a changelog entry or a bundle becomes a record, and the one way it becomes text again. Nothing outside this assembly touches the
/// file's shape (the DTOs, the YAML deserializer): text goes in, a checked <see cref="Result{T}"/> comes out, and <c>Serialize</c> writes a record
/// back without a writer having to know the shape either.
/// <para>
/// Every load runs the aggregate's read-stage rule set once (<see cref="ChangelogEntryReadConstraints"/>, <see cref="BundleReadConstraints"/>); the
/// rules that only hold when an entry is authored or validated are the validator's (<see cref="ChangelogEntryValidator"/>), not a read's. A load that
/// parsed returns its record together with every finding, even an error, so the caller (which knows whether a rule error stops it) decides; a load that
/// could not parse returns no record and the parser's message as an error.
/// </para>
/// </summary>
public static class ReleaseNotesLoader
{
	/// <summary>
	/// Loads one changelog entry (or marker, or note) file: normalizes the legacy <c>version:</c> key and comments, parses, maps and checks it.
	/// The record carries <paramref name="fileName"/>.
	/// </summary>
	/// <param name="text">The file's text.</param>
	/// <param name="fileName">The file's name (without directories); stamped on the record.</param>
	/// <param name="file">The path diagnostics are reported against; defaults to <paramref name="fileName"/>.</param>
	public static Result<ChangelogEntry> LoadEntry(string text, string fileName, string? file = null) =>
		LoaderPipeline.Run(
			file ?? fileName,
			() => ReleaseNotesSerialization.DeserializeEntry(ReleaseNotesSerialization.NormalizeYaml(text)) with { FileName = fileName },
			ChangelogEntryReadConstraints.All,
			errorsAreFatal: false
		);

	/// <summary>Loads one bundle file: parses, maps and checks it against <see cref="BundleReadConstraints"/>.</summary>
	/// <param name="text">The file's text.</param>
	/// <param name="file">The path diagnostics are reported against.</param>
	public static Result<Bundle> LoadBundle(string text, string file) =>
		LoaderPipeline.Run(file, () => ReleaseNotesSerialization.DeserializeBundle(text), BundleReadConstraints.All, errorsAreFatal: false);

	/// <summary>Writes an entry as the YAML a note or entry file holds.</summary>
	public static string Serialize(ChangelogEntry entry) => ReleaseNotesSerialization.SerializeEntry(entry);

	/// <summary>Writes a bundle as the YAML a bundle file holds.</summary>
	public static string Serialize(Bundle bundle) => ReleaseNotesSerialization.SerializeBundle(bundle);

	/// <summary>Normalizes legacy YAML (<c>version:</c> read as <c>target:</c>, comment lines dropped); also what a file's checksum is computed over.</summary>
	public static string NormalizeYaml(string yaml) => ReleaseNotesSerialization.NormalizeYaml(yaml);

	/// <summary>The entry an inlined bundle entry stands for (the reverse of <see cref="ChangelogEntry.ToBundledEntry()"/>).</summary>
	public static ChangelogEntry ToEntry(BundledEntry entry) => ReleaseNotesSerialization.ConvertBundledEntry(entry);
}
