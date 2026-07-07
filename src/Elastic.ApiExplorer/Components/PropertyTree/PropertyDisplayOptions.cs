// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Configuration.Versions;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>
/// Controls how property sections collapse/expand by default.
/// </summary>
public enum CollapseMode
{
	/// <summary>Properties always start collapsed when toggle is shown (OperationView behavior).</summary>
	AlwaysCollapsed,

	/// <summary>Depth-based: depth 0 collapsed, deeper levels expand if less than 5 properties (SchemaView behavior).</summary>
	DepthBased
}

/// <summary>
/// Page-level display settings consumed by <see cref="ApiPropertyTreeBuilder"/>; views never see this.
/// </summary>
public record PropertyDisplayOptions
{
	/// <summary>Function to render markdown descriptions to HTML.</summary>
	public required Func<string?, HtmlString> RenderMarkdown { get; init; }

	/// <summary>Root URL of the current API product (e.g. <c>/api/elasticsearch</c>) used for type page links.</summary>
	public required string ApiRootUrl { get; init; }

	public bool ShowDeprecated { get; init; } = true;
	public bool ShowVersionInfo { get; init; } = true;
	public bool ShowExternalDocs { get; init; } = true;
	public bool UseHiddenUntilFound { get; init; } = true;
	public CollapseMode CollapseMode { get; init; } = CollapseMode.AlwaysCollapsed;
	public int MaxDepth { get; init; } = SchemaHelpers.MaxDepth;
	public VersionsConfiguration? VersionsConfiguration { get; init; }
}
