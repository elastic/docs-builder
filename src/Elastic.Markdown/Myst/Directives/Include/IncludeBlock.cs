// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Include;

public class LiteralIncludeBlock : IncludeBlock
{
	public LiteralIncludeBlock(DirectiveBlockParser parser, ParserContext context) : base(parser, context) =>
		Literal = true;

	public override string Directive => "literalinclude";
}

public class IncludeBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "include";

	public ParserContext Context { get; } = context;

	public string? IncludePath { get; private set; }

	public string? IncludePathRelativeToSource { get; private set; }

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
			this.EmitError("include requires an argument.");
			return;
		}

		var includeFrom = context.MarkdownSourcePath.Directory!.FullName;
		if (includePath.StartsWith('/'))
			includeFrom = Build.DocumentationSourceDirectory.FullName;

		IncludePath = Path.Combine(includeFrom, includePath.TrimStart('/'));
		IncludePathRelativeToSource = Path.GetRelativePath(Build.DocumentationSourceDirectory.FullName, IncludePath);

		if (Build.ReadFileSystem.File.Exists(IncludePath))
			Found = true;
		else
			this.EmitError($"`{IncludePath}` does not exist.");

		// literal includes may point to locations other than `_snippets` since they do not
		// participate in emitting links
		if (Literal)
			return;

		var file = Build.ReadFileSystem.FileInfo.New(IncludePath);

		if (file.Directory != null && file.Directory.FullName.IndexOf("_snippets", StringComparison.Ordinal) < 0)
		{
			this.EmitError($"{{include}} only supports including snippets from `_snippet` folders. `{IncludePath}` is not a snippet");
			Found = false;
		}

		if (file.FullName == context.MarkdownSourcePath.FullName)
		{
			this.EmitError($"{{include}} cyclical include detected `{IncludePath}` points to itself");
			Found = false;
		}
	}

}
