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

		foreach (var part in args.Split(','))
		{
			var currentPart = args[part];
			if (currentPart.Contains('='))
			{
				var equalIndex = currentPart.IndexOf('=');
				var key = currentPart[..equalIndex].Trim();
				var value = currentPart[(equalIndex + 1)..].Trim();

				if (!Enum.TryParse<CodeBlockArgument>(key, true, out var arg))
					return false;

				if (!bool.TryParse(value, out var boolValue))
					return false;

				arguments[arg] = boolValue;
			}
			else
			{
				var trimmed = currentPart.Trim();
				if (Enum.TryParse<CodeBlockArgument>(trimmed, true, out var arg))
					arguments[arg] = true;
				else
					return false;
			}
		}
		codeBlockArgs = new CodeBlockArguments(arguments);
		return true;
	}

	public bool UseCallouts { get; }
	public bool UseSubstitutions { get; }

	private CodeBlockArguments(Dictionary<CodeBlockArgument, bool> arguments)
	{
		UseCallouts = arguments.GetValueOrDefault(CodeBlockArgument.Callouts, true);
		UseSubstitutions = arguments.GetValueOrDefault(CodeBlockArgument.Subs, false);
	}
}
