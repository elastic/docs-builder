// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Refactor.Formatters;
using Elastic.Documentation.Services;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Refactor;

public class FormatService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<FormatService>();

	// List of formatters to apply - easily extensible for future formatting operations
	private static readonly IFormatter[] Formatters =
	[
		new IrregularSpaceFormatter()
		// Future formatters can be added here:
		// new TrailingWhitespaceFormatter(),
		// new LineEndingFormatter(),
		// etc.
	];

	public async Task<bool> Format(
		IDiagnosticsCollector collector,
		string? path,
		bool checkOnly,
		IFileSystem fs,
		Cancel ctx
	)
	{
		// Create BuildContext to load the documentation set
		var context = new BuildContext(collector, fs, fs, configurationContext, ExportOptions.MetadataOnly, path, null);
		var set = new DocumentationSet(context, logFactory, NoopCrossLinkResolver.Instance);

		var mode = checkOnly ? "Checking" : "Formatting";
		_logger.LogInformation("{Mode} documentation in: {Path}", mode, set.SourceDirectory.FullName);

		var totalFilesProcessed = 0;
		var totalFilesModified = 0;
		var formatterStats = new Dictionary<string, int>();

		// Initialize stats for each formatter
		foreach (var formatter in Formatters)
			formatterStats[formatter.Name] = 0;

		// Only process markdown files that are part of the documentation set
		foreach (var docFile in set.Files.OfType<MarkdownFile>())
		{
			if (ctx.IsCancellationRequested)
				break;

			totalFilesProcessed++;
			var (modified, changes) = await ProcessFile(docFile.SourceFile, checkOnly, fs, formatterStats);

			if (modified)
				totalFilesModified++;
		}

		_logger.LogInformation("");

		if (checkOnly)
		{
			if (totalFilesModified > 0)
			{
				_logger.LogInformation("Formatting needed:");
				_logger.LogInformation("  Files needing formatting: {Modified}", totalFilesModified);

				// Log stats for each formatter that would make changes
				foreach (var (formatterName, changeCount) in formatterStats.Where(kvp => kvp.Value > 0))
					_logger.LogInformation("  {Formatter} fixes needed: {Count}", formatterName, changeCount);

				_logger.LogInformation("");

				// Emit error to trigger exit code 1
				collector.EmitError(string.Empty, $"{totalFilesModified} file(s) need formatting. Run 'docs-builder format --write' to apply changes.");

				return false;
			}
			else
			{
				_logger.LogInformation("All files are properly formatted");
				return true;
			}
		}
		else
		{
			_logger.LogInformation("Formatting complete:");
			_logger.LogInformation("  Files processed: {Processed}", totalFilesProcessed);
			_logger.LogInformation("  Files modified: {Modified}", totalFilesModified);

			// Log stats for each formatter that made changes
			foreach (var (formatterName, changeCount) in formatterStats.Where(kvp => kvp.Value > 0))
				_logger.LogInformation("  {Formatter} fixes: {Count}", formatterName, changeCount);

			return true;
		}
	}

	private static async Task<(bool modified, int totalChanges)> ProcessFile(
		IFileInfo file,
		bool checkOnly,
		IFileSystem fs,
		Dictionary<string, int> stats
	)
	{
		var content = await fs.File.ReadAllTextAsync(file.FullName);
		var originalContent = content;
		var totalChanges = 0;

		// Apply each formatter in sequence
		foreach (var formatter in Formatters)
		{
			var result = formatter.Format(content);

			if (result.Changes > 0)
			{
				content = result.Content;
				totalChanges += result.Changes;
				stats[formatter.Name] += result.Changes;
			}
		}

		var modified = content != originalContent;

		// Only write if content changed and in write mode
		if (modified && !checkOnly)
			await fs.File.WriteAllTextAsync(file.FullName, content);

		return (modified, totalChanges);
	}
}
