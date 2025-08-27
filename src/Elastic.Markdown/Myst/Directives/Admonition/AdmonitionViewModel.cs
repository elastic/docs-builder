// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Components;

namespace Elastic.Markdown.Myst.Directives.Admonition;

public class AdmonitionViewModel : DirectiveViewModel
{
	public required string Title { get; init; }
	public required string Directive { get; init; }
	public required string? CrossReferenceName { get; init; }
	public required string? Classes { get; init; }
	public required string? Open { get; init; }
	public required ApplicableToViewModel? ApplicableToViewModel { get; init; }
}
