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

internal sealed class InboundLinkCommands(ILoggerFactory logger, ICoreService githubActionsService)
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
	[Command("validate-all")]
	public async Task<int> ValidateAllInboundLinks(Cancel ctx = default)
	{
		AssignOutputLogger();
		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);
		return await _linkIndexLinkChecker.CheckAll(collector, ctx);
	}

	/// <summary> Validate all published cross_links in all published links.json files. </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="ctx"></param>
	[Command("validate")]
	public async Task<int> ValidateRepoInboundLinks(string? from = null, string? to = null, Cancel ctx = default)
	{
		AssignOutputLogger();
		var fs = new FileSystem();
		var root = fs.DirectoryInfo.New(Paths.Root.FullName);
		if (from == null && to == null)
		{
			from ??= GitCheckoutInformation.Create(root, new FileSystem(), logger.CreateLogger(nameof(GitCheckoutInformation))).RepositoryName;
			if (from == null)
				throw new Exception("Unable to determine repository name");
		}
		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);
		return await _linkIndexLinkChecker.CheckRepository(collector, to, from, ctx);
	}

	/// <summary>
	/// Validate a locally published links.json file against all published links.json files in the registry
	/// </summary>
	/// <param name="file">Path to `links.json` defaults to '.artifacts/docs/html/links.json'</param>
	/// <param name="ctx"></param>
	[Command("validate-link-reference")]
	public async Task<int> ValidateLocalLinkReference([Argument] string? file = null, Cancel ctx = default)
	{
		AssignOutputLogger();
		file ??= ".artifacts/docs/html/links.json";
		var fs = new FileSystem();
		var root = fs.DirectoryInfo.New(Paths.Root.FullName);
		var repository = GitCheckoutInformation.Create(root, new FileSystem(), logger.CreateLogger(nameof(GitCheckoutInformation))).RepositoryName
						?? throw new Exception("Unable to determine repository name");

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);
		return await _linkIndexLinkChecker.CheckWithLocalLinksJson(collector, repository, file, ctx);
	}
}
