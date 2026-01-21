// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Input data for bundling changelog files
/// </summary>
public class ChangelogBundleInput
{
	public string Directory { get; set; } = string.Empty;
	public string? Output { get; set; }
	public bool All { get; set; }
	public List<ProductInfo>? InputProducts { get; set; }
	public List<ProductInfo>? OutputProducts { get; set; }
	public bool Resolve { get; set; }
	public string[]? Prs { get; set; }
	public string? Owner { get; set; }
	public string? Repo { get; set; }
}
