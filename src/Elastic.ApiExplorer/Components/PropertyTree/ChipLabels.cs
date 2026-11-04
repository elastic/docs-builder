// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>
/// Short chip labels for a list of variant names: the words every name shares at its start or end are dropped, so
/// <c>bedrock_secrets</c>, <c>email_secrets</c> read <c>bedrock</c>, <c>email</c>, and <c>EqlRuleUpdateProps</c>,
/// <c>QueryRuleUpdateProps</c> read <c>Eql</c>, <c>Query</c>.
/// </summary>
public static class ChipLabels
{
	/// <summary>
	/// The label for each name, in order. Words split at <c>_</c>, <c>-</c>, <c>.</c> and capital letters; every label
	/// keeps at least one word, and a list with fewer than two distinct names stays as it is.
	/// </summary>
	public static IReadOnlyList<string> Shorten(IReadOnlyList<string> names)
	{
		if (names.Distinct(StringComparer.Ordinal).Count() < 2)
			return names;

		var words = names.Select(Words).ToArray();
		var shortest = words.Min(w => w.Length);
		var prefix = SharedCount(words, shortest, static (w, i) => w[i]);
		var suffix = SharedCount(words, shortest - prefix, static (w, i) => w[w.Count - 1 - i]);
		// Each label keeps a word of its own; drop the suffix first, it is the likelier to be noise ("_config").
		while (prefix + suffix >= shortest && suffix > 0)
			suffix--;
		while (prefix + suffix >= shortest && prefix > 0)
			prefix--;
		if (prefix + suffix == 0)
			return names;

		return names.Select((name, n) => Slice(name, words[n], prefix, suffix)).ToArray();
	}

	private readonly record struct Word(int Start, int End, string Text);

	/// <summary>How many words, from the given end, every name shares (at most <paramref name="limit"/>).</summary>
	private static int SharedCount(Word[][] words, int limit, Func<IReadOnlyList<Word>, int, Word> at)
	{
		var count = 0;
		while (count < limit && words.All(w => w.Length > count && at(w, count).Text == at(words[0], count).Text))
			count++;
		return count;
	}

	/// <summary>The name from its first kept word to its last, separators between them included.</summary>
	private static string Slice(string name, Word[] words, int prefix, int suffix) =>
		name[words[prefix].Start..words[words.Length - 1 - suffix].End];

	private static Word[] Words(string name)
	{
		var words = new List<Word>();
		var start = -1;
		for (var i = 0; i <= name.Length; i++)
		{
			var atEnd = i == name.Length;
			var separator = !atEnd && name[i] is '_' or '-' or '.' or ' ';
			if (start >= 0 && (atEnd || separator || StartsWord(name, i)))
			{
				words.Add(new Word(start, i, name[start..i]));
				start = -1;
			}
			if (!atEnd && !separator && start < 0)
				start = i;
		}
		return [.. words];
	}

	/// <summary>A capital after a lower-case letter or digit (<c>eqlRule</c>), or the last capital of a run before lower case (<c>HTTPServer</c>).</summary>
	private static bool StartsWord(string name, int i) =>
		char.IsUpper(name[i])
			&& i > 0
			&& (char.IsLower(name[i - 1])
				|| char.IsDigit(name[i - 1])
				|| (char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1])));
}
