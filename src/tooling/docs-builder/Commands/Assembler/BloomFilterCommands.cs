// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LegacyDocs;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class BloomFilterCommands(ILoggerFactory logFactory, IDiagnosticsCollector collector)
{
	/// <summary> Generate the bloom filter binary file </summary>
	/// <param name="builtDocsDir">The local dir of local elastic/built-docs repository</param>
	/// <param name="ctx"></param>
	[Command("create")]
	public async Task<int> CreateBloomBin(string builtDocsDir, Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var pagesProvider = new LocalPagesProvider(builtDocsDir);
		var legacyPageService = new LegacyPageService(logFactory);

		serviceInvoker.AddCommand(legacyPageService, pagesProvider, static (s, _, pagesProvider, _) =>
		{
			var result = s.GenerateBloomFilterBinary(pagesProvider);
			return Task.FromResult(result);
		});
		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>Lookup whether <paramref name="path"/> exists in the bloomfilter </summary>
	/// <param name="path">The local dir of local elastic/built-docs repository</param>
	/// <param name="ctx"></param>
	[Command("lookup")]
	public async Task<int> PageExists(string path, Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var legacyPageService = new LegacyPageService(logFactory);
		serviceInvoker.AddCommand(legacyPageService, path, static (s, _, path, _) =>
		{
			var result = s.PathExists(path, logResult: true);
			return Task.FromResult(result);
		});
		return await serviceInvoker.InvokeAsync(ctx);
	}
}
