// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Assembler.Navigation;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Elastic.Documentation.Tooling.Filters;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Discovery;
using Elastic.Markdown.Links.InboundLinks;
using Elastic.Markdown.Links.LinkNamespaces;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class NavigationCommands(ILoggerFactory logger, ICoreService githubActionsService)
{
	private readonly LinkIndexLinkChecker _linkIndexLinkChecker = new(logger);

	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
	}

	/// <summary> Validates navigation.yml does not contain colliding path prefixes </summary>
	/// <param name="ctx"></param>
	[Command("validate")]
	[ConsoleAppFilter<StopwatchFilter>]
	[ConsoleAppFilter<CatchExceptionFilter>]
	public async Task<int> Validate(Cancel ctx = default)
	{
		AssignOutputLogger();
		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);
		var assembleContext = new AssembleContext("dev", collector, new FileSystem(), new FileSystem(), null, null);
		_ = collector.StartAsync(ctx);

		// this validates all path prefixes are unique, early exit if duplicates are detected
		if (!GlobalNavigationFile.ValidatePathPrefixes(assembleContext) || assembleContext.Collector.Errors > 0)
		{
			assembleContext.Collector.Channel.TryComplete();
			await assembleContext.Collector.StopAsync(ctx);
			return 1;
		}

		return 0;
	}

	/// <summary> Validate all published links in links.json do not collide with navigation path_prefixes. </summary>
	/// <param name="file">Path to `links.json` defaults to '.artifacts/docs/html/links.json'</param>
	/// <param name="ctx"></param>
	[Command("validate-link-reference")]
	[ConsoleAppFilter<StopwatchFilter>]
	[ConsoleAppFilter<CatchExceptionFilter>]
	public async Task<int> ValidateLocalLinkReference([Argument] string? file = null, Cancel ctx = default)
	{
		AssignOutputLogger();
		file ??= ".artifacts/docs/html/links.json";

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);
		_ = collector.StartAsync(ctx);

		var assembleContext = new AssembleContext("dev", collector, new FileSystem(), new FileSystem(), null, null);

		var fs = new FileSystem();
		var root = fs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		var repository = GitCheckoutInformation.Create(root, new FileSystem(), logger.CreateLogger(nameof(GitCheckoutInformation))).RepositoryName
						?? throw new Exception("Unable to determine repository name");

		var prefixes = GlobalNavigationFile.GetAllPathPrefixes(assembleContext);

		var namespaceChecker = new LinkGlobalNamespaceChecker(logger, prefixes);

		await namespaceChecker.CheckWithLocalLinksJson(assembleContext.Collector, repository, file, ctx);

		return await _linkIndexLinkChecker.CheckAll(collector, ctx);
	}

}
