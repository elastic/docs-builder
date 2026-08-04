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
	private readonly string? _binaryName;
	private readonly string[] _fullPath;
	private readonly string[]? _reservedMetaCommands;
	private readonly IReadOnlyList<(string Segment, List<CliParamSchema>? Options)>? _ancestorNamespaceOptions;
	private readonly List<CliParamSchema>? _globalOptions;
	private readonly List<CliShortcutSchema>? _shortcuts;

	public CliCommandFile(
		IFileInfo sourceFile,
		IDirectoryInfo rootPath,
		MarkdownParser parser,
		BuildContext build,
		CliCommandSchema command,
		IFileInfo? supplementalDoc,
		string[]? fullPath = null,
		string? binaryName = null,
		string[]? reservedMetaCommands = null,
		IReadOnlyList<(string Segment, List<CliParamSchema>? Options)>? ancestorNamespaceOptions = null,
		List<CliParamSchema>? globalOptions = null,
		List<CliShortcutSchema>? shortcuts = null
	) : base(sourceFile, rootPath, parser, build)
	{
		_command = command;
		_supplementalDoc = supplementalDoc;
		_fullPath = fullPath ?? [command.Name];
		_binaryName = binaryName;
		_reservedMetaCommands = reservedMetaCommands;
		_ancestorNamespaceOptions = ancestorNamespaceOptions;
		_globalOptions = globalOptions;
		_shortcuts = shortcuts;
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
		var rawSupplemental = _supplementalDoc?.Exists == true
			? _supplementalDoc.FileSystem.File.ReadAllText(_supplementalDoc.FullName)
			: null;
		var supplemental = CliSupplementalDoc.Parse(rawSupplemental);
		return CliMarkdownGenerator.CommandPage(_command, supplemental, _fullPath, _binaryName, _reservedMetaCommands,
			error => Collector.EmitError(_supplementalDoc ?? SourceFile, error),
			_ancestorNamespaceOptions, _globalOptions, _shortcuts);
	}
}
