// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.LinkTile;

public sealed record LinkTileModel
{
	public required string Title { get; init; }
	public required string Href { get; init; }
	public string? Subtitle { get; init; }
	public string? Icon { get; init; }
}
