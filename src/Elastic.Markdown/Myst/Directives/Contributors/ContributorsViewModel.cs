// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Contributors;

/// <summary>View model for the contributors grid directive.</summary>
public class ContributorsViewModel
{
	/// <summary>Resolved contributor entries.</summary>
	public required IReadOnlyList<Contributor> Contributors { get; init; }
}
