// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Settings;

public class SettingsBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "settings";

	public ParserContext Context { get; } = context;

	public string? IncludePath { get; private set; }

	public IFileInfo IncludeFrom { get; } = context.MarkdownSourcePath;

	public bool Found { get; private set; }


	//TODO add all options from
	//https://mystmd.org/guide/directives#directive-include
	public override void FinalizeAndValidate(ParserContext context) => ExtractInclusionPath(context);

	private void ExtractInclusionPath(ParserContext context)
	{
		var includePath = Arguments;
		if (string.IsNullOrWhiteSpace(includePath))
		{
			this.EmitError("include requires an argument.");
			return;
		}

		var includeFrom = context.MarkdownSourcePath.Directory!.FullName;
		if (includePath.StartsWith('/'))
			includeFrom = Build.DocumentationSourceDirectory.FullName;

		IncludePath = Path.Combine(includeFrom, includePath.TrimStart('/'));
		if (Build.ReadFileSystem.File.Exists(IncludePath))
			Found = true;
		else
			this.EmitError($"`{IncludePath}` does not exist.");
	}
}


