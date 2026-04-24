// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Documentation;

/// <summary>Early-parse utilities for use before the DI host is built.</summary>
public static class GlobalCli
{
	/// <summary>
	/// Scans <paramref name="args"/> for known startup flags without modifying the array.
	/// Used for pre-host setup before argh routing runs.
	/// </summary>
	public static GlobalCliArgs ScanArgs(string[] args)
	{
		var options = new GlobalCliArgs();
		for (var i = 0; i < args.Length; i++)
		{
			if (args[i] == "--log-level" && i + 1 < args.Length)
				options = options with { LogLevel = ParseLogLevel(args[++i]) };
			else if (args[i] is "--config-source" or "--configuration-source" or "-c" && i + 1 < args.Length)
			{
				if (ConfigurationSourceExtensions.TryParse(args[i + 1], out var cs, true, true))
					options = options with { ConfigurationSource = cs };
				i++;
			}
			else if (args[i] == "--skip-private-repositories")
				options = options with { SkipPrivateRepositories = true };
		}
		return options;
	}

	/// <summary>Returns <see langword="true"/> when the first non-flag argument is <c>mcp</c>.</summary>
	public static bool IsMcpMode(string[] args) => args.Length > 0 && args[0] == "mcp";

	private static LogLevel ParseLogLevel(string? logLevel) => logLevel switch
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

/// <summary>Startup args parsed before the DI host builds (not injected into commands).</summary>
public record GlobalCliArgs
{
	public LogLevel LogLevel { get; init; } = LogLevel.Information;
	public ConfigurationSource? ConfigurationSource { get; init; }
	public bool SkipPrivateRepositories { get; init; }
}
