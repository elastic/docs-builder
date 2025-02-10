// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
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

		var codeBlock = new EnhancedCodeBlock(this, context) { IndentCount = processor.Indent };

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
			(codeBlock.Info?.IndexOf("{") ?? -1) != -1
				? codeBlock.Arguments
				: codeBlock.Info
		) ?? "unknown";

		var language = codeBlock.Language;
		codeBlock.Language = language switch
		{
			"console" => "json",
			"console-response" => "json",
			"console-result" => "json",
			"terminal" => "bash",
			_ => codeBlock.Language
		};
		if (!string.IsNullOrEmpty(codeBlock.Language) && !CodeBlock.Languages.Contains(codeBlock.Language))
			codeBlock.EmitWarning($"Unknown language: {codeBlock.Language}");

		var lines = codeBlock.Lines;
		var callOutIndex = 0;

		var originatingLine = 0;
		for (var index = 0; index < lines.Lines.Length; index++)
		{
			originatingLine++;
			var line = lines.Lines[index];
			if (index == 0 && language == "console")
			{
				codeBlock.ApiCallHeader = line.ToString();
				var s = new StringSlice("");
				lines.Lines[index] = new StringLine(ref s);
				continue;
			}

			var span = line.Slice.AsSpan();

			if (span.ReplaceSubstitutions(context.FrontMatter?.Properties, out var replacement))
			{
				var s = new StringSlice(replacement);
				lines.Lines[index] = new StringLine(ref s);
				span = lines.Lines[index].Slice.AsSpan();
			}

			if (codeBlock.OpeningFencedCharCount > 3)
				continue;

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

			foreach (var callout in callouts.Values)
			{
				var line = lines.Lines[callout.Line - 1];
				var newSpan = line.Slice.AsSpan()[..callout.SliceStart];
				var s = new StringSlice(newSpan.ToString());
				lines.Lines[callout.Line - 1] = new StringLine(ref s);

			}
		}

		var inlineAnnotations = codeBlock.CallOuts?.Where(c => c.InlineCodeAnnotation).Count() ?? 0;
		var classicAnnotations = codeBlock.CallOuts?.Count - inlineAnnotations ?? 0;
		if (inlineAnnotations > 0 && classicAnnotations > 0)
			codeBlock.EmitError("Both inline and classic callouts are not supported");

		if (inlineAnnotations > 0)
			codeBlock.InlineAnnotations = true;

		return base.Close(processor, block);
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
		var startIndex = Math.Max(span.LastIndexOf("//"), span.LastIndexOf('#'));
		if (startIndex <= 0)
			return null;

		callOutIndex++;
		var callout = span.Slice(match.Index + startIndex, match.Length - startIndex);

		return new CallOut
		{
			Index = callOutIndex,
			Text = callout.TrimStart('/').TrimStart('#').TrimStart().ToString(),
			InlineCodeAnnotation = true,
			SliceStart = startIndex,
			Line = originatingLine,
		};
	}

	private static List<CallOut> ParseClassicCallOuts(ValueMatch match, ref ReadOnlySpan<char> span, ref int callOutIndex, int originatingLine)
	{
		var indexOfLastComment = Math.Max(span.LastIndexOf('#'), span.LastIndexOf("//"));
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
			var endIndex = span.Slice(match.Index + individualStartIndex).IndexOf('>') + 1;
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
}
