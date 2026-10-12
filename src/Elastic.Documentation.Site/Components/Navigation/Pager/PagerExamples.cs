// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Navigation.Pager;

public static class PagerExamples
{
	public static ComponentSection Pagers { get; } = new(
		"Previous and next",
		"Links to the neighbouring pages at the end of an article.",
		"pager",
		[
			ComponentExample.Of(
				"Both",
				_Pager.Create,
				new PagerModel { Previous = new("Install Elasticsearch", "#"), Next = new("Index your first document", "#") }
			),
			ComponentExample.Of("Next only", _Pager.Create, new PagerModel { Next = new("Index your first document", "#") })
		]
	);
}
