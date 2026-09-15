// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;

namespace Elastic.Documentation.Http;

/// <summary>
/// Chooses Markdown only when <c>Accept</c> lists <c>text/markdown</c> ahead of <c>text/html</c>.
/// </summary>
public static class MarkdownAccept
{
	public static bool PrefersMarkdown(string? acceptHeader)
	{
		if (string.IsNullOrWhiteSpace(acceptHeader))
			return false;

		var markdown = MediaQuality(acceptHeader, "text/markdown");
		if (markdown is null)
			return false;

		var html = MediaQuality(acceptHeader, "text/html") ?? 0;
		return markdown.Value > html;
	}

	private static double? MediaQuality(string accept, string mediaType)
	{
		double? best = null;
		foreach (var part in accept.Split(','))
		{
			var item = part.Trim();
			if (item.Length == 0)
				continue;

			var segments = item.Split(';');
			var type = segments[0].Trim();
			if (!type.Equals(mediaType, StringComparison.OrdinalIgnoreCase))
				continue;

			var quality = 1.0;
			for (var i = 1; i < segments.Length; i++)
			{
				var parameter = segments[i].Trim();
				if (
					parameter.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
					&& double.TryParse(parameter[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
				)
					quality = parsed;
			}

			best = best is null ? quality : Math.Max(best.Value, quality);
		}

		return best;
	}
}
