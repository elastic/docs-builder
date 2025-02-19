// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cysharp.IO;
using Elastic.Markdown.Diagnostics;
using Errata;
using Spectre.Console;
using Diagnostic = Elastic.Markdown.Diagnostics.Diagnostic;

namespace Documentation.Builder.Diagnostics.Console;

public class ErrataFileSourceRepository : ISourceRepository
{
	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	public bool TryGet(string id, [NotNullWhen(true)] out Source? source)
	{
		using var reader = new Utf8StreamReader(id);
		var text = Encoding.UTF8.GetString(reader.ReadToEndAsync().GetAwaiter().GetResult());
		source = new Source(id, text);
		return true;
	}

	public void WriteDiagnosticsToConsole(IReadOnlyCollection<Diagnostic> errors, IReadOnlyCollection<Diagnostic> warnings)
	{
		var report = new Report(this);
		var limttedErrors = errors.Take(100).ToArray();
		var limittedWarnings = warnings.Take(100 - limttedErrors.Length);
		var limitted = limittedWarnings.Concat(limttedErrors).ToArray();

		foreach (var item in limitted)
		{
			var d = item.Severity switch
			{
				Severity.Error => Errata.Diagnostic.Error(item.Message),
				Severity.Warning => Errata.Diagnostic.Warning(item.Message),
				_ => Errata.Diagnostic.Info(item.Message)
			};
			if (item is { Line: not null, Column: not null })
			{
				var location = new Location(item.Line ?? 0, item.Column ?? 0);
				d = d.WithLabel(new Label(item.File, location, "")
					.WithLength(item.Length == null ? 1 : Math.Clamp(item.Length.Value, 1, item.Length.Value + 3))
					.WithPriority(1)
					.WithColor(item.Severity == Severity.Error ? Color.Red : Color.Blue));
			}
			else
				d = d.WithNote(item.File);

			_ = report.AddDiagnostic(d);
		}

		var totalErrorCount = errors.Count + warnings.Count;

		AnsiConsole.WriteLine();
		if (totalErrorCount > 0)
		{
			AnsiConsole.Write(new Markup($"	[bold]The following errors and warnings were found in the documentation[/]"));
			AnsiConsole.WriteLine();
			AnsiConsole.WriteLine();
			// Render the report
			report.Render(AnsiConsole.Console);

			AnsiConsole.WriteLine();
			AnsiConsole.WriteLine();

			if (limitted.Length <= totalErrorCount)
				AnsiConsole.Write(new Markup($"	[bold]Only shown the first [yellow]{limitted.Length}[/] diagnostics out of [yellow]{totalErrorCount}[/][/]"));

			AnsiConsole.WriteLine();

		}
	}
}
