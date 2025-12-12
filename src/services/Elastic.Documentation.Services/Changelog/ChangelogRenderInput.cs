// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.Changelog;

/// <summary>
/// Input data for rendering changelog bundle to markdown
/// </summary>
public class ChangelogRenderInput
{
	public string BundleFile { get; set; } = string.Empty;
	public string? Output { get; set; }
	public string? Directory { get; set; }
	public string? Repo { get; set; }
	public bool Subsections { get; set; }
}

