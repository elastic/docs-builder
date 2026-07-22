// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Codex;

internal static class CodexConfigurationLoader
{
	/// <summary>
	/// Loads a <see cref="CodexConfiguration"/> and its required <paramref name="environment"/>
	/// field from <paramref name="configFile"/>. Returns <c>false</c> and emits a diagnostic on
	/// any failure (file missing, unreadable, missing environment). Use <c>out _</c> to discard
	/// the environment when the caller resolves it from other sources.
	/// </summary>
	internal static bool TryLoad(
		IFileInfo configFile,
		string originalPath,
		IDiagnosticsCollector collector,
		out CodexConfiguration config,
		out string environment)
	{
		environment = string.Empty;
		if (!TryLoadCore(configFile, originalPath, collector, out config))
			return false;

		if (string.IsNullOrWhiteSpace(config.Environment))
		{
			collector.EmitGlobalError(
				"Codex configuration must specify an 'environment' (e.g., 'internal', 'security').");
			return false;
		}

		environment = config.Environment;
		return true;
	}

	/// <summary>
	/// Loads a <see cref="CodexConfiguration"/> without requiring the <c>environment</c> field.
	/// Use for commands that resolve the environment from other sources (CLI argument, env var).
	/// </summary>
	internal static bool TryLoad(
		IFileInfo configFile,
		string originalPath,
		IDiagnosticsCollector collector,
		out CodexConfiguration config) =>
		TryLoadCore(configFile, originalPath, collector, out config);

	private static bool TryLoadCore(
		IFileInfo configFile,
		string originalPath,
		IDiagnosticsCollector collector,
		out CodexConfiguration config)
	{
		config = default!;
		try
		{
			if (!configFile.Exists)
			{
				collector.EmitGlobalError($"Codex configuration file not found: {originalPath}");
				return false;
			}

			config = CodexConfiguration.Load(configFile);
			return true;
		}
		catch (Exception ex)
		{
			collector.EmitGlobalError(
				$"Failed to read codex configuration '{originalPath}': {ex.Message}", ex);
			return false;
		}
	}
}
