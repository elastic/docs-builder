// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.Stepper;

public sealed record StepperModel
{
	/// <summary>The rendered steps, each an <c>li</c>.</summary>
	public required IHtmlContent Steps { get; init; }
}

public sealed record StepModel
{
	public required IHtmlContent Body { get; init; }
	public string? Title { get; init; }

	/// <summary>The title, already rendered as inline HTML so substitutions and links work.</summary>
	public IHtmlContent? TitleHtml { get; init; }

	public string? Anchor { get; init; }
	public int HeadingLevel { get; init; } = 3;

	/// <summary>Uses a real heading element so the step appears in the page outline.</summary>
	public bool AsHeading { get; init; }
}
