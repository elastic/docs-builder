// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SiteHeader;

public static class SiteHeaderExamples
{
	public static ComponentSection Headers { get; } = new(
		"Header",
		"The header of a documentation set: brand, search and repository links. It is server rendered, and search works as a plain form.",
		"site-header",
		[
			ComponentExample.Of(
				"Documentation set",
				_SiteHeader.Create,
				new SiteHeaderModel
				{
					Title = "docs-builder",
					HomeHref = "#",
					Search = true,
					Links = [new("GitHub", "#", "logo_github")],
					Deployment = new Elastic.Documentation.Site.Components.DeploymentInfo.DeploymentInfoModel
					{
						Branch = "main",
						Commit = "65728c5",
						Repository = "elastic/docs-builder",
						GitHubRef = "refs/pull/123/merge"
					}
				}
			)
		]
	);
}
