// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Input for a single bundle file with optional directory, repo, and link visibility
/// </summary>
public class BundleInput
{
	public string BundleFile { get; set; } = string.Empty;
	public string? Directory { get; set; }
	public string? Repo { get; set; }
	/// <summary>
	/// Whether to hide PR/issue links for entries from this bundle.
	/// When true, links are commented out in the markdown output.
	/// Defaults to false (links are shown).
	/// </summary>
	public bool HideLinks { get; set; }
}
