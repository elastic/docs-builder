// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// File information in a bundled changelog entry.
/// </summary>
/// <remarks>
/// Constructed through <c>Bundle</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
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
