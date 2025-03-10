// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.CodeBlocks;


internal enum CodeBlockArgument
{
	Callouts,
	Subs
}

public record CodeBlockArguments
{
	public static bool TryParse(ReadOnlySpan<char> args, [NotNullWhen(true)] out CodeBlockArguments? codeBlockArgs)
	{
		codeBlockArgs = null;

		Dictionary<CodeBlockArgument, bool> arguments = [];

		if (args.IsWhiteSpace())
		{
			codeBlockArgs = new CodeBlockArguments([]);
			return true;
		}

		var remaining = args;
		while (!remaining.IsEmpty)
		{
			var commaIndex = remaining.IndexOf(',');
			var current = commaIndex == -1 ? remaining : remaining[..commaIndex];

			var equalIndex = current.IndexOf('=');
			if (equalIndex == -1)
			{
				var trimmed = current.Trim();
				if (Enum.TryParse<CodeBlockArgument>(trimmed, true, out var arg))
					arguments[arg] = true;
				else
					return false;
			}
			else
			{
				var key = current[..equalIndex].Trim();
				var value = current[(equalIndex + 1)..].Trim();

				if (!Enum.TryParse<CodeBlockArgument>(key, true, out var arg))
					return false;

				if (!bool.TryParse(value, out var boolValue))
					return false;

				arguments[arg] = boolValue;
			}

			if (commaIndex == -1)
				break;

			remaining = remaining[(commaIndex + 1)..];
		}

		codeBlockArgs = new CodeBlockArguments(arguments);
		return true;
	}

	public bool IsCalloutsEnabled { get; }
	public bool IsSubstitutionsEnabled { get; }

	private CodeBlockArguments(Dictionary<CodeBlockArgument, bool> arguments)
	{
		IsCalloutsEnabled = arguments.GetValueOrDefault(CodeBlockArgument.Callouts, true);
		IsSubstitutionsEnabled = arguments.GetValueOrDefault(CodeBlockArgument.Subs, false);
	}
}
