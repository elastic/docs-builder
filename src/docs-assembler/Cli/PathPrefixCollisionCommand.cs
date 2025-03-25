// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Elastic.Markdown.InboundLinks;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Discovery;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class PathPrefixCollisionCommand(ILoggerFactory logger, ICoreService githubActionsService)
{
	private readonly LinkIndexLinkChecker _linkIndexLinkChecker = new(logger);

	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
	}

	/// <summary> Validate all published cross_links in all published links.json files. </summary>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task<int> DetectCollisions(Cancel ctx = default)
	{
		AssignOutputLogger();
		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);

		var assembleContext = new AssembleContext("dev", collector, new FileSystem(), new FileSystem(), null, null);
		return await _linkIndexLinkChecker.CheckAll(collector, ctx);
	}

}
