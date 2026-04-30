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

/// <summary>Fetch and manage the central assembler configuration repository.</summary>
internal sealed class ConfigurationCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration assemblyConfiguration
)
{
	/// <summary>Fetch the assembler configuration into local application data.</summary>
	/// <remarks>
	/// All assembler and codex commands read their repository list from a central configuration repository.
	/// Run this once before the first <c>assembler clone</c> or <c>assemble</c> invocation, and whenever
	/// the configuration has changed upstream.
	/// </remarks>
	/// <param name="gitRef">Git ref to fetch. Defaults to <c>main</c>.</param>
	/// <param name="local">Write the configuration into <c>cwd</c> so subsequent commands treat it as a local override.</param>
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
