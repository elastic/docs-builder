// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Suggestions;

public class Suggestion(IReadOnlySet<string> candidates, string input)
{
	private IReadOnlyCollection<string> GetSuggestions() =>
		candidates
			.Select(source => (source, Distance: LevenshteinDistance(input, source)))
			.OrderBy(suggestion => suggestion.Distance)
			.Where(suggestion => suggestion.Distance <= 2)
			.Select(suggestion => suggestion.source)
			.Take(3)
			.ToList();

	public string GetSuggestionQuestion()
	{
		var suggestions = GetSuggestions();
		if (suggestions.Count == 0)
			return string.Empty;

		return "Did you mean " + string.Join(", ", suggestions.SkipLast(1).Select(s => $"\"{s}\"")) + (suggestions.Count > 1 ? " or " : "") + (suggestions.LastOrDefault() != null ? $"\"{suggestions.LastOrDefault()}\"" : "") + "?";
	}

	private static int LevenshteinDistance(string source, string target)
	{
		if (string.IsNullOrEmpty(target))
			return int.MaxValue;

		var sourceLength = source.Length;
		var targetLength = target.Length;

		if (sourceLength == 0)
			return targetLength;

		if (targetLength == 0)
			return sourceLength;

		var distance = new int[sourceLength + 1, targetLength + 1];

		for (var i = 0; i <= sourceLength; i++)
			distance[i, 0] = i;

		for (var j = 0; j <= targetLength; j++)
			distance[0, j] = j;

		for (var i = 1; i <= sourceLength; i++)
		{
			for (var j = 1; j <= targetLength; j++)
			{
				var cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

				distance[i, j] = Math.Min(
					Math.Min(
						distance[i - 1, j] + 1,
						distance[i, j - 1] + 1),
					distance[i - 1, j - 1] + cost);
			}
		}

		return distance[sourceLength, targetLength];
	}
}
