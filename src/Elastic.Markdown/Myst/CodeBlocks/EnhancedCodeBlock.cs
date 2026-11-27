// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Myst.Directives;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.CodeBlocks;

public class ApiSegment
{
	public string Header { get; set; } = "";
	public List<string> ContentLines { get; set; } = [];
	public int LineNumber { get; set; }
	public List<(string Content, int LineNumber)> ContentLinesWithNumbers { get; set; } = [];
}

public class EnhancedCodeBlock(BlockParser parser, ParserContext context) : FencedCodeBlock(parser), IBlockExtension
{
	public BuildContext Build { get; } = context.Build;

	public ParserContext Context { get; } = context;

	public IFileInfo CurrentFile { get; } = context.MarkdownSourcePath;

	public bool SkipValidation { get; } = context.SkipValidation;

	public int OpeningLength => Info?.Length ?? (0 + 3);

	public List<CallOut> CallOuts { get; set; } = [];

	public IReadOnlyCollection<CallOut> UniqueCallOuts => [.. CallOuts.DistinctBy(c => c.Index)];

	public bool InlineAnnotations { get; set; }

	public string Language { get; set; } = "plaintext";

	public string? Caption { get; set; }

	public List<ApiSegment> ApiSegments { get; set; } = [];

	/// <summary>The code lines as text, trailing blank lines dropped, with console request lines (moved to <see cref="ApiSegments"/> headers) put back.</summary>
	public IReadOnlyList<string> TextLines()
	{
		var headers = ApiSegments.Where(s => s.LineNumber > 0).ToDictionary(s => s.LineNumber - 1, s => s.Header);
		var lines = new List<string>(Lines.Count);
		for (var i = 0; i < Lines.Count; i++)
			lines.Add(headers.TryGetValue(i, out var header) ? header : Lines.Lines[i].ToString());
		while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
			lines.RemoveAt(lines.Count - 1);
		return lines;
	}
}
