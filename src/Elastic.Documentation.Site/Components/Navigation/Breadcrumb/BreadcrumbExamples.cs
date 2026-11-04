// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Navigation.Breadcrumb;

public static class BreadcrumbExamples
{
	public static ComponentSection Trails { get; } = new(
		"Breadcrumb",
		"The path to the current page. One trail style replaces the slash, chevron and pill variants.",
		"breadcrumb",
		[
			ComponentExample.Of(
				"Trail",
				_Breadcrumb.Create,
				new BreadcrumbModel { Items = [new("Docs", "#"), new("Elasticsearch", "#"), new("Writing docs")] }
			)
		]
	);
}
