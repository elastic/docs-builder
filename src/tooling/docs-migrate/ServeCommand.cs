// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using ProcNet;

namespace Documentation.Migrate;

internal sealed class ServeCommand
{
	/// <summary>Serve the converted output using docs-builder.</summary>
	/// <param name="port">Port to serve on (default 3000)</param>
	/// <param name="ct">Cancellation token</param>
	public async Task<int> Serve(int port = 3001, CancellationToken ct = default)
	{
		var outputDir = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "migrated");

		if (!Directory.Exists(outputDir))
		{
			Console.Error.WriteLine($"Output directory not found at {outputDir}. Run 'docs-migrate convert' first.");
			return 1;
		}

		string[] args = ["run", "--project", "src/tooling/docs-builder", "--", "serve", "--path", outputDir, "--port", $"{port}", "--no-hud"];
		Console.WriteLine($"dotnet {string.Join(' ', args)}");

		var arguments = new ExecArguments("dotnet", args)
		{
			WorkingDirectory = Paths.WorkingDirectoryRoot.FullName
		};
		try
		{
			return await Proc.ExecAsync(arguments, ct);
		}
		catch (OperationCanceledException)
		{
			return 0;
		}
	}
}
