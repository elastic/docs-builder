// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Configuration;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class ConfigurationCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration assemblyConfiguration
)
{
	/// <summary>Clone the assembler configuration folder into application data.</summary>
	/// <param name="gitRef">Git reference to clone. Defaults to <c>main</c></param>
	/// <param name="local">Save the remote configuration locally in <c>pwd</c> so later commands can pick it up as a local source</param>
	[NoOptionsInjection]
	public async Task<int> Init(string? gitRef = null, bool local = false, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealRead;
		var service = new ConfigurationCloneService(logFactory, assemblyConfiguration, fs);
		serviceInvoker.AddCommand(service, (gitRef, local), static async (s, collector, state, ctx) =>
			await s.InitConfigurationToApplicationData(collector, state.gitRef, state.local, ctx));
		return await serviceInvoker.InvokeAsync(ct);
	}
}
