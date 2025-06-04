// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Site;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Hosting;

namespace Documentation.Builder.Http;

public class ParcelWatchService : IHostedService
{
	private Process? _process;

	public Task StartAsync(Cancel cancellationToken)
	{
		_process = Process.Start(new ProcessStartInfo
		{
			FileName = "npm",
			Arguments = "run watch",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "src", "Elastic.Documentation.Site")
		})!;

		_process.EnableRaisingEvents = true;
		_process.OutputDataReceived += (_, e) => Console.WriteLine($"[npm run watch]: {e.Data}");
		_process.ErrorDataReceived += (_, e) => Console.WriteLine($"[npm run watch]: {e.Data}");

		_process.BeginOutputReadLine();
		_process.BeginErrorReadLine();

		return Task.CompletedTask;
	}

	public Task StopAsync(Cancel cancellationToken)
	{
		_process?.Kill(entireProcessTree: true);
		_process?.Kill();
		return Task.CompletedTask;
	}
}
