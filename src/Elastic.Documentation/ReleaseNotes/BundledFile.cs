// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// File information in a bundled changelog entry.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> <c>ReleaseNotesLoader.LoadBundle</c> / <c>BundleLoader</c> in <c>Elastic.Documentation.Configuration</c> (a bundle YAML file) or the pure operations <see cref="Bundle.Assemble"/>, <see cref="Bundle.ForRelease"/> and <see cref="Bundle.AmendOf"/>, or <see cref="BundledFile.Of"/>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Name</c>, <c>Checksum</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="BundleConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new BundledFile { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public record BundledFile
{
	/// <summary>The filename.</summary>
	public string Name { get; internal init; } = default!;

	/// <summary>The file checksum.</summary>
	public string Checksum { get; internal init; } = default!;

	/// <summary>The file a bundle entry was read from.</summary>
	public static BundledFile Of(string name, string checksum) => new() { Name = name, Checksum = checksum };
}
