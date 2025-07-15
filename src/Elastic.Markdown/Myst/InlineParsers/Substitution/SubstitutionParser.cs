// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Diagnostics;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using NetEscapades.EnumGenerators;

namespace Elastic.Markdown.Myst.InlineParsers.Substitution;

[DebuggerDisplay("{GetType().Name} Line: {Line}, Found: {Found}, Replacement: {Replacement}")]
public class SubstitutionLeaf(string content, bool found, string replacement)
	: CodeInline(content)
{
	public bool Found { get; } = found;
	public string Replacement { get; } = replacement;
	public IReadOnlyCollection<SubstitutionMutation>? Mutations { get; set; }
}

[EnumExtensions]
public enum SubstitutionMutation
{
	[Display(Name = "M")] MajorComponent,
	[Display(Name = "M.x")] MajorX,
	[Display(Name = "M.M")] MajorMinor,
	[Display(Name = "M+1")] IncreaseMajor,
	[Display(Name = "M.M+1")] IncreaseMinor,
	[Display(Name = "lc")] LowerCase,
	[Display(Name = "uc")] UpperCase,
	[Display(Name = "tc")] TitleCase,
	[Display(Name = "c")] Capitalize,
	[Display(Name = "kc")] KebabCase,
	[Display(Name = "sc")] SnakeCase,
	[Display(Name = "cc")] CamelCase,
	[Display(Name = "pc")] PascalCase,
	[Display(Name = "trim")] Trim
}

public class SubstitutionRenderer : HtmlObjectRenderer<SubstitutionLeaf>
{
	protected override void Write(HtmlRenderer renderer, SubstitutionLeaf leaf)
	{
		if (!leaf.Found)
		{
			_ = renderer.Write(leaf.Content);
			return;
		}

		var replacement = leaf.Replacement;
		if (leaf.Mutations is null or { Count: 0 })
		{
			_ = renderer.Write(replacement);
			return;
		}

		foreach (var mutation in leaf.Mutations)
		{
			var (success, update) = mutation switch
			{
				SubstitutionMutation.MajorComponent => TryGetVersion(replacement, v => $"{v.Major}"),
				SubstitutionMutation.MajorX => TryGetVersion(replacement, v => $"{v.Major}.x"),
				SubstitutionMutation.MajorMinor => TryGetVersion(replacement, v => $"{v.Major}.{v.Minor}"),
				SubstitutionMutation.IncreaseMajor => TryGetVersion(replacement, v => $"{v.Major + 1}.0.0"),
				SubstitutionMutation.IncreaseMinor => TryGetVersion(replacement, v => $"{v.Major}.{v.Minor + 1}.0"),
				SubstitutionMutation.LowerCase => (true, replacement.ToLowerInvariant()),
				SubstitutionMutation.UpperCase => (true, replacement.ToUpperInvariant()),
				SubstitutionMutation.Capitalize => (true, Capitalize(replacement)),
				SubstitutionMutation.KebabCase => (true, ToKebabCase(replacement)),
				SubstitutionMutation.CamelCase => (true, ToCamelCase(replacement)),
				SubstitutionMutation.PascalCase => (true, ToPascalCase(replacement)),
				SubstitutionMutation.SnakeCase => (true, ToSnakeCase(replacement)),
				SubstitutionMutation.TitleCase => (true, TitleCase(replacement)),
				SubstitutionMutation.Trim => (true, Trim(replacement)),
				_ => throw new Exception($"encountered an unknown mutation '{mutation.ToStringFast(true)}'")
			};
			if (!success)
			{
				_ = renderer.Write(leaf.Content);
				return;
			}
			replacement = update;
		}
		_ = renderer.Write(replacement);
	}

	private static string ToCamelCase(string str) => JsonNamingPolicy.CamelCase.ConvertName(str.Replace(" ", string.Empty));
	private static string ToSnakeCase(string str) => JsonNamingPolicy.SnakeCaseLower.ConvertName(str).Replace(" ", string.Empty);
	private static string ToKebabCase(string str) => JsonNamingPolicy.KebabCaseLower.ConvertName(str).Replace(" ", string.Empty);
	private static string ToPascalCase(string str) => TitleCase(str).Replace(" ", string.Empty);

	private static string TitleCase(string str) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str);

	private static string Trim(string str) =>
		str.AsSpan().Trim(['!', ' ', '\t', '\r', '\n', '.', ',', ')', '(', ':', ';', '<', '>', '[', ']']).ToString();

	private static string Capitalize(string input) =>
		input switch
		{
			null => string.Empty,
			"" => string.Empty,
			_ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
		};

	private (bool, string) TryGetVersion(string version, Func<SemVersion, string> mutate)
	{
		if (!SemVersion.TryParse(version, out var v) && !SemVersion.TryParse(version + ".0", out v))
			return (false, string.Empty);

		return (true, mutate(v));
	}
}

public class SubstitutionParser : InlineParser
{
	public SubstitutionParser() => OpeningCharacters = ['{'];

	private readonly SearchValues<char> _values = SearchValues.Create(['\r', '\n', '\t', '}']);

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var match = slice.CurrentChar;
		if (slice.PeekCharExtra(1) != match)
			return false;

		if (processor.Context is not ParserContext context)
			return false;

		Debug.Assert(match is not ('\r' or '\n'));

		// Match the opened sticks
		var openSticks = slice.CountAndSkipChar(match);

		var span = slice.AsSpan();

		var i = span.IndexOfAny(_values);

		if ((uint)i >= (uint)span.Length)
		{
			// We got to the end of the input before seeing the match character.
			return false;
		}

		var closeSticks = 0;

		while ((uint)i < (uint)span.Length && span[i] == '}')
		{
			closeSticks++;
			i++;
		}

		span = span[i..];

		if (closeSticks != 2)
			return false;

		var rawContent = slice.AsSpan()[..(slice.Length - span.Length)];

		var content = new LazySubstring(slice.Text, slice.Start, rawContent.Length);

		var startPosition = slice.Start;
		slice.Start = startPosition + rawContent.Length;

		// We've already skipped the opening sticks. Account for that here.
		startPosition -= openSticks;
		startPosition = Math.Max(startPosition, 0);

		var key = content.ToString().Trim(['{', '}']).Trim().ToLowerInvariant();
		var found = false;
		var replacement = string.Empty;
		var components = key.Split('|');
		if (components.Length > 1)
			key = components[0].Trim(['{', '}']).Trim().ToLowerInvariant();

		if (context.Substitutions.TryGetValue(key, out var value))
		{
			found = true;
			replacement = value;
		}
		else if (context.ContextSubstitutions.TryGetValue(key, out value))
		{
			found = true;
			replacement = value;
		}
		if (found)
			context.Build.Collector.CollectUsedSubstitutionKey(key);

		var start = processor.GetSourcePosition(startPosition, out var line, out var column);
		var end = processor.GetSourcePosition(slice.Start);
		var sourceSpan = new SourceSpan(start, end);
		var substitutionLeaf = new SubstitutionLeaf(content.ToString(), found, replacement)
		{
			Delimiter = '{',
			Span = sourceSpan,
			Line = line,
			Column = column,
			DelimiterCount = openSticks
		};

		if (!found)
			// We temporarily diagnose variable spaces as hints. We used to not read this at all.
			processor.Emit(key.Contains(' ') ? Severity.Error : Severity.Hint, line + 1, column + 3, substitutionLeaf.Span.Length - 3, $"Substitution key {{{key}}} is undefined");
		else
		{
			List<SubstitutionMutation>? mutations = null;
			if (components.Length >= 10)
				processor.EmitError(line + 1, column + 3, substitutionLeaf.Span.Length - 3, $"Substitution key {{{key}}} defines too many mutations, none will be applied");
			else if (components.Length > 1)
			{
				foreach (var c in components[1..])
				{
					if (SubstitutionMutationExtensions.TryParse(c.Trim(), out var mutation, true, true))
					{
						mutations ??= [];
						mutations.Add(mutation);
					}
					else
						processor.EmitError(line + 1, column + 3, substitutionLeaf.Span.Length - 3, $"Mutation '{c}' on {{{key}}} is undefined");
				}
			}

			substitutionLeaf.Mutations = mutations;
		}


		if (processor.TrackTrivia)
		{
			// startPosition and slice.Start include the opening/closing sticks.
			substitutionLeaf.ContentWithTrivia =
				new StringSlice(slice.Text, startPosition + openSticks, slice.Start - openSticks - 1);
		}

		processor.Inline = substitutionLeaf;
		return true;
	}
}
