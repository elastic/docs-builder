// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using ConsoleAppFramework;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

// TODO This copy is scheduled for deletion soon
internal sealed class NavigationCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext
)
{
	/// <summary> Validates navigation.yml does not contain colliding path prefixes </summary>
	/// <param name="ctx"></param>
	[Command("validate")]
	public async Task<int> Validate(Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new GlobalNavigationService(logFactory, configuration, configurationContext, new FileSystem());
		serviceInvoker.AddCommand(service, static async (s, collector, ctx) => await s.Validate(collector, ctx));
		return await serviceInvoker.InvokeAsync(ctx);
	}


	/// <summary> Validate all published links in links.json do not collide with navigation path_prefixes. </summary>
	/// <param name="file">Path to `links.json` defaults to '.artifacts/docs/html/links.json'</param>
	/// <param name="ctx"></param>
	[Command("validate-link-reference")]
	public async Task<int> ValidateLocalLinkReference([Argument] string? file = null, Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new GlobalNavigationService(logFactory, configuration, configurationContext, new FileSystem());
		serviceInvoker.AddCommand(service, file, static async (s, collector, file, ctx) => await s.ValidateLocalLinkReference(collector, file, ctx));
		return await serviceInvoker.InvokeAsync(ctx);
	}

}
