// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Documentation;

public static class GlobalCommandLine
{
	public static void Process(ref string[] args, ref LogLevel defaultLogLevel, out bool skipPrivateRepositories)
	{
		skipPrivateRepositories = false;
		var newArgs = new List<string>();
		for (var i = 0; i < args.Length; i++)
		{
			if (args[i] == "--log-level")
			{
				if (args.Length > i + 1)
					defaultLogLevel = GetLogLevel(args[i + 1]);
				i++;
			}
			else if (args[i] == "--skip-private-repositories")
				skipPrivateRepositories = true;
			else
				newArgs.Add(args[i]);
		}

		args = [.. newArgs];
	}

	private static LogLevel GetLogLevel(string? logLevel) => logLevel switch
	{
		"trace" => LogLevel.Trace,
		"debug" => LogLevel.Debug,
		"information" => LogLevel.Information,
		"info" => LogLevel.Information,
		"warning" => LogLevel.Warning,
		"error" => LogLevel.Error,
		"critical" => LogLevel.Critical,
		_ => LogLevel.Information
	};

}
