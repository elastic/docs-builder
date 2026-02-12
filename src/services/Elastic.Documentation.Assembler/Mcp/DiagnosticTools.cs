// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Elastic.Documentation.Assembler.Mcp.Responses;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Assembler.Mcp;

[McpServerToolType]
public class DiagnosticTools
{
	/// <summary>
	/// Returns docs-builder version, runtime, and workspace diagnostics.
	/// </summary>
	[McpServerTool, Description(
		"Returns docs-builder version, runtime environment, and workspace diagnostics. " +
		"Reports whether a docset configuration file (_docset.yml) was found in the current directory tree.")]
	public string GetDiagnostics()
	{
		try
		{
			var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
				?.InformationalVersion ?? "unknown";
			var runtime = $".NET {Environment.Version}";
			var cwd = Environment.CurrentDirectory;
			var docsetPath = FindDocsetConfig(cwd);

			return JsonSerializer.Serialize(
				new DiagnosticsResponse(version, runtime, cwd, docsetPath is not null, docsetPath),
				McpJsonContext.Default.DiagnosticsResponse);
		}
		catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException and not StackOverflowException)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	private static string? FindDocsetConfig(string startDir)
	{
		var dir = new DirectoryInfo(startDir);
		while (dir is not null)
		{
			var candidate = Path.Combine(dir.FullName, "_docset.yml");
			if (File.Exists(candidate))
				return candidate;

			dir = dir.Parent;
		}

		return null;
	}
}
