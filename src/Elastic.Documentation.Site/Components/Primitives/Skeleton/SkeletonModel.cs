// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Primitives.Skeleton;

/// <summary>A placeholder for content that is still loading. It is hidden from assistive technology; the region that loads says so.</summary>
public sealed record SkeletonModel
{
	public int Lines { get; init; } = 3;
	public bool Heading { get; init; }

	/// <summary>Draws an image block above the lines, the shape of a card.</summary>
	public bool Media { get; init; }
}
