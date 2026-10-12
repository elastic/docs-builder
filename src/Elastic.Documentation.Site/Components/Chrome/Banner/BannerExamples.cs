// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Chrome.Banner;

public static class BannerExamples
{
	public static ComponentSection Banners { get; } = new(
		"Banner",
		"A site-wide announcement. It is a labelled region, so it can be reached by landmark.",
		"banner",
		[
			ComponentExample.Of(
				"Banner",
				_Banner.Create,
				new BannerModel { Message = "Elastic 9.1 is available.", LinkLabel = "Read the release notes", LinkHref = "#release-notes" }
			),
			ComponentExample.Of("Info", _Banner.Create, new BannerModel { Message = "Search is being reindexed.", Kind = BannerKind.Info }),
			ComponentExample.Of(
				"Warning",
				_Banner.Create,
				new BannerModel
				{
					Message = "This version is reaching end of life.",
					Kind = BannerKind.Warning,
					LinkLabel = "Upgrade",
					LinkHref = "#upgrade"
				}
			),
			ComponentExample.Of(
				"Danger",
				_Banner.Create,
				new BannerModel { Message = "Some pages are temporarily unavailable.", Kind = BannerKind.Danger }
			)
		]
	);
}
