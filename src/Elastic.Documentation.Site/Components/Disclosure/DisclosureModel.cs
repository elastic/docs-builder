// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.Disclosure;

public sealed record DisclosureModel
{
	public required string Title { get; init; }
	public required IHtmlContent Body { get; init; }
	public string? Id { get; init; }
	public string? Classes { get; init; }
	public bool Open { get; init; }
	public IHtmlContent? AppliesTo { get; init; }

	public string CssClass => string.IsNullOrWhiteSpace(Classes) ? "disclosure" : $"disclosure {Classes}";
}
