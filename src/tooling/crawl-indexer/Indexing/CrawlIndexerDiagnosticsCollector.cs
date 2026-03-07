// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Diagnostics;
using Errata;
using Spectre.Console;
using Diagnostic = Elastic.Documentation.Diagnostics.Diagnostic;

namespace CrawlIndexer.Indexing;

/// <summary>
/// Diagnostics collector for crawl-indexer that collects errors for display.
/// </summary>
public class CrawlIndexerDiagnosticsCollector(IndexingErrorTracker errorTracker)
	: DiagnosticsCollector([errorTracker])
{
	private readonly List<Diagnostic> _errors = [];
	private readonly List<Diagnostic> _warnings = [];
	private readonly List<Diagnostic> _hints = [];

	protected override void HandleItem(Diagnostic diagnostic)
	{
		if (diagnostic.Severity == Severity.Error)
			_errors.Add(diagnostic);
		else if (diagnostic.Severity == Severity.Warning)
			_warnings.Add(diagnostic);
		else if (diagnostic.Severity == Severity.Hint && !NoHints)
			_hints.Add(diagnostic);
	}

	/// <summary>
	/// Writes collected diagnostics to console using Errata-style formatting.
	/// </summary>
	public void WriteErrorsToConsole()
	{
		if (_errors.Count == 0 && _warnings.Count == 0 && _hints.Count == 0)
			return;

		AnsiConsole.WriteLine();
		var ruleColor = _errors.Count > 0 ? "red" : _warnings.Count > 0 ? "yellow" : "blue";
		AnsiConsole.Write(new Rule($"[{ruleColor} bold]Diagnostics[/]") { Style = Style.Parse(ruleColor) });
		AnsiConsole.WriteLine();

		var report = new Report(new UrlSourceRepository());
		var limited = _errors
			.Concat(_warnings)
			.Concat(_hints)
			.OrderBy(d => d.Severity switch { Severity.Error => 0, Severity.Warning => 1, Severity.Hint => 2, _ => 3 })
			.Take(50)
			.ToArray();

		foreach (var item in limited)
		{
			var d = item.Severity switch
			{
				Severity.Error => Errata.Diagnostic.Error(item.Message),
				Severity.Warning => Errata.Diagnostic.Warning(item.Message),
				Severity.Hint => Errata.Diagnostic.Info(item.Message).WithColor(Color.Blue).WithCategory("Hint"),
				_ => Errata.Diagnostic.Info(item.Message)
			};

			// For URL-based errors, add as a note since we don't have file content
			d = d.WithNote(item.File);
			_ = report.AddDiagnostic(d);
		}

		report.Render(AnsiConsole.Console, new ReportSettings
		{
			Formatter = new CrawlDiagnosticFormatter()
		});

		AnsiConsole.WriteLine();

		var totalCount = _errors.Count + _warnings.Count + _hints.Count;
		if (totalCount > limited.Length)
			AnsiConsole.MarkupLine($"[dim]Showing first {limited.Length} of {totalCount:N0} diagnostics[/]");

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[red bold]{_errors.Count}[/] errors, [yellow bold]{_warnings.Count}[/] warnings, [blue bold]{_hints.Count}[/] hints");
		AnsiConsole.WriteLine();
	}
}

/// <summary>
/// Custom formatter for crawl diagnostics that handles multiline messages.
/// </summary>
internal sealed class CrawlDiagnosticFormatter : DiagnosticFormatter
{
	public override Markup Format(Errata.Diagnostic diagnostic)
	{
		var message = diagnostic.Message.EscapeMarkup();
		var color = diagnostic.Color;

		// Handle exception messages that may be multiline
		if (message.Contains("Exception:"))
			return new Markup($"[{color}]Exception:[/] [white]{message}[/]");

		var prefix = diagnostic.Category?.EscapeMarkup() ?? "";
		if (!string.IsNullOrEmpty(prefix))
			prefix = $"[bold {color}]{prefix}[/]: ";

		return new Markup($"{prefix}[white]{message}[/]");
	}
}

/// <summary>
/// Empty source repository for URL-based diagnostics (no file content to display).
/// </summary>
internal sealed class UrlSourceRepository : ISourceRepository
{
	public bool TryGet(string id, [NotNullWhen(true)] out Source? source)
	{
		// URLs don't have file content to display
		source = new Source(id, string.Empty);
		return true;
	}
}
