// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.Changelog;

/// <summary>
/// Input data for creating a changelog fragment
/// </summary>
public class ChangelogInput
{
	public string? Title { get; set; }
	public string? Type { get; set; }
	public required List<ProductInfo> Products { get; set; }
	public string? Subtype { get; set; }
	public string[] Areas { get; set; } = [];
	public string? Pr { get; set; }
	public string? Owner { get; set; }
	public string? Repo { get; set; }
	public string[] Issues { get; set; } = [];
	public string? Description { get; set; }
	public string? Impact { get; set; }
	public string? Action { get; set; }
	public string? FeatureId { get; set; }
	public bool? Highlight { get; set; }
	public string? Output { get; set; }
	public string? Config { get; set; }
}

