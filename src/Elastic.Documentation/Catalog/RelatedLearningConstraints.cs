// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Catalog;

/// <summary>Every rule a <see cref="RelatedLearningCatalog"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class RelatedLearningConstraints
{
	public static Constraint<RelatedLearningCatalog> LinkHasTitle { get; } = Constraints<RelatedLearningCatalog>.Each(
		m => m.Links.Values,
		l => !string.IsNullOrWhiteSpace(l.Title),
		l => $"related-learning.yml link '{l.Id}' is missing required 'title'."
	).Describe("Every related-learning link has a title.");

	public static Constraint<RelatedLearningCatalog> LinkHasUrl { get; } = Constraints<RelatedLearningCatalog>.Each(
		m => m.Links.Values,
		l => string.IsNullOrWhiteSpace(l.Title) || !string.IsNullOrWhiteSpace(l.Url),
		l => $"related-learning.yml link '{l.Id}' is missing required 'url'."
	).Describe("Every related-learning link has a url.");

	public static Constraint<RelatedLearningCatalog> LinkUrlAbsoluteHttp { get; } = Constraints<RelatedLearningCatalog>.Each(
		m => m.Links.Values,
		l => string.IsNullOrWhiteSpace(l.Title) || string.IsNullOrWhiteSpace(l.Url) || IsAbsoluteHttpUrl(l.Url),
		l => $"related-learning.yml link '{l.Id}' has invalid url '{l.Url}'. Every url must be an absolute http or https URL."
	).Describe("Every related-learning url is an absolute http or https URL.");

	private static bool IsAbsoluteHttpUrl(string url) =>
		Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
