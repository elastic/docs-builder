// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.Changelog;

/// <summary>
/// Input data for bundling changelog fragments
/// </summary>
public class ChangelogBundleInput
{
	public string Directory { get; set; } = string.Empty;
	public string? Output { get; set; }
	public bool All { get; set; }
	public string? ProductVersion { get; set; }
	public string[]? Prs { get; set; }
	public string? PrsFile { get; set; }
	public string? Owner { get; set; }
	public string? Repo { get; set; }
}

