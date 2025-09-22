// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Sourcing;

public class AssemblerCloneService(
	ILoggerFactory logFactory,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
) : IService
{
	public async Task<bool> CloneAll(IDiagnosticsCollector collector, bool? strict, string? environment, bool? fetchLatest, bool? assumeCloned, Cancel ctx)
	{
		strict ??= false;
		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		var fs = new FileSystem();
		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, fs, fs, null, null);
		var cloner = new AssemblerRepositorySourcer(logFactory, assembleContext);

		_ = await cloner.CloneAll(fetchLatest ?? false, assumeCloned ?? false, ctx);

		return strict.Value ? collector.Errors + collector.Warnings > 0 : collector.Errors > 0;
	}
}
