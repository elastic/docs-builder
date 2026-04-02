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
	/// Renders PR and issue links for dropdown entries
	/// </summary>
	protected static void RenderPrIssueLinks(StringBuilder sb, ChangelogEntry entry, string entryRepo, string entryOwner, bool entryHideLinks)
	{
		var prParts = new List<string>();
		foreach (var pr in entry.Prs ?? [])
		{
			var s = ChangelogTextUtilities.FormatPrLink(pr, entryRepo, entryHideLinks, entryOwner);
			if (!string.IsNullOrEmpty(s))
				prParts.Add(s);
		}

		var issueParts = new List<string>();
		foreach (var issue in entry.Issues ?? [])
		{
			var s = ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks, entryOwner);
			if (!string.IsNullOrEmpty(s))
				issueParts.Add(s);
		}

		if (prParts.Count == 0 && issueParts.Count == 0)
			return;

		if (entryHideLinks)
		{
			foreach (var s in prParts)
				_ = sb.AppendLine(s);
			foreach (var s in issueParts)
				_ = sb.AppendLine(s);

			_ = sb.AppendLine("For more information, check the pull request or issue above.");
		}
		else
		{
			_ = sb.Append("For more information, check ");
			var first = true;
			foreach (var s in prParts)
			{
				if (!first)
					_ = sb.Append(' ');
				_ = sb.Append(s);
				first = false;
			}

			foreach (var s in issueParts)
			{
				if (!first)
					_ = sb.Append(' ');
				_ = sb.Append(s);
				first = false;
			}

			_ = sb.AppendLine(".");
		}

		_ = sb.AppendLine();
	}
}
