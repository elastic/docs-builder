// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Site;

/// <summary>
/// Renders Mermaid diagrams to SVG using the beautiful-mermaid library via Node.js.
/// </summary>
public class MermaidRenderer
{
	private readonly ILogger<MermaidRenderer>? _logger;
	private readonly string _projectRoot;
	private readonly string _scriptPath;

	public MermaidRenderer(ILogger<MermaidRenderer>? logger = null)
	{
		_logger = logger;
		_projectRoot = GetProjectRoot();
		_scriptPath = Path.Combine(_projectRoot, "scripts", "mermaid-renderer.mjs");
	}

	/// <summary>
	/// Gets the project root directory using the source file path at compile time.
	/// </summary>
	private static string GetProjectRoot([CallerFilePath] string? filePath = null)
	{
		if (string.IsNullOrEmpty(filePath))
			throw new InvalidOperationException("Could not determine project root from caller file path");

		return Path.GetDirectoryName(filePath)!;
	}

	/// <summary>
	/// Renders Mermaid diagram code to SVG.
	/// </summary>
	/// <param name="mermaidCode">The Mermaid diagram code to render.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The rendered SVG string.</returns>
	/// <exception cref="InvalidOperationException">Thrown when rendering fails.</exception>
	public async Task<string> RenderAsync(string mermaidCode, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(mermaidCode))
			throw new ArgumentException("Mermaid code cannot be null or empty", nameof(mermaidCode));

		if (!File.Exists(_scriptPath))
		{
			throw new InvalidOperationException(
				$"Mermaid renderer script not found at: {_scriptPath}. " +
				"Ensure npm dependencies are installed by running 'npm ci' in the Elastic.Documentation.Site directory.");
		}

		var nodeModulesPath = Path.Combine(_projectRoot, "node_modules");
		if (!Directory.Exists(nodeModulesPath))
		{
			throw new InvalidOperationException(
				$"Node modules not found at: {nodeModulesPath}. " +
				"Run 'npm ci' in the Elastic.Documentation.Site directory to install dependencies.");
		}

		_logger?.LogDebug("Rendering Mermaid diagram using script at {ScriptPath}", _scriptPath);

		var startInfo = new ProcessStartInfo
		{
			FileName = "node",
			Arguments = $"\"{_scriptPath}\"",
			WorkingDirectory = _projectRoot,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = new Process { StartInfo = startInfo };

		try
		{
			_ = process.Start();

			// Write Mermaid code to stdin
			await process.StandardInput.WriteAsync(mermaidCode);
			process.StandardInput.Close();

			// Read output
			var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
			var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

			await process.WaitForExitAsync(cancellationToken);

			var output = await outputTask;
			var error = await errorTask;

			if (process.ExitCode != 0)
			{
				_logger?.LogError("Mermaid rendering failed with exit code {ExitCode}: {Error}",
					process.ExitCode, error);
				throw new InvalidOperationException(
					$"Mermaid rendering failed: {(string.IsNullOrWhiteSpace(error) ? "Unknown error" : error.Trim())}");
			}

			if (string.IsNullOrWhiteSpace(output))
			{
				throw new InvalidOperationException("Mermaid rendering produced no output");
			}

			_logger?.LogDebug("Successfully rendered Mermaid diagram ({Length} bytes)", output.Length);
			return output;
		}
		catch (Exception ex) when (ex is not InvalidOperationException)
		{
			_logger?.LogError(ex, "Failed to execute Mermaid renderer");
			throw new InvalidOperationException(
				$"Failed to execute Mermaid renderer: {ex.Message}. " +
				"Ensure Node.js is installed and available in PATH.", ex);
		}
	}

	/// <summary>
	/// Synchronously renders Mermaid diagram code to SVG.
	/// </summary>
	/// <param name="mermaidCode">The Mermaid diagram code to render.</param>
	/// <returns>The rendered SVG string.</returns>
	public string Render(string mermaidCode) =>
		RenderAsync(mermaidCode).GetAwaiter().GetResult();
}
