// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Myst;
using Markdig.Syntax;

namespace Elastic.Markdown.Extensions.Listing;

/// <summary>
/// A listing root or group index page. When a real <c>index.md</c> exists on disk its content is
/// preserved and the <c>{listing}</c> directive is appended. When no file exists a minimal synthetic
/// page is generated from the folder name.
/// </summary>
public record ListingIndexFile : IO.MarkdownFile
{
	public ListingIndexFile(IFileInfo sourceFile, IDirectoryInfo rootPath, MarkdownParser parser, BuildContext build) : base(
			sourceFile,
			rootPath,
			parser,
			build
		)
	{ }

	protected override Task<MarkdownDocument> GetMinimalParseDocumentAsync(Cancel ctx)
	{
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
		string body;
		if (SourceFile.Exists)
		{
			body = SourceFile.FileSystem.File.ReadAllText(SourceFile.FullName);
		}
		else
		{
			// Generate a minimal title from the folder name
			var folderName = SourceFile.Directory?.Name ?? "Index";
			var title = HumanizeFolderName(folderName);
			body = $"# {title}\n";
		}

		// Append the listing directive
		return body.TrimEnd() + "\n\n:::{listing}\n:::\n";
	}

	private static string HumanizeFolderName(string name)
	{
		// "detection-rules" → "Detection Rules", "rfcs" → "Rfcs"
		var words = name.Replace('_', '-').Split('-');
		return string.Join(" ", words.Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));
	}
}
