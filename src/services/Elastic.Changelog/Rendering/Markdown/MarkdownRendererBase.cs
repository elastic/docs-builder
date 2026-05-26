// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.ReleaseNotes;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Options for rendering PR and issue links
/// </summary>
public record PrIssueLinkOptions(
	ChangelogEntry Entry,
	string Repo,
	string Owner,
	bool HideLinks,
	bool IndentForListItem = false
);

/// <summary>
/// Abstract base class for changelog markdown renderers
/// </summary>
public abstract class MarkdownRendererBase(ScopedFileSystem fileSystem) : IChangelogMarkdownRenderer
{
	protected ScopedFileSystem FileSystem { get; } = fileSystem;

	/// <inheritdoc />
	public abstract string OutputFileName { get; }

	/// <inheritdoc />
	public abstract Task RenderAsync(ChangelogRenderContext context, Cancel ctx);

	/// <summary>
	/// Writes the output file to the specified directory
	/// </summary>
	protected async Task WriteOutputFileAsync(string outputDir, string titleSlug, string content, Cancel ctx)
	{
		var outputPath = FileSystem.Path.Join(outputDir, titleSlug, OutputFileName);
		var outputDirectory = FileSystem.Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrWhiteSpace(outputDirectory) && !FileSystem.Directory.Exists(outputDirectory))
			_ = FileSystem.Directory.CreateDirectory(outputDirectory);

		await FileSystem.File.WriteAllTextAsync(outputPath, content, Encoding.UTF8, ctx);
	}

	/// <summary>
	/// Renders PR and issue links with configurable formatting options
	/// </summary>
	protected static void RenderPrIssueLinks(StringBuilder sb, PrIssueLinkOptions options)
	{
		var prParts = new List<string>();
		foreach (var pr in options.Entry.Prs ?? [])
		{
			var s = ChangelogTextUtilities.FormatPrLink(pr, options.Repo, options.HideLinks, options.Owner);
			if (!string.IsNullOrEmpty(s))
				prParts.Add(s);
		}

		var issueParts = new List<string>();
		foreach (var issue in options.Entry.Issues ?? [])
		{
			var s = ChangelogTextUtilities.FormatIssueLink(issue, options.Repo, options.HideLinks, options.Owner);
			if (!string.IsNullOrEmpty(s))
				issueParts.Add(s);
		}

		if (prParts.Count == 0 && issueParts.Count == 0)
			return;

		if (options.HideLinks)
		{
			foreach (var s in prParts)
			{
				var line = options.IndentForListItem ? ChangelogTextUtilities.Indent(s) : s;
				_ = sb.AppendLine(line);
			}
			foreach (var s in issueParts)
			{
				var line = options.IndentForListItem ? ChangelogTextUtilities.Indent(s) : s;
				_ = sb.AppendLine(line);
			}

			var infoLine = "For more information, check the pull request or issue above.";
			_ = sb.AppendLine(options.IndentForListItem ? ChangelogTextUtilities.Indent(infoLine) : infoLine);
		}
		else
		{
			var lineParts = new List<string> { "For more information, check" };
			lineParts.AddRange(prParts);
			lineParts.AddRange(issueParts);

			var fullLine = string.Join(" ", lineParts) + ".";
			_ = sb.AppendLine(options.IndentForListItem ? ChangelogTextUtilities.Indent(fullLine) : fullLine);
		}

		_ = sb.AppendLine();
	}
}
