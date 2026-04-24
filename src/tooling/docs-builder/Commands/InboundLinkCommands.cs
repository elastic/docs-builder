// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.InboundLinks;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands;

internal sealed class InboundLinkCommands(ILoggerFactory logFactory, IDiagnosticsCollector collector)
{
	private readonly LinkIndexService _linkIndexService = new(logFactory, FileSystemFactory.RealRead);

	/// <summary>Validate all published cross-links across all published <c>links.json</c> files.</summary>
	[NoOptionsInjection]
	public async Task<int> ValidateAll(CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		serviceInvoker.AddCommand(_linkIndexService, static async (s, collector, ctx) => await s.CheckAll(collector, ctx));
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Validate cross-links for a specific repository.</summary>
	/// <param name="from">Source repository</param>
	/// <param name="to">Target repository</param>
	[NoOptionsInjection]
	public async Task<int> Validate(string? from = null, string? to = null, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		serviceInvoker.AddCommand(_linkIndexService, (to, from),
			static async (s, collector, state, ctx) => await s.CheckRepository(collector, state.to, state.from, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>
	/// Validate a locally published <c>links.json</c> against all published link registries.
	/// </summary>
	/// <param name="file">Path to <c>links.json</c>. Defaults to <c>.artifacts/docs/html/links.json</c></param>
	/// <param name="path">-p, Defaults to the <c>cwd</c> folder</param>
	[NoOptionsInjection]
	public async Task<int> ValidateLinkReference(string? file = null, string? path = null, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		serviceInvoker.AddCommand(_linkIndexService, (file, path),
			static async (s, collector, state, ctx) => await s.CheckWithLocalLinksJson(collector, state.file, state.path, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
