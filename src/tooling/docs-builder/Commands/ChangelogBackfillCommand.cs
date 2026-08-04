// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Elastic.Changelog.Backfill.Inventory;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

namespace Documentation.Builder.Commands;

/// <summary>Backfill historical release-note bundles (docs-eng-team#656): census, planning, and guarded publication.</summary>
internal sealed class ChangelogBackfillCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	AssemblyConfiguration assemblyConfiguration
)
{
	/// <summary>Build the backfill census: an inventory document covering every release-notes product.</summary>
	/// <remarks>
	/// Enumerates every product in <c>products.yml</c> that participates in release notes (the
	/// <c>release-notes</c> feature defaults to enabled, so <c>products.yml</c> alone cannot say which
	/// products have release-note surfaces), merges in the hand-maintained census seed, and writes the
	/// versioned inventory document that backfill planning consumes. Products the seed does not cover
	/// stay visible as <c>source-unresolved</c> entries with a warning — an unresolved scope must never
	/// silently produce empty bundles. Attributed repositories are checked against the link allowlist in
	/// the local <c>assembler.yml</c>; planning re-validates against the deployed scrubber allowlist
	/// before any upload. This command only reads configuration and writes a local file: no S3 access,
	/// no writes to any remote system.
	/// </remarks>
	/// <param name="sources">Path to the census seed YAML mapping products to their release-note sources. Without it, every release-notes product is reported as source-unresolved.</param>
	/// <param name="output">Where to write the inventory document JSON.</param>
	/// <param name="ct">Cancellation token</param>
	[NoOptionsInjection]
	public async Task<int> Inventory(
		[Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo? sources = null,
		[ExpandUserProfile, RejectSymbolicLinks] string output = "backfill-inventory.json",
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new InventoryCensusService(logFactory, configurationContext, FileSystemFactory.RealGitRootForPathWrite(null, output));
		var args = new BuildInventoryArguments
		{
			SourcesPath = sources?.FullName,
			OutputPath = output,
			AllowRepos = LinkAllowlistSanitizer.BuildAllowReposFromAssembler(assemblyConfiguration)
		};
		serviceInvoker.AddCommand(service, args,
			static async (s, c, state, ct) => await s.BuildInventoryAsync(c, state, ct)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
