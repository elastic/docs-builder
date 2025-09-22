// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation.Assembler.Repository;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Services;
using Elastic.Documentation.Tooling;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class ContentSourceCommands(
	ILoggerFactory logFactory,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	[Command("validate")]
	public async Task<int> Validate(Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(new ConsoleDiagnosticsCollector(logFactory, githubActionsService) { NoHints = true });

		var fs = new FileSystem();
		var service = new RepositoryPublishValidationService(logFactory, configuration, configurationContext, fs);
		serviceInvoker.AddCommand(service, static async (s, collector, ctx) => await s.ValidatePublishStatus(collector, ctx));

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>  </summary>
	/// <param name="repository"></param>
	/// <param name="branchOrTag"></param>
	/// <param name="ctx"></param>
	[Command("match")]
	public async Task<int> Match([Argument] string? repository = null, [Argument] string? branchOrTag = null, Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(new ConsoleDiagnosticsCollector(logFactory, githubActionsService) { NoHints = true });

		var fs = new FileSystem();
		var service = new RepositoryBuildMatchingService(logFactory, configuration, configurationContext, githubActionsService, fs);
		serviceInvoker.AddCommand(service, (repository, branchOrTag),
			static async (s, state, collector, _) => await s.ShouldBuild(collector, state.repository, state.branchOrTag)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

}
