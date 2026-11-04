// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Actions.Core.Services;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

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
	[CommandIntent(Intent.Idempotent)]
	[MutationScope(MutationScope.Directory)]
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
		IFileSystem? writeFs = inMemory ? new MockFileSystem() : null;
		var strictCommand = service.IsStrict(options.Strict);

		serviceInvoker.AddCommand(
			service,
			(options, writeFs),
			strictCommand,
			static async (s, col, state, ctx) => await s.Build(col, state.options, state.writeFs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
