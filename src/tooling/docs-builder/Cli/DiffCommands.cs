// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Builder.Tracking;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Cli;

internal sealed class DiffCommands(ILoggerFactory logFactory, ICoreService githubActionsService, VersionsConfiguration versionsConfig)
{
	/// <summary>
	/// Validates redirect updates in the current branch using the redirect file against changes reported by git.
	/// </summary>
	/// <param name="path"> -p, Defaults to the`{pwd}/docs` folder</param>
	/// <param name="ctx"></param>
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	[Command("validate")]
	public async Task<int> ValidateRedirects(string? path = null, Cancel ctx = default)
	{
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService).StartAsync(ctx);

		var fs = new FileSystem();

		var buildContext = new BuildContext(collector, fs, fs, versionsConfig, path, null);
		var sourceFile = buildContext.ConfigurationPath;
		var redirectFileName = sourceFile.Name.StartsWith('_') ? "_redirects.yml" : "redirects.yml";
		var redirectFileInfo = sourceFile.FileSystem.FileInfo.New(Path.Combine(sourceFile.Directory!.FullName, redirectFileName));

		var redirectFileParser = new RedirectFile(redirectFileInfo, buildContext);
		var redirects = redirectFileParser.Redirects;

		if (redirects is null)
		{
			collector.EmitError(redirectFileInfo, "It was not possible to parse the redirects file.");
			await collector.StopAsync(ctx);
			return collector.Errors;
		}

		var tracker = new LocalGitRepositoryTracker(collector, buildContext);
		var changed = tracker.GetChangedFiles();

		foreach (var notFound in changed.DistinctBy(c => c.FilePath).Where(c => c.ChangeType is GitChangeType.Deleted or GitChangeType.Renamed
																	&& !redirects.ContainsKey(c is RenamedGitChange renamed ? renamed.OldFilePath : c.FilePath)))
		{
			if (notFound is RenamedGitChange renamed)
			{
				collector.EmitError(redirectFileInfo.Name,
					$"File '{renamed.OldFilePath}' was renamed to '{renamed.NewFilePath}' but it has no redirect configuration set.");
			}
			else if (notFound.ChangeType is GitChangeType.Deleted)
			{
				collector.EmitError(redirectFileInfo.Name,
					$"File '{notFound.FilePath}' was deleted but it has no redirect targets. This will lead to broken links.");
			}
		}

		await collector.StopAsync(ctx);
		return collector.Errors;
	}
}
