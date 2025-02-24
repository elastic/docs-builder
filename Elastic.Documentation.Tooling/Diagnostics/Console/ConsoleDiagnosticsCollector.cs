// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using Elastic.Markdown.Diagnostics;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Diagnostic = Elastic.Markdown.Diagnostics.Diagnostic;

namespace Elastic.Documentation.Tooling.Diagnostics.Console;

public class ConsoleDiagnosticsCollector(ILoggerFactory loggerFactory, ICoreService? githubActions = null)
	: DiagnosticsCollector([new Log(loggerFactory.CreateLogger<Log>()), new GithubAnnotationOutput(githubActions)]
	)
{
	private readonly List<Diagnostic> _errors = [];
	private readonly List<Diagnostic> _warnings = [];

	protected override void HandleItem(Diagnostic diagnostic)
	{
		if (diagnostic.Severity == Severity.Warning)
			_warnings.Add(diagnostic);
		else
			_errors.Add(diagnostic);
	}

	public override async Task StopAsync(Cancel cancellationToken)
	{
		var repository = new ErrataFileSourceRepository();
		repository.WriteDiagnosticsToConsole(_errors, _warnings);

		AnsiConsole.WriteLine();
		AnsiConsole.Write(new Markup($"	[bold red]{Errors} Errors[/] / [bold blue]{Warnings} Warnings[/]"));
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();

		await Task.CompletedTask;
	}
}
