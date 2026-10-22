// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Chrome.Feedback;

public static class FeedbackExamples
{
	public static ComponentSection Feedbacks { get; } = new(
		"Page feedback",
		"Asks whether a page helped, then why. It is hidden until its script loads, because a form that cannot send is worse than none. Open the Code section to see the full markup the script reveals.",
		"feedback",
		[
			ComponentExample.Of(
				"Docs",
				_Feedback.Create,
				new FeedbackModel { PageUrl = "/docs/example", PageTitle = "Example", ApiBase = "/docs/_api" }
			),
			ComponentExample.Of(
				"API",
				_Feedback.Create,
				new FeedbackModel
				{
					PageUrl = "/docs/api/example",
					PageTitle = "Example operation",
					ApiBase = "/docs/_api",
					Surface = FeedbackSurface.Api
				}
			)
		]
	);
}
