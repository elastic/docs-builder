// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Tooling.Diagnostics;

// named Log for terseness on console output
public class Log(ILogger logger) : IDiagnosticsOutput
{
	public void Write(Diagnostic diagnostic)
	{
		if (diagnostic.Severity == Severity.Error)
			logger.LogError("{Message} ({File}:{Line})", diagnostic.Message, diagnostic.File, diagnostic.Line);
		else
			logger.LogWarning("{Message} ({File}:{Line})", diagnostic.Message, diagnostic.File, diagnostic.Line);
	}
}

