// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.RelatedLearning;

namespace Elastic.Markdown.Myst.Directives.RelatedLearning;

public class RelatedLearningViewModel : DirectiveViewModel
{
	public required string Heading { get; init; }

	public required string Slug { get; init; }

	public required IReadOnlyList<RelatedLearningLink> Items { get; init; }
}
