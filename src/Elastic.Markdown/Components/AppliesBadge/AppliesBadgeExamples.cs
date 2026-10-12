// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components;

namespace Elastic.Markdown.Components.AppliesBadge;

public static class AppliesBadgeExamples
{
	public static ComponentSection Badges { get; } = new(
		"Applies to",
		"Shows where a feature applies and its lifecycle stage. The block form sits inline; the outlined form sits under a page title.",
		"applies-badge",
		[
			ComponentExample.Of("GA", _AppliesBadge.Create, new AppliesBadgeModel { Product = "Stack", Status = "GA 9.0+" }),
			ComponentExample.Of(
				"Preview",
				_AppliesBadge.Create,
				new AppliesBadgeModel { Product = "Serverless", Status = "Preview", Lifecycle = Lifecycle.Preview }
			),
			ComponentExample.Of(
				"Deprecated",
				_AppliesBadge.Create,
				new AppliesBadgeModel { Product = "Stack", Status = "Deprecated 8.4", Lifecycle = Lifecycle.Deprecated }
			),
			ComponentExample.Of(
				"Unavailable",
				_AppliesBadge.Create,
				new AppliesBadgeModel { Product = "ECK", Status = "Unavailable", Lifecycle = Lifecycle.Unavailable }
			),
			ComponentExample.Of(
				"Outlined",
				_AppliesBadge.Create,
				new AppliesBadgeModel { Product = "Elasticsearch", Status = "GA 9.0+", Outlined = true }
			),
			ComponentExample.Of(
				"Outlined preview",
				_AppliesBadge.Create,
				new AppliesBadgeModel { Product = "Kibana", Status = "Preview 9.1", Lifecycle = Lifecycle.Preview, Outlined = true }
			)
		]
	);
}
