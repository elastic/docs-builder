// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.Diagnostics;
using Microsoft.Extensions.Logging;
using Diagnostic = Elastic.Documentation.Diagnostics.Diagnostic;

namespace Documentation.Builder.Diagnostics.LiveMode;

public class LiveModeDiagnosticsCollector(ILoggerFactory logFactory)
	: DiagnosticsCollector([new Log(logFactory.CreateLogger<Log>())])
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
		else
			_hints.Add(diagnostic);
	}

	public override async Task StopAsync(Cancel cancellationToken)
	{
		if (_errors.Count > 0 || _warnings.Count > 0 || _hints.Count > 0)
		{
			var repository = new Elastic.Documentation.Tooling.Diagnostics.Console.ErrataFileSourceRepository();
			repository.WriteDiagnosticsToConsole(_errors, _warnings, _hints);
		}
		await Task.CompletedTask;
	}
}
