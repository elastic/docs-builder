// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

/// <summary>Validate the global navigation structure and cross-doc-set link references.</summary>
internal sealed class NavigationCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext
)
{
	/// <summary>Check <c>navigation.yml</c> for duplicate path prefixes and non-unique URLs.</summary>
	[NoOptionsInjection]
	public async Task<int> Validate(CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new GlobalNavigationService(logFactory, configuration, configurationContext, FileSystemFactory.RealRead);
		serviceInvoker.AddCommand(service, static async (s, collector, ctx) => await s.Validate(collector, ctx));
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Check that no link in a local <c>links.json</c> conflicts with a path prefix defined in <c>navigation.yml</c>.</summary>
	/// <param name="file">Path to <c>links.json</c>. Defaults to <c>.artifacts/docs/html/links.json</c>.</param>
	[NoOptionsInjection]
	public async Task<int> ValidateLinkReference([Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "json")] FileInfo? file = null, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new GlobalNavigationService(logFactory, configuration, configurationContext, FileSystemFactory.RealRead);
		serviceInvoker.AddCommand(service, file, static async (s, collector, file, ctx) => await s.ValidateLocalLinkReference(collector, file?.FullName, ctx));
		return await serviceInvoker.InvokeAsync(ct);
	}
}
