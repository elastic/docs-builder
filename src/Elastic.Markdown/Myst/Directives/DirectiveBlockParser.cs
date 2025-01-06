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

	private readonly string[] _admonitions = ["important", "warning", "note", "tip"];

	private readonly string[] _versionBlocks = ["versionadded", "versionchanged", "versionremoved", "deprecated"];

	private readonly string[] _codeBlocks = ["code", "code-block", "sourcecode"];

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
		{ "caution", 3 },
		{ "danger", 3 },
		{ "error", 3 },
		{ "hint", 3 },
		{ "seealso", 3 }
	}.ToFrozenDictionary();

	protected override DirectiveBlock CreateFencedBlock(BlockProcessor processor)
	{
		var info = processor.Line.AsSpan();

		if (processor.Context is not ParserContext context)
			throw new Exception("Expected parser context to be of type ParserContext");

		// TODO alternate lookup .NET 9
		var directive = info.ToString().Trim(['{', '}', '`']);
		if (_unsupportedBlocks.TryGetValue(directive, out var issueId))
			return new UnsupportedDirectiveBlock(this, directive, issueId, context);

		if (info.IndexOf("{tab-set}") > 0)
			return new TabSetBlock(this, context);

		if (info.IndexOf("{tab-item}") > 0)
			return new TabItemBlock(this, context);

		if (info.IndexOf("{dropdown}") > 0)
			return new DropdownBlock(this, context);

		if (info.IndexOf("{image}") > 0)
			return new ImageBlock(this, context);

		if (info.IndexOf("{figure}") > 0)
			return new FigureBlock(this, context);

		if (info.IndexOf("{figure-md}") > 0)
			return new FigureBlock(this, context);

		// this is currently listed as unsupported
		// leaving the parsing in until we are confident we don't want this
		// for dev-docs
		if (info.IndexOf("{mermaid}") > 0)
			return new MermaidBlock(this, context);

		if (info.IndexOf("{include}") > 0)
			return new IncludeBlock(this, context);

		if (info.IndexOf("{literalinclude}") > 0)
			return new LiteralIncludeBlock(this, context);

		if (info.IndexOf("{applies}") > 0)
			return new AppliesBlock(this, context);

		if (info.IndexOf("{settings}") > 0)
			return new SettingsBlock(this, context);

		foreach (var admonition in _admonitions)
		{
			if (info.IndexOf($"{{{admonition}}}") > 0)
				return new AdmonitionBlock(this, admonition, context);
		}

		foreach (var version in _versionBlocks)
		{
			if (info.IndexOf($"{{{version}}}") > 0)
				return new VersionBlock(this, version, context);
		}

		return new UnknownDirectiveBlock(this, info.ToString(), context);
	}

	public override bool Close(BlockProcessor processor, Block block)
	{
		if (block is DirectiveBlock directiveBlock)
			directiveBlock.FinalizeAndValidate(processor.GetContext());

		return base.Close(processor, block);
	}

	public override BlockState TryOpen(BlockProcessor processor)
	{
		if (processor.Context is not ParserContext)
			throw new Exception("Expected parser context to be of type ParserContext");

		// We expect no indentation for a fenced code block.
		if (processor.IsCodeIndent)
			return BlockState.None;

		var line = processor.Line;

		foreach (var code in _codeBlocks)
		{
			if (line.IndexOf($"{{{code}}}") > 0)
				return BlockState.None;
		}

		if (line.IndexOf("{") == -1)
			return BlockState.None;

		return base.TryOpen(processor);
	}

	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		var line = processor.Line.AsSpan();

		if (!line.StartsWith(":"))
			return base.TryContinue(processor, block);

		if (block is not DirectiveBlock directiveBlock)
			return base.TryContinue(processor, block);

		var tokens = line.ToString().Split(':', 3, RemoveEmptyEntries | TrimEntries);
		if (tokens.Length < 1)
			return base.TryContinue(processor, block);

		var name = tokens[0];
		var data = tokens.Length > 1 ? string.Join(":", tokens[1..]) : string.Empty;
		directiveBlock.AddProperty(name, data);

		return BlockState.Continue;

	}
}
