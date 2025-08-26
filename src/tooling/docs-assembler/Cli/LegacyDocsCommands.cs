// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation.LegacyDocs;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class LegacyDocsCommands(ILoggerFactory logFactory, ICoreService githubActionsService)
{
	private readonly ILogger<Program> _log = logFactory.CreateLogger<Program>();

	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		ConsoleApp.Log = msg => _log.LogInformation(msg);
		ConsoleApp.LogError = msg => _log.LogError(msg);
	}

	/// <summary> Generate the bloom filter binary file </summary>
	/// <param name="builtDocsDir">The local dir of local elastic/built-docs repository</param>
	/// <param name="ctx"></param>
	public async Task<int> CreateBloomBin(string builtDocsDir, Cancel ctx = default)
	{
		AssignOutputLogger();
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService)
		{
			NoHints = true
		}.StartAsync(ctx);
		var pagesProvider = new LocalPagesProvider(builtDocsDir);
		var legacyPageChecker = new LegacyPageChecker();
		legacyPageChecker.GenerateBloomFilterBinary(pagesProvider);
		await collector.StopAsync(ctx);
		return collector.Errors;
	}

	/// <summary> Generate the bloom filter binary file </summary>
	/// <param name="path">The local dir of local elastic/built-docs repository</param>
	/// <param name="ctx"></param>
	public async Task<int> PageExists(string path, Cancel ctx = default)
	{
		AssignOutputLogger();
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService)
		{
			NoHints = true
		}.StartAsync(ctx);
		var legacyPageChecker = new LegacyPageChecker();
		var result = legacyPageChecker.PathExists(path);
		Console.WriteLine(result ? "exists" : "does not exist");
		await collector.StopAsync(ctx);
		return collector.Errors;
	}
}
