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

	/// <summary>
	/// Rows follow the same track count as <see cref="StepColumns"/>. A step with options
	/// takes a row of its own. A row of one ordinary step is centered at the width of one
	/// cell, using <see cref="SoloShare"/>.
	/// </summary>
	public IReadOnlyList<IReadOnlyList<GetStartedStepViewModel>> Rows
	{
		get
		{
			var rows = new List<IReadOnlyList<GetStartedStepViewModel>>();
			var buffer = new List<GetStartedStepViewModel>();

			void Flush()
			{
				if (buffer.Count == 0)
					return;

				var columns = StepColumns;
				for (var index = 0; index < buffer.Count; index += columns)
				{
					var remaining = buffer.Count - index;
					var take = remaining < columns ? remaining : columns;
					rows.Add(buffer.GetRange(index, take));
				}

				buffer.Clear();
			}

			foreach (var step in Steps)
			{
				if (step.Options.Count > 0)
				{
					Flush();
					rows.Add([step]);
				}
				else
					buffer.Add(step);
			}

			Flush();
			return rows;
		}
	}

	/// <summary>
	/// How many cells a shared row uses. A step sitting alone matches that width.
	/// When every row is a single step, half the track is wide enough to read and
	/// narrow enough not to stretch across the page.
	/// </summary>
	public int SoloShare => Rows.FirstOrDefault(row => row.Count > 1)?.Count ?? 2;
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
