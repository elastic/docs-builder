// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.CtaCard;

public sealed record CtaCardModel
{
	public required string Label { get; init; }
	public required string Url { get; init; }
	public required string Name { get; init; }
	public IReadOnlyList<string> Benefits { get; init; } = [];
}
