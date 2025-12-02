// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.ReleaseNotes;

/// <summary>
/// Input data for creating a release notes changelog fragment
/// </summary>
public class ReleaseNotesInput
{
	public required string Title { get; set; }
	public required string Type { get; set; }
	public required List<ProductInfo> Products { get; set; }
	public string? Subtype { get; set; }
	public string[] Areas { get; set; } = [];
	public string? Pr { get; set; }
	public string[] Issues { get; set; } = [];
	public string? Description { get; set; }
	public string? Impact { get; set; }
	public string? Action { get; set; }
	public string? FeatureId { get; set; }
	public bool? Highlight { get; set; }
	public int? Id { get; set; }
	public string? Output { get; set; }
}

