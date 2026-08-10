// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

public class GetStartedViewModel : HubDirectiveViewModel
{
	public required string? Title { get; init; }
	public required string? IntroHtml { get; init; }
	public required IReadOnlyList<GetStartedStepViewModel> Steps { get; init; }
}

public sealed record GetStartedStepViewModel
{
	public required int Number { get; init; }
	public required string? Title { get; init; }
	public required string? DescriptionHtml { get; init; }
	public required string? Link { get; init; }
	public required string? LinkLabel { get; init; }
	public required IReadOnlyList<GetStartedOptionViewModel> Options { get; init; }
}

public sealed record GetStartedOptionViewModel
{
	public required string? Label { get; init; }
	public required string? DescriptionHtml { get; init; }
	public required string? Code { get; init; }
	public required string? Language { get; init; }
	public required string? Url { get; init; }
	public required string? UrlLabel { get; init; }
}
