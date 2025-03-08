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

public class InvalidCodeBlockArgumentException(string message) : ArgumentException(message);

public class CodeBlockArguments
{
	public static bool TryParse(string args, [NotNullWhen(true)] out CodeBlockArguments? codeBlockArgs)
	{
		codeBlockArgs = null;

		Dictionary<CodeBlockArgument, bool> arguments = [];

		if (string.IsNullOrWhiteSpace(args))
		{
			codeBlockArgs = new CodeBlockArguments(ImmutableDictionary<CodeBlockArgument, bool>.Empty);
			return true;
		}

		foreach (var i in args.Split(","))
		{
			var parts = i.Split("=");
			switch (parts.Length)
			{
				case 1 when Enum.TryParse<CodeBlockArgument>(parts[0].Trim(), true, out var arg):
					arguments[arg] = true;
					break;
				case 2 when Enum.TryParse<CodeBlockArgument>(parts[0].Trim(), true, out var arg):
					{
						if (bool.TryParse(parts[1].Trim(), out var value))
							arguments[arg] = value;
						else
							return false;
						break;
					}
				default:
					return false;
			}
		}

		codeBlockArgs = new CodeBlockArguments(arguments.ToImmutableDictionary());
		return true;
	}

	public bool IsCalloutsEnabled { get; }
	public bool IsSubstitutionsEnabled { get; }

	private CodeBlockArguments(ImmutableDictionary<CodeBlockArgument, bool> arguments)
	{
		IsCalloutsEnabled = arguments.GetValueOrDefault(CodeBlockArgument.Callouts, true);
		IsSubstitutionsEnabled = arguments.GetValueOrDefault(CodeBlockArgument.Subs, false);
	}
}
