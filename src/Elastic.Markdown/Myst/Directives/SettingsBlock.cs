// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.Myst.FrontMatter;

namespace Elastic.Markdown.Myst.Directives;

public class SettingsBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "settings";

	public Func<IFileInfo, DocumentationFile?> GetDocumentationFile { get; } = context.GetDocumentationFile;

	public ConfigurationFile Configuration { get; } = context.Configuration;

	public ICrossLinkResolver LinksResolver { get; } = context.LinksResolver;

	public IFileSystem FileSystem { get; } = context.Build.ReadFileSystem;

	public IFileInfo IncludeFrom { get; } = context.CurrentPath;

	public IDirectoryInfo DocumentationSourcePath { get; } = context.Parser.SourcePath;

	public YamlFrontMatter? FrontMatter { get; } = context.FrontMatter;

	public string? IncludePath { get; private set; }

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

		var includeFrom = context.CurrentPath.Directory!.FullName;
		if (includePath.StartsWith('/'))
			includeFrom = DocumentationSourcePath.FullName;

		IncludePath = Path.Combine(includeFrom, includePath.TrimStart('/'));
		if (FileSystem.File.Exists(IncludePath))
			Found = true;
		else
			this.EmitError($"`{IncludePath}` does not exist.");
	}
}


