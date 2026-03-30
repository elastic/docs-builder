// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
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

		var trimmedPath = includePath.TrimStart('/');
		if (Path.IsPathRooted(trimmedPath))
		{
			this.EmitError("Include path must not be an absolute path.");
			return;
		}

		IncludePath = Path.GetFullPath(Path.Join(includeFrom, trimmedPath));
		IncludePathRelativeToSource = Path.GetRelativePath(Build.DocumentationSourceDirectory.FullName, IncludePath);

		var file = Build.ReadFileSystem.FileInfo.New(IncludePath);
		if (!file.IsSubPathOf(Build.DocumentationSourceDirectory))
		{
			this.EmitError("Include path must resolve within the documentation source directory.");
			Found = false;
			return;
		}

		if (Build.ReadFileSystem.File.Exists(IncludePath))
			Found = true;
		else
			this.EmitError($"`{IncludePath}` does not exist.");

		if (SymlinkValidator.ValidateFileAccess(file, Build.DocumentationSourceDirectory) is { } accessError)
		{
			this.EmitError(accessError);
			Found = false;
			return;
		}

		if (Literal)
			return;

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
