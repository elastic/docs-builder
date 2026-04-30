// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Markdown.Myst;
using Markdig.Syntax;

namespace Elastic.Markdown.Extensions.CliReference;

public record CliCommandFile : IO.MarkdownFile
{
	private readonly CliCommandSchema _command;
	private readonly IFileInfo? _supplementalDoc;

	public CliCommandFile(
		IFileInfo sourceFile,
		IDirectoryInfo rootPath,
		MarkdownParser parser,
		BuildContext build,
		CliCommandSchema command,
		IFileInfo? supplementalDoc
	) : base(sourceFile, rootPath, parser, build)
	{
		_command = command;
		_supplementalDoc = supplementalDoc;
		Title = command.Name;
	}

	public override string NavigationTitle => $"[cmd]{_command.Name}";

	protected override Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx)
	{
		Title = _command.Name;
		var markdown = BuildMarkdown();
		return Task.FromResult(MarkdownParser.MinimalParseStringAsync(markdown, SourceFile, null));
	}

	protected override Task<MarkdownDocument> GetParseDocumentAsync(Cancel ctx)
	{
		var markdown = BuildMarkdown();
		return Task.FromResult(MarkdownParser.ParseStringAsync(markdown, SourceFile, null));
	}

	private string BuildMarkdown()
	{
		var supplemental = _supplementalDoc?.Exists == true
			? _supplementalDoc.FileSystem.File.ReadAllText(_supplementalDoc.FullName)
			: null;
		return CliMarkdownGenerator.CommandPage(_command, supplemental);
	}
}
