// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Markdown.Myst;
using Markdig.Syntax;

namespace Elastic.Markdown.Extensions.CliReference;

public record CliAliasFile : IO.MarkdownFile
{
	private readonly CliShortcutSchema _shortcut;
	private readonly string _binaryName;
	private readonly string _canonicalRelativePath;

	public CliAliasFile(
		IFileInfo sourceFile,
		IDirectoryInfo rootPath,
		MarkdownParser parser,
		BuildContext build,
		CliShortcutSchema shortcut,
		string binaryName,
		string canonicalRelativePath
	) : base(sourceFile, rootPath, parser, build)
	{
		_shortcut = shortcut;
		_binaryName = binaryName;
		_canonicalRelativePath = canonicalRelativePath;
		Title = shortcut.From;
	}

	public override string NavigationTitle => $"[alias]{_shortcut.From}";

	public override string? RedirectUrl => _canonicalRelativePath;

	protected override Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx)
	{
		Title = _shortcut.From;
		var markdown = BuildMarkdown();
		return Task.FromResult(MarkdownParser.MinimalParseStringAsync(markdown, SourceFile, null));
	}

	protected override Task<MarkdownDocument> GetParseDocumentAsync(Cancel ctx)
	{
		var markdown = BuildMarkdown();
		return Task.FromResult(MarkdownParser.ParseStringAsync(markdown, SourceFile, null));
	}

	private string BuildMarkdown() =>
		CliMarkdownGenerator.AliasPage(_shortcut, _binaryName, _canonicalRelativePath);
}
