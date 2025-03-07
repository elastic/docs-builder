// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.CodeBlocks;


internal enum CodeBlockArgument
{
	Callouts,
	Subs,
	Unknown
}

public class InvalidCodeBlockArgumentException(string message) : ArgumentException(message);

public class CodeBlockArguments
{
	public bool IsCalloutsEnabled { get; }
	public bool IsSubstitutionsEnabled { get; }

	public CodeBlockArguments() : this("") { }

	// An example of a code block argument string is "callouts=true,substitutions=false"
	public CodeBlockArguments(ReadOnlySpan<char> arguments)
	{
		var parsedArguments = ParseArgumentsString(arguments);
		IsCalloutsEnabled = parsedArguments.GetValueOrDefault(CodeBlockArgument.Callouts, true);
		IsSubstitutionsEnabled = parsedArguments.GetValueOrDefault(CodeBlockArgument.Subs, false);
	}

	private static ImmutableDictionary<CodeBlockArgument, bool> ParseArgumentsString(ReadOnlySpan<char> arguments) =>
		arguments
		.ToString()
			.Split(",")
			.Select(i => i.Split('='))
			.Where(i =>
			{
				if (string.IsNullOrEmpty(i[0]))
					return false;
				if (!Enum.TryParse<CodeBlockArgument>(i[0].Trim(), true, out _))
				{
					var validArguments = string.Join(", ",
						Enum.GetNames<CodeBlockArgument>().Where(n => n != CodeBlockArgument.Unknown.ToString()).Select(n => $"\"{n.ToLower()}\""));
					throw new InvalidCodeBlockArgumentException(
						$"Unknown code block argument \"{i[0]}\". Valid arguments are {validArguments}");
				}
				return true;
			})
			.ToDictionary(
				i => Enum.TryParse<CodeBlockArgument>(i[0], true, out var key) ? key : CodeBlockArgument.Unknown,
				i => i.Length >= 2 && bool.TryParse(i[1].Trim(), out var value) && value
			)
		.ToImmutableDictionary();
}
