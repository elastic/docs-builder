// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation;

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

		await FileSystem.File.WriteAllTextAsync(outputPath, content, ctx);
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
		var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
		var hasIssues = entry.Issues is { Count: > 0 };
		if (!hasPr && !hasIssues)
			return;

		if (entryHideLinks)
		{
			// When hiding private links, put them on separate lines as comments
			if (hasPr)
				_ = sb.AppendLine(ChangelogTextUtilities.FormatPrLink(entry.Pr!, entryRepo, entryHideLinks));
			if (hasIssues)
			{
				foreach (var issue in entry.Issues!)
					_ = sb.AppendLine(ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks));
			}

			_ = sb.AppendLine("For more information, check the pull request or issue above.");
		}
		else
		{
			_ = sb.Append("For more information, check ");
			if (hasPr)
				_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr!, entryRepo, entryHideLinks));
			if (hasIssues)
			{
				foreach (var issue in entry.Issues!)
				{
					_ = sb.Append(' ');
					_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks));
				}
			}

			_ = sb.AppendLine(".");
		}

		_ = sb.AppendLine();
	}
}
