// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Codex;

/// <summary>What a loaded codex configuration is for; <see cref="Publish"/> also requires what publishing needs.</summary>
internal enum CodexLoadPurpose
{
	/// <summary>Read the configuration (update redirects, ...): only the file's own rules apply.</summary>
	Read,

	/// <summary>Publish the codex: additionally checks <see cref="CodexPublishingConstraints"/>, e.g. that it names its environment.</summary>
	Publish
}

internal static class CodexConfigurationLoader
{
	/// <summary>
	/// Loads a <see cref="CodexConfiguration"/> from <paramref name="configFile"/> and checks it once: against
	/// <see cref="CodexConfigurationConstraints"/>, and for <see cref="CodexLoadPurpose.Publish"/> also <see cref="CodexPublishingConstraints"/>.
	/// Every violation is written to <paramref name="collector"/>. Returns <c>false</c> when the file is missing or unreadable or a rule at error severity failed.
	/// </summary>
	internal static bool Load(
		IFileInfo configFile,
		string originalPath,
		IDiagnosticsCollector collector,
		CodexLoadPurpose purpose,
		out CodexConfiguration config
	)
	{
		config = default!;
		try
		{
			if (!configFile.Exists)
			{
				collector.EmitGlobalError($"Codex configuration file not found: {originalPath}");
				return false;
			}

			config = CodexSiteLoader.Load(configFile);
			var ok = collector.Write(CodexConfigurationConstraints.All.Report(config), originalPath);
			if (purpose == CodexLoadPurpose.Publish)
				ok &= collector.Write(CodexPublishingConstraints.All.Report(config), originalPath);
			return ok;
		}
		catch (Exception ex)
		{
			collector.EmitGlobalError($"Failed to read codex configuration '{originalPath}': {ex.Message}", ex);
			return false;
		}
	}
}
