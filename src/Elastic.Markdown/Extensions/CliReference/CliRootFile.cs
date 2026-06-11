// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Markdown.Myst;
using Markdig.Syntax;

namespace Elastic.Markdown.Extensions.CliReference;

public record CliRootFile : IO.MarkdownFile
{
	private readonly CliSchema _schema;
	private readonly IFileInfo? _supplementalDoc;
	private readonly string _title;
	private readonly string _navigationTitle;

	public CliRootFile(
		IFileInfo sourceFile,
		IDirectoryInfo rootPath,
		MarkdownParser parser,
		BuildContext build,
		CliSchema schema,
		IFileInfo? supplementalDoc,
		string? title = null,
		string? navigationTitle = null
	) : base(sourceFile, rootPath, parser, build)
	{
		_schema = schema;
		_supplementalDoc = supplementalDoc;
		_title = string.IsNullOrWhiteSpace(title) ? schema.Name : title.Trim();
		_navigationTitle = string.IsNullOrWhiteSpace(navigationTitle) ? $"{schema.Name} CLI" : navigationTitle.Trim();
		Title = _title;
	}

	public override string NavigationTitle => _navigationTitle;

	protected override Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx)
	{
		Title = _title;
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
		return CliMarkdownGenerator.RootPage(_schema, supplemental, _title);
	}
}
