// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives;

/// <summary>Shared parsing and normalization for the :widths: directive option.</summary>
public static class WidthsParser
{
	/// <summary>
	/// Parses a :widths: option value into an array of positive integers.
	/// Returns null for empty, whitespace, or "auto" values.
	/// Emits errors on the block for invalid values.
	/// </summary>
	public static int[]? Parse(string? widthsProp, DirectiveBlock block)
	{
		if (string.IsNullOrWhiteSpace(widthsProp))
			return null;

		// "auto" means let the browser decide — no explicit widths
		if (widthsProp.Trim().Equals("auto", StringComparison.OrdinalIgnoreCase))
			return null;

		var parts = widthsProp.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var widths = new List<int>(parts.Length);

		foreach (var part in parts)
		{
			if (!int.TryParse(part, out var width) || width <= 0)
			{
				block.EmitError($"Invalid column width '{part}' in {{{block.Directive}}} :widths: option. Values must be positive integers.");
				return null;
			}

			widths.Add(width);
		}

		return [.. widths];
	}

	/// <summary>
	/// Normalizes an array of relative widths to percentages that sum to 100.
	/// </summary>
	public static double[] NormalizeToPercentages(int[] widths)
	{
		var total = (float)widths.Sum();
		return widths.Select(w => System.Math.Round(w / total * 100f, 2)).ToArray();
	}
}
