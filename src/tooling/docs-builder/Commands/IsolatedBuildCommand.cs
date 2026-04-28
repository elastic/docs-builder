// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands;

internal sealed class IsolatedBuildCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	ICoreService githubActionsService,
	IConfigurationContext configurationContext,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>Build a single documentation set from source.</summary>
	/// <remarks>
	/// Locates the documentation root by searching for a <c>docset.yml</c> file starting at <paramref name="options"/> <c>.Path</c>.
	/// The output directory is wiped and rebuilt on each run unless incremental build detects no changes.
	/// </remarks>
	[DefaultCommand]
	[CommandName("build")]
	public async Task<int> Build(
		GlobalCliOptions _,
		[AsParameters] IsolatedBuildOptions options,
		bool inMemory = false,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new IsolatedBuildService(logFactory, configurationContext, githubActionsService, environmentVariables);
		var readFs = inMemory ? FileSystemFactory.InMemory() : FileSystemFactory.RealGitRootForPath(options.Path);
		var writeFs = inMemory ? null : FileSystemFactory.RealGitRootForPathWrite(options.Path, options.Output);
		var strictCommand = service.IsStrict(options.Strict);

		serviceInvoker.AddCommand(service, (options, readFs, writeFs), strictCommand,
			static async (s, col, state, ctx) => await s.Build(col, state.readFs, state.options, state.writeFs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
