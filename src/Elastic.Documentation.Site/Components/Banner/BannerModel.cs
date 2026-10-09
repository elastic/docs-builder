// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Banner;

/// <summary>A full-width announcement above the header, with an optional link.</summary>
public sealed record BannerModel
{
	public required string Message { get; init; }
	public string? LinkLabel { get; init; }
	public string? LinkHref { get; init; }
}
