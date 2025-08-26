// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Builder.Tracking;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Cli;

internal sealed class DiffCommands(
	ILoggerFactory logFactory,
	ICoreService githubActionsService,
	IConfigurationContext configurationContext
)
{
	private readonly ILogger<Program> _log = logFactory.CreateLogger<Program>();

	/// <summary>
	/// Validates redirect updates in the current branch using the redirect file against changes reported by git.
	/// </summary>
	/// <param name="path"> -p, Defaults to the`{pwd}/docs` folder</param>
	/// <param name="ctx"></param>
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	[Command("validate")]
	public async Task<int> ValidateRedirects(string? path = null, Cancel ctx = default)
	{
		var runningOnCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService).StartAsync(ctx);

		var fs = new FileSystem();

		var buildContext = new BuildContext(collector, fs, fs, configurationContext, ExportOptions.MetadataOnly, path, null);
		var redirectFile = new RedirectFile(buildContext);
		if (!redirectFile.Source.Exists)
		{
			await collector.StopAsync(ctx);
			return 0;
		}

		var redirects = redirectFile.Redirects;

		if (redirects is null)
		{
			collector.EmitError(redirectFile.Source, "It was not possible to parse the redirects file.");
			await collector.StopAsync(ctx);
			return collector.Errors;
		}

		var root = Paths.DetermineSourceDirectoryRoot(buildContext.DocumentationSourceDirectory);
		if (root is null)
		{
			collector.EmitError(redirectFile.Source, $"Unable to determine the root of the source directory {buildContext.DocumentationSourceDirectory}.");
			await collector.StopAsync(ctx);
			return collector.Errors;
		}
		var relativePath = Path.GetRelativePath(root.FullName, buildContext.DocumentationSourceDirectory.FullName);
		_log.LogInformation("Using relative path {RelativePath} for validating changes", relativePath);
		IRepositoryTracker tracker = runningOnCi ? new IntegrationGitRepositoryTracker(relativePath) : new LocalGitRepositoryTracker(collector, root, relativePath);
		var changed = tracker.GetChangedFiles()
			.Where(c =>
			{
				var fi = fs.FileInfo.New(c.FilePath);
				return fi.Extension is ".md" && !fi.HasParent("_snippets");
			})
			.ToArray();

		if (changed.Length != 0)
			_log.LogInformation("Found {Count} changes to files related to documentation in the current branch.", changed.Length);

		var missingRedirects = changed
			.Where(c =>
				c.ChangeType is GitChangeType.Deleted or GitChangeType.Renamed
				&& !redirects.ContainsKey(c is RenamedGitChange renamed ? renamed.OldFilePath : c.FilePath)
			)
			.ToArray();

		if (missingRedirects.Length != 0)
		{
			var relativeRedirectFile = Path.GetRelativePath(root.FullName, redirectFile.Source.FullName);
			_log.LogInformation("Found {Count} changes that still require updates to: {RedirectFile}", missingRedirects.Length, relativeRedirectFile);
		}

		foreach (var notFound in missingRedirects)
		{
			if (notFound is RenamedGitChange renamed)
			{
				collector.EmitError(redirectFile.Source,
					$"File '{renamed.OldFilePath}' was renamed to '{renamed.NewFilePath}' but it has no redirect configuration set.");
			}
			else if (notFound.ChangeType is GitChangeType.Deleted)
			{
				collector.EmitError(redirectFile.Source,
					$"File '{notFound.FilePath}' was deleted but it has no redirect targets. This will lead to broken links.");
			}
		}

		await collector.StopAsync(ctx);
		return collector.Errors;
	}
}
