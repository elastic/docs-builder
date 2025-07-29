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
using static Documentation.Builder.Tracking.FileChangeType;

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
		var redirectFile = new RedirectFile(buildContext);
		var redirects = redirectFile.Redirects;

		if (redirects is null)
		{
			collector.EmitError(redirectFile.Source, "It was not possible to parse the redirects file.");
			await collector.StopAsync(ctx);
			return collector.Errors;
		}

		var tracker = new LocalGitRepositoryTracker(collector, buildContext);
		var changed = tracker.GetChangedFiles();

		var noRedirects = changed.DistinctBy(c => c.FilePath)
			.Where(c => c.ChangeType is Deleted or Renamed)
			.Where(c => !redirects.ContainsKey(c is RenamedFileChange renamed ? renamed.OldFilePath : c.FilePath))
			.ToArray();

		foreach (var notFound in noRedirects)
		{
			if (notFound is RenamedFileChange renamed)
			{
				collector.EmitError(redirectFile.Source,
					$"File '{renamed.OldFilePath}' was renamed to '{renamed.NewFilePath}' but it has no redirect configuration set.");
			}
			else if (notFound.ChangeType is Deleted)
			{
				collector.EmitError(redirectFile.Source,
					$"File '{notFound.FilePath}' was deleted but it has no redirect targets. This will lead to broken links.");
			}
		}

		await collector.StopAsync(ctx);
		return collector.Errors;
	}
}
