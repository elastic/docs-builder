// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Diagnostic = Elastic.Documentation.Diagnostics.Diagnostic;

namespace Elastic.Documentation.Tooling.Diagnostics.Console;

public class ConsoleDiagnosticsCollector(ILoggerFactory loggerFactory, ICoreService? githubActions = null)
	: DiagnosticsCollector([new Log(loggerFactory.CreateLogger<Log>()), new GithubAnnotationOutput(githubActions)]
	)
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
		else if (!NoHints)
			_hints.Add(diagnostic);
	}

	private bool _stopped;
	public override async Task StopAsync(Cancel cancellationToken)
	{
		if (_stopped)
			return;
		_stopped = true;
		var repository = new ErrataFileSourceRepository();
		repository.WriteDiagnosticsToConsole(_errors, _warnings, _hints);

		AnsiConsole.WriteLine();
		AnsiConsole.Write(new Markup($"	[bold red]{Errors} Errors[/] / [bold blue]{Warnings} Warnings[/] / [bold yellow]{Hints} Hints[/]"));
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();

		await Task.CompletedTask;
	}
}
