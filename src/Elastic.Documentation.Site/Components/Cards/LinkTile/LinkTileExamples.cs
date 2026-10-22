// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Cards.LinkTile;

public static class LinkTileExamples
{
	public static ComponentSection Tiles { get; } = new(
		"Link tile",
		"A compact row for lists of destinations, such as quick links on a hub page.",
		"link-tile",
		[
			ComponentExample.Of(
				"With subtitle",
				_LinkTile.Create,
				new LinkTileModel { Title = "Get started", Subtitle = "Install and run in minutes", Href = "#", Icon = "launch" }
			),
			ComponentExample.Of("Title only", _LinkTile.Create, new LinkTileModel { Title = "API reference", Href = "#" })
		]
	);
}
