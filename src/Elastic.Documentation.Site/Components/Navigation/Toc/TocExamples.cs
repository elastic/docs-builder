// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Navigation.Toc;

public static class TocExamples
{
	public static ComponentSection Tocs { get; } = new(
		"On this page",
		"The in-page outline. The scroll-spy enhancement marks the heading in view with aria-current=location.",
		"toc",
		[
			ComponentExample.Of(
				"Outline",
				_Toc.Create,
				new TocModel
				{
					Items =
					[
						new("Overview", "overview", 2),
						new("Install", "install", 2),
						new("From source", "source", 3),
						new("Configure", "configure", 2)
					]
				}
			)
		]
	);
}
