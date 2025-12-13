// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.Changelog;

/// <summary>
/// Input data for rendering changelog bundle to markdown
/// </summary>
public class ChangelogRenderInput
{
	public List<BundleInput> Bundles { get; set; } = [];
	public string? Output { get; set; }
	public string? Title { get; set; }
	public bool Subsections { get; set; }
}

