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

internal sealed class BloomFilterCommands(ILoggerFactory logFactory, IDiagnosticsCollector collector)
{
	/// <summary>Generate the bloom filter binary file from a local <c>elastic/built-docs</c> repository.</summary>
	/// <param name="builtDocsDir">Path to the local <c>elastic/built-docs</c> repository</param>
	[NoOptionsInjection]
	public async Task<int> Create(string builtDocsDir, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var pagesProvider = new LocalPagesProvider(builtDocsDir);
		var legacyPageService = new LegacyPageService(logFactory);

		serviceInvoker.AddCommand(legacyPageService, pagesProvider, static (s, _, pagesProvider, _) =>
		{
			var result = s.GenerateBloomFilterBinary(pagesProvider);
			return Task.FromResult(result);
		});
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Look up whether a path exists in the bloom filter.</summary>
	/// <param name="path">The URL path to look up</param>
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
