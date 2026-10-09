// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.LinkMenu;

public static class LinkMenuExamples
{
	public static ComponentSection Menus { get; } = new(
		"Link menu",
		"A native details menu of links, used for versions and sections. It opens without JavaScript; a small enhancement closes it on outside click and Escape.",
		"link-menu",
		[
			ComponentExample.Of(
				"Versions",
				_LinkMenu.Create,
				new LinkMenuModel
				{
					Label = "Version",
					Items = [new("9.0+ (Current)", "#", true), new("8.19", "#"), new("7.17", "#")],
					Footer = new("All versions", "#")
				}
			)
		]
	);
}
