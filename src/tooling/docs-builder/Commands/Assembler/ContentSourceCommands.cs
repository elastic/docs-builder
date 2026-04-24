// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.ContentSources;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class ContentSourceCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	/// <summary>Validate that all configured repositories have been published.</summary>
	[NoOptionsInjection]
	public async Task<int> Validate(CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealRead;
		var service = new RepositoryPublishValidationService(logFactory, configuration, configurationContext, fs);
		serviceInvoker.AddCommand(service, static async (s, collector, ctx) => await s.ValidatePublishStatus(collector, ctx));

		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Match a repository to a branch or tag and determine whether it should be built.</summary>
	/// <param name="repository">Repository to match</param>
	/// <param name="branchOrTag">Branch or tag to match against</param>
	[NoOptionsInjection]
	public async Task<int> Match([Argument] string? repository = null, [Argument] string? branchOrTag = null, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealRead;
		var service = new RepositoryBuildMatchingService(logFactory, configuration, configurationContext, githubActionsService, fs);
		serviceInvoker.AddCommand(service, (repository, branchOrTag),
			static async (s, collector, state, ctx) =>
			{
				_ = await s.ShouldBuild(collector, state.repository, state.branchOrTag, ctx);
				return true;
			});

		return await serviceInvoker.InvokeAsync(ct);
	}
}
