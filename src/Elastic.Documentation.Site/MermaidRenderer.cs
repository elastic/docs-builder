// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Site;

/// <summary>
/// Renders Mermaid diagrams to SVG using the beautiful-mermaid library via Node.js.
/// Falls back to client-side rendering when Node.js is not available.
/// </summary>
public class MermaidRenderer
{
	private static bool? IsNodeAvailableCached;

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
	/// Checks if Node.js is available for server-side rendering.
	/// </summary>
	public bool IsNodeAvailable()
	{
		if (IsNodeAvailableCached.HasValue)
			return IsNodeAvailableCached.Value;

		// Check if script and node_modules exist
		if (!File.Exists(_scriptPath) || !Directory.Exists(Path.Combine(_projectRoot, "node_modules")))
		{
			IsNodeAvailableCached = false;
			return false;
		}

		// Check if node is in PATH
		try
		{
			using var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "node",
					Arguments = "--version",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};
			if (!process.Start())
			{
				IsNodeAvailableCached = false;
				return false;
			}
			_ = process.WaitForExit(5000);
			IsNodeAvailableCached = process.ExitCode == 0;
		}
		catch (Exception)
		{
			IsNodeAvailableCached = false;
		}

		_logger?.LogDebug("Node.js availability: {IsAvailable}", IsNodeAvailableCached);
		return IsNodeAvailableCached.Value;
	}

	/// <summary>
	/// Returns HTML for client-side Mermaid rendering when Node.js is not available.
	/// </summary>
	public static string GetClientSideHtml(string mermaidCode) =>
		$"<pre class=\"mermaid\">{HttpUtility.HtmlEncode(mermaidCode)}</pre>";

	/// <summary>
	/// Renders Mermaid diagram code to SVG.
	/// </summary>
	/// <param name="mermaidCode">The Mermaid diagram code to render.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The rendered SVG string, or client-side HTML if Node.js is not available.</returns>
	public async Task<string> RenderAsync(string mermaidCode, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(mermaidCode))
			throw new ArgumentException("Mermaid code cannot be null or empty", nameof(mermaidCode));

		// Fall back to client-side rendering if Node.js is not available
		if (!IsNodeAvailable())
		{
			_logger?.LogDebug("Node.js not available, using client-side Mermaid rendering");
			return GetClientSideHtml(mermaidCode);
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
				var errorMessage = string.IsNullOrWhiteSpace(error) ? "Unknown error" : error.Trim();
				_logger?.LogError("Mermaid rendering failed with exit code {ExitCode}: {Error}",
					process.ExitCode, errorMessage);
				throw new InvalidOperationException(errorMessage);
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
