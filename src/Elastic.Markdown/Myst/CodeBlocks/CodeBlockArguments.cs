// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.CodeBlocks;

public record CodeBlockArguments
{
	public static CodeBlockArguments Default { get; } = new();
	public static string[] KnownKeys { get; } = ["callouts", "subs"];
	public static string KnownKeysString { get; } = string.Join(", ", KnownKeys);

	public bool UseCallouts { get; private set; } = true;
	public bool UseSubstitutions { get; private set; }

	private CodeBlockArguments() { }

	public static bool TryParse(ReadOnlySpan<char> args, [NotNullWhen(true)] out CodeBlockArguments? codeBlockArgs)
	{
		codeBlockArgs = null;

		if (args.IsWhiteSpace())
		{
			codeBlockArgs = Default;
			return true;
		}

		var blockArgs = new CodeBlockArguments();
		foreach (var part in args.Split(','))
		{
			var currentPart = args[part];
			if (currentPart.Contains('='))
			{
				var equalIndex = currentPart.IndexOf('=');
				var key = currentPart[..equalIndex].Trim();
				var value = currentPart[(equalIndex + 1)..].Trim();

				if (!Assign(key, blockArgs, bool.TryParse(value, out var b) ? b : null))
					return false;
			}
			else
			{
				var key = currentPart.Trim();
				if (!Assign(key, blockArgs, true))
					return false;
			}
		}

		codeBlockArgs = blockArgs;
		return true;
	}

	private static bool Assign(ReadOnlySpan<char> key, CodeBlockArguments blockArgs, bool? value = null)
	{
		switch (key)
		{
			case "callouts":
				blockArgs.UseCallouts = value ?? true;
				break;
			case "subs":
				blockArgs.UseSubstitutions = value ?? false;
				break;
			default:
				return false;
		}

		return true;
	}
}
