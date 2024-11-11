// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;

namespace Elastic.Markdown.Myst.Directives;

public class IncludeBlock(DirectiveBlockParser parser, Dictionary<string, string> properties, ParserContext context)
	: DirectiveBlock(parser, properties)
{
	public BuildContext Build { get; } = context.Build;

	public IFileSystem FileSystem { get; } = context.Build.ReadFileSystem;

	public IDirectoryInfo DocumentationSourcePath { get; } = context.Parser.SourcePath;

	public IFileInfo IncludeFromPath { get; } = context.Path;

	public YamlFrontMatter? FrontMatter { get; } = context.FrontMatter;

	public string? IncludePath { get; private set; }

	public bool Literal { get; protected set; }

	public string? Language { get; private set; }

	public bool Found { get; private set; }

	//TODO add all options from
	//https://mystmd.org/guide/directives#directive-include
	public override void FinalizeAndValidate(ParserContext context)
	{
		var includePath = Arguments; //todo validate
		Literal |= bool.TryParse(Properties.GetValueOrDefault("literal"), out var b) && b;
		Language = Properties.GetValueOrDefault("language");
		if (includePath is null)
		{
			//TODO emit empty error
		}
		else
		{
			var includeFrom = IncludeFromPath.Directory!.FullName;
			if (includePath.StartsWith('/'))
				includeFrom = DocumentationSourcePath.FullName;

			IncludePath = Path.Combine(includeFrom, includePath.TrimStart('/'));
			if (FileSystem.File.Exists(IncludePath))
				Found = true;
			else
			{
				//TODO emit error
			}
		}


	}
}


public class LiteralIncludeBlock : IncludeBlock
{
	public LiteralIncludeBlock(DirectiveBlockParser parser, Dictionary<string, string> properties, ParserContext context)
		: base(parser, properties, context) => Literal = true;
}
