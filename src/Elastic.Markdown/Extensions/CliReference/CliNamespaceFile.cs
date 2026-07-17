// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Markdown.Myst;
using Markdig.Syntax;

namespace Elastic.Markdown.Extensions.CliReference;

public record CliNamespaceFile : IO.MarkdownFile
{
	private readonly CliNamespaceSchema _namespace;
	private readonly IFileInfo? _supplementalDoc;
	private readonly string? _binaryName;
	private readonly string[] _fullPath;
	private readonly string[]? _reservedMetaCommands;
	private readonly List<CliShortcutSchema>? _shortcuts;

	public CliNamespaceFile(
		IFileInfo sourceFile,
		IDirectoryInfo rootPath,
		MarkdownParser parser,
		BuildContext build,
		CliNamespaceSchema @namespace,
		IFileInfo? supplementalDoc,
		string[]? fullPath = null,
		string? binaryName = null,
		string[]? reservedMetaCommands = null,
		List<CliShortcutSchema>? shortcuts = null,
		ApplicableTo? appliesTo = null
	) : base(sourceFile, rootPath, parser, build)
	{
		_namespace = @namespace;
		_supplementalDoc = supplementalDoc;
		_fullPath = fullPath ?? [@namespace.Segment];
		_binaryName = binaryName;
		_reservedMetaCommands = reservedMetaCommands;
		_shortcuts = shortcuts;
		Title = @namespace.Segment;
		FallbackAppliesTo = appliesTo;
	}

	public override string NavigationTitle => $"[ns]{_namespace.Segment}";

	protected override Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx)
	{
		Title = _namespace.Segment;
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
		var body = CliMarkdownGenerator.NamespacePage(_namespace, supplemental, _fullPath, _binaryName, _reservedMetaCommands,
			error => Collector.EmitError(_supplementalDoc ?? SourceFile, error), _shortcuts);
		// Prepend supplemental front matter so applies_to (or any other field) in ns-*.md overrides the fallback
		return supplemental?.FrontMatter is { } fm ? $"{fm}\n\n{body}" : body;
	}
}
