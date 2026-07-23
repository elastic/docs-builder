// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Reflection;
using Elastic.Documentation.Site;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Isolated;

public sealed class PagefindSearchIndexer(ILoggerFactory loggerFactory)
{
	private const string ResourceName = "Elastic.Documentation.Site.pagefind-indexer.gz";
	private readonly ILogger _logger = loggerFactory.CreateLogger<PagefindSearchIndexer>();

	public async Task BuildAsync(IDirectoryInfo outputDirectory, Cancel ctx = default)
	{
		var executable = await ExtractExecutable(ctx);
		using var process = new Process
		{
			StartInfo = CreateStartInfo(executable, outputDirectory.FullName)
		};

		_logger.LogInformation("Generating static search index");
		if (!process.Start())
			throw new InvalidOperationException("Could not start the Pagefind indexer.");

		try
		{
			var standardOutput = process.StandardOutput.ReadToEndAsync(ctx);
			var standardError = process.StandardError.ReadToEndAsync(ctx);
			await process.WaitForExitAsync(ctx);

			var output = await standardOutput;
			var error = await standardError;
			if (process.ExitCode != 0)
				throw new InvalidOperationException($"Pagefind exited with code {process.ExitCode}: {error}");

			_logger.LogInformation("Generated static search index. {Output}", output.Trim());
		}
		catch (OperationCanceledException)
		{
			if (!process.HasExited)
				process.Kill(entireProcessTree: true);
			throw;
		}
	}

	private static ProcessStartInfo CreateStartInfo(string executable, string outputDirectory)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = executable,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		startInfo.ArgumentList.Add("--site");
		startInfo.ArgumentList.Add(outputDirectory);
		startInfo.ArgumentList.Add("--output-subdir");
		startInfo.ArgumentList.Add("pagefind");
		return startInfo;
	}

	private static async Task<string> ExtractExecutable(Cancel ctx)
	{
		var assembly = typeof(GlobalLayoutViewModel).Assembly;
		var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly.GetName().Version?.ToString()
			?? "dev";
		var directory = Path.Join(Path.GetTempPath(), "docs-builder", version);
		var executable = Path.Join(directory, OperatingSystem.IsWindows() ? "pagefind.exe" : "pagefind");
		if (File.Exists(executable))
			return executable;

		_ = Directory.CreateDirectory(directory);
		var temporaryFile = $"{executable}.{Environment.ProcessId}.tmp";
		await using (var resource = assembly.GetManifestResourceStream(ResourceName)
			?? throw new InvalidOperationException($"Embedded Pagefind resource '{ResourceName}' was not found."))
		await using (var compressed = new GZipStream(resource, CompressionMode.Decompress))
		await using (var destination = File.Create(temporaryFile))
			await compressed.CopyToAsync(destination, ctx);

		if (!OperatingSystem.IsWindows())
			File.SetUnixFileMode(temporaryFile, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

		File.Move(temporaryFile, executable, overwrite: true);
		return executable;
	}
}
