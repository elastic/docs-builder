// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Operations;

/// <summary>
/// One endpoint row; <paramref name="ShortestRoute"/> feeds the optional-segment tooltip.
/// <paramref name="LinkedParameters"/> names the path parameters documented on the same page, whose placeholders
/// link there; leave it null where the path sits inside another link.
/// </summary>
public record EndpointPathModel(EndpointRow Row, string ShortestRoute, IReadOnlySet<string>? LinkedParameters = null)
{
	/// <summary>The id of the documented path parameter this segment's placeholder stands for, if any.</summary>
	public string? AnchorFor(EndpointSegment segment) =>
		segment.Parameter is { } name && LinkedParameters?.Contains(name) == true ? $"path-{name}" : null;
}
