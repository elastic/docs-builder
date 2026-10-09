// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.PageCard;

public static class PageCardExamples
{
	public static ComponentSection Cards { get; } = new(
		"Page card",
		"A row that links to another page. The whole row is the target, so it carries one arrow.",
		"page-card",
		[
			ComponentExample.Of(
				"With a description",
				_PageCard.Create,
				new PageCardModel
				{
					Title = "Install Elasticsearch",
					Url = "#install",
					Description = new HtmlString("Download the package and start a node.")
				}
			),
			ComponentExample.Of("Title only", _PageCard.Create, new PageCardModel { Title = "Release notes", Url = "#release-notes" })
		]
	);
}
