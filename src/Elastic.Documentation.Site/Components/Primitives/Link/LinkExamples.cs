// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Primitives.Link;

public static class LinkExamples
{
	public static ComponentSection Links { get; } = new(
		"Link",
		"Inline links are underlined; arrow links stand alone as a call to action. External links open in a new tab.",
		"link",
		[
			ComponentExample.Of("Inline", _Link.Create, new LinkModel { Label = "Read the guide", Href = "#" }),
			ComponentExample.Of("Arrow", _Link.Create, new LinkModel { Label = "Explore the reference", Href = "#", Arrow = true }),
			ComponentExample.Of(
				"External",
				_Link.Create,
				new LinkModel { Label = "elastic.co", Href = "https://www.elastic.co", Arrow = true, External = true }
			)
		]
	);
}
