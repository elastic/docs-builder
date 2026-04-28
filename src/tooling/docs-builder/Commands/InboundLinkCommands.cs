// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.InboundLinks;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands;

/// <summary>Validate cross-doc-set links against the published link registry.</summary>
/// <remarks>
/// <para>
/// Every documentation set publishes a <c>links.json</c> file containing the URLs of all its pages.
/// These files are aggregated into a shared link registry. Inbound-links commands validate that
/// cross-links between documentation sets resolve to real pages in the registry.
/// </para>
/// </remarks>
internal sealed class InboundLinkCommands(ILoggerFactory logFactory, IDiagnosticsCollector collector)
{
	private readonly LinkIndexService _linkIndexService = new(logFactory, FileSystemFactory.RealRead);

	/// <summary>Validate all cross-links across every published <c>links.json</c> in the registry.</summary>
	[NoOptionsInjection]
	public async Task<int> ValidateAll(CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		serviceInvoker.AddCommand(_linkIndexService, static async (s, collector, ctx) => await s.CheckAll(collector, ctx));
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Validate all cross-links originating from or targeting a specific repository.</summary>
	/// <param name="from">Only check links published by this repository slug.</param>
	/// <param name="to">Only check links that point to this repository slug.</param>
	[NoOptionsInjection]
	public async Task<int> Validate(string? from = null, string? to = null, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		serviceInvoker.AddCommand(_linkIndexService, (to, from),
			static async (s, collector, state, ctx) => await s.CheckRepository(collector, state.to, state.from, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Validate a locally built <c>links.json</c> against the published link registry.</summary>
	/// <remarks>
	/// Use this to verify cross-links before publishing. The local <c>links.json</c> is checked against
	/// all currently published registries to ensure every outbound cross-link resolves.
	/// </remarks>
	/// <param name="file">Path to <c>links.json</c>. Defaults to <c>.artifacts/docs/html/links.json</c>.</param>
	/// <param name="path">-p, Root of the documentation source. Defaults to <c>cwd</c>.</param>
	[NoOptionsInjection]
	public async Task<int> ValidateLinkReference([Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "json")] FileInfo? file = null, string? path = null, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		serviceInvoker.AddCommand(_linkIndexService, (file, path),
			static async (s, collector, state, ctx) => await s.CheckWithLocalLinksJson(collector, state.file?.FullName, state.path, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
