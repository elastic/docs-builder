// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.LegacyDocs.Migration.Asciidoc;

public static partial class ConditionalProcessor
{
	public static IReadOnlyList<Token> Process(IReadOnlyList<Token> tokens, IReadOnlyDictionary<string, string> attributes)
	{
		var result = new List<Token>();
		var conditionStack = new Stack<bool>();

		foreach (var token in tokens)
		{
			if (token.Type == TokenType.ConditionalStart)
			{
				var directive = token.Metadata!.BlockStyle!;
				var condition = token.Metadata.Condition!;
				var inlineContent = token.Metadata.Content;

				var isTrue = EvaluateCondition(directive, condition, attributes);

				if (!string.IsNullOrEmpty(inlineContent))
				{
					if (IsIncluding(conditionStack) && isTrue)
						result.Add(new Token(TokenType.Text, inlineContent, token.LineNumber));
				}
				else
				{
					conditionStack.Push(isTrue);
				}
				continue;
			}

			if (token.Type == TokenType.ConditionalEnd)
			{
				if (conditionStack.Count > 0)
					_ = conditionStack.Pop();
				continue;
			}

			if (IsIncluding(conditionStack))
				result.Add(token);
		}

		return result;
	}

	private static bool IsIncluding(Stack<bool> stack)
	{
		foreach (var condition in stack)
		{
			if (!condition)
				return false;
		}
		return true;
	}

	private static bool EvaluateCondition(string directive, string condition, IReadOnlyDictionary<string, string> attributes) =>
		directive.ToLowerInvariant() switch
		{
			"ifdef" => EvaluateIfdef(condition, attributes),
			"ifndef" => EvaluateIfndef(condition, attributes),
			"ifeval" => EvaluateIfeval(condition, attributes),
			_ => true
		};

	private static bool EvaluateIfdef(string condition, IReadOnlyDictionary<string, string> attributes)
	{
		if (condition.Contains('+'))
			return condition.Split('+').All(attr => attributes.ContainsKey(attr.Trim()));

		if (condition.Contains(','))
			return condition.Split(',').Any(attr => attributes.ContainsKey(attr.Trim()));

		return attributes.ContainsKey(condition.Trim());
	}

	private static bool EvaluateIfndef(string condition, IReadOnlyDictionary<string, string> attributes)
	{
		if (condition.Contains('+'))
			return condition.Split('+').All(attr => !attributes.ContainsKey(attr.Trim()));

		if (condition.Contains(','))
			return condition.Split(',').Any(attr => !attributes.ContainsKey(attr.Trim()));

		return !attributes.ContainsKey(condition.Trim());
	}

	private static bool EvaluateIfeval(string condition, IReadOnlyDictionary<string, string> attributes)
	{
		var resolved = SubstituteAttributes(condition, attributes);
		var match = IfevalRegex().Match(resolved);
		if (!match.Success)
			return false;

		var left = UnquoteValue(match.Groups[1].Value.Trim());
		var op = match.Groups[2].Value.Trim();
		var right = UnquoteValue(match.Groups[3].Value.Trim());

		if (double.TryParse(left, out var leftNum) && double.TryParse(right, out var rightNum))
		{
			return op switch
			{
				"==" => Math.Abs(leftNum - rightNum) < 0.0001,
				"!=" => Math.Abs(leftNum - rightNum) >= 0.0001,
				"<" => leftNum < rightNum,
				">" => leftNum > rightNum,
				"<=" => leftNum <= rightNum,
				">=" => leftNum >= rightNum,
				_ => false
			};
		}

		return op switch
		{
			"==" => string.Equals(left, right, StringComparison.Ordinal),
			"!=" => !string.Equals(left, right, StringComparison.Ordinal),
			"<" => string.Compare(left, right, StringComparison.Ordinal) < 0,
			">" => string.Compare(left, right, StringComparison.Ordinal) > 0,
			"<=" => string.Compare(left, right, StringComparison.Ordinal) <= 0,
			">=" => string.Compare(left, right, StringComparison.Ordinal) >= 0,
			_ => false
		};
	}

	private static string SubstituteAttributes(string text, IReadOnlyDictionary<string, string> attributes) =>
		AttrRefRegex().Replace(text, match =>
		{
			var name = match.Groups[1].Value;
			return attributes.TryGetValue(name, out var value) ? value : match.Value;
		});

	private static string UnquoteValue(string value)
	{
		if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
			return value[1..^1];
		return value;
	}

	[GeneratedRegex(@"\{([a-zA-Z0-9_-]+)\}")]
	private static partial Regex AttrRefRegex();

	[GeneratedRegex(@"(.+?)\s*(==|!=|<=|>=|<|>)\s*(.+)")]
	private static partial Regex IfevalRegex();
}
