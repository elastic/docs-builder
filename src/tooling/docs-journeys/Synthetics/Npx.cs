// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Diagnostics;

namespace Elastic.Documentation.Journeys.Cli.Synthetics;

/// <summary>Runs <c>npx</c>, streaming what it prints, and says plainly when Node is missing.</summary>
internal static class Npx
{
	public static string Command => OperatingSystem.IsWindows() ? "npx.cmd" : "npx";

	public static string NpmCommand => OperatingSystem.IsWindows() ? "npm.cmd" : "npm";

	public static string Describe(IReadOnlyList<string> arguments, string? command = null) =>
		$"{command ?? Command} {string.Join(' ', arguments.Select(Quote))}";

	public static Task<int> RunAsync(IReadOnlyList<string> arguments, CancellationToken ct, string? workingDirectory = null) =>
		RunAsync(Command, arguments, ct, workingDirectory);

	/// <summary>
	/// Runs <paramref name="command"/> and, when it fails, says what was being done and with which exit code, then returns 1.
	/// npm and node report a failure as their own exit code (a Node error number such as -39 reads as 217), which means
	/// nothing to the person who ran docs-journeys, and the tool's own failures are all 1.
	/// </summary>
	public static async Task<int> RunReportedAsync(
		string doing,
		string command,
		IReadOnlyList<string> arguments,
		CancellationToken ct,
		string? workingDirectory = null
	)
	{
		var exit = await RunAsync(command, arguments, ct, workingDirectory).ConfigureAwait(false);
		if (exit == 0)
			return 0;
		await Console
			.Error
			.WriteLineAsync($"docs-journeys: {doing} failed: {command} exited with code {exit}. Its output is above.")
			.ConfigureAwait(false);
		return 1;
	}

	public static async Task<int> RunAsync(
		string command,
		IReadOnlyList<string> arguments,
		CancellationToken ct,
		string? workingDirectory = null
	)
	{
		// Some arguments come from the site that was probed, and cmd.exe would read an operator in one as syntax.
		if (OperatingSystem.IsWindows())
			RequireCmdSafe(arguments);
		// npm.cmd and npx.cmd are batch files; CreateProcess cannot start them, cmd.exe can.
		var info = new ProcessStartInfo(OperatingSystem.IsWindows() ? "cmd.exe" : command) { UseShellExecute = false };
		if (OperatingSystem.IsWindows())
		{
			info.ArgumentList.Add("/c");
			info.ArgumentList.Add(command);
		}
		if (workingDirectory is not null)
			info.WorkingDirectory = workingDirectory;
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		try
		{
			using var process = Process.Start(info) ?? throw new InvalidOperationException($"{command} did not start.");
			await process.WaitForExitAsync(ct).ConfigureAwait(false);
			return process.ExitCode;
		}
		catch (Win32Exception e)
		{
			throw new InvalidOperationException($"{command} is required (it ships with Node.js) and was not found: {e.Message}");
		}
	}

	// ArgumentList does not escape cmd.exe operators, so an argument that carries one would run as shell syntax.
	internal static void RequireCmdSafe(IEnumerable<string> arguments)
	{
		foreach (var argument in arguments)
		{
			if (argument.AsSpan().ContainsAny(CmdMetacharacters) || argument.Any(char.IsControl))
				throw new ArgumentException(
					$"The argument '{argument}' contains characters that cmd.exe would interpret (one of & | < > ^ % ! \" ( )), so it is refused on Windows."
				);
		}
	}

	private static readonly System.Buffers.SearchValues<char> CmdMetacharacters = System.Buffers.SearchValues.Create("&|<>^%!\"()");

	private static string Quote(string argument) => argument.Any(char.IsWhiteSpace) ? $"\"{argument}\"" : argument;
}
