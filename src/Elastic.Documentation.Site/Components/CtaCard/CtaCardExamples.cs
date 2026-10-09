// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.CtaCard;

public static class CtaCardExamples
{
	public static ComponentSection Ctas { get; } = new(
		"Call to action card",
		"A single primary action with the reasons to take it, shown in the rail of assembled docs.",
		"cta-card",
		[
			ComponentExample.Of(
				"Trial",
				_CtaCard.Create,
				new CtaCardModel
				{
					Label = "Get started free",
					Url = "#",
					Name = "trial",
					Benefits = ["14-day free trial", "All features included", "No setup required"]
				}
			)
		]
	);
}
