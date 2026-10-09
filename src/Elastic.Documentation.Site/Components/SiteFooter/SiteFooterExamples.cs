// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SiteFooter;

public static class SiteFooterExamples
{
	public static ComponentSection Footers { get; } = new(
		"Footer",
		"The site footer: logo, legal line and links, with optional notices and consent links. Branded builds drop it.",
		"site-footer",
		[
			ComponentExample.Of(
				"Default",
				_SiteFooter.Create,
				new SiteFooterModel { LogoSrc = "/_static/logo-elastic-horizontal-white.svg", Year = 2026 }
			),
			ComponentExample.Of(
				"With notices",
				_SiteFooter.Create,
				new SiteFooterModel
				{
					LogoSrc = "/_static/logo-elastic-horizontal-white.svg",
					Year = 2026,
					Notices = ["This content is available in different formats for convenience only."],
					PrivacyChoices = true
				}
			)
		]
	);
}
