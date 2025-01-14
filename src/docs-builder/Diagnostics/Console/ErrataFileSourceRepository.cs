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
	public bool TryGet(string id, [NotNullWhen(true)] out Source? source)
	{
		using var reader = new Utf8StreamReader(id);
		var text = Encoding.UTF8.GetString(reader.ReadToEndAsync().GetAwaiter().GetResult());
		source = new Source(id, text);
		return true;
	}

	public void WriteDiagnosticsToConsole(IReadOnlyCollection<Diagnostic> items)
	{
		var report = new Report(this);
		foreach (var item in items)
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

			report.AddDiagnostic(d);
		}

		// Render the report
		report.Render(AnsiConsole.Console);
	}
}
