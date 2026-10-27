// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Navigation.PageActions;

public static class PageActionsExamples
{
	public static ComponentSection Actions { get; } = new(
		"Page actions",
		"Links that act on the current page, shown in the right-hand rail.",
		"page-actions",
		[
			ComponentExample.Of(
				"All actions",
				_PageActions.Create,
				new PageActionsModel { MarkdownUrl = "#", ReportIssueUrl = "#", EditUrl = "#", ContributeUrl = "#" }
			)
		]
	);
}
