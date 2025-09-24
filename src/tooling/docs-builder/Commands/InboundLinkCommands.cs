// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using ConsoleAppFramework;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.InboundLinks;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed class InboundLinkCommands(ILoggerFactory logFactory, IDiagnosticsCollector collector)
{
	private readonly LinkIndexService _linkIndexService = new(logFactory, new FileSystem());

	/// <summary> Validate all published cross_links in all published links.json files. </summary>
	/// <param name="ctx"></param>
	[Command("validate-all")]
	public async Task<int> ValidateAllInboundLinks(Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		serviceInvoker.AddCommand(_linkIndexService, static async (s, collector, ctx) => await s.CheckAll(collector, ctx));
		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary> Validate all published cross_links in all published links.json files. </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="ctx"></param>
	[Command("validate")]
	public async Task<int> ValidateRepoInboundLinks(string? from = null, string? to = null, Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		serviceInvoker.AddCommand(_linkIndexService, (to, from),
			static async (s, collector, state, ctx) => await s.CheckRepository(collector, state.to, state.from, ctx)
		);
		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Validate a locally published links.json file against all published links.json files in the registry
	/// </summary>
	/// <param name="file">Path to `links.json` defaults to '.artifacts/docs/html/links.json'</param>
	/// <param name="path"> -p, Defaults to the `{pwd}` folder</param>
	/// <param name="ctx"></param>
	[Command("validate-link-reference")]
	public async Task<int> ValidateLocalLinkReference(string? file = null, string? path = null, Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		serviceInvoker.AddCommand(_linkIndexService, (file, path),
			static async (s, collector, state, ctx) => await s.CheckWithLocalLinksJson(collector, state.file, state.path, ctx)
		);
		return await serviceInvoker.InvokeAsync(ctx);
	}
}
