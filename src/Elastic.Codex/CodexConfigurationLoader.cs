// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Configuration.Loading;
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
		out string environment
	)
	{
		config = default!;
		environment = string.Empty;
		if (!TryReadText(configFile, originalPath, collector, out var text))
			return false;

		var result = CodexSiteLoader.LoadForPublish(text, originalPath);
		if (!Report(result.Findings(), collector) || result.ValueOrNull() is not { } published)
			return false;

		config = published.Configuration;
		environment = config.Environment!;
		return true;
	}

	/// <summary>
	/// Loads a <see cref="CodexConfiguration"/> without requiring the <c>environment</c> field.
	/// Use for commands that resolve the environment from other sources (CLI argument, env var).
	/// </summary>
	internal static bool TryLoad(IFileInfo configFile, string originalPath, IDiagnosticsCollector collector, out CodexConfiguration config)
	{
		config = default!;
		if (!TryReadText(configFile, originalPath, collector, out var text))
			return false;

		var result = CodexSiteLoader.Load(text, originalPath);
		if (!Report(result.Findings(), collector) || result.ValueOrNull() is not { } loaded)
			return false;

		config = loaded;
		return true;
	}

	private static bool TryReadText(IFileInfo configFile, string originalPath, IDiagnosticsCollector collector, out string text)
	{
		text = string.Empty;
		try
		{
			if (!configFile.Exists)
			{
				collector.EmitGlobalError($"Codex configuration file not found: {originalPath}");
				return false;
			}

			text = configFile.OpenText().ReadToEnd();
			return true;
		}
		catch (Exception ex)
		{
			collector.EmitGlobalError($"Failed to read codex configuration '{originalPath}': {ex.Message}", ex);
			return false;
		}
	}

	// Errors are global (no file), as the codex commands have always reported them; returns whether there were none.
	private static bool Report(IReadOnlyList<Diagnostic> findings, IDiagnosticsCollector collector)
	{
		var ok = true;
		foreach (var finding in findings)
		{
			if (finding.Severity == Severity.Error)
			{
				collector.EmitGlobalError(finding.Message);
				ok = false;
			}
			else
				collector.Write(finding);
		}
		return ok;
	}
}
