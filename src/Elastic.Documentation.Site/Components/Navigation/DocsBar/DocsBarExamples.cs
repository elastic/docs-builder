// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Navigation.LinkMenu;

namespace Elastic.Documentation.Site.Components.Navigation.DocsBar;

public static class DocsBarExamples
{
	public static ComponentSection Bars { get; } = new(
		"Docs bar",
		"The section bar under the site header: a home link, the sections, an optional dropdown, and a version picker. The active section carries aria-current.",
		"docs-bar",
		[
			ComponentExample.Of(
				"Sections",
				_DocsBar.Create,
				new DocsBarModel
				{
					HomeLabel = "Docs",
					HomeHref = "#",
					Items =
					[
						new DocsBarItem { Title = "Guides", Href = "#", Icon = "documentation", Active = true },
						new DocsBarItem { Title = "Reference", Href = "#", Icon = "list_bullet" },
						new DocsBarItem
						{
							Title = "Products",
							Icon = "grid",
							Groups =
							[
								new("Search", [new("Elasticsearch", "#"), new("Kibana", "#")]),
								new("Observability", [new("APM", "#")])
							]
						},
						new DocsBarItem { Title = "Release notes", Href = "#", Icon = "refresh_time" }
					],
					Versions = new LinkMenuModel
					{
						Label = "Version",
						Items = [new("9.0+ (Current)", "#", true), new("8.19", "#")],
						Footer = new("All versions", "#")
					}
				}
			)
		]
	);
}
