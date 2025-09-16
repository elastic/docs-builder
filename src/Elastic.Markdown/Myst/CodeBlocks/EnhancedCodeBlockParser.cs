// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.AppliesTo;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.Directives.AppliesTo;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.CodeBlocks;

public class EnhancedCodeBlockParser : FencedBlockParserBase<EnhancedCodeBlock>
{
	private const string DefaultInfoPrefix = "language-";

	/// <summary>
	/// Initializes a new instance of the <see cref="FencedCodeBlockParser"/> class.
	/// </summary>
	public EnhancedCodeBlockParser()
	{
		OpeningCharacters = ['`'];
		InfoPrefix = DefaultInfoPrefix;
		InfoParser = RoundtripInfoParser;
	}

	protected override EnhancedCodeBlock CreateFencedBlock(BlockProcessor processor)
	{
		if (processor.Context is not ParserContext context)
			throw new Exception("Expected parser context to be of type ParserContext");

		var lineSpan = processor.Line.AsSpan();
		var codeBlock = lineSpan.IndexOf("{applies_to}") > -1
			? new AppliesToDirective(this, context)
			{
				IndentCount = processor.Indent
			}
			: new EnhancedCodeBlock(this, context)
			{
				IndentCount = processor.Indent
			};

		if (processor.TrackTrivia)
		{
			// mimic what internal method LinesBefore() does
			codeBlock.LinesBefore = processor.LinesBefore;
			processor.LinesBefore = null;

			codeBlock.TriviaBefore = processor.UseTrivia(processor.Start - 1);
			codeBlock.NewLine = processor.Line.NewLine;
		}

		return codeBlock;
	}

	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		var result = base.TryContinue(processor, block);
		if (result == BlockState.Continue && !processor.TrackTrivia)
		{
			var fence = (EnhancedCodeBlock)block;
			// Remove any indent spaces
			var c = processor.CurrentChar;
			var indentCount = fence.IndentCount;
			while (indentCount > 0 && c.IsSpace())
			{
				indentCount--;
				c = processor.NextChar();
			}
		}

		return result;
	}

	public override bool Close(BlockProcessor processor, Block block)
	{
		if (block is not EnhancedCodeBlock codeBlock)
			return base.Close(processor, block);

		if (processor.Context is not ParserContext context)
			throw new Exception("Expected parser context to be of type ParserContext");

		codeBlock.Language = (
			(codeBlock.Info?.IndexOf('{') ?? -1) != -1
				? codeBlock.Arguments?.Split()[0]
				: codeBlock.Info
		) ?? "unknown";

		var language = codeBlock.Language;
		codeBlock.Language = language switch
		{
			"console" => "json",
			"console-response" => "json",
			"console-result" => "json",
			"terminal" => "bash",
			"painless" => "java",
			//TODO support these natively
			"kuery" => "json",
			"lucene" => "json",
			_ => codeBlock.Language
		};
		if (!string.IsNullOrEmpty(codeBlock.Language) && !CodeBlock.Languages.Contains(codeBlock.Language))
			codeBlock.EmitWarning($"Unknown language: {codeBlock.Language}");

		var lines = codeBlock.Lines;
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (lines.Lines is null)
			return base.Close(processor, block);

		if (codeBlock is not AppliesToDirective appliesToDirective)
			ProcessCodeBlock(lines, language, codeBlock, context);
		else
			ProcessAppliesToDirective(appliesToDirective, lines);

		return base.Close(processor, block);
	}

	private static void ProcessAppliesToDirective(AppliesToDirective appliesToDirective, StringLineGroup lines)
	{
		var yaml = lines.ToSlice().AsSpan().ToString();

		try
		{
			var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(yaml);
			appliesToDirective.AppliesTo = applicableTo;
			if (appliesToDirective.AppliesTo.Diagnostics is null)
				return;
			foreach (var (severity, message) in appliesToDirective.AppliesTo.Diagnostics)
				appliesToDirective.Emit(severity, message);
			applicableTo.Diagnostics = null;
		}
		catch (Exception e)
		{
			appliesToDirective.EmitError($"Unable to parse applies_to directive: {yaml}", e);
		}
	}

	private static void ProcessCodeBlock(
		StringLineGroup lines,
		string language,
		EnhancedCodeBlock codeBlock,
		ParserContext context)
	{
		string argsString;
		if (codeBlock.Arguments == null)
			argsString = "";
		else if (codeBlock.Info?.IndexOf('{') == -1)
			argsString = codeBlock.Arguments ?? "";
		else
		{
			// if the code block starts with {code-block} and is followed by a language, we need to skip the language
			var parts = codeBlock.Arguments.Split();
			argsString = parts.Length > 1 && CodeBlock.Languages.Contains(parts[0])
				? string.Join(" ", parts[1..])
				: codeBlock.Arguments;
		}

		var codeBlockArgs = CodeBlockArguments.Default;
		if (!CodeBlockArguments.TryParse(argsString, out var codeArgs))
			codeBlock.EmitError($"Unable to parse code block arguments: {argsString}. Valid arguments are {CodeBlockArguments.KnownKeysString}.");
		else
			codeBlockArgs = codeArgs;

		// Process console blocks with multiple API segments
		if (language == "console")
		{
			ProcessConsoleCodeBlock(lines, codeBlock, codeBlockArgs, context);
			return;
		}

		var callOutIndex = 0;
		var originatingLine = 0;
		for (var index = 0; index < lines.Lines.Length; index++)
		{
			originatingLine++;
			var line = lines.Lines[index];

			var span = line.Slice.AsSpan();
			if (codeBlockArgs.UseSubstitutions)
			{
				if (span.ReplaceSubstitutions(context.YamlFrontMatter?.Properties, context.Build.Collector, out var frontMatterReplacement))
				{
					var s = new StringSlice(frontMatterReplacement);
					lines.Lines[index] = new StringLine(ref s);
					span = lines.Lines[index].Slice.AsSpan();
				}

				if (span.ReplaceSubstitutions(context.Substitutions, context.Build.Collector, out var globalReplacement))
				{
					var s = new StringSlice(globalReplacement);
					lines.Lines[index] = new StringLine(ref s);
					span = lines.Lines[index].Slice.AsSpan();
				}
			}

			if (codeBlock.OpeningFencedCharCount > 3)
				continue;

			if (codeBlockArgs.UseCallouts)
				ProcessCalloutsForLine(span, codeBlock, ref callOutIndex, originatingLine);
		}

		ProcessCalloutPostProcessing(lines, codeBlock);
		ProcessInlineAnnotations(codeBlock);
	}

	private static List<CallOut> EnumerateAnnotations(Regex.ValueMatchEnumerator matches,
		ref ReadOnlySpan<char> span,
		ref int callOutIndex,
		int originatingLine,
		bool inlineCodeAnnotation)
	{
		var callOuts = new List<CallOut>();
		foreach (var match in matches)
		{
			if (match.Length == 0)
				continue;

			if (inlineCodeAnnotation)
			{
				var callOut = ParseMagicCallout(match, ref span, ref callOutIndex, originatingLine);
				if (callOut != null)
					return [callOut];
				continue;
			}

			var classicCallOuts = ParseClassicCallOuts(match, ref span, ref callOutIndex, originatingLine);
			callOuts.AddRange(classicCallOuts);
		}

		return callOuts;
	}

	private static CallOut? ParseMagicCallout(ValueMatch match, ref ReadOnlySpan<char> span, ref int callOutIndex, int originatingLine)
	{
		var startIndex = Math.Max(span.LastIndexOf(" // "), span.LastIndexOf(" # "));
		if (startIndex <= 0)
			return null;

		callOutIndex++;
		var callout = span.Slice(match.Index + startIndex, match.Length - startIndex);

		return new CallOut
		{
			Index = callOutIndex,
			Text = callout.TrimStart().TrimStart('/').TrimStart('#').TrimStart().ToString(),
			InlineCodeAnnotation = true,
			SliceStart = startIndex,
			Line = originatingLine,
		};
	}

	private static List<CallOut> ParseClassicCallOuts(ValueMatch match, ref ReadOnlySpan<char> span, ref int callOutIndex, int originatingLine)
	{
		var indexOfLastComment = Math.Max(span.LastIndexOf(" # "), span.LastIndexOf(" // "));
		var startIndex = span.LastIndexOf('<');
		if (startIndex <= 0)
			return [];

		var allStartIndices = new List<int>();
		for (var i = 0; i < span.Length; i++)
		{
			if (span[i] == '<')
				allStartIndices.Add(i);
		}

		var callOuts = new List<CallOut>();
		foreach (var individualStartIndex in allStartIndices)
		{
			callOutIndex++;
			var endIndex = span[(match.Index + individualStartIndex)..].IndexOf('>') + 1;
			var callout = span.Slice(match.Index + individualStartIndex, endIndex);
			if (int.TryParse(callout.Trim(['<', '>']), out var index))
			{
				callOuts.Add(new CallOut
				{
					Index = index,
					Text = callout.TrimStart('/').TrimStart('#').TrimStart().ToString(),
					InlineCodeAnnotation = false,
					SliceStart = indexOfLastComment > 0 ? indexOfLastComment : startIndex,
					Line = originatingLine,
				});
			}
		}

		return callOuts;
	}

	private static void ProcessConsoleCodeBlock(
		StringLineGroup lines,
		EnhancedCodeBlock codeBlock,
		CodeBlockArguments codeBlockArgs,
		ParserContext context)
	{
		var currentSegment = new ApiSegment();
		var callOutIndex = 0;
		var originatingLine = 0;

		for (var index = 0; index < lines.Lines.Length; index++)
		{
			originatingLine++;
			var line = lines.Lines[index];
			var lineText = line.ToString();
			var span = line.Slice.AsSpan();

			// Apply substitutions if enabled
			if (codeBlockArgs.UseSubstitutions)
			{
				if (span.ReplaceSubstitutions(context.YamlFrontMatter?.Properties, context.Build.Collector, out var frontMatterReplacement))
				{
					var s = new StringSlice(frontMatterReplacement);
					lines.Lines[index] = new StringLine(ref s);
					span = lines.Lines[index].Slice.AsSpan();
					lineText = frontMatterReplacement;
				}

				if (span.ReplaceSubstitutions(context.Substitutions, context.Build.Collector, out var globalReplacement))
				{
					var s = new StringSlice(globalReplacement);
					lines.Lines[index] = new StringLine(ref s);
					span = lines.Lines[index].Slice.AsSpan();
					lineText = globalReplacement;
				}
			}

			// Check if this line is an HTTP verb (API call header)
			if (IsHttpVerb(lineText))
			{
				if (!string.IsNullOrEmpty(currentSegment.Header) || currentSegment.ContentLines.Count > 0)
					codeBlock.ApiSegments.Add(currentSegment);

				// Process callouts before creating the segment to capture them on the original line
				if (codeBlockArgs.UseCallouts && codeBlock.OpeningFencedCharCount <= 3)
					ProcessCalloutsForLine(span, codeBlock, ref callOutIndex, originatingLine);

				currentSegment = new ApiSegment
				{
					Header = lineText,
					LineNumber = originatingLine
				};

				// Clear this line from the content since it's now a header
				var s = new StringSlice("");
				lines.Lines[index] = new StringLine(ref s);
			}
			else
			{
				if (!string.IsNullOrEmpty(lineText.Trim()))
				{
					currentSegment.ContentLines.Add(lineText);
					currentSegment.ContentLinesWithNumbers.Add((lineText, originatingLine));
				}

				if (codeBlockArgs.UseCallouts && codeBlock.OpeningFencedCharCount <= 3)
					ProcessCalloutsForLine(span, codeBlock, ref callOutIndex, originatingLine);
			}
		}

		// Add the last segment if it has content
		if (!string.IsNullOrEmpty(currentSegment.Header) || currentSegment.ContentLines.Count > 0)
			codeBlock.ApiSegments.Add(currentSegment);

		ProcessCalloutPostProcessing(lines, codeBlock);
		ProcessInlineAnnotations(codeBlock);
	}

	private static bool IsHttpVerb(string line)
	{
		var trimmed = line.Trim();
		return trimmed.StartsWith("GET ", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("POST ", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("PUT ", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("PATCH ", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("HEAD ", StringComparison.OrdinalIgnoreCase) ||
			trimmed.StartsWith("OPTIONS ", StringComparison.OrdinalIgnoreCase);
	}

	private static void ProcessCalloutsForLine(ReadOnlySpan<char> span, EnhancedCodeBlock codeBlock, ref int callOutIndex, int originatingLine)
	{
		List<CallOut> callOuts = [];
		var hasClassicCallout = span.IndexOf("<") > 0 && span.LastIndexOf(">") == span.Length - 1;
		if (hasClassicCallout)
		{
			var matchClassicCallout = CallOutParser.CallOutNumber().EnumerateMatches(span);
			callOuts.AddRange(
				EnumerateAnnotations(matchClassicCallout, ref span, ref callOutIndex, originatingLine, false)
			);
		}

		// only support magic callouts for smaller line lengths
		if (callOuts.Count == 0 && span.Length < 200)
		{
			var matchInline = CallOutParser.MathInlineAnnotation().EnumerateMatches(span);
			callOuts.AddRange(
				EnumerateAnnotations(matchInline, ref span, ref callOutIndex, originatingLine, true)
			);
		}

		codeBlock.CallOuts.AddRange(callOuts);
	}

	private static void ProcessCalloutPostProcessing(StringLineGroup lines, EnhancedCodeBlock codeBlock)
	{
		//update string slices to ignore call outs
		if (codeBlock.CallOuts.Count > 0)
		{
			var callouts = codeBlock.CallOuts.Aggregate(new Dictionary<int, CallOut>(), (acc, curr) =>
			{
				if (acc.TryAdd(curr.Line, curr))
					return acc;
				if (acc[curr.Line].SliceStart > curr.SliceStart)
					acc[curr.Line] = curr;
				return acc;
			});

			// Console code blocks use ApiSegments for rendering, so we need to update headers directly
			// Note: console language gets converted to "json" for syntax highlighting
			if ((codeBlock.Language == "json" || codeBlock.Language == "console") && codeBlock.ApiSegments.Count > 0)
			{
				foreach (var callout in callouts.Values)
				{
					foreach (var segment in codeBlock.ApiSegments)
					{
						var calloutPattern = $"<{callout.Index}>";
						if (segment.Header.Contains(calloutPattern))
						{
							segment.Header = segment.Header.Replace(calloutPattern, "").Trim();
							break;
						}
					}
				}
			}
			else
			{
				foreach (var callout in callouts.Values)
				{
					var line = lines.Lines[callout.Line - 1];
					var span = line.Slice.AsSpan();

					// Skip callouts on cleared lines to avoid ArgumentOutOfRangeException
					if (span.Length == 0 || callout.SliceStart >= span.Length)
						continue;

					var newSpan = span[..callout.SliceStart];
					var s = new StringSlice(newSpan.ToString());
					lines.Lines[callout.Line - 1] = new StringLine(ref s);
				}
			}
		}
	}

	private static void ProcessInlineAnnotations(EnhancedCodeBlock codeBlock)
	{
		var inlineAnnotations = codeBlock.CallOuts.Count(c => c.InlineCodeAnnotation);
		var classicAnnotations = codeBlock.CallOuts.Count - inlineAnnotations;
		if (inlineAnnotations > 0 && classicAnnotations > 0)
			codeBlock.EmitError("Both inline and classic callouts are not supported");

		if (inlineAnnotations > 0)
			codeBlock.InlineAnnotations = true;
	}
}
