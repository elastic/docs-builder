// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.RelatedLearning;

namespace Elastic.Markdown.Page;

public class RelatedLearningViewModel
{
	public required IReadOnlyList<RelatedLearningLink> Links { get; init; }
}
