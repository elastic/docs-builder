// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO.Abstractions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration;
using Markdig.Helpers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives;

public interface IBlockTitle
{
	string Title { get; }
}

public interface IBlockAppliesTo
{
	string? AppliesToDefinition { get; }
	ApplicableTo? AppliesTo { get; }
}

public interface IBlockExtension : IBlock
{
	BuildContext Build { get; }

	bool SkipValidation { get; }

	IFileInfo CurrentFile { get; }

	int OpeningLength { get; }
}

/// <summary>
/// A block custom container.
/// </summary>
/// <seealso cref="ContainerBlock" />
/// <seealso cref="IFencedBlock" />
/// <remarks>
/// Initializes a new instance of the <see cref="DirectiveBlock"/> class.
/// </remarks>
/// <param name="parser">The parser used to create this block.</param>
/// <param name="context"></param>
public abstract class DirectiveBlock(
	DirectiveBlockParser parser,
	ParserContext context)
	: ContainerBlock(parser), IFencedBlock, IBlockExtension
{
	private Dictionary<string, string>? _properties;
	protected IReadOnlyDictionary<string, string>? Properties => _properties;

	public BuildContext Build { get; } = context.Build;

	public IFileInfo CurrentFile { get; } = context.MarkdownSourcePath;

	public bool SkipValidation { get; } = context.SkipValidation;

	/// <summary>
	/// The line number of the include directive that brought this block into the document.
	/// Null if the block is not inside an included snippet.
	/// </summary>
	protected int? IncludeLine { get; } = context.IncludeLine;

	public int OpeningLength => Directive.Length;

	public abstract string Directive { get; }

	public string? CrossReferenceName { get; protected set; }

	/// <inheritdoc />
	public char FencedChar { get; set; }

	/// <inheritdoc />
	public int OpeningFencedCharCount { get; set; }

	/// <inheritdoc />
	public StringSlice TriviaAfterFencedChar { get; set; }

	/// <inheritdoc />
	public string? Info { get; set; }

	/// <inheritdoc />
	public StringSlice UnescapedInfo { get; set; }

	/// <inheritdoc />
	public StringSlice TriviaAfterInfo { get; set; }

	/// <inheritdoc />
	public string? Arguments { get; set; }

	/// <inheritdoc />
	public StringSlice UnescapedArguments { get; set; }

	/// <inheritdoc />
	public StringSlice TriviaAfterArguments { get; set; }

	/// <inheritdoc />
	public NewLine InfoNewLine { get; set; }

	/// <inheritdoc />
	public StringSlice TriviaBeforeClosingFence { get; set; }

	/// <inheritdoc />
	public int ClosingFencedCharCount { get; set; }

	/// <summary>
	/// Allows blocks to finalize setting properties once fully parsed
	/// </summary>
	/// <param name="context"></param>
	public abstract void FinalizeAndValidate(ParserContext context);

	internal void AddProperty(string key, string value)
	{
		_properties ??= [];
		_properties[key] = value;
	}

	protected bool PropBool(params string[] keys)
	{
		if (Properties is null)
			return false;
		var value = Prop(keys);
		if (string.IsNullOrEmpty(value))
			return keys.Any(k => Properties.ContainsKey(k));

		return bool.TryParse(value, out var result) && result;
	}

	protected bool? TryPropBool(params string[] keys)
	{
		if (Properties is null)
			return null;
		var value = Prop(keys);
		if (string.IsNullOrEmpty(value))
			return keys.Any(k => Properties.ContainsKey(k)) ? true : null;

		return bool.TryParse(value, out var result) ? result : null;
	}


	protected string? Prop(params string[] keys)
	{
		if (Properties is null)
			return null;
		foreach (var key in keys)
		{
			if (Properties.TryGetValue(key, out var value))
				return value;
		}

		return default;
	}

	/// <summary>
	/// Gets a unique index based on the block's line number that accounts for include context.
	/// When the block is inside an included snippet, combines the include directive's line
	/// with the snippet line to ensure uniqueness across multiple includes and multiple blocks.
	/// </summary>
	/// <returns>A unique integer index suitable for generating HTML IDs.</returns>
	protected int GetUniqueLineIndex() =>
		IncludeLine.HasValue ? (IncludeLine.Value * 1000) + Line : Line;

	/// <summary>
	/// Additional anchors that this directive will generate during rendering.
	/// Override in directives that dynamically generate content with anchors.
	/// </summary>
	/// <remarks>
	/// These anchors are registered during parsing so that link validation
	/// can find them before the directive content is fully rendered.
	/// </remarks>
	public virtual IEnumerable<string> GeneratedAnchors => [];
}
