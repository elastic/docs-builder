// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cysharp.IO;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Diagnostics;
using Errata;
using Spectre.Console;
using Diagnostic = Elastic.Documentation.Diagnostics.Diagnostic;

namespace Elastic.Documentation.Tooling.Diagnostics.Console;

public class ErrataFileSourceRepository : ISourceRepository
{
	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	public bool TryGet(string id, [NotNullWhen(true)] out Source? source)
	{
		source = new Source(id, string.Empty);
		if (id == string.Empty)
			return true;

		using var reader = new Utf8StreamReader(id);
		var text = Encoding.UTF8.GetString(reader.ReadToEndAsync().GetAwaiter().GetResult());
		source = new Source(id, text);
		return true;
	}

	public void WriteDiagnosticsToConsole(IReadOnlyCollection<Diagnostic> errors, IReadOnlyCollection<Diagnostic> warnings, List<Diagnostic> hints)
	{
		var report = new Report(this);
		var limited = errors
			.Concat(warnings)
			.OrderBy(d => d.Severity switch { Severity.Error => 0, Severity.Warning => 1, Severity.Hint => 2, _ => 3 })
			.Take(100)
			.ToArray();

		// show hints if we don't have plenty of errors/warnings to show
		if (limited.Length < 100)
			limited = limited.Concat(hints).Take(100).ToArray();

		foreach (var item in limited)
		{
			var d = item.Severity switch
			{
				Severity.Error => Errata.Diagnostic.Error(item.Message),
				Severity.Warning => Errata.Diagnostic.Warning(item.Message),
				Severity.Hint => Errata.Diagnostic.Info(item.Message),
				_ => Errata.Diagnostic.Info(item.Message)
			};
			if (item is { Line: not null, Column: not null })
			{
				var location = new Location(item.Line ?? 0, item.Column ?? 0);
				d = d.WithLabel(new Label(item.File, location, "")
					.WithLength(item.Length == null ? 1 : Math.Clamp(item.Length.Value, 1, item.Length.Value + 3))
					.WithPriority(1)
					.WithColor(item.Severity switch
					{
						Severity.Error => Color.Red,
						Severity.Warning => Color.Blue,
						Severity.Hint => Color.Yellow,
						_ => Color.Blue
					}));
			}
			else
				d = d.WithNote(item.File);

			if (item.Severity == Severity.Hint)
				d = d.WithColor(Color.Yellow).WithCategory("Hint");

			_ = report.AddDiagnostic(d);
		}

		var totalErrorCount = errors.Count + warnings.Count;

		AnsiConsole.WriteLine();
		if (totalErrorCount <= 0)
		{
			if (hints.Count > 0)
				DisplayHintsOnly(report, hints);
			return;
		}
		DisplayErrorAndWarningSummary(report, totalErrorCount, limited);
	}

	private static void DisplayHintsOnly(Report report, List<Diagnostic> hints)
	{
		AnsiConsole.Write(new Markup($"	[bold]The following improvement hints found in the documentation[/]"));
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();
		// Render the report
		report.Render(AnsiConsole.Console);

		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();

		if (hints.Count >= 100)
			AnsiConsole.Write(new Markup($"	[bold]Only shown the first [yellow]{100}[/] hints out of [yellow]{hints.Count}[/][/]"));

		AnsiConsole.WriteLine();
	}

	private static void DisplayErrorAndWarningSummary(Report report, int totalErrorCount, Diagnostic[] limited)
	{
		AnsiConsole.Write(new Markup($"	[bold]The following errors and warnings were found in the documentation[/]"));
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();
		// Render the report
		report.Render(AnsiConsole.Console);

		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();

		if (totalErrorCount > limited.Length)
			AnsiConsole.Write(new Markup($"	[bold]Only shown the first [yellow]{limited.Length}[/] diagnostics out of [yellow]{totalErrorCount}[/][/]"));

		AnsiConsole.WriteLine();
	}
}
