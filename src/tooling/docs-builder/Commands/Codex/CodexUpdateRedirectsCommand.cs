// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Deploying;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Codex;

/// <summary>Update CloudFront KeyValueStore redirects for a codex deployment.</summary>
internal sealed class CodexUpdateRedirectsCommand(
	IDiagnosticsCollector collector,
	ILoggerFactory logFactory
)
{
	/// <summary>Push the codex redirects mapping to CloudFront's KeyValueStore.</summary>
	/// <remarks>Run after <c>codex build</c> produces a <c>redirects.json</c>.</remarks>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file (used to resolve the environment).</param>
	/// <param name="environment">Named deployment target. Defaults to the value in <c>codex.yml</c> or the <c>ENVIRONMENT</c> env var.</param>
	/// <param name="redirectsFile">Path to <c>redirects.json</c>. Defaults to <c>.artifacts/codex/docs/redirects.json</c>.</param>
	public async Task<int> UpdateRedirects(
		GlobalCliOptions _,
		[Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo config,
		string? environment = null,
		[Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "json")] FileInfo? redirectsFile = null,
		CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealRead;
		var configFile = fs.FileInfo.New(config.FullName);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {config.FullName}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);
		var resolvedEnvironment = environment
			?? codexConfig.Environment
			?? Environment.GetEnvironmentVariable("ENVIRONMENT")
			?? "internal";

		var service = new DeployUpdateRedirectsService(logFactory, fs);
		serviceInvoker.AddCommand(service, (environment: resolvedEnvironment, redirectsFile, kvsNamePrefix: "codex", defaultRedirectsFile: ".artifacts/codex/docs/redirects.json"),
			static async (s, col, state, c) => await s.UpdateRedirects(col, state.environment, state.redirectsFile?.FullName, state.kvsNamePrefix, state.defaultRedirectsFile, c)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
