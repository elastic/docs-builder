// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.SearchField;
using Elastic.Documentation.Site.Components.Segmented;

namespace Elastic.Documentation.Site.Components.FilterBar;

/// <summary>A view switch on the left and a text filter on the right, with how many items are showing.</summary>
public sealed record FilterBarModel
{
	public required SegmentedModel Views { get; init; }
	public required SearchFieldModel Filter { get; init; }

	/// <summary>For example "4 of 24"; announced politely when it changes.</summary>
	public string? Summary { get; init; }
}
