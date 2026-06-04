// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Refactor.Tracking;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands;

internal sealed class DiffCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>Verify every renamed or removed page in the current branch has a redirect entry.</summary>
	/// <remarks>
	/// Compares the git diff of the working branch against the redirect file. Exits 1 if any moved
	/// or deleted page is missing a redirect entry. Run before merging to catch broken links early.
	/// </remarks>
	/// <param name="path">-p, Root of the documentation source. Defaults to <c>cwd/docs</c>.</param>
	[NoOptionsInjection]
	[CommandName("diff")]
	public async Task<int> Validate(string? path = null, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new LocalChangeTrackingService(logFactory, configurationContext);
		var fs = FileSystemFactory.RealGitRootForPath(path);

		serviceInvoker.AddCommand(service, (path, fs),
			async static (s, collector, state, _) => await s.ValidateRedirects(collector, state.path, state.fs)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
