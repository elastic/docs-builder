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
	/// <summary>Verify that every repository in the assembler configuration has an active published entry in the link registry.</summary>
	[NoOptionsInjection]
	public async Task<int> Validate(CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealRead;
		var service = new RepositoryPublishValidationService(logFactory, configuration, configurationContext, fs);
		serviceInvoker.AddCommand(service, static async (s, collector, ctx) => await s.ValidatePublishStatus(collector, ctx));

		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Check whether a repository at a specific branch or tag should be included in the next build.</summary>
	/// <remarks>Exits 0 if the repository matches; 1 otherwise. Useful for conditional CI steps.</remarks>
	/// <param name="repository">Repository slug to match (e.g. <c>elastic/elasticsearch</c>).</param>
	/// <param name="branchOrTag">Branch name or version tag to test against.</param>
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
