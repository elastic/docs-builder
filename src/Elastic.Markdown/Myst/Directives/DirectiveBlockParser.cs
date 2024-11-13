// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Frozen;
using Markdig.Parsers;
using Markdig.Syntax;
using static System.StringSplitOptions;

namespace Elastic.Markdown.Myst.Directives;

/// <summary>
/// The block parser for a <see cref="DirectiveBlock"/>.
/// </summary>
/// <seealso cref="FencedBlockParserBase{CustomContainer}" />
public class DirectiveBlockParser : FencedBlockParserBase<DirectiveBlock>
{
	/// <summary>
    /// Initializes a new instance of the <see cref="DirectiveBlockParser"/> class.
    /// </summary>
    public DirectiveBlockParser()
    {
        OpeningCharacters = [':', '`'];
        // We don't need a prefix
        InfoPrefix = null;
    }

	private Dictionary<string, string> _admonitionData = new();

	private readonly string[] _admonitions = [ "attention", "caution", "note", "tip" ];

	private readonly string[] _versionBlocks = [ "versionadded", "versionchanged", "versionremoved", "deprecated" ];

	private readonly string[] _codeBlocks = [ "code", "code-block", "sourcecode"];

	private readonly FrozenDictionary<string, int> _unsupportedBlocks = new Dictionary<string, int>
	{
		{ "bibliography", 5 },
		{ "blockquote", 6 },
		{ "csv-table", 9 },
		{ "iframe", 14 },
		{ "list-table", 17 },
		{ "myst", 22 },
		{ "topic", 24 },
		{ "exercise", 30 },
		{ "solution", 31 },
		{ "toctree", 32 },
		{ "grid", 26 },
		{ "grid-item-card", 26 },
		{ "card", 25 },
		{ "mermaid", 20 },
		{ "aside", 4 },
		{ "margin", 4 },
		{ "sidebar", 4 },
		{ "code-cell", 8 },

		{ "admonition", 3 },
		{ "attention", 3 },
		{ "danger", 3 },
		{ "error", 3 },
		{ "hint", 3 },
		{ "important", 3 },
		{ "seealso", 3 }
	}.ToFrozenDictionary();

    protected override DirectiveBlock CreateFencedBlock(BlockProcessor processor)
    {
	    _admonitionData = new Dictionary<string, string>();
	    var info = processor.Line.AsSpan();

		if (processor.Context is not ParserContext context)
			throw new Exception("Expected parser context to be of type ParserContext");

	    if (info.IndexOf("{") == -1)
		    return new CodeBlock(this, "raw", _admonitionData);

	    // TODO alternate lookup .NET 9
	    var directive = info.ToString().Trim(['{', '}', '`']);
	    if (_unsupportedBlocks.TryGetValue(directive, out var issueId))
		    return new UnsupportedDirectiveBlock(this, directive, _admonitionData, issueId);

	    if (info.IndexOf("{tab-set}") > 0)
		    return new TabSetBlock(this, _admonitionData);

	    if (info.IndexOf("{tab-item}") > 0)
		    return new TabItemBlock(this, _admonitionData);

	    if (info.IndexOf("{dropdown}") > 0)
		    return new DropdownBlock(this, _admonitionData);

	    if (info.IndexOf("{image}") > 0)
		    return new ImageBlock(this, _admonitionData, context);

	    if (info.IndexOf("{figure}") > 0)
		    return new FigureBlock(this, _admonitionData, context);

	    if (info.IndexOf("{figure-md}") > 0)
		    return new FigureBlock(this, _admonitionData, context);

	    // this is currently listed as unsupported
	    // leaving the parsing in until we are confident we don't want this
	    // for dev-docs
	    if (info.IndexOf("{mermaid}") > 0)
		    return new MermaidBlock(this, _admonitionData);

	    if (info.IndexOf("{include}") > 0)
			return new IncludeBlock(this, _admonitionData, context);

	    if (info.IndexOf("{literalinclude}") > 0)
			return new LiteralIncludeBlock(this, _admonitionData, context);

	    foreach (var admonition in _admonitions)
	    {
		    if (info.IndexOf($"{{{admonition}}}") > 0)
			    return new AdmonitionBlock(this, admonition, _admonitionData);
	    }

	    foreach (var version in _versionBlocks)
	    {
		    if (info.IndexOf($"{{{version}}}") > 0)
			    return new VersionBlock(this, version, _admonitionData);
	    }

	    foreach (var code in _codeBlocks)
	    {
		    if (info.IndexOf($"{{{code}}}") > 0)
			    return new CodeBlock(this, code, _admonitionData);
	    }
	    return new UnknownDirectiveBlock(this, info.ToString(), _admonitionData);
    }

    public override bool Close(BlockProcessor processor, Block block)
    {
	    if (block is DirectiveBlock directiveBlock)
		    directiveBlock.FinalizeAndValidate(processor.GetContext());

		return base.Close(processor, block);
    }

    public override BlockState TryContinue(BlockProcessor processor, Block block)
    {
	    var line = processor.Line.AsSpan();

	    // TODO only parse this if no content proceeds it (and not in a code fence)
	    if (line.StartsWith(":"))
	    {
		    var tokens = line.ToString().Split(':', 3, RemoveEmptyEntries | TrimEntries);
		    if (tokens.Length < 1)
				return base.TryContinue(processor, block);

		    var name = tokens[0];
		    var data = tokens.Length > 1 ? string.Join(":", tokens[1..]) : string.Empty;
		    _admonitionData[name] = data;
		    return BlockState.Continue;
	    }

	    return base.TryContinue(processor, block);
    }
}
