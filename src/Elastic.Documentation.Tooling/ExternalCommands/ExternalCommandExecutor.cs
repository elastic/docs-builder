// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using ProcNet;
using ProcNet.Std;

namespace Elastic.Documentation.ExternalCommands;

public abstract class ExternalCommandExecutor(IDiagnosticsCollector collector, IDirectoryInfo workingDirectory, TimeSpan? timeout = null)
{
	protected abstract ILogger Logger { get; }

	private void Log(Action<ILogger> logAction)
	{
		if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")))
			return;
		logAction(Logger);
	}

	protected IDirectoryInfo WorkingDirectory => workingDirectory;
	protected IDiagnosticsCollector Collector => collector;
	protected void ExecIn(Dictionary<string, string> environmentVars, string binary, params string[] args)
	{
		var arguments = new ExecArguments(binary, args)
		{
			WorkingDirectory = workingDirectory.FullName,
			Environment = environmentVars,
			Timeout = timeout
		};
		var result = Proc.Exec(arguments);
		if (result != 0)
			collector.EmitError("", $"Exit code: {result} while executing {binary} {string.Join(" ", args)} in {workingDirectory}");
	}

	protected void ExecInSilent(Dictionary<string, string> environmentVars, string binary, params string[] args)
	{
		var arguments = new StartArguments(binary, args)
		{
			Environment = environmentVars,
			WorkingDirectory = workingDirectory.FullName,
			ConsoleOutWriter = NoopConsoleWriter.Instance,
			Timeout = timeout
		};
		var result = Proc.Start(arguments);
		if (result.ExitCode != 0)
			collector.EmitError("", $"Exit code: {result.ExitCode} while executing {binary} {string.Join(" ", args)} in {workingDirectory}");
	}

	protected string[] CaptureMultiple(string binary, params string[] args) => CaptureMultiple(false, 10, binary, args);
	protected string[] CaptureMultiple(int attempts, string binary, params string[] args) => CaptureMultiple(false, attempts, binary, args);
	private string[] CaptureMultiple(bool muteExceptions, int attempts, string binary, params string[] args)
	{
		// Try 10 times to capture the output of the command, if it fails, we'll throw an exception on the last try
		Exception? e = null;
		for (var i = 1; i <= attempts; i++)
		{
			try
			{
				return CaptureOutput(e, i, attempts);
			}
			catch (Exception ex)
			{
				if (ex is not null)
					e = ex;
			}
		}

		if (e is not null && !muteExceptions)
			collector.EmitError("", "failure capturing stdout", e);
		if (e is not null)
			Log(l => l.LogError(e, "[{Binary} {Args}] failure capturing stdout executing in {WorkingDirectory}", binary, string.Join(" ", args), workingDirectory.FullName));

		return [];

		string[] CaptureOutput(Exception? previousException, int iteration, int max)
		{
			var arguments = new StartArguments(binary, args)
			{
				WorkingDirectory = workingDirectory.FullName,
				Timeout = TimeSpan.FromSeconds(3),
				WaitForExit = TimeSpan.FromSeconds(3),
				// Capture the output of the command if it's the last iteration
				ConsoleOutWriter = iteration == max ? new ConsoleOutWriter() : NoopConsoleWriter.Instance,
			};
			var result = Proc.Start(arguments);

			string[]? output;
			switch (result.ExitCode, muteExceptions)
			{
				case (0, _) or (not 0, true):
					output = result.ConsoleOut.Select(x => x.Line).ToArray();
					if (output.Length == 0)
					{
						Log(l => l.LogInformation("[{Binary} {Args}] captured no output. ({Iteration}/{MaxIteration}) pwd: {WorkingDirectory}",
							binary, string.Join(" ", args), iteration, max, workingDirectory.FullName)
						);
						throw new Exception($"No output captured executing in pwd: {workingDirectory} from {binary} {string.Join(" ", args)}", previousException);
					}
					break;
				case (not 0, false):
					Log(l => l.LogInformation("[{Binary} {Args}] Exit code is not 0 but {ExitCode}. ({Iteration}/{MaxIteration}) pwd: {WorkingDirectory}",
						binary, string.Join(" ", args), result.ExitCode, iteration, max, workingDirectory.FullName)
					);
					throw new Exception($"Exit code not 0. Received {result.ExitCode} in pwd: {workingDirectory} from {binary} {string.Join(" ", args)}", previousException);
			}

			return output;
		}
	}

	protected string CaptureQuiet(string binary, params string[] args) => Capture(true, 10, binary, args);
	protected string Capture(string binary, params string[] args) => Capture(false, 10, binary, args);

	private string Capture(bool muteExceptions, int attempts, string binary, params string[] args)
	{
		var lines = CaptureMultiple(muteExceptions, attempts, binary, args);
		return lines.FirstOrDefault() ??
			(muteExceptions ? string.Empty : throw new Exception($"[{binary} {string.Join(" ", args)}] No output captured executing in : {workingDirectory}"));
	}
}
