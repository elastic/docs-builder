// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Navigation.LinkMenu;

public static class LinkMenuExamples
{
	public static ComponentSection Menus { get; } = new(
		"Link menu",
		"A button that opens a popover menu of links, used for versions and sections. The browser closes it on outside click and Escape; floating.ts keeps it beside the button.",
		"link-menu",
		[
			ComponentExample.Of(
				"Versions",
				_LinkMenu.Create,
				new LinkMenuModel
				{
					Id = "example-version-menu",
					Label = "Version",
					Items = [new("9.0+ (Current)", "#", true), new("8.19", "#"), new("7.17", "#")],
					Footer = new("All versions", "#")
				}
			)
		]
	);
}
