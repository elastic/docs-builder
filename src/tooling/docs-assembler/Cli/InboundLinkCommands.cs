// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Elastic.Markdown.Links.InboundLinks;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class InboundLinkCommands(ILoggerFactory logFactory, ICoreService githubActionsService)
{
	private readonly LinkIndexLinkChecker _linkIndexLinkChecker = new(logFactory);
	private readonly ILogger<Program> _log = logFactory.CreateLogger<Program>();

	/// <summary> Validate all published cross_links in all published links.json files. </summary>
	/// <param name="ctx"></param>
	[Command("validate-all")]
	public async Task<int> ValidateAllInboundLinks(Cancel ctx = default)
	{
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService).StartAsync(ctx);
		await _linkIndexLinkChecker.CheckAll(collector, ctx);
		await collector.StopAsync(ctx);
		return collector.Errors;
	}

	/// <summary> Validate all published cross_links in all published links.json files. </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="ctx"></param>
	[Command("validate")]
	public async Task<int> ValidateRepoInboundLinks(string? from = null, string? to = null, Cancel ctx = default)
	{
		var fs = new FileSystem();
		var root = fs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		if (from == null && to == null)
		{
			from ??= GitCheckoutInformation.Create(root, new FileSystem(), logFactory.CreateLogger(nameof(GitCheckoutInformation))).RepositoryName;
			if (from == null)
				throw new Exception("Unable to determine repository name");
		}
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService).StartAsync(ctx);
		await _linkIndexLinkChecker.CheckRepository(collector, to, from, ctx);
		await collector.StopAsync(ctx);
		return collector.Errors;
	}

	/// <summary>
	/// Validate a locally published links.json file against all published links.json files in the registry
	/// </summary>
	/// <param name="file">Path to `links.json` defaults to '.artifacts/docs/html/links.json'</param>
	/// <param name="path"> -p, Defaults to the `{pwd}` folder</param>
	/// <param name="ctx"></param>
	[Command("validate-link-reference")]
	public async Task<int> ValidateLocalLinkReference(string? file = null, string? path = null, Cancel ctx = default)
	{
		file ??= ".artifacts/docs/html/links.json";
		var fs = new FileSystem();
		var root = !string.IsNullOrEmpty(path) ? fs.DirectoryInfo.New(path) : fs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		var repository = GitCheckoutInformation.Create(root, fs, logFactory.CreateLogger(nameof(GitCheckoutInformation))).RepositoryName
						?? throw new Exception("Unable to determine repository name");

		var resolvedFile = fs.FileInfo.New(Path.Combine(root.FullName, file));

		var runningOnCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
		if (runningOnCi && !resolvedFile.Exists)
		{
			_log.LogInformation("Running on CI after a build that produced no {File}, skipping the validation", resolvedFile.FullName);
			return 0;
		}
		if (runningOnCi && !Paths.TryFindDocsFolderFromRoot(fs, root, out _, out _))
		{
			_log.LogInformation("Running on CI, {Directory} has no documentation, skipping the validation", root.FullName);
			return 0;
		}

		_log.LogInformation("Validating {File} in {Directory}", file, root.FullName);

		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService).StartAsync(ctx);
		await _linkIndexLinkChecker.CheckWithLocalLinksJson(collector, repository, resolvedFile.FullName, ctx);
		await collector.StopAsync(ctx);
		return collector.Errors;
	}
}
