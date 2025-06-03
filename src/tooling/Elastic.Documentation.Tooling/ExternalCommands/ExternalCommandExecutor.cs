// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using ProcNet;

namespace Elastic.Documentation.Tooling.ExternalCommands;

public abstract class ExternalCommandExecutor(DiagnosticsCollector collector, IDirectoryInfo workingDirectory)
{
	protected IDirectoryInfo WorkingDirectory => workingDirectory;
	protected void ExecIn(string binary, params string[] args)
	{
		var arguments = new ExecArguments(binary, args)
		{
			WorkingDirectory = workingDirectory.FullName,
			Environment = new Dictionary<string, string>
			{
				// Disable git editor prompts:
				// There are cases where `git pull` would prompt for an editor to write a commit message.
				// This env variable prevents that.
				{ "GIT_EDITOR", "true" }
			},
		};
		var result = Proc.Exec(arguments);
		if (result != 0)
			collector.EmitError("", $"Exit code: {result} while executing {binary} {string.Join(" ", args)} in {workingDirectory}");
	}

	protected string[] CaptureMultiple(string binary, params string[] args)
	{
		// Try 10 times to capture the output of the command, if it fails, we'll throw an exception on the last try
		Exception? e = null;
		for (var i = 0; i <= 9; i++)
		{
			try
			{
				return CaptureOutput();
			}
			catch (Exception ex)
			{
				if (ex is not null)
					e = ex;
			}
		}

		if (e is not null)
			collector.EmitError("", "failure capturing stdout", e);

		return [];

		string[] CaptureOutput()
		{
			var arguments = new StartArguments(binary, args)
			{
				WorkingDirectory = workingDirectory.FullName,
				Timeout = TimeSpan.FromSeconds(3),
				WaitForExit = TimeSpan.FromSeconds(3),
				ConsoleOutWriter = NoopConsoleWriter.Instance
			};
			var result = Proc.Start(arguments);
			var output = result.ExitCode != 0
				? throw new Exception($"Exit code is not 0. Received {result.ExitCode} from {binary}: {workingDirectory}")
				: result.ConsoleOut.Select(x => x.Line).ToArray() ?? throw new Exception($"No output captured for {binary}: {workingDirectory}");
			return output;
		}
	}


	protected string Capture(string binary, params string[] args)
	{
		// Try 10 times to capture the output of the command, if it fails, we'll throw an exception on the last try
		Exception? e = null;
		for (var i = 0; i <= 9; i++)
		{
			try
			{
				return CaptureOutput();
			}
			catch (Exception ex)
			{
				if (ex is not null)
					e = ex;
			}
		}

		if (e is not null)
			collector.EmitError("", "failure capturing stdout", e);

		return string.Empty;

		string CaptureOutput()
		{
			var arguments = new StartArguments(binary, args)
			{
				WorkingDirectory = workingDirectory.FullName,
				Timeout = TimeSpan.FromSeconds(3),
				WaitForExit = TimeSpan.FromSeconds(3),
				ConsoleOutWriter = NoopConsoleWriter.Instance
			};
			var result = Proc.Start(arguments);
			var line = result.ExitCode != 0
				? throw new Exception($"Exit code is not 0. Received {result.ExitCode} from {binary}: {workingDirectory}")
				: result.ConsoleOut.FirstOrDefault()?.Line ?? throw new Exception($"No output captured for {binary}: {workingDirectory}");
			return line;
		}
	}
}
