// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.AppliesPopover;

public static class AppliesPopoverExamples
{
	private static AppliesPopoverContent Details =>
		new()
		{
			ProductDescription = new HtmlString("The <strong>Elastic APM Java agent</strong> traces operations in your Java applications."),
			Availability =
			[
				new("Generally available since 1.2", "This functionality is generally available and ready for production usage."),
				new("Beta in 1.0")
			],
			VersionNote = new HtmlString(
				"This documentation corresponds to the latest patch. Check the <a href=\"#\">release notes</a> for changes."
			)
		};

	public static ComponentSection Popovers { get; } = new(
		"Applies to with details",
		"The real applies-to badge. With details it opens a panel from a native details element, so it works without JavaScript and without ids; without details it is a static badge.",
		"applies-popover",
		[
			ComponentExample.Of(
				"With details",
				_AppliesPopover.Create,
				new AppliesPopoverModel
				{
					Product = "APM Agent Java",
					LifecycleClass = "ga",
					LifecycleName = "GA",
					Version = "1.2+",
					ShowLifecycleName = true,
					ShowVersion = true,
					HasMultipleLifecycles = true,
					Popover = Details
				}
			),
			ComponentExample.Of(
				"Static",
				_AppliesPopover.Create,
				new AppliesPopoverModel
				{
					Product = "Serverless",
					LifecycleClass = "preview",
					LifecycleName = "Preview",
					ShowLifecycleName = true
				}
			),
			ComponentExample.Of(
				"Deprecated",
				_AppliesPopover.Create,
				new AppliesPopoverModel
				{
					Product = "Stack",
					LifecycleClass = "deprecated",
					LifecycleName = "Deprecated",
					Version = "8.4",
					ShowLifecycleName = true,
					ShowVersion = true
				}
			),
			ComponentExample.Of(
				"Unavailable",
				_AppliesPopover.Create,
				new AppliesPopoverModel { Product = "ECK", LifecycleClass = "unavailable", LifecycleText = "Unavailable" }
			)
		]
	);
}
