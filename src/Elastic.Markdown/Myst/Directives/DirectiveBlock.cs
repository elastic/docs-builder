// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Elastic.Markdown.Diagnostics;
using Markdig.Helpers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives;

/// <summary>
/// A block custom container.
/// </summary>
/// <seealso cref="ContainerBlock" />
/// <seealso cref="IFencedBlock" />
/// <remarks>
/// Initializes a new instance of the <see cref="DirectiveBlock"/> class.
/// </remarks>
/// <param name="parser">The parser used to create this block.</param>
/// <param name="properties"></param>
/// <param name="context"></param>
public abstract class DirectiveBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: ContainerBlock(parser), IFencedBlock
{
	public IReadOnlyDictionary<string, string> Properties { get; } = properties;

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

	protected bool PropBool(params string[] keys)
	{
		var value = Prop(keys);
		if (string.IsNullOrEmpty(value))
			return keys.Any(k => Properties.ContainsKey(k));

		return bool.TryParse(value, out var result) && result;
	}

	protected string? Prop(params string[] keys)
	{
		foreach (var key in keys)
		{
			if (Properties.TryGetValue(key, out var value))
				return value;
		}

		return default;
	}

	public abstract string Directive { get; }

	protected void EmitError(ParserContext context, string message) =>
		context.EmitError(Line + 1, 1, Directive.Length + 4 , message);


}
