// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Elastic.Documentation;

public record GlobalCliArgs
{
	public LogLevel LogLevel { get; init; } = LogLevel.Information;
	public ConfigurationSource? ConfigurationSource { get; init; }
	public bool SkipPrivateRepositories { get; init; }
	public bool IsHelpOrVersion { get; init; }
	public bool IsMcp { get; init; }
}
public static class GlobalCli
{
	public static void Process(ref string[] args, out GlobalCliArgs cli) => Process(ref args, out cli, out _);
	public static void Process(ref string[] args, out GlobalCliArgs cli, out string[] globalArguments)
	{
		cli = new GlobalCliArgs();
		globalArguments = [];
		var globalArgs = new List<string>();
		var filteredArguments = new List<string>();
		for (var i = 0; i < args.Length; i++)
		{
			if (args[i] == "--log-level")
			{
				if (args.Length > i + 1)
				{
					cli = cli with { LogLevel = GetLogLevel(args[i + 1]) };
					globalArgs.Add("--log-level");
					globalArgs.Add(args[i + 1]);
				}
				i++;
			}
			else if (args[i] is "--config-source" or "--configuration-source" or "-c")
			{
				if (args.Length > i + 1 && ConfigurationSourceExtensions.TryParse(args[i + 1], out var cs, true, true))
				{
					cli = cli with { ConfigurationSource = cs };
					globalArgs.Add("--config-source");
					globalArgs.Add(args[i + 1]);
				}
				i++;
			}
			else if (args[i] == "--skip-private-repositories")
			{
				cli = cli with { SkipPrivateRepositories = true };
				globalArgs.Add("--skip-private-repositories");
			}
			else if (args[i] is "--help" or "--version")
			{
				cli = cli with { IsHelpOrVersion = true };
				globalArgs.Add(args[i]);
				filteredArguments.Add(args[i]);
			}
			else
				filteredArguments.Add(args[i]);
		}

		args = [.. filteredArguments];
		globalArguments = [.. globalArgs];

		if (filteredArguments.Count > 0 && filteredArguments[0] == "mcp")
			cli = cli with { IsMcp = true };
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
