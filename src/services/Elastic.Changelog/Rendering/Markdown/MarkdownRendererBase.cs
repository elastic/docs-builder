// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Abstract base class for changelog markdown renderers
/// </summary>
public abstract class MarkdownRendererBase(IFileSystem fileSystem) : IChangelogMarkdownRenderer
{
	protected IFileSystem FileSystem { get; } = fileSystem;

	/// <inheritdoc />
	public abstract string OutputFileName { get; }

	/// <inheritdoc />
	public abstract Task RenderAsync(ChangelogRenderContext context, Cancel ctx);

	/// <summary>
	/// Writes the output file to the specified directory
	/// </summary>
	protected async Task WriteOutputFileAsync(string outputDir, string titleSlug, string content, Cancel ctx)
	{
		var outputPath = FileSystem.Path.Combine(outputDir, titleSlug, OutputFileName);
		var outputDirectory = FileSystem.Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrWhiteSpace(outputDirectory) && !FileSystem.Directory.Exists(outputDirectory))
			_ = FileSystem.Directory.CreateDirectory(outputDirectory);

		await FileSystem.File.WriteAllTextAsync(outputPath, content, Encoding.UTF8, ctx);
	}

	/// <summary>
	/// Gets the entry context (bundleProducts, repo, hideLinks) for a specific entry
	/// </summary>
	protected static (HashSet<string> bundleProductIds, string entryRepo, bool hideLinks) GetEntryContext(
		ChangelogEntry entry,
		ChangelogRenderContext context)
	{
		var bundleProductIds = context.EntryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		var entryRepo = context.EntryToRepo.GetValueOrDefault(entry, context.Repo);
		var hideLinks = context.EntryToHideLinks.GetValueOrDefault(entry, false);
		return (bundleProductIds, entryRepo, hideLinks);
	}

	/// <summary>
	/// Renders PR and issue links for dropdown entries
	/// </summary>
	protected static void RenderPrIssueLinks(StringBuilder sb, ChangelogEntry entry, string entryRepo, bool entryHideLinks)
	{
		var hasPrs = entry.Prs is { Count: > 0 };
		var hasIssues = entry.Issues is { Count: > 0 };
		if (!hasPrs && !hasIssues)
			return;

		if (entryHideLinks)
		{
			foreach (var pr in entry.Prs ?? [])
				_ = sb.AppendLine(ChangelogTextUtilities.FormatPrLink(pr, entryRepo, entryHideLinks));
			foreach (var issue in entry.Issues ?? [])
				_ = sb.AppendLine(ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks));

			_ = sb.AppendLine("For more information, check the pull request or issue above.");
		}
		else
		{
			_ = sb.Append("For more information, check ");
			var first = true;
			foreach (var pr in entry.Prs ?? [])
			{
				if (!first)
					_ = sb.Append(' ');
				_ = sb.Append(ChangelogTextUtilities.FormatPrLink(pr, entryRepo, entryHideLinks));
				first = false;
			}
			foreach (var issue in entry.Issues ?? [])
			{
				if (!first)
					_ = sb.Append(' ');
				_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks));
				first = false;
			}

			_ = sb.AppendLine(".");
		}

		_ = sb.AppendLine();
	}
}
