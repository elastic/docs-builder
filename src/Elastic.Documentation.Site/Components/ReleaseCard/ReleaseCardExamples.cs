// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.ReleaseCard;

public static class ReleaseCardExamples
{
	public static ComponentSection Releases { get; } = new(
		"Release card",
		"A release in a list: the version as a tag, a category and a short summary.",
		"release-card",
		[
			ComponentExample.Of(
				"Default",
				_ReleaseCard.Create,
				new ReleaseCardModel
				{
					Version = "9.1.0",
					Category = "Elasticsearch",
					Title = "Faster hybrid search",
					Summary = "Reciprocal rank fusion is now the default for hybrid queries.",
					Href = "#"
				}
			)
		]
	);
}
