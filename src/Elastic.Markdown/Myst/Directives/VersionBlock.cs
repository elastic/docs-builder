// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Markdown.Diagnostics;
using static System.StringSplitOptions;

namespace Elastic.Markdown.Myst.Directives;

public class VersionBlock(DirectiveBlockParser parser, string directive, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => directive;
	public string Class => directive.Replace("version", "");
	public SemVersion? Version { get; private set; }

	public string Title { get; private set; } = string.Empty;

	public override void FinalizeAndValidate(ParserContext context)
	{
		var tokens = Arguments?.Split(" ", 2, RemoveEmptyEntries) ?? [];
		if (tokens.Length < 1)
		{
			this.EmitError($"{directive} needs exactly 2 arguments: <version> <title>");
			return;
		}

		if (!SemVersion.TryParse(tokens[0], out var version))
		{
			var numbers = tokens[0].Split('.', RemoveEmptyEntries);
			if (numbers.Length != 2 || !SemVersion.TryParse($"{numbers[0]}.{numbers[1]}.0", out version))
				this.EmitError($"'{tokens[0]}' is not a valid version");
		}

		Version = version;
		var title = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(directive.Replace("version", "version "));
		title += $" ({Version})";
		if (tokens.Length > 1 && !string.IsNullOrWhiteSpace(tokens[1]))
			title += $": {tokens[1]}";
		Title = title;
	}
}
