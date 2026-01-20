// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Input data for rendering changelog bundle to markdown or asciidoc
/// </summary>
public class ChangelogRenderInput
{
	public List<BundleInput> Bundles { get; set; } = [];
	public string? Output { get; set; }
	public string? Title { get; set; }
	public bool Subsections { get; set; }
	public string[]? HideFeatures { get; set; }
	public string? Config { get; set; }
	public string FileType { get; set; } = "markdown";
}
