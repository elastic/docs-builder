// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

public class GetStartedViewModel : HubDirectiveViewModel
{
	public required string? Title { get; init; }
	public required string? IntroHtml { get; init; }
	public required IReadOnlyList<GetStartedStepViewModel> Steps { get; init; }

	/// <summary>
	/// The first step. It sits in its own tinted box above the others, whatever it contains,
	/// because it is the one step with a choice to make before the rest can happen.
	/// </summary>
	public GetStartedStepViewModel? First => Steps.Count > 0 ? Steps[0] : null;

	/// <summary>The steps after the first. They share one grid and line up across its columns.</summary>
	public IReadOnlyList<GetStartedStepViewModel> Rest => Steps.Count > 1 ? [.. Steps.Skip(1)] : [];

	/// <summary>
	/// Column count for the steps after the first. It is picked to divide them evenly and leave
	/// no short last row: three across when they divide by three, two when they are even,
	/// otherwise as many as there are, up to three.
	/// </summary>
	public int StepColumns
	{
		get
		{
			var count = Rest.Count;
			if (count == 0)
				return 1;
			if (count % 3 == 0)
				return 3;
			return count % 2 == 0 ? 2 : count < 3 ? count : 3;
		}
	}
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
