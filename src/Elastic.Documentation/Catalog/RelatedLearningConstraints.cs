// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Constraints;

namespace Elastic.Documentation.Catalog;

/// <summary>Every rule a <see cref="RelatedLearningCatalog"/> must satisfy, in the order they are reported.</summary>
public static class RelatedLearningConstraints
{
	public static IReadOnlyList<Constraint<RelatedLearningCatalog>> All { get; } =
	[
		new(
			"link-has-title",
			"Every related-learning link has a title.",
			m => m
				.Links
				.Values
				.Where(l => string.IsNullOrWhiteSpace(l.Title))
				.Select(l => Finding.Error($"related-learning.yml link '{l.Id}' is missing required 'title'."))
		),
		new(
			"link-has-url",
			"Every related-learning link has a url.",
			m => m
				.Links
				.Values
				.Where(l => !string.IsNullOrWhiteSpace(l.Title) && string.IsNullOrWhiteSpace(l.Url))
				.Select(l => Finding.Error($"related-learning.yml link '{l.Id}' is missing required 'url'."))
		),
		new(
			"link-url-absolute-http",
			"Every related-learning url is an absolute http or https URL.",
			m => m
				.Links
				.Values
				.Where(l => !string.IsNullOrWhiteSpace(l.Title) && !string.IsNullOrWhiteSpace(l.Url) && !IsAbsoluteHttpUrl(l.Url))
				.Select(
					l => Finding.Error(
						$"related-learning.yml link '{l.Id}' has invalid url '{l.Url}'. Every url must be an absolute http or https URL."
					)
				)
		)
	];

	public static IReadOnlyList<ConstraintViolation> Check(RelatedLearningCatalog catalog) => All.CheckAll(catalog);

	private static bool IsAbsoluteHttpUrl(string url) =>
		Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
