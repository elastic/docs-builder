// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LegacyDocs;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

/// <summary>Build and query the bloom filter used for legacy-URL redirect coverage.</summary>
internal sealed class BloomFilterCommands(ILoggerFactory logFactory, IDiagnosticsCollector collector)
{
	/// <summary>Build a bloom filter binary from a local legacy-docs repository.</summary>
	/// <remarks>
	/// The bloom filter is a compact data structure that records which legacy URLs existed before migration.
	/// It is used to verify redirect coverage: if a legacy URL is absent from the filter, any redirect
	/// pointing to it cannot be validated. Run once after cloning the legacy-docs repository.
	/// </remarks>
	/// <param name="builtDocsDir">Path to the local legacy-docs repository checkout.</param>
	[NoOptionsInjection]
	public async Task<int> Create(DirectoryInfo builtDocsDir, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var pagesProvider = new LocalPagesProvider(builtDocsDir.FullName);
		var legacyPageService = new LegacyPageService(logFactory);

		serviceInvoker.AddCommand(legacyPageService, pagesProvider, static (s, _, pagesProvider, _) =>
		{
			var result = s.GenerateBloomFilterBinary(pagesProvider);
			return Task.FromResult(result);
		});
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Test whether a URL path is recorded in the bloom filter.</summary>
	/// <param name="path">URL path to look up (e.g. <c>/guide/en/elasticsearch/reference/current/index.html</c>).</param>
	[NoOptionsInjection]
	public async Task<int> Lookup(string path, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var legacyPageService = new LegacyPageService(logFactory);
		serviceInvoker.AddCommand(legacyPageService, path, static (s, _, path, _) =>
		{
			var result = s.PathExists(path, logResult: true);
			return Task.FromResult(result);
		});
		return await serviceInvoker.InvokeAsync(ct);
	}
}
