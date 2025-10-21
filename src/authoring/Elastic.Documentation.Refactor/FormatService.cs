// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Refactor;

public class FormatService(
	ILoggerFactory logFactory
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<FormatService>();

	// Collection of irregular whitespace characters that may impair Markdown rendering
	private static readonly char[] IrregularWhitespaceChars =
	[
		'\u000B', // Line Tabulation (\v) - <VT>
		'\u000C', // Form Feed (\f) - <FF>
		'\u00A0', // No-Break Space - <NBSP>
		'\u0085', // Next Line
		'\u1680', // Ogham Space Mark
		'\u180E', // Mongolian Vowel Separator - <MVS>
		'\ufeff', // Zero Width No-Break Space - <BOM>
		'\u2000', // En Quad
		'\u2001', // Em Quad
		'\u2002', // En Space - <ENSP>
		'\u2003', // Em Space - <EMSP>
		'\u2004', // Tree-Per-Em
		'\u2005', // Four-Per-Em
		'\u2006', // Six-Per-Em
		'\u2007', // Figure Space
		'\u2008', // Punctuation Space - <PUNCSP>
		'\u2009', // Thin Space
		'\u200A', // Hair Space
		'\u200B', // Zero Width Space - <ZWSP>
		'\u2028', // Line Separator
		'\u2029', // Paragraph Separator
		'\u202F', // Narrow No-Break Space
		'\u205F', // Medium Mathematical Space
		'\u3000'  // Ideographic Space
	];

	private static readonly SearchValues<char> IrregularWhitespaceSearchValues = SearchValues.Create(IrregularWhitespaceChars);

	public async Task<bool> Format(
		IDiagnosticsCollector collector,
		string? path,
		bool? dryRun,
		IFileSystem fs,
		Cancel ctx
	)
	{
		var isDryRun = dryRun ?? false;
		var rootPath = string.IsNullOrEmpty(path) ? fs.Directory.GetCurrentDirectory() : path;
		var rootDir = fs.DirectoryInfo.New(rootPath);

		if (!rootDir.Exists)
		{
			collector.EmitError(string.Empty, $"Directory not found: {rootPath}");
			return false;
		}

		_logger.LogInformation("Formatting documentation in: {Path}", rootDir.FullName);
		if (isDryRun)
			_logger.LogInformation("Running in dry-run mode - no files will be modified");

		var markdownFiles = rootDir.GetFiles("*.md", SearchOption.AllDirectories);
		var totalFilesProcessed = 0;
		var totalFilesModified = 0;
		var totalReplacements = 0;

		foreach (var file in markdownFiles)
		{
			if (ctx.IsCancellationRequested)
				break;

			totalFilesProcessed++;
			var (modified, replacements) = await ProcessFile(file, isDryRun, fs);

			if (modified)
			{
				totalFilesModified++;
				totalReplacements += replacements;
				_logger.LogInformation("Fixed {Count} irregular whitespace(s) in: {File}", replacements, GetRelativePath(rootDir, file));
			}
		}

		_logger.LogInformation("");
		_logger.LogInformation("Formatting complete:");
		_logger.LogInformation("  Files processed: {Processed}", totalFilesProcessed);
		_logger.LogInformation("  Files modified: {Modified}", totalFilesModified);
		_logger.LogInformation("  Total replacements: {Replacements}", totalReplacements);

		if (isDryRun && totalFilesModified > 0)
		{
			_logger.LogInformation("");
			_logger.LogInformation("Run without --dry-run to apply changes");
		}

		return true;
	}

	private static async Task<(bool modified, int replacements)> ProcessFile(IFileInfo file, bool isDryRun, IFileSystem fs)
	{
		var content = await fs.File.ReadAllTextAsync(file.FullName);
		var modified = false;
		var replacements = 0;

		// Check if file contains any irregular whitespace
		if (content.AsSpan().IndexOfAny(IrregularWhitespaceSearchValues) == -1)
			return (false, 0);

		// Replace irregular whitespace with regular spaces
		var sb = new StringBuilder(content.Length);
		foreach (var c in content)
		{
			if (IrregularWhitespaceSearchValues.Contains(c))
			{
				_ = sb.Append(' ');
				replacements++;
				modified = true;
			}
			else
			{
				_ = sb.Append(c);
			}
		}

		if (modified && !isDryRun)
		{
			await fs.File.WriteAllTextAsync(file.FullName, sb.ToString());
		}

		return (modified, replacements);
	}

	private static string GetRelativePath(IDirectoryInfo root, IFileInfo file)
	{
		var rootPath = root.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var filePath = file.FullName;

		if (filePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
		{
			return filePath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		return filePath;
	}
}
