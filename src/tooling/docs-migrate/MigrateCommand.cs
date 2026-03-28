// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Elastic.LegacyDocs.Migration;
using Microsoft.Extensions.Logging;

namespace Documentation.Migrate;

internal sealed class MigrateCommand(ILoggerFactory logFactory)
{
	private readonly ILogger _logger = logFactory.CreateLogger<MigrateCommand>();

	/// <summary>Migrate legacy /guide AsciiDoc content to Markdown docsets.</summary>
	/// <param name="conf">Path to conf.yaml (or fetched from GitHub if omitted)</param>
	/// <param name="reposDir">Directory where source repos are cloned</param>
	/// <param name="output">Output directory (required)</param>
	/// <param name="mode">Mode: archive, latest, or all (default: all)</param>
	/// <param name="book">Filter to specific book by prefix (e.g. en/elasticsearch/reference)</param>
	/// <param name="all">Process all branches, not just M.latest + M.latest-1</param>
	/// <param name="minVersion">Minimum major version to process (e.g. 7)</param>
	/// <param name="ctx">Cancellation token</param>
	[Command("")]
	public async Task<int> Migrate(
		string? conf = null,
		string? reposDir = null,
		string output = "",
		string mode = "all",
		string? book = null,
		bool all = false,
		int? minVersion = null,
		Cancel ctx = default
	)
	{
		if (string.IsNullOrEmpty(output))
		{
			_logger.LogError("--output is required");
			return 1;
		}

		reposDir ??= Path.Combine(output, ".repos");

		var yaml = conf is not null
			? await File.ReadAllTextAsync(conf, ctx)
			: throw new NotImplementedException("GitHub fetch not yet implemented");

		var legacyConf = LegacyConfParser.Parse(yaml);
		_logger.LogInformation(
			"Parsed conf.yaml: {BookCount} books across {CategoryCount} categories",
			legacyConf.Contents.SelectMany(c => c.Sections).Count(),
			legacyConf.Contents.Count
		);

		var repoOptions = new SourceRepoOptions
		{
			ReposDirectory = reposDir,
			RepoUrls = legacyConf.Repos
		};
		var repoManager = new SourceRepoManager(repoOptions, logFactory.CreateLogger<SourceRepoManager>());

		var parsedMode = mode.ToLowerInvariant();

		if (parsedMode is "archive" or "all")
		{
			var archiveOptions = new ArchiveGeneratorOptions
			{
				OutputDirectory = Path.Combine(output, "archive"),
				BookFilter = book,
				AllVersions = all,
				MinMajorVersion = minVersion,
				RepoManager = repoManager
			};
			var archiveGen = new ArchiveDocsetGenerator(logFactory.CreateLogger<ArchiveDocsetGenerator>());
			await archiveGen.GenerateAsync(legacyConf, archiveOptions, ctx);
		}

		if (parsedMode is "latest" or "all")
		{
			var latestOptions = new LatestGeneratorOptions
			{
				OutputDirectory = Path.Combine(output, "latest"),
				BookFilter = book,
				RepoManager = repoManager
			};
			var latestGen = new LatestDocsetGenerator(logFactory.CreateLogger<LatestDocsetGenerator>());
			await latestGen.GenerateAsync(legacyConf, latestOptions, ctx);
		}

		_logger.LogInformation("Migration complete. Output written to {Output}", output);
		return 0;
	}
}
