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
using Elastic.Documentation.Tooling.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Documentation.Builder.Cli;

internal sealed class DiffCommands(ILoggerFactory logger, ICoreService githubActionsService, IOptions<VersionsConfiguration> versionsConfigOption)
{
	/// <summary>
	/// Validates redirect updates in the current branch using the redirects file against changes reported by git.
	/// </summary>
	/// <param name="path">The baseline path to perform the check</param>
	/// <param name="ctx"></param>
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	[Command("validate")]
	public async Task<int> ValidateRedirects([Argument] string? path = null, Cancel ctx = default)
	{
		path ??= "docs";

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService).StartAsync(ctx);

		var fs = new FileSystem();
		var root = fs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);

		var buildContext = new BuildContext(collector, fs, fs, versionsConfigOption.Value, root.FullName, null);
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

		var tracker = new LocalGitRepositoryTracker(collector, root);
		var changed = tracker.GetChangedFiles(path);

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
