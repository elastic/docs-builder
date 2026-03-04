// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using ConsoleAppFramework;
using Elastic.Documentation.Assembler.Deploying;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands.Codex;

/// <summary>
/// Command for updating CloudFront KeyValueStore redirects for codex.
/// </summary>
internal sealed class CodexUpdateRedirectsCommand(
	IDiagnosticsCollector collector,
	ILoggerFactory logFactory
)
{
	/// <summary>Refreshes the redirects mapping in CloudFront's KeyValueStore for codex.</summary>
	/// <param name="config">Path to the codex configuration file (used to resolve environment).</param>
	/// <param name="environment">The environment to deploy to. Defaults to config or ENVIRONMENT env var.</param>
	/// <param name="redirectsFile">Path to the redirects mapping. Defaults to .artifacts/codex/docs/redirects.json.</param>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task<int> Run(
		[Argument] string config,
		string? environment = null,
		string? redirectsFile = null,
		Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = new FileSystem();
		var configPath = fs.Path.GetFullPath(config);
		var configFile = fs.FileInfo.New(configPath);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {configPath}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);
		var resolvedEnvironment = environment
			?? codexConfig.Environment
			?? Environment.GetEnvironmentVariable("ENVIRONMENT")
			?? "internal";

		var service = new DeployUpdateRedirectsService(logFactory, fs);
		serviceInvoker.AddCommand(service, (environment: resolvedEnvironment, redirectsFile, kvsNamePrefix: "codex", defaultRedirectsFile: ".artifacts/codex/docs/redirects.json"),
			static async (s, col, state, c) => await s.UpdateRedirects(col, state.environment, state.redirectsFile, state.kvsNamePrefix, state.defaultRedirectsFile, c)
		);
		return await serviceInvoker.InvokeAsync(ctx);
	}
}
