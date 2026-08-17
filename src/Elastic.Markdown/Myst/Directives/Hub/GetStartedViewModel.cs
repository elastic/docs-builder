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
	/// Track count for the step grid. A step carrying options spans the full row, so only the
	/// remaining steps compete for columns. The count is picked to divide them evenly and leave
	/// no short last row: three across when they divide by three, two when they are even,
	/// otherwise as many as there are, up to three.
	/// </summary>
	public int StepColumns
	{
		get
		{
			var inFlow = Steps.Count(s => s.Options.Count == 0);
			if (inFlow == 0)
				return 1;
			if (inFlow % 3 == 0)
				return 3;
			return inFlow % 2 == 0 ? 2 : inFlow < 3 ? inFlow : 3;
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
