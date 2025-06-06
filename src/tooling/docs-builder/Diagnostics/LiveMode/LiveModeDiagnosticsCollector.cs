// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.Diagnostics;
using Microsoft.Extensions.Logging;
using Diagnostic = Elastic.Documentation.Diagnostics.Diagnostic;

namespace Documentation.Builder.Diagnostics.LiveMode;

public class LiveModeDiagnosticsCollector(ILoggerFactory loggerFactory)
	: DiagnosticsCollector([new Log(loggerFactory.CreateLogger<Log>())])
{
	protected override void HandleItem(Diagnostic diagnostic) { }

	public override async Task StopAsync(Cancel cancellationToken) => await Task.CompletedTask;
}
