// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.FrontMatter;

namespace Elastic.Markdown.Myst.Directives;

public class IncludeBlock(DirectiveBlockParser parser, Dictionary<string, string> properties, ParserContext context)
	: DirectiveBlock(parser, properties, context)
{
	public override string Directive => "include";

	public Func<IFileInfo, DocumentationFile?>? GetDocumentationFile { get; } = context.GetDocumentationFile;

	public ConfigurationFile Configuration { get; } = context.Configuration;

	public IFileSystem FileSystem { get; } = context.Build.ReadFileSystem;

	public IDirectoryInfo DocumentationSourcePath { get; } = context.Parser.SourcePath;

	public YamlFrontMatter? FrontMatter { get; } = context.FrontMatter;

	public string? IncludePath { get; private set; }

	public bool Found { get; private set; }

	public bool Literal { get; protected set; }
	public string? Language { get; private set; }
	public string? Caption { get; private set; }
	public string? Label { get; private set; }


	//TODO add all options from
	//https://mystmd.org/guide/directives#directive-include
	public override void FinalizeAndValidate(ParserContext context)
	{
		Literal |= PropBool("literal");
		Language = Prop("lang", "language", "code");
		Caption = Prop("caption");
		Label = Prop("label");

		ExtractInclusionPath(context);
	}

	private void ExtractInclusionPath(ParserContext context)
	{
		var includePath = Arguments;
		if (string.IsNullOrWhiteSpace(includePath))
		{
			context.EmitError(Line, Column, $"```{{{Directive}}}".Length , "include requires an argument.");
			return;
		}

		var includeFrom = context.Path.Directory!.FullName;
		if (includePath.StartsWith('/'))
			includeFrom = DocumentationSourcePath.FullName;

		IncludePath = Path.Combine(includeFrom, includePath.TrimStart('/'));
		if (FileSystem.File.Exists(IncludePath))
			Found = true;
		else
			this.EmitError($"`{IncludePath}` does not exist.");
	}
}


public class LiteralIncludeBlock : IncludeBlock
{
	public LiteralIncludeBlock(DirectiveBlockParser parser, Dictionary<string, string> properties, ParserContext context)
		: base(parser, properties, context) => Literal = true;

	public override string Directive => "literalinclude";

}
