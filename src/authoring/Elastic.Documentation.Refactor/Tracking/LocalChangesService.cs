// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Refactor.Tracking;

public class LocalChangeTrackingService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<LocalChangeTrackingService>();

	public Task<bool> ValidateRedirects(IDiagnosticsCollector collector, string? path, FileSystem fs)
	{
		var runningOnCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

		var buildContext = new BuildContext(collector, fs, fs, configurationContext, ExportOptions.MetadataOnly, path, null);
		var redirectFile = new RedirectFile(buildContext);
		if (!redirectFile.Source.Exists)
		{
			_logger.LogInformation("Redirect file {RedirectFile} does not exist, no redirects to validate.", redirectFile.Source);
			return Task.FromResult(true);
		}

		var redirects = redirectFile.Redirects;
		if (redirects is null)
		{
			collector.EmitError(redirectFile.Source, "It was not possible to parse the redirects file.");
			return Task.FromResult(false);
		}

		var root = Paths.DetermineSourceDirectoryRoot(buildContext.DocumentationSourceDirectory);
		if (root is null)
		{
			collector.EmitError(redirectFile.Source, $"Unable to determine the root of the source directory {buildContext.DocumentationSourceDirectory}.");
			return Task.FromResult(false);
		}
		var relativePath = Path.GetRelativePath(root.FullName, buildContext.DocumentationSourceDirectory.FullName);
		_logger.LogInformation("Using relative path {RelativePath} for validating changes", relativePath);
		IRepositoryTracker tracker = runningOnCi ? new IntegrationGitRepositoryTracker(relativePath) : new LocalGitRepositoryTracker(collector, root, relativePath);
		var changed = tracker.GetChangedFiles()
			.Where(c =>
			{
				var fi = fs.FileInfo.New(c.FilePath);
				return fi.Extension is ".md" && !fi.HasParent("_snippets");
			})
			.ToArray();

		if (changed.Length != 0)
			_logger.LogInformation("Found {Count} changes to files related to documentation in the current branch.", changed.Length);

		var deletedAndRenamed = changed.Where(c => c.ChangeType is GitChangeType.Deleted or GitChangeType.Renamed).ToArray();
		var missingCount = 0;
		foreach (var change in deletedAndRenamed)
		{
			var lookupPath = change is RenamedGitChange renamed ? renamed.OldFilePath : change.FilePath;
			var docSetRelativePath = Path.GetRelativePath(buildContext.DocumentationSourceDirectory.FullName, Path.Combine(root.FullName, lookupPath));
			var rootRelativePath = Path.GetRelativePath(root.FullName, Path.Combine(root.FullName, lookupPath));
			if (redirects.ContainsKey(docSetRelativePath))
				continue;
			if (redirects.ContainsKey(rootRelativePath))
			{
				collector.EmitError(redirectFile.Source,
					$"Redirect contains path relative to root '{rootRelativePath}' but should be relative to the documentation set '{docSetRelativePath}'");
				continue;
			}
			missingCount++;

			if (change is RenamedGitChange rename)
				collector.EmitError(redirectFile.Source, $"Missing '{docSetRelativePath}' in redirects.yml. '{rename.OldFilePath}' was renamed to '{rename.NewFilePath}' but it has no redirect configuration set.");
			else if (change.ChangeType is GitChangeType.Deleted)
				collector.EmitError(redirectFile.Source, $"Missing '{docSetRelativePath}' in redirects.yml. '{change.FilePath}' was deleted but it has no redirect targets. This will lead to broken links.");
		}

		if (missingCount != 0)
		{
			var relativeRedirectFile = Path.GetRelativePath(root.FullName, redirectFile.Source.FullName);
			_logger.LogInformation("Found {Count} changes that still require updates to: {RedirectFile}", missingCount, relativeRedirectFile);
		}

		return Task.FromResult(collector.Errors == 0);
	}
}
