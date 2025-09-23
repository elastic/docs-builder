// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using ConsoleAppFramework;
using Elastic.Documentation.Assembler.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class ConfigurationCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration assemblyConfiguration
)
{
	/// <summary> Clone the configuration folder </summary>
	/// <param name="gitRef">The git reference of the config, defaults to 'main'</param>
	/// <param name="ctx"></param>
	[Command("init")]
	public async Task<int> CloneConfigurationFolder(string? gitRef = null, Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = new FileSystem();
		var service = new ConfigurationCloneService(logFactory, assemblyConfiguration, fs);
		serviceInvoker.AddCommand(service, gitRef,
			static async (s, collector, gitRef, ctx) => await s.InitConfigurationToApplicationData(collector, gitRef, ctx));
		return await serviceInvoker.InvokeAsync(ctx);
	}
}
