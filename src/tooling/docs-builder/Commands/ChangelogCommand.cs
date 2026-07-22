// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Actions.Core.Services;
using Documentation.Builder.Arguments;
using Elastic.Changelog;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Creation;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.GithubRelease;
using Elastic.Changelog.Rendering;
using Elastic.Changelog.Uploading;
using Elastic.Changelog.Utilities;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

namespace Documentation.Builder.Commands;

/// <summary>Create, bundle, and publish changelog entries.</summary>
internal sealed partial class ChangelogCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	[GeneratedRegex(@"^( *directory:\s*).+$", RegexOptions.Multiline)]
	private static partial Regex BundleDirectoryRegex();

	[GeneratedRegex(@"^( *output_directory:\s*).+$", RegexOptions.Multiline)]
	private static partial Regex BundleOutputDirectoryRegex();

	private readonly IFileSystem _fileSystem = FileSystemFactory.RealRead;
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogCommands>();
	/// <summary>Create <c>changelog.yml</c> and the changelog/releases directory structure.</summary>
	/// <remarks>
	/// Discovers the docs folder via <c>docset.yml</c>; falls back to creating <c>PATH/docs</c>.
	/// When <c>changelog.yml</c> already exists, updates only the paths specified via <see paramref="changelogDir"/> or <see paramref="bundlesDir"/>.
	/// Seeds <c>bundle.owner</c>, <c>bundle.repo</c>, and <c>bundle.link_allow_repos</c> from the git remote origin when available.
	/// </remarks>
	/// <param name="path">Repository root. Defaults to <c>cwd</c>.</param>
	/// <param name="changelogDir">Changelog entry directory. Defaults to <c>docs/changelog</c>.</param>
	/// <param name="bundlesDir">Bundle output directory. Defaults to <c>docs/releases</c>.</param>
	/// <param name="owner">GitHub owner for seeding bundle defaults. Overrides the value inferred from git remote origin.</param>
	/// <param name="repo">GitHub repository name for seeding bundle defaults. Overrides the value inferred from git remote origin.</param>
	[NoOptionsInjection]
	public Task<int> Init(
		[ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? path = null,
		[ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? changelogDir = null,
		[ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? bundlesDir = null,
		string? owner = null,
		string? repo = null
	)
	{
		var rootPath = path?.FullName ?? Path.GetFullPath(".");
		var rootDir = _fileSystem.DirectoryInfo.New(rootPath);

		IDirectoryInfo docsFolder;
		if (Paths.TryFindDocsFolderFromKnownLocationsOnly(_fileSystem, rootDir, out var foundDocsFolder, out _))
		{
			docsFolder = foundDocsFolder;
		}
		else
		{
			var docsFolderPath = Path.Join(rootPath, "docs");
			if (!_fileSystem.Directory.Exists(docsFolderPath))
			{
				_logger.LogInformation("Creating docs folder at {DocsFolderPath}", docsFolderPath);
				_ = _fileSystem.Directory.CreateDirectory(docsFolderPath);
			}

			docsFolder = _fileSystem.DirectoryInfo.New(docsFolderPath);
		}

		var configPath = _fileSystem.Path.Join(docsFolder.FullName, "changelog.yml");
		var changelogPath = changelogDir?.FullName ?? _fileSystem.Path.Join(docsFolder.FullName, "changelog");
		var bundlesPath = bundlesDir?.FullName ?? _fileSystem.Path.Join(docsFolder.FullName, "releases");

		var useNonDefaultChangelogDir = changelogDir != null;
		var useNonDefaultBundlesDir = bundlesDir != null;
		var repoRoot = Paths.FindGitRoot(docsFolder)?.FullName ?? docsFolder.FullName;

		// Create changelog.yml from example if it does not exist
		if (!_fileSystem.File.Exists(configPath))
		{
			byte[]? templateBytes = null;
			using (var stream = typeof(ChangelogCommands).Assembly.GetManifestResourceStream("Documentation.Builder.changelog.example.yml"))
			{
				if (stream == null)
				{
					// Fallback: try config relative to current directory (for development)
					var localConfigDir = _fileSystem.Path.Join(Directory.GetCurrentDirectory(), "config");
					var localConfigPath = _fileSystem.Path.Join(localConfigDir, "changelog.example.yml");
					if (_fileSystem.File.Exists(localConfigPath))
					{
						templateBytes = _fileSystem.File.ReadAllBytes(localConfigPath);
					}
				}
				else
				{
					using var ms = new MemoryStream();
					stream.CopyTo(ms);
					templateBytes = ms.ToArray();
				}
			}

			if (templateBytes == null || templateBytes.Length == 0)
			{
				collector.EmitError(string.Empty, "Could not find changelog.example.yml template. Ensure docs-builder is built correctly.");
				return Task.FromResult(1);
			}

			var content = Encoding.UTF8.GetString(templateBytes);

			// Update bundle.directory and bundle.output_directory when non-default paths are specified
			if (useNonDefaultChangelogDir)
			{
				var directoryValue = GetPathForConfig(repoRoot, changelogPath);
				content = content.Replace("directory: docs/changelog", $"directory: {directoryValue}");
			}

			if (useNonDefaultBundlesDir)
			{
				var outputValue = GetPathForConfig(repoRoot, bundlesPath);
				content = content.Replace("output_directory: docs/releases", $"output_directory: {outputValue}");
			}

			content = ApplyChangelogInitBundleRepoSeed(content, owner, repo, repoRoot);

			try
			{
				_fileSystem.File.WriteAllBytes(configPath, Encoding.UTF8.GetBytes(content));
				_logger.LogInformation("Created changelog configuration: {ConfigPath}", configPath);
			}
			catch (IOException ex)
			{
				collector.EmitError(string.Empty, $"Failed to write changelog configuration to '{configPath}': {ex.Message}", ex);
				return Task.FromResult(1);
			}
		}
		else if (useNonDefaultChangelogDir || useNonDefaultBundlesDir)
		{
			try
			{
				var content = _fileSystem.File.ReadAllText(configPath);
				// Strip any leading BOM that might be present after reading
				content = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(content);

				if (useNonDefaultChangelogDir)
				{
					var directoryValue = GetPathForConfig(repoRoot, changelogPath);
					content = BundleDirectoryRegex().Replace(content, "$1" + directoryValue);
				}

				if (useNonDefaultBundlesDir)
				{
					var outputValue = GetPathForConfig(repoRoot, bundlesPath);
					content = BundleOutputDirectoryRegex().Replace(content, "$1" + outputValue);
				}

				// Ensure normalized content is written without BOM
				var normalizedContent = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(content);
				_fileSystem.File.WriteAllText(configPath, normalizedContent);
				_logger.LogInformation("Updated bundle paths in changelog configuration: {ConfigPath}", configPath);
			}
			catch (IOException ex)
			{
				collector.EmitError(string.Empty, $"Failed to update changelog configuration at '{configPath}': {ex.Message}", ex);
				return Task.FromResult(1);
			}
		}
		else
		{
			_logger.LogInformation("Changelog configuration already exists: {ConfigPath}", configPath);
		}

		// Create docs/changelog and docs/releases if they do not exist
		foreach (var dir in new[] { changelogPath, bundlesPath })
		{
			if (!_fileSystem.Directory.Exists(dir))
			{
				try
				{
					_ = _fileSystem.Directory.CreateDirectory(dir);
					_logger.LogInformation("Created directory: {Directory}", dir);
				}
				catch (IOException ex)
				{
					collector.EmitError(string.Empty, $"Failed to create directory '{dir}': {ex.Message}", ex);
					return Task.FromResult(1);
				}
			}
			else
			{
				_logger.LogInformation("Directory already exists: {Directory}", dir);
			}
		}

		return Task.FromResult(0);
	}

	/// <summary>Create a new changelog entry YAML file.</summary>
	/// <param name="products">Optional: Products affected in format "product target lifecycle, ..." (e.g., "elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05"). If not specified, will be inferred from repository or config defaults.</param>
	/// <param name="action">Optional: What users must do to mitigate</param>
	/// <param name="areas">Optional: Area(s) affected (comma-separated or specify multiple times)</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="description">Optional: Additional information about the change (max 600 characters)</param>
	/// <param name="noExtractReleaseNotes">Optional: Turn off extraction of release notes from PR descriptions. By default, release notes are extracted when using --prs. Matched release note text is used as the changelog description (only if --description is not explicitly provided). The changelog title comes from --title or the PR title, not from the release note section.</param>
	/// <param name="noExtractIssues">Optional: Turn off extraction of linked references. When using --prs: turns off extraction of linked issues from the PR body (e.g., "Fixes #123"). When using --issues: turns off extraction of linked PRs from the issue body (e.g., "Fixed by #123"). By default, linked references are extracted in both cases.</param>
	/// <param name="featureId">Optional: Feature flag ID</param>
	/// <param name="highlight">Optional: Include in release highlights</param>
	/// <param name="impact">Optional: How the user's environment is affected</param>
	/// <param name="issues">Optional: Issue URL(s) or number(s) (comma-separated), or a path to a newline-delimited file containing issue URLs or numbers. Can be specified multiple times. Each occurrence can be either comma-separated issues (e.g., `--issues "https://github.com/owner/repo/issues/123,456"`) or a file path (e.g., `--issues /path/to/file.txt`). If --owner and --repo are provided, issue numbers can be used instead of URLs. If specified, --title can be derived from the issue. Creates one changelog file per issue. Mutually exclusive with --release-version and --report.</param>
	/// <param name="owner">Optional: GitHub repository owner (used when --prs or --issues contains just numbers, or when using --release-version). Falls back to bundle.owner in changelog.yml when not specified. If that value is also absent, "elastic" is used.</param>
	/// <param name="output">Optional: Output directory for the changelog. Falls back to bundle.directory in changelog.yml when not specified. Defaults to current directory.</param>
	/// <param name="prs">Optional: Pull request URL(s) or PR number(s) (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. Each occurrence can be either comma-separated PRs (e.g., `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (e.g., `--prs /path/to/file.txt`). When specifying PRs directly, provide comma-separated values. When specifying a file path, provide a single value that points to a newline-delimited file. If --owner and --repo are provided, PR numbers can be used instead of URLs. If specified, --title can be derived from the PR. If mappings are configured, --areas and --type can also be derived from the PR. Creates one changelog file per PR. Mutually exclusive with --release-version and --report.</param>
	/// <param name="report">Optional: URL or file path to a promotion report HTML document. Extracts GitHub pull request URLs and creates one changelog per PR (same parsing as `changelog bundle --report`). Mutually exclusive with --prs, --issues, and --release-version.</param>
	/// <param name="repo">Optional: GitHub repository name (used when --prs or --issues contains just numbers, or when using --release-version). Falls back to bundle.repo in changelog.yml when not specified.</param>
	/// <param name="stripTitlePrefix">Optional: When used with --prs or --report, remove square brackets and text within them from the beginning of PR titles, and also remove a colon if it follows the closing bracket (e.g., "[Inference API] Title" becomes "Title", "[ES|QL]: Title" becomes "Title", "[Discover][ESQL] Title" becomes "Title")</param>
	/// <param name="strictFetch">Optional: Treat a failure to fetch any PR or issue from GitHub (with --prs, --issues, or --report) as an error that exits non-zero, instead of a warning. Use in CI so a missing or unauthorized GITHUB_TOKEN fails the run rather than silently producing unfiltered changelogs with missing titles. Files are still written so they can be inspected.</param>
	/// <param name="subtype">Optional: Subtype for breaking changes (api, behavioral, configuration, etc.)</param>
	/// <param name="title">Optional: A short, user-facing title (max 80 characters). Required if neither --prs, --issues, nor --report is specified. If --prs and --title are specified, the latter value is used instead of what exists in the PR.</param>
	/// <param name="type">Optional: Type of change (feature, enhancement, bug-fix, breaking-change, etc.). Required if neither --prs, --issues, nor --report is specified. If mappings are configured, type can be derived from the PR or issue.</param>
	/// <param name="concise">Optional: Omit schema reference comments from generated YAML files. Useful in CI to produce compact output.</param>
	/// <param name="usePrNumber">Optional: Use PR numbers for filenames instead of timestamp-slug. With --prs, --report, or --issues (where PRs are resolved), each changelog filename will be derived from its PR numbers. Requires --prs, --report, or --issues. Mutually exclusive with --use-issue-number.</param>
	/// <param name="useIssueNumber">Optional: Use issue numbers for filenames instead of timestamp-slug. With both --prs (which creates one changelog per specified PR) and --issues (which creates one changelog per specified issue), each changelog filename will be derived from its issues. Requires --prs or --issues. Mutually exclusive with --use-pr-number.</param>
	/// <param name="releaseVersion">Optional: GitHub release tag to fetch PRs from (e.g., "v9.2.0" or "latest"). When specified, creates one changelog per PR in the release notes. Requires --repo (or bundle.repo in changelog.yml). Mutually exclusive with --prs, --issues, and --report. Does not create a bundle; use 'changelog gh-release' for that.</param>
	/// <param name="ctx">Cancellation token</param>
	[NoOptionsInjection]
	public async Task<int> Add(
		[ArgumentParser(typeof(ProductInfoParser))] ProductArgumentList? products = null,
		string? action = null,
		string[]? areas = null,
		bool concise = false,
		[Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo? config = null,
		string? description = null,
		bool noExtractReleaseNotes = false,
		bool noExtractIssues = false,
		string? featureId = null,
		bool? highlight = null,
		string? impact = null,
		string[]? issues = null,
		string? owner = null,
		string? output = null,
		string[]? prs = null,
		string? report = null,
		string? releaseVersion = null,
		string? repo = null,
		bool stripTitlePrefix = false,
		bool strictFetch = false,
		string? subtype = null,
		string? title = null,
		string? type = null,
		bool usePrNumber = false,
		bool useIssueNumber = false,
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		await using var serviceInvoker = new ServiceInvoker(collector);

		var hasReport = !string.IsNullOrWhiteSpace(report);
		if (hasReport)
		{
			if (prs is { Length: > 0 })
			{
				collector.EmitError(string.Empty, "--report and --prs cannot be specified together.");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			if (issues is { Length: > 0 })
			{
				collector.EmitError(string.Empty, "--report and --issues cannot be specified together.");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}
		}

		// Mutual exclusivity: --release-version cannot be combined with --prs, --issues, or --report
		if (releaseVersion != null)
		{
			if (hasReport)
			{
				collector.EmitError(string.Empty, "--release-version and --report are mutually exclusive.");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			if (prs is { Length: > 0 })
			{
				collector.EmitError(string.Empty, "--release-version and --prs are mutually exclusive.");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			if (issues is { Length: > 0 })
			{
				collector.EmitError(string.Empty, "--release-version and --issues are mutually exclusive.");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}
		}

		// Load changelog config and apply fallbacks for all modes.
		// Precedence: CLI option > bundle section in changelog.yml > built-in default.
		// This applies to --prs, --issues, --release-version, and --report alike.
		var bundleConfig = await new ChangelogConfigurationLoader(logFactory, configurationContext, _fileSystem)
			.LoadChangelogConfiguration(collector, config?.FullName, ctx);
		var resolvedRepo = !string.IsNullOrWhiteSpace(repo) ? repo : bundleConfig?.Bundle?.Repo;
		var resolvedOwner = owner ?? bundleConfig?.Bundle?.Owner ?? "elastic";
		var resolvedOutput = !string.IsNullOrWhiteSpace(output) ? output : bundleConfig?.Bundle?.Directory;

		// Resolve stripTitlePrefix: CLI flag true → explicit true; otherwise null (use config default)
		var stripTitlePrefixResolved = stripTitlePrefix ? true : (bool?)null;

		// --release-version mode: delegate entirely to GitHubReleaseChangelogService without creating a bundle
		if (releaseVersion != null)
		{
			if (string.IsNullOrWhiteSpace(resolvedRepo))
			{
				collector.EmitError(string.Empty, "--release-version requires --repo to be specified (or bundle.repo set in changelog.yml).");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			var repoArg = resolvedRepo.Contains('/') ? resolvedRepo : $"{resolvedOwner}/{resolvedRepo}";
			IGitHubReleaseService releaseService = new GitHubReleaseService(logFactory);
			IGitHubPrService prService = new GitHubPrService(logFactory);
			var releaseChangelogService = new GitHubReleaseChangelogService(logFactory, configurationContext, releaseService, prService);

			var releaseInput = new CreateChangelogsFromReleaseArguments
			{
				Repository = repoArg,
				Version = releaseVersion,
				Config = config?.FullName,
				Output = resolvedOutput,
				StripTitlePrefix = stripTitlePrefixResolved,
				CreateBundle = false
			};

			serviceInvoker.AddCommand(releaseChangelogService, releaseInput,
				async static (s, collector, state, ctx) => await s.CreateChangelogsFromRelease(collector, state, ctx)
			);

			return await serviceInvoker.InvokeAsync(ctx);
		}

		IGitHubPrService githubPrService = new GitHubPrService(logFactory);
		var service = new ChangelogCreationService(logFactory, configurationContext, githubPrService, env: SystemEnvironmentVariables.Instance);

		// Parse PRs: promotion report (--report), or comma-separated values and file paths (--prs)
		string[]? parsedPrs = null;
		if (hasReport)
		{
			var reportSource = report!.Trim();
			if (!reportSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
				!reportSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
				reportSource = NormalizePath(reportSource);

			var reportParser = new PromotionReportParser(logFactory, null);
			parsedPrs = await reportParser.ParseReportToPrUrlsAsync(collector, reportSource, ctx);
			if (parsedPrs == null)
			{
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}
		}
		else if (prs is { Length: > 0 })
		{
			var allPrs = new List<string>();
			var validPrs = prs.Where(prValue => !string.IsNullOrWhiteSpace(prValue));
			foreach (var trimmedValue in validPrs.Select(prValue => prValue.Trim()))
			{
				// Try to normalize as a file path to handle ~ and relative paths
				var normalizedPath = NormalizePath(trimmedValue);

				// Check if this is a file path
				if (_fileSystem.File.Exists(normalizedPath))
				{
					// Read all lines from the file (newline-delimited)
					try
					{
						var fileLines = await _fileSystem.File.ReadAllLinesAsync(normalizedPath, ctx);
						foreach (var line in fileLines)
						{
							if (!string.IsNullOrWhiteSpace(line))
							{
								allPrs.Add(line.Trim());
							}
						}
					}
					catch (IOException ex)
					{
						collector.EmitError(string.Empty, $"Failed to read PRs from file '{normalizedPath}': {ex.Message}", ex);
						return 1;
					}
				}
				else
				{
					// Treat as comma-separated PRs
					var commaSeparatedPrs = trimmedValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					allPrs.AddRange(commaSeparatedPrs);
				}
			}
			parsedPrs = allPrs.ToArray();
		}

		// null = use config default; explicit false when --no-extract-* passed
		var extractReleaseNotes = noExtractReleaseNotes ? false : (bool?)null;
		var extractIssues = noExtractIssues ? false : (bool?)null;

		// Parse issues: handle both comma-separated values and file paths (mirrors PR parsing)
		string[]? parsedIssues = null;
		if (issues is { Length: > 0 })
		{
			var allIssues = new List<string>();
			var validIssues = issues.Where(i => !string.IsNullOrWhiteSpace(i));
			foreach (var trimmedValue in validIssues.Select(i => i.Trim()))
			{
				var normalizedPath = NormalizePath(trimmedValue);
				if (_fileSystem.File.Exists(normalizedPath))
				{
					try
					{
						var fileLines = await _fileSystem.File.ReadAllLinesAsync(normalizedPath, ctx);
						foreach (var line in fileLines)
						{
							if (!string.IsNullOrWhiteSpace(line))
								allIssues.Add(line.Trim());
						}
					}
					catch (IOException ex)
					{
						collector.EmitError(string.Empty, $"Failed to read issues from file '{normalizedPath}': {ex.Message}", ex);
						return 1;
					}
				}
				else
				{
					var commaSeparated = trimmedValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					allIssues.AddRange(commaSeparated);
				}
			}
			parsedIssues = allIssues.ToArray();
		}

		// Use provided products or empty list (service will infer from repo/config if empty)
		var resolvedProducts = (IReadOnlyList<ProductArgument>?)products ?? [];

		if (usePrNumber && useIssueNumber)
		{
			collector.EmitError(string.Empty, "--use-pr-number and --use-issue-number are mutually exclusive; specify only one.");
			_ = collector.StartAsync(ctx);
			await collector.WaitForDrain();
			await collector.StopAsync(ctx);
			return 1;
		}

		// --use-pr-number with --issues is allowed: PRs can be extracted from the issue body (Fixed by #123, etc.)
		if (usePrNumber && (parsedPrs == null || parsedPrs.Length == 0) && (parsedIssues == null || parsedIssues.Length == 0))
		{
			collector.EmitError(string.Empty, "--use-pr-number requires --prs, --issues, or --report to be specified.");
			_ = collector.StartAsync(ctx);
			await collector.WaitForDrain();
			await collector.StopAsync(ctx);
			return 1;
		}

		// --use-issue-number with --prs is allowed: issues can be extracted from the PR body (Fixes #123, etc.)
		if (useIssueNumber && (parsedIssues == null || parsedIssues.Length == 0) && (parsedPrs == null || parsedPrs.Length == 0))
		{
			collector.EmitError(string.Empty, "--use-issue-number requires --prs or --issues to be specified.");
			_ = collector.StartAsync(ctx);
			await collector.WaitForDrain();
			await collector.StopAsync(ctx);
			return 1;
		}

		var input = new CreateChangelogArguments
		{
			Title = title,
			Type = type,
			Products = resolvedProducts,
			Subtype = subtype,
			Areas = areas ?? [],
			Prs = parsedPrs,
			Owner = resolvedOwner,
			Repo = resolvedRepo,
			Issues = parsedIssues ?? [],
			Description = description,
			Impact = impact,
			Action = action,
			FeatureId = featureId,
			Highlight = highlight,
			Output = resolvedOutput,
			Config = config?.FullName,
			UsePrNumber = usePrNumber,
			UseIssueNumber = useIssueNumber,
			StripTitlePrefix = stripTitlePrefixResolved,
			ExtractReleaseNotes = extractReleaseNotes,
			ExtractIssues = extractIssues,
			Concise = concise,
			StrictFetch = strictFetch
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.CreateChangelog(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>Aggregate changelog entries matching a filter into a single bundle YAML.</summary>
	/// <remarks>
	/// <para><b>Profile-based commands</b> (<c>bundle &lt;profile&gt; &lt;version|report&gt; [report] [--plan]</c>): filters, paths, repo metadata,
	/// resolve, description, hide-features, and release-date behaviour come from <c>changelog.yml</c>. Only <c>--plan</c> is supported
	/// alongside profile positional arguments; other flags documented below as unsupported in profile-based commands must be set in
	/// configuration instead. Config is auto-discovered from <c>./changelog.yml</c> or <c>./docs/changelog.yml</c>. Use
	/// <c>bundle.release_dates</c> or <c>bundle.profiles.&lt;name&gt;.release_dates</c> to control auto-population;
	/// <c>--release-date</c> and <c>--no-release-date</c> require option-based mode.</para>
	/// <para><b>Option-based mode</b> (no profile argument): exactly one filter must be specified —
	/// <c>--all</c>, <c>--input-products</c>, <c>--prs</c>, <c>--issues</c>, <c>--release-version</c>, <c>--report</c>, or <c>--files</c>.</para>
	/// </remarks>
	/// <param name="profile">Profile name from <c>bundle.profiles</c> in config (for example, "elasticsearch-release"). Used as the first positional argument in profile-based commands. The equivalent configuration entry is <c>bundle.profiles.&lt;name&gt;</c>.</param>
	/// <param name="profileArg">Version number or promotion report URL/path when using a profile (for example, "9.2.0" or "https://buildkite.../promotion-report.html"). Required second positional argument in profile-based commands.</param>
	/// <param name="profileReport">Promotion report, URL list file, or changelog path list file when also providing a version. When provided, the second argument must be a version string and this is the filter source (for example, "bundle serverless-release 2026-02 ./report.html"). Optional third positional argument in profile-based commands.</param>
	/// <param name="all">Include all changelogs in the directory. This option is not supported in profile-based commands. The equivalent configuration option is <c>bundle.profiles.&lt;name&gt;.products: "* * *"</c>.</param>
	/// <param name="config">Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml' in option-based mode. This option is not supported in profile-based commands; configuration is auto-discovered.</param>
	/// <param name="directory">Directory containing changelog YAML files. Uses config <c>bundle.directory</c> or defaults to current directory. This option is not supported in profile-based commands. The equivalent configuration option is <c>bundle.directory</c>.</param>
	/// <param name="description">Bundle description text with placeholder support ({version}, {lifecycle}, {owner}, {repo}). Overrides <c>bundle.description</c> from config. In option-based mode, placeholders require --output-products. This option is not supported in profile-based commands. The equivalent configuration options are <c>bundle.description</c> or <c>bundle.profiles.&lt;name&gt;.description</c>.</param>
	/// <param name="hideFeatures">Feature IDs (comma-separated) or a path to a newline-delimited file. Entries with matching feature-id values are hidden when the bundle is rendered. This option is not supported in profile-based commands. The equivalent configuration option is <c>bundle.profiles.&lt;name&gt;.hide_features</c>.</param>
	/// <param name="noReleaseDate">Skip auto-population of release date in the bundle. Mutually exclusive with --release-date. This option is not supported in profile-based commands. The equivalent configuration options are <c>bundle.release_dates: false</c> or <c>bundle.profiles.&lt;name&gt;.release_dates: false</c>.</param>
	/// <param name="releaseDate">Explicit release date for the bundle in YYYY-MM-DD format. Overrides auto-population behaviour. Mutually exclusive with --no-release-date. This option is not supported in profile-based commands; use option-based mode, or set <c>bundle.release_dates</c> in configuration to control auto-population.</param>
	/// <param name="inputProducts">Filter by products in format "product target lifecycle, ..." (for example, "cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"). All three parts are required but can be wildcards (*). This option is not supported in profile-based commands. The equivalent configuration option is <c>bundle.profiles.&lt;name&gt;.products</c>.</param>
	/// <param name="issues">Filter by issue URLs (comma-separated), or a path to a newline-delimited file containing fully-qualified GitHub issue URLs. Can be specified multiple times. This option is not supported in profile-based commands. Pass a promotion report as the second or third positional argument instead, or set <c>source: github_release</c> on the profile.</param>
	/// <param name="output">Output path for the bundled changelog (directory or .yml/.yaml file). Uses config <c>bundle.output_directory</c> or defaults to 'changelog-bundle.yaml' in the input directory. This option is not supported in profile-based commands. The equivalent configuration option is <c>bundle.profiles.&lt;name&gt;.output</c>.</param>
	/// <param name="outputProducts">Explicitly set the products array in the output file in format "product target lifecycle, ...". This option is not supported in profile-based commands. The equivalent configuration option is <c>bundle.profiles.&lt;name&gt;.output_products</c>.</param>
	/// <param name="owner">GitHub repository owner for PR/issue numbers or --release-version. Falls back to <c>bundle.owner</c> or "elastic". This option is not supported in profile-based commands. The equivalent configuration options are <c>bundle.owner</c> or <c>bundle.profiles.&lt;name&gt;.owner</c>.</param>
	/// <param name="branch">Branch whose CDN changelog entry pool (<c>changelog/{org}/{repo}/{branch}/...</c>) is sourced from. Falls back to <c>bundle.branch</c> or "main". This option is not supported in profile-based commands. The equivalent configuration options are <c>bundle.branch</c> or <c>bundle.profiles.&lt;name&gt;.branch</c>.</param>
	/// <param name="prs">Filter by pull request URLs (comma-separated), or a path to a newline-delimited file containing fully-qualified GitHub PR URLs. Can be specified multiple times. This option is not supported in profile-based commands. Pass a promotion report as the second or third positional argument instead, or set <c>source: github_release</c> on the profile.</param>
	/// <param name="files">Filter by changelog YAML paths (comma-separated), or a path to a newline-delimited file containing changelog paths. Can be specified multiple times. Forces local entry sourcing. This option is not supported in profile-based commands; pass a path list file as the second or third positional argument instead.</param>
	/// <param name="forceLocal">Force local entry sourcing for this run (equivalent to <c>bundle.use_local_changelogs: true</c> without editing config). Allowed in profile-based commands.</param>
	/// <param name="repo">GitHub repository name for PR/issue numbers or --release-version. Falls back to <c>bundle.repo</c> or the product ID. This option is not supported in profile-based commands. The equivalent configuration options are <c>bundle.repo</c> or <c>bundle.profiles.&lt;name&gt;.repo</c>.</param>
	/// <param name="report">URL or file path to a promotion report; extracts PR URLs as the filter. This option is not supported in profile-based commands. Pass the report as the second or third positional argument instead.</param>
	/// <param name="releaseVersion">GitHub release tag to use as a filter source (for example, "v9.2.0" or "latest"). Fetches PR references from release notes. This option is not supported in profile-based commands. The equivalent configuration option is <c>bundle.profiles.&lt;name&gt;.source: github_release</c>.</param>
	/// <param name="resolve">Copy the contents of each changelog file into the entries array. Uses config <c>bundle.resolve</c> or defaults to false. This option is not supported in profile-based commands. The equivalent configuration option is <c>bundle.resolve</c>.</param>
	/// <param name="plan">Emit GitHub Actions step outputs (<c>needs_network</c>, <c>needs_github_token</c>, <c>output_path</c>, and <c>cdn_url</c> when a product is resolvable) describing network requirements, the resolved output path, and the public CDN URL of the scrubbed bundle, then exit without generating the bundle. Intended for CI actions. Supported in profile-based commands.</param>
	/// <param name="ctx"></param>
	[NoOptionsInjection]
	public async Task<int> Bundle(
		[Argument] string? profile = null,
		[Argument] string? profileArg = null,
		[Argument] string? profileReport = null,
		bool all = false,
		[Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo? config = null,
		[ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? directory = null,
		string? description = null,
		string[]? hideFeatures = null,
		bool noReleaseDate = false,
		string? releaseDate = null,
		[ArgumentParser(typeof(ProductInfoParser))] ProductArgumentList? inputProducts = null,
		string? output = null,
		[ArgumentParser(typeof(ProductInfoParser))] ProductArgumentList? outputProducts = null,
		string[]? issues = null,
		string[]? files = null,
		bool forceLocal = false,
		string? owner = null,
		string? branch = null,
		bool plan = false,
		string[]? prs = null,
		string? releaseVersion = null,
		string? repo = null,
		string? report = null,
		bool? resolve = null,
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogBundlingService(logFactory, configurationContext);

		var isProfileMode = !string.IsNullOrWhiteSpace(profile);

		// --release-version mode: resolve the release into a PR list and proceed as if --prs was specified
		if (releaseVersion != null)
		{
			if (all || (inputProducts is { Count: > 0 }) || (prs is { Length: > 0 }) || (issues is { Length: > 0 }) || (files is { Length: > 0 }))
			{
				collector.EmitError(string.Empty,
					"--release-version is mutually exclusive with --all, --input-products, --prs, --issues, and --files.");
				return 1;
			}

			if (!plan)
			{
				// Precedence: --repo CLI > bundle.repo config; --owner CLI > bundle.owner config > "elastic"
				var bundleConfig = await new ChangelogConfigurationLoader(logFactory, configurationContext, _fileSystem)
					.LoadChangelogConfiguration(collector, config?.FullName, ctx);
				var resolvedRepo = !string.IsNullOrWhiteSpace(repo) ? repo : bundleConfig?.Bundle?.Repo;
				var resolvedOwner = owner ?? bundleConfig?.Bundle?.Owner ?? "elastic";

				if (string.IsNullOrWhiteSpace(resolvedRepo))
				{
					collector.EmitError(string.Empty, "--release-version requires --repo to be specified (or bundle.repo set in changelog.yml).");
					return 1;
				}

				IGitHubReleaseService releaseService = new GitHubReleaseService(logFactory);
				var release = await releaseService.FetchReleaseAsync(resolvedOwner, resolvedRepo, releaseVersion, ctx);
				if (release == null)
				{
					collector.EmitError(string.Empty,
						$"Failed to fetch release '{releaseVersion}' for {resolvedOwner}/{resolvedRepo}. Ensure the tag exists and credentials are set.");
					return 1;
				}

				var parsedNotes = ReleaseNoteParser.Parse(release.Body);
				if (parsedNotes.PrReferences.Count == 0)
				{
					collector.EmitWarning(string.Empty,
						$"No PR references found in release notes for {resolvedOwner}/{resolvedRepo}@{release.TagName}. No bundle will be created.");
					return 0;
				}

				// Build full PR URLs and inject them as the PR filter
				prs = parsedNotes.PrReferences
					.Select(r => $"https://github.com/{resolvedOwner}/{resolvedRepo}/pull/{r.PrNumber}")
					.ToArray();
			}
		}

		var allPrs = ExpandCommaSeparated(prs);
		var allIssues = ExpandCommaSeparated(issues);
		var allFiles = ExpandCommaSeparated(files);

		// Validate filter/output options against profile mode
		if (isProfileMode)
		{
			var forbidden = new List<string>();
			if (all)
				forbidden.Add("--all");
			if (inputProducts is { Count: > 0 })
				forbidden.Add("--input-products");
			if (outputProducts is { Count: > 0 })
				forbidden.Add("--output-products");
			if (allPrs.Count > 0)
				forbidden.Add("--prs");
			if (allIssues.Count > 0)
				forbidden.Add("--issues");
			if (allFiles.Count > 0)
				forbidden.Add("--files");
			if (!string.IsNullOrWhiteSpace(report))
				forbidden.Add("--report");
			if (!string.IsNullOrWhiteSpace(output))
				forbidden.Add("--output");
			if (!string.IsNullOrWhiteSpace(repo))
				forbidden.Add("--repo");
			if (!string.IsNullOrWhiteSpace(owner))
				forbidden.Add("--owner");
			if (resolve.HasValue)
				forbidden.Add("--resolve / --no-resolve");
			if (hideFeatures is { Length: > 0 })
				forbidden.Add("--hide-features");
			if (config != null)
				forbidden.Add("--config");
			if (directory != null)
				forbidden.Add("--directory");
			if (!string.IsNullOrWhiteSpace(description))
				forbidden.Add("--description");

			if (forbidden.Count > 0)
			{
				collector.EmitError(
					string.Empty,
					$"When using a profile, the following options are not allowed: {string.Join(", ", forbidden)}. " +
					"All paths and filters are derived from the changelog configuration file."
				);
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}
		}
		else
		{
			// profileReport (3rd positional arg) is only valid in profile mode
			if (!string.IsNullOrWhiteSpace(profileReport))
			{
				collector.EmitError(
					string.Empty,
					"A third positional argument is only valid in profile mode (e.g., 'bundle my-profile 2026-02 ./report.html')."
				);
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			// Raw mode: require exactly one filter option
			var specifiedFilters = new List<string>();
			if (all)
				specifiedFilters.Add("--all");
			if (inputProducts != null && inputProducts.Count > 0)
				specifiedFilters.Add("--input-products");
			if (allPrs.Count > 0)
				specifiedFilters.Add("--prs");
			if (allIssues.Count > 0)
				specifiedFilters.Add("--issues");
			if (allFiles.Count > 0)
				specifiedFilters.Add("--files");
			if (!string.IsNullOrWhiteSpace(report))
				specifiedFilters.Add("--report");

			if (specifiedFilters.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, --prs, --issues, --report, --files, or use a profile (e.g., 'bundle elasticsearch-release 9.2.0')");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			if (specifiedFilters.Count > 1)
			{
				collector.EmitError(string.Empty, $"Multiple filter options cannot be specified together. You specified: {string.Join(", ", specifiedFilters)}. Please use only one filter option: --all, --input-products, --prs, --issues, --report, or --files");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}
		}

		// Validate that if inputProducts is provided, all three parts (product, target, lifecycle) are present for each entry
		// They can be wildcards (*) but must be present
		if (inputProducts != null && inputProducts.Count > 0)
		{
			foreach (var product in inputProducts)
			{
				if (string.IsNullOrWhiteSpace(product.Product))
				{
					collector.EmitError(string.Empty, "--input-products: product is required (use '*' for wildcard)");
					_ = collector.StartAsync(ctx);
					await collector.WaitForDrain();
					await collector.StopAsync(ctx);
					return 1;
				}

				// When --input-products is used, target and lifecycle are required (but can be "*")
				// If they're null, it means they weren't provided in the input
				if (product.Target == null)
				{
					collector.EmitError(string.Empty, $"--input-products: target is required for product '{product.Product}' (use '*' for wildcard)");
					_ = collector.StartAsync(ctx);
					await collector.WaitForDrain();
					await collector.StopAsync(ctx);
					return 1;
				}

				if (product.Lifecycle == null)
				{
					collector.EmitError(string.Empty, $"--input-products: lifecycle is required for product '{product.Product}' (use '*' for wildcard)");
					_ = collector.StartAsync(ctx);
					await collector.WaitForDrain();
					await collector.StopAsync(ctx);
					return 1;
				}
			}

			// Check if --input-products * * * is specified (equivalent to --all)
			var isAllWildcard = inputProducts.Count == 1 &&
				inputProducts[0].Product == "*" &&
				inputProducts[0].Target == "*" &&
				inputProducts[0].Lifecycle == "*";

			if (isAllWildcard)
			{
				all = true;
				inputProducts = null; // Clear inputProducts so service treats it as --all
			}
		}

		// Process and validate output parameter
		string? processedOutput = null;
		if (!string.IsNullOrWhiteSpace(output))
		{
			var outputLower = output.ToLowerInvariant();
			var endsWithYml = outputLower.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
			var endsWithYaml = outputLower.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);

			if (endsWithYml || endsWithYaml)
			{
				// It's a file path - use as-is
				processedOutput = output;
			}
			else
			{
				// Check if it has a file extension (other than .yml/.yaml)
				var extension = Path.GetExtension(output);
				if (!string.IsNullOrEmpty(extension))
				{
					// Has an extension that's not .yml/.yaml - this is invalid
					collector.EmitError(string.Empty, $"--output: If a filename is provided, it must end in .yml or .yaml. Found: {extension}");
					_ = collector.StartAsync(ctx);
					await collector.WaitForDrain();
					await collector.StopAsync(ctx);
					return 1;
				}

				// It's a directory path - append default filename
				processedOutput = Path.Join(output, "changelog-bundle.yaml");
			}
		}

		// --plan mode: resolve config/profile metadata and set CI outputs without executing
		if (plan)
		{
			var planInput = new BundleChangelogsArguments
			{
				Output = processedOutput,
				Profile = profile,
				ProfileArgument = profileArg,
				ProfileReport = isProfileMode ? profileReport : null,
				Files = allFiles.Count > 0 ? allFiles.ToArray() : null,
				ForceLocal = forceLocal,
				Directory = directory?.FullName,
				Repo = repo,
				Config = config?.FullName,
				Description = description
			};
			var planResult = await service.PlanBundleAsync(collector, planInput, releaseVersion != null, ctx);
			if (planResult == null)
				return 1;

			await githubActionsService.SetOutputAsync("needs_network", planResult.NeedsNetwork ? "true" : "false");
			await githubActionsService.SetOutputAsync("needs_github_token", planResult.NeedsGithubToken ? "true" : "false");
			if (planResult.OutputPath != null)
				await githubActionsService.SetOutputAsync("output_path", planResult.OutputPath);
			if (planResult.CdnUrl != null)
				await githubActionsService.SetOutputAsync("cdn_url", planResult.CdnUrl);
			return 0;
		}

		// Validate release date flags
		if (noReleaseDate && !string.IsNullOrWhiteSpace(releaseDate))
		{
			collector.EmitError(string.Empty, "--no-release-date and --release-date are mutually exclusive.");
			return 1;
		}

		// Profile mode doesn't support release date CLI flags (use YAML configuration instead)
		if (isProfileMode && (noReleaseDate || !string.IsNullOrWhiteSpace(releaseDate)))
		{
			var forbidden = new List<string>();
			if (noReleaseDate)
				forbidden.Add("--no-release-date");
			if (!string.IsNullOrWhiteSpace(releaseDate))
				forbidden.Add("--release-date");

			collector.EmitError(string.Empty,
				$"Profile mode does not support {string.Join(" and ", forbidden)}. " +
				"Use bundle.release_dates or bundle.profiles.<name>.release_dates in changelog.yml instead.");
			return 1;
		}

		// Validate release date format if provided
		if (!string.IsNullOrWhiteSpace(releaseDate) && !DateOnly.TryParseExact(releaseDate, "yyyy-MM-dd", out _))
		{
			collector.EmitError(string.Empty, $"Invalid --release-date format '{releaseDate}'. Expected YYYY-MM-DD format.");
			return 1;
		}

		// Determine resolve: CLI --no-resolve and --resolve override config. null = use config default.
		var shouldResolve = resolve;

		var allFeatureIdsForBundle = ExpandCommaSeparated(hideFeatures);

		var input = new BundleChangelogsArguments
		{
			Directory = directory?.FullName,
			Output = processedOutput,
			All = all,
			InputProducts = inputProducts,
			OutputProducts = outputProducts,
			Resolve = shouldResolve,
			Prs = allPrs.Count > 0 ? allPrs.ToArray() : null,
			Issues = allIssues.Count > 0 ? allIssues.ToArray() : null,
			Files = allFiles.Count > 0 ? allFiles.ToArray() : null,
			ForceLocal = forceLocal,
			Owner = owner,
			Repo = repo,
			Branch = branch,
			Profile = profile,
			ProfileArgument = profileArg,
			ProfileReport = isProfileMode ? profileReport : null,
			Report = !isProfileMode ? report : null,
			Config = config?.FullName,
			HideFeatures = allFeatureIdsForBundle.Count > 0 ? allFeatureIdsForBundle.ToArray() : null,
			Description = description,
			ReleaseDate = releaseDate,
			SuppressReleaseDate = noReleaseDate
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.BundleChangelogs(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>Delete changelog entry files matching a filter.</summary>
	/// <remarks>
	/// Blocks when a file is referenced by an unresolved bundle to avoid breaking the <c>{changelog}</c>
	/// directive in published documentation. Pass <c>--force</c> to override.
	/// </remarks>
	/// <param name="profile">Optional: Profile name from bundle.profiles in config (for example, "elasticsearch-release"). When specified, the second argument is the version or promotion report URL.</param>
	/// <param name="profileArg">Optional: Version number or promotion report URL/path when using a profile (for example, "9.2.0" or "https://buildkite.../promotion-report.html")</param>
	/// <param name="profileReport">Optional: Promotion report, URL list file, or changelog path list file when also providing a version. When provided, the second argument must be a version string and this is the filter source.</param>
	/// <param name="all">Remove all changelogs in the directory. Exactly one filter option must be specified: --all, --products, --prs, --issues, --report, or --files.</param>
	/// <param name="bundlesDir">Optional: Override the directory that is scanned for bundles during the dependency check. Auto-discovered from config or fallback paths when not specified.</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="directory">Optional: Directory containing changelog YAML files. Uses config bundle.directory or defaults to current directory</param>
	/// <param name="dryRun">Print the files that would be removed without deleting them. Valid in both profile and raw mode.</param>
	/// <param name="force">Proceed with removal even when files are referenced by unresolved bundles. Emits warnings instead of errors for each dependency. Valid in both profile and raw mode.</param>
	/// <param name="issues">Filter by issue URLs (comma-separated) or a path to a newline-delimited file containing fully-qualified GitHub issue URLs. Can be specified multiple times.</param>
	/// <param name="files">Filter by changelog YAML paths (comma-separated), or a path to a newline-delimited file containing changelog paths. Can be specified multiple times. Not supported in profile-based commands; pass a path list file as a positional argument instead.</param>
	/// <param name="owner">Optional: GitHub repository owner, which is used when PRs or issues are specified as numbers or when using --release-version. Falls back to bundle.owner in changelog.yml when not specified. If that value is also absent, "elastic" is used.</param>
	/// <param name="products">Filter by products in format "product target lifecycle, ..." (for example, "elasticsearch 9.3.0 ga"). All three parts are required but can be wildcards (*).</param>
	/// <param name="prs">Filter by pull request URLs (comma-separated) or a path to a newline-delimited file containing fully-qualified GitHub PR URLs. Can be specified multiple times.</param>
	/// <param name="releaseVersion">GitHub release tag to use as a filter source (for example, "v9.2.0" or "latest"). Fetches the release, parses PR references from the release notes, and removes changelogs whose PR URLs match — equivalent to passing the PR list using --prs.</param>
	/// <param name="repo">GitHub repository name, which is used when PRs or issues are specified as numbers or when --release-version is used. Falls back to bundle.repo in changelog.yml when not specified. If that value is also absent, the product ID is used.</param>
	/// <param name="report">Optional (option-based mode only): URL or file path to a promotion report. Extracts PR URLs and uses them as the filter. Mutually exclusive with --all, --products, --prs, --release-version, --issues, and --files.</param>
	/// <param name="ctx"></param>
	[CommandIntent(Intent.Destructive | Intent.RequiresConfirmation)]
	[MutationScope(MutationScope.Directory)]
	[NoOptionsInjection]
	public async Task<int> Remove(
		[Argument] string? profile = null,
		[Argument] string? profileArg = null,
		[Argument] string? profileReport = null,
		bool all = false,
		[ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? bundlesDir = null,
		[Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo? config = null,
		[ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? directory = null,
		[DryRun] bool dryRun = false,
		[ConfirmationSkip] bool force = false,
		string[]? issues = null,
		string[]? files = null,
		string? owner = null,
		[ArgumentParser(typeof(ProductInfoParser))] ProductArgumentList? products = null,
		string[]? prs = null,
		string? releaseVersion = null,
		string? repo = null,
		string? report = null,
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogRemoveService(logFactory, configurationContext);

		var isProfileMode = !string.IsNullOrWhiteSpace(profile);

		// --release-version mode: resolve the release into a PR list and proceed as if --prs was specified
		if (releaseVersion != null)
		{
			if (all || (products is { Count: > 0 }) || (prs is { Length: > 0 }) || (issues is { Length: > 0 }) || (files is { Length: > 0 }))
			{
				collector.EmitError(string.Empty,
					"--release-version is mutually exclusive with --all, --products, --prs, --issues, and --files.");
				return 1;
			}

			// Precedence: --repo CLI > bundle.repo config; --owner CLI > bundle.owner config > "elastic"
			var bundleConfig = await new ChangelogConfigurationLoader(logFactory, configurationContext, _fileSystem)
				.LoadChangelogConfiguration(collector, config?.FullName, ctx);
			var resolvedRepo = !string.IsNullOrWhiteSpace(repo) ? repo : bundleConfig?.Bundle?.Repo;
			var resolvedOwner = owner ?? bundleConfig?.Bundle?.Owner ?? "elastic";

			if (string.IsNullOrWhiteSpace(resolvedRepo))
			{
				collector.EmitError(string.Empty, "--release-version requires --repo to be specified (or bundle.repo set in changelog.yml).");
				return 1;
			}

			IGitHubReleaseService releaseService = new GitHubReleaseService(logFactory);
			var release = await releaseService.FetchReleaseAsync(resolvedOwner, resolvedRepo, releaseVersion, ctx);
			if (release == null)
			{
				collector.EmitError(string.Empty,
					$"Failed to fetch release '{releaseVersion}' for {resolvedOwner}/{resolvedRepo}. Ensure the tag exists and credentials are set.");
				return 1;
			}

			var parsedNotes = ReleaseNoteParser.Parse(release.Body);
			if (parsedNotes.PrReferences.Count == 0)
			{
				collector.EmitWarning(string.Empty,
					$"No PR references found in release notes for {resolvedOwner}/{resolvedRepo}@{release.TagName}. No changelogs will be removed.");
				return 0;
			}

			// Build full PR URLs and inject them as the PR filter
			prs = parsedNotes.PrReferences
				.Select(r => $"https://github.com/{resolvedOwner}/{resolvedRepo}/pull/{r.PrNumber}")
				.ToArray();
		}

		var allPrs = ExpandCommaSeparated(prs);
		var allIssues = ExpandCommaSeparated(issues);
		var allFiles = ExpandCommaSeparated(files);

		if (isProfileMode)
		{
			// Profile mode: filter options and --repo/--owner must not be used; all paths and filters come from config
			var forbidden = new List<string>();
			if (all)
				forbidden.Add("--all");
			if (products is { Count: > 0 })
				forbidden.Add("--products");
			if (allPrs.Count > 0)
				forbidden.Add("--prs");
			if (allIssues.Count > 0)
				forbidden.Add("--issues");
			if (allFiles.Count > 0)
				forbidden.Add("--files");
			if (releaseVersion != null)
				forbidden.Add("--release-version");
			if (!string.IsNullOrWhiteSpace(repo))
				forbidden.Add("--repo");
			if (!string.IsNullOrWhiteSpace(owner))
				forbidden.Add("--owner");

			if (forbidden.Count > 0)
			{
				collector.EmitError(
					string.Empty,
					$"When using a profile, the following options are not allowed: {string.Join(", ", forbidden)}. " +
					"All paths and filters are derived from the changelog configuration file.");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			// profileArg is required when profile is specified
			if (string.IsNullOrWhiteSpace(profileArg))
			{
				collector.EmitError(string.Empty, $"Profile '{profile}' requires a version number or promotion report URL as the second argument");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}
		}
		else
		{
			// profileReport (3rd positional arg) is only valid in profile mode
			if (!string.IsNullOrWhiteSpace(profileReport))
			{
				collector.EmitError(
					string.Empty,
					"A third positional argument is only valid in profile mode (e.g., 'remove my-profile 2026-02 ./report.html')."
				);
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			// Raw mode: validate product filter parts and apply wildcard shortcut
			if (products is { Count: > 0 })
			{
				foreach (var product in products)
				{
					if (string.IsNullOrWhiteSpace(product.Product))
					{
						collector.EmitError(string.Empty, "--products: product is required (use '*' for wildcard)");
						_ = collector.StartAsync(ctx);
						await collector.WaitForDrain();
						await collector.StopAsync(ctx);
						return 1;
					}

					if (product.Target == null)
					{
						collector.EmitError(string.Empty, $"--products: target is required for product '{product.Product}' (use '*' for wildcard)");
						_ = collector.StartAsync(ctx);
						await collector.WaitForDrain();
						await collector.StopAsync(ctx);
						return 1;
					}

					if (product.Lifecycle == null)
					{
						collector.EmitError(string.Empty, $"--products: lifecycle is required for product '{product.Product}' (use '*' for wildcard)");
						_ = collector.StartAsync(ctx);
						await collector.WaitForDrain();
						await collector.StopAsync(ctx);
						return 1;
					}
				}

				// --products * * * is equivalent to --all
				var isAllWildcard = products.Count == 1 &&
					products[0].Product == "*" &&
					products[0].Target == "*" &&
					products[0].Lifecycle == "*";

				if (isAllWildcard)
				{
					all = true;
					products = null;
				}
			}
		}

		// In profile mode, directory is derived from the changelog config (not from CLI).
		// In raw mode, pass null when --directory is not specified so ApplyConfigDefaults can consult
		// bundle.directory before falling back to CWD.
		var resolvedDirectory = isProfileMode ? null : directory?.FullName;

		var input = new ChangelogRemoveArguments
		{
			Directory = resolvedDirectory,
			All = all,
			Products = products,
			Prs = allPrs.Count > 0 ? allPrs.ToArray() : null,
			Issues = allIssues.Count > 0 ? allIssues.ToArray() : null,
			Files = allFiles.Count > 0 ? allFiles.ToArray() : null,
			Owner = owner,
			Repo = repo,
			DryRun = dryRun,
			BundlesDir = bundlesDir?.FullName,
			Force = force,
			Config = config?.FullName,
			Profile = isProfileMode ? profile : null,
			ProfileArgument = isProfileMode ? profileArg : null,
			ProfileReport = isProfileMode ? profileReport : null,
			Report = !isProfileMode ? report : null
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.RemoveChangelogs(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>Render one or more changelog bundles to Markdown or AsciiDoc.</summary>
	/// <param name="input">Required: Bundle input(s) in format "bundle-file-path|changelog-file-path|repo|link-visibility" (use pipe as delimiter). To merge multiple bundles, separate them with commas. Only bundle-file-path is required. link-visibility can be "hide-links" or "keep-links" (default). Use "hide-links" for private repositories; when set, all PR and issue links for each affected entry are hidden (entries may have multiple links via the prs and issues arrays). Paths support tilde (~) expansion and relative paths.</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="fileType">Optional: Output file type. Valid values: "markdown", "asciidoc", or "gfm". Defaults to "markdown"</param>
	/// <param name="hideFeatures">Filter by feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs. Can be specified multiple times. Entries with matching feature-id values will be commented out in the output.</param>
	/// <param name="output">Optional: Output directory for rendered files. Defaults to current directory</param>
	/// <param name="subsections">Optional: Group entries by area/component in subsections. For breaking changes with a subtype, groups by subtype instead of area. Defaults to false</param>
	/// <param name="dropdowns">Optional: Render separated types (breaking changes, deprecations, known issues, highlights) as MyST dropdowns. When false (default), renders as flattened bulleted lists. Defaults to false</param>
	/// <param name="noDescriptions">Optional: Hide changelog record descriptions from output. When enabled, entry titles, PR/issue links, Impact and Action sections remain visible. Bundle-level descriptions are unaffected. Defaults to false</param>
	/// <param name="title">Optional: Title to use for section headers in output files. Defaults to version from first bundle</param>
	/// <param name="ctx"></param>
	[NoOptionsInjection]
	public async Task<int> Render(
		string[]? input = null,
		[Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo? config = null,
		string? fileType = "markdown",
		string[]? hideFeatures = null,
		string? output = null,
		bool subsections = false,
		bool dropdowns = false,
		bool noDescriptions = false,
		string? title = null,
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogRenderingService(logFactory, configurationContext);

		var allFeatureIds = ExpandCommaSeparated(hideFeatures);

		ChangelogFileType? ft = fileType?.ToLowerInvariant() switch
		{
			"markdown" => ChangelogFileType.Markdown,
			"asciidoc" => ChangelogFileType.Asciidoc,
			"gfm" => ChangelogFileType.Gfm,
			_ => null
		};
		if (ft is null)
		{
			collector.EmitError(string.Empty, $"Invalid file-type '{fileType}'. Valid values are 'markdown', 'asciidoc', or 'gfm'.");
			return 1;
		}

		// Parse each --input value into BundleInput objects
		var bundles = BundleInputParser.ParseAll(input);

		var renderInput = new RenderChangelogsArguments
		{
			Bundles = bundles,
			Output = output,
			Title = title,
			Subsections = subsections,
			Dropdowns = dropdowns,
			HideDescriptions = noDescriptions,
			HideFeatures = allFeatureIds.Count > 0 ? allFeatureIds.ToArray() : null,
			FileType = ft.Value,
			Config = config?.FullName
		};

		serviceInvoker.AddCommand(service, renderInput,
			async static (s, collector, state, ctx) => await s.RenderChangelogs(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>Create changelog entries from the PRs referenced in a GitHub release.</summary>
	/// <param name="repo">Required: GitHub repository in owner/repo format (e.g., "elastic/elasticsearch" or just "elasticsearch" which defaults to elastic/elasticsearch)</param>
	/// <param name="version">Optional: Version tag to fetch (e.g., "v9.0.0", "9.0.0"). Defaults to "latest"</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="description">Optional: Bundle description text with placeholder support. Supports VERSION, LIFECYCLE, OWNER, and REPO placeholders. Overrides bundle.description from config.</param>
	/// <param name="output">Optional: Output directory for changelog files. Falls back to bundle.directory in changelog.yml when not specified. Defaults to './changelogs'</param>
	/// <param name="releaseDate">Optional: Explicit release date for the bundle in YYYY-MM-DD format. Overrides GitHub release published date.</param>
	/// <param name="stripTitlePrefix">Optional: Remove square brackets and text within them from the beginning of PR titles (e.g., "[Inference API] Title" becomes "Title")</param>
	/// <param name="warnOnTypeMismatch">Optional: Warn when the type inferred from release notes section headers doesn't match the type derived from PR labels. Defaults to true</param>
	/// <param name="ctx"></param>
	[NoOptionsInjection]
	public async Task<int> GhRelease(
		[Argument] string repo,
		[Argument] string version = "latest",
		[Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo? config = null,
		string? description = null,
		string? output = null,
		string? releaseDate = null,
		bool stripTitlePrefix = false,
		bool warnOnTypeMismatch = true,
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		await using var serviceInvoker = new ServiceInvoker(collector);

		// --output CLI > bundle.directory config > ./changelogs (service default)
		var bundleConfig = await new ChangelogConfigurationLoader(logFactory, configurationContext, _fileSystem)
			.LoadChangelogConfiguration(collector, config?.FullName, ctx);
		var resolvedOutput = !string.IsNullOrWhiteSpace(output) ? output : bundleConfig?.Bundle?.Directory;

		IGitHubReleaseService releaseService = new GitHubReleaseService(logFactory);
		IGitHubPrService prService = new GitHubPrService(logFactory);
		var service = new GitHubReleaseChangelogService(logFactory, configurationContext, releaseService, prService);

		// Validate release date format if provided
		if (!string.IsNullOrWhiteSpace(releaseDate) && !DateOnly.TryParseExact(releaseDate, "yyyy-MM-dd", out _))
		{
			collector.EmitError(string.Empty, $"Invalid --release-date format '{releaseDate}'. Expected YYYY-MM-DD format.");
			return 1;
		}

		// Resolve stripTitlePrefix: CLI flag true → explicit true; otherwise null (use config default)
		var stripTitlePrefixResolved = stripTitlePrefix ? true : (bool?)null;

		var input = new CreateChangelogsFromReleaseArguments
		{
			Repository = repo,
			Version = version,
			Config = config?.FullName,
			Output = resolvedOutput,
			StripTitlePrefix = stripTitlePrefixResolved,
			WarnOnTypeMismatch = warnOnTypeMismatch,
			Description = description,
			ReleaseDate = releaseDate
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.CreateChangelogsFromRelease(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>Append or exclude changelog entries in a published bundle without modifying it.</summary>
	/// <remarks>Creates an immutable <c>.amend-N.yaml</c> sidecar file alongside the original bundle.</remarks>
	/// <param name="bundlePath">Required: Path to the original bundle file to amend</param>
	/// <param name="add">Optional: Changelog YAML paths to add. Repeat <c>--add</c> or pass a comma-separated list in one value (for example, <c>--add "file1.yaml,file2.yaml"</c>). Supports tilde (~) expansion and relative paths.</param>
	/// <param name="remove">Optional: Changelog YAML paths to exclude from the effective bundle. Repeat <c>--remove</c> or pass a comma-separated list in one value. Supports tilde (~) expansion and relative paths.</param>
	/// <param name="resolve">Optional: When using <c>--add</c>, inline each added changelog's content in the amend file. Use <c>--no-resolve</c> to record file references only. When omitted, inferred from the parent bundle. Does not apply to <c>--remove</c>.</param>
	/// <param name="force">Optional: When removing, match by file name even if the bundle checksum differs from the file on disk.</param>
	/// <param name="dryRun">Optional: Preview changes without writing an amend file.</param>
	[NoOptionsInjection]
	public async Task<int> BundleAmend(
		[Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo bundlePath,
		string[]? add = null,
		string[]? remove = null,
		bool? resolve = null,
		bool force = false,
		bool dryRun = false,
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogBundleAmendService(logFactory, configurationContext: configurationContext);

		var normalizedAddFiles = add != null
			? ExpandCommaSeparated(add).Select(NormalizePath).ToList()
			: [];
		var normalizedRemoveFiles = remove != null
			? ExpandCommaSeparated(remove).Select(NormalizePath).ToList()
			: [];

		if (normalizedAddFiles.Count == 0 && normalizedRemoveFiles.Count == 0)
		{
			collector.EmitError(string.Empty, "At least one file must be specified with --add or --remove");
			_ = collector.StartAsync(ctx);
			await collector.WaitForDrain();
			await collector.StopAsync(ctx);
			return 1;
		}

		var normalizedBundlePath = bundlePath.FullName;

		var input = new AmendBundleArguments
		{
			BundlePath = normalizedBundlePath,
			AddFiles = normalizedAddFiles,
			RemoveFiles = normalizedRemoveFiles,
			Resolve = resolve,
			Force = force,
			DryRun = dryRun
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.AmendBundle(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>(CI) Evaluate a pull request for changelog generation eligibility and set GitHub Actions outputs.</summary>
	/// <remarks>
	/// Runs pre-flight checks (body-only edit, bot loop, manual edit), applies label rules from
	/// <c>changelog.yml</c>, and resolves the entry type and title. Designed to be called from a
	/// GitHub Actions workflow step.
	/// </remarks>
	/// <param name="config">Path to the changelog.yml configuration file</param>
	/// <param name="owner">GitHub repository owner</param>
	/// <param name="repo">GitHub repository name</param>
	/// <param name="prNumber">Pull request number</param>
	/// <param name="prTitle">Pull request title</param>
	/// <param name="prLabels">Comma-separated PR labels</param>
	/// <param name="headRef">PR head branch ref</param>
	/// <param name="headSha">PR head commit SHA</param>
	/// <param name="eventAction">Optional: GitHub event action (e.g., opened, synchronize, edited). When omitted, body-only-edit and bot-loop checks are skipped.</param>
	/// <param name="titleChanged">Whether the PR title changed (for edited events)</param>
	/// <param name="bodyChanged">Whether the PR body changed (for edited events)</param>
	/// <param name="stripTitlePrefix">Remove square-bracket prefixes from the PR title</param>
	/// <param name="botName">Bot login name for loop detection</param>
	/// <param name="ctx"></param>
	[NoOptionsInjection]
	public async Task<int> EvaluatePr(
		[FileExtensions(Extensions = "yml,yaml")] FileInfo config,
		string owner,
		string repo,
		int prNumber,
		string prTitle,
		string prLabels,
		string headRef,
		string headSha,
		string? eventAction = null,
		bool titleChanged = false,
		bool bodyChanged = false,
		bool stripTitlePrefix = false,
		string botName = "github-actions[bot]",
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		await using var serviceInvoker = new ServiceInvoker(collector);

		IGitHubPrService prService = new GitHubPrService(logFactory);
		var service = new ChangelogPrEvaluationService(logFactory, configurationContext, prService, githubActionsService);

		var prBody = await ReadPrBodyFromEnvironmentAsync(ctx);

		var args = new EvaluatePrArguments
		{
			Config = config.FullName,
			Owner = owner,
			Repo = repo,
			PrNumber = prNumber,
			PrTitle = prTitle,
			PrBody = prBody,
			PrLabels = prLabels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
			HeadRef = headRef,
			HeadSha = headSha,
			EventAction = eventAction,
			TitleChanged = titleChanged,
			BodyChanged = bodyChanged,
			StripTitlePrefix = stripTitlePrefix,
			BotName = botName
		};

		serviceInvoker.AddCommand(service, args,
			async static (s, collector, state, ctx) => await s.EvaluatePr(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>(CI) Package changelog artifact for cross-workflow transfer.</summary>
	/// <remarks>
	/// Resolves final status from evaluate-pr + changelog add outcomes, copies generated YAML,
	/// writes metadata.json, and sets GitHub Actions outputs. Always succeeds (exit 0) so the upload step runs.
	///
	/// <para>
	/// The <c>isFork</c>, <c>canCommit</c> and <c>maintainerCanModify</c> parameters are declared
	/// as <c>bool?</c> so the generated CLI emits both <c>--flag</c> and <c>--no-flag</c> pairs
	/// (Argh convention). A plain <c>bool</c> would expose presence-only switches: passing
	/// <c>--can-commit "false"</c> would set <c>canCommit = true</c> (the flag is present) and
	/// silently discard the literal <c>"false"</c> as a stray positional. Callers that forward a
	/// dynamic value (<c>--can-commit "$VAR"</c>) would then misroute fork PRs into the
	/// commit-and-push branch and die on a detached-HEAD push. See elastic/docs-actions#172
	/// for the workflow-side fix.
	/// </para>
	/// </remarks>
	/// <param name="stagingDir">Directory where changelog add wrote the generated YAML</param>
	/// <param name="outputDir">Directory to write the artifact (metadata.json + YAML)</param>
	/// <param name="evaluateStatus">Status output from the evaluate-pr step</param>
	/// <param name="generateOutcome">Outcome of the changelog add step (success/failure)</param>
	/// <param name="prNumber">Pull request number</param>
	/// <param name="headRef">PR head branch ref</param>
	/// <param name="headSha">PR head commit SHA</param>
	/// <param name="isFork">Whether the PR is from a fork (pass --is-fork / --no-is-fork; omit to leave null which is treated as false)</param>
	/// <param name="canCommit">Whether the commit strategy allows committing (pass --can-commit / --no-can-commit; omit to leave null which is treated as false)</param>
	/// <param name="maintainerCanModify">Whether the fork PR allows maintainer edits (pass --maintainer-can-modify / --no-maintainer-can-modify; omit to leave null which is treated as false)</param>
	/// <param name="headRepo">Fork repository full name (owner/repo)</param>
	/// <param name="labelTable">Optional: markdown label table from evaluate-pr</param>
	/// <param name="productLabelTable">Optional: markdown product label table from evaluate-pr</param>
	/// <param name="skipLabels">Optional: comma-separated skip labels from evaluate-pr</param>
	/// <param name="config">Optional: path to changelog.yml</param>
	/// <param name="existingChangelogFilename">Optional: filename of a previously committed changelog for this PR</param>
	[NoOptionsInjection]
	public async Task<int> PrepareArtifact(
		string stagingDir,
		string outputDir,
		string evaluateStatus,
		string generateOutcome,
		int prNumber,
		string headRef,
		string headSha,
		bool? isFork = null,
		bool? canCommit = null,
		bool? maintainerCanModify = null,
		string? headRepo = null,
		string? labelTable = null,
		string? productLabelTable = null,
		string? skipLabels = null,
		string? config = null,
		string? existingChangelogFilename = null,
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealGitRootForPathWrite(null, outputDir);
		var service = new ChangelogPrepareArtifactService(logFactory, configurationContext, githubActionsService, fs);

		var args = new PrepareArtifactArguments
		{
			StagingDir = stagingDir,
			OutputDir = outputDir,
			EvaluateStatus = evaluateStatus,
			GenerateOutcome = generateOutcome,
			PrNumber = prNumber,
			HeadRef = headRef,
			HeadSha = headSha,
			IsFork = isFork,
			HeadRepo = headRepo,
			CanCommit = canCommit,
			MaintainerCanModify = maintainerCanModify,
			LabelTable = labelTable,
			ProductLabelTable = productLabelTable,
			SkipLabels = skipLabels,
			Config = config,
			ExistingChangelogFilename = existingChangelogFilename
		};

		serviceInvoker.AddCommand(service, args,
			async static (s, collector, state, ctx) => await s.PrepareArtifact(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>(CI) Evaluate downloaded artifact in the resolving workflow.</summary>
	/// <remarks>
	/// Reads metadata, validates PR state (SHA, labels), and sets GitHub Actions outputs
	/// for downstream steps (commit, comment).
	/// </remarks>
	/// <param name="metadata">Path to the downloaded metadata.json file</param>
	/// <param name="owner">GitHub repository owner</param>
	/// <param name="repo">GitHub repository name</param>
	[NoOptionsInjection]
	public async Task<int> EvaluateArtifact(
		string metadata,
		string owner,
		string repo,
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealGitRootForPathWrite(null, metadata);
		IGitHubPrService prService = new GitHubPrService(logFactory);
		var service = new ChangelogArtifactEvaluationService(logFactory, prService, githubActionsService, fs);

		var args = new EvaluateArtifactArguments
		{
			MetadataPath = metadata,
			Owner = owner,
			Repo = repo
		};

		serviceInvoker.AddCommand(service, args,
			async static (s, collector, state, ctx) => await s.EvaluateArtifact(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}


	private static List<string> ExpandCommaSeparated(string[]? values)
	{
		if (values is not { Length: > 0 })
			return [];

		var result = new List<string>();
		foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)))
		{
			if (value.Contains(','))
				result.AddRange(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
			else
				result.Add(value);
		}
		return result;
	}

	// PR_BODY can hit GitHub's 65,536-char limit and exceed runner env-var
	// budgets when passed inline. PR_BODY_FILE lets callers stage the body
	// in a file under RUNNER_TEMP and pass the path instead, which keeps
	// the body off the env block entirely. Cap reads at 256 KiB to bound
	// memory if a caller hands us a hostile path.
	private const int MaxPrBodyFileBytes = 256 * 1024;

	private async Task<string?> ReadPrBodyFromEnvironmentAsync(CancellationToken ct)
	{
		var prBodyFile = environmentVariables.GetEnvironmentVariable("PR_BODY_FILE");
		if (string.IsNullOrWhiteSpace(prBodyFile))
			return environmentVariables.GetEnvironmentVariable("PR_BODY");

		var info = _fileSystem.FileInfo.New(prBodyFile);
		if (!info.Exists)
		{
			collector.EmitWarning(string.Empty, $"PR_BODY_FILE points to a missing file: {prBodyFile}");
			return null;
		}

		if (info.Length <= MaxPrBodyFileBytes)
			return await _fileSystem.File.ReadAllTextAsync(prBodyFile, ct);

		collector.EmitHint(string.Empty, $"PR_BODY_FILE exceeds {MaxPrBodyFileBytes} bytes ({info.Length}); truncating.");

		var buffer = ArrayPool<byte>.Shared.Rent(MaxPrBodyFileBytes);
		try
		{
			await using var stream = info.OpenRead();
			var slice = buffer.AsMemory(0, MaxPrBodyFileBytes);
			await stream.ReadExactlyAsync(slice, ct);
			return Encoding.UTF8.GetString(slice.Span);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private static string GetPathForConfig(string repoPath, string targetPath)
	{
		var relativePath = Path.GetRelativePath(repoPath, targetPath);

		// Prefer relative path when it does not escape the repo (e.g. not ".." or "..\..")
		var useRelative = !relativePath.StartsWith("..", StringComparison.Ordinal) &&
			!Path.IsPathRooted(relativePath) &&
			relativePath != targetPath;

		var pathForConfig = useRelative ? relativePath : targetPath;
		pathForConfig = pathForConfig.Replace('\\', '/');

		// Quote if path contains characters that need escaping in YAML
		if (pathForConfig.Contains(':') || pathForConfig.Contains(' ') || pathForConfig.Contains('#'))
			return $"\"{pathForConfig.Replace("\"", "\\\"")}\"";

		return pathForConfig;
	}

	private string ApplyChangelogInitBundleRepoSeed(string content, string? ownerCli, string? repoCli, string repoRoot)
	{
		string? gitOwner = null;
		string? gitRepo = null;
		if (GitRemoteConfigurationReader.TryReadOriginUrl(_fileSystem, repoRoot, out var originUrl))
			_ = GitHubRemoteParser.TryParseGitHubComOwnerRepo(originUrl, out gitOwner, out gitRepo);

		return ChangelogTemplateSeeder.ApplyBundleRepoSeed(content, ownerCli, repoCli, gitOwner, gitRepo);
	}

	/// <summary>Upload changelog entries or bundle artifacts to S3 or Elasticsearch.</summary>
	/// <remarks>
	/// Uses content-hash–based incremental transfer — only changed files are uploaded.
	/// <para>
	/// Changelog entries are uploaded once under <c>changelog/{org}/{repo}/{branch}/{file}</c>, keyed by the
	/// authoring owner (<c>--owner</c> &gt; <c>bundle.owner</c> &gt; git remote), repository (<c>--repo</c>
	/// &gt; <c>bundle.repo</c> &gt; git remote), and branch (<c>--branch</c> &gt; the current checkout's
	/// branch). The branch is stored verbatim, so a branch's <c>/</c> become real key separators. Bundles
	/// are uploaded under <c>bundle/{product}/{file}</c>, product-scoped from the bundle YAML, and do not
	/// require an owner/repo/branch.
	/// </para>
	/// </remarks>
	/// <param name="artifactType">Artifact type to upload: 'changelog' (individual entries) or 'bundle' (consolidated bundles).</param>
	/// <param name="target">Upload destination: 's3' or 'elasticsearch'.</param>
	/// <param name="s3BucketName">S3 bucket name (required when target is 's3').</param>
	/// <param name="config">Path to changelog.yml configuration file. Defaults to docs/changelog.yml.</param>
	/// <param name="directory">Override changelog directory instead of reading it from config.</param>
	/// <param name="repo">GitHub repository name, the second segment of changelog entry keys (changelog/{org}/{repo}/{branch}/...). Falls back to bundle.repo in changelog.yml, then the git remote origin. Required for changelog uploads; ignored for bundle uploads.</param>
	/// <param name="owner">GitHub owner (org), the first segment of changelog entry keys (changelog/{org}/{repo}/{branch}/...). Falls back to bundle.owner in changelog.yml, then the git remote origin. Required for changelog uploads; ignored for bundle uploads.</param>
	/// <param name="branch">Branch, the third segment of changelog entry keys (changelog/{org}/{repo}/{branch}/...), stored verbatim. Falls back to the current checkout's branch. Required for changelog uploads; ignored for bundle uploads.</param>
	/// <param name="skipEtagCheck">Upload every discovered file even when its content hash matches the remote object. Use to re-trigger downstream scrubbers without changing file content.</param>
	[NoOptionsInjection]
	public async Task<int> Upload(
		string artifactType,
		string target,
		string s3BucketName = "",
		[Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo? config = null,
		[ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? directory = null,
		string? repo = null,
		string? owner = null,
		string? branch = null,
		bool skipEtagCheck = false,
		CancellationToken ct = default
	)
	{
		var ctx = ct;
		if (!Enum.TryParse<ArtifactType>(artifactType, ignoreCase: true, out var parsedArtifactType))
		{
			collector.EmitError(string.Empty, $"Invalid artifact type '{artifactType}'. Valid values: changelog, bundle");
			return 1;
		}

		if (!Enum.TryParse<UploadTargetKind>(target, ignoreCase: true, out var parsedTarget))
		{
			collector.EmitError(string.Empty, $"Invalid target '{target}'. Valid values: s3, elasticsearch");
			return 1;
		}

		if (parsedTarget == UploadTargetKind.S3 && string.IsNullOrWhiteSpace(s3BucketName))
		{
			collector.EmitError(string.Empty, "--s3-bucket-name is required when target is 's3'");
			return 1;
		}

		var resolvedDirectory = directory != null ? directory?.FullName : null;
		var resolvedConfig = config != null ? config?.FullName : null;

		// Resolve the authoring owner/repo/branch for entry keys: CLI flags > bundle.{owner,repo}
		// (changelog.yml) > git. The repo is reduced to a single path segment (owner/repo -> repo) for the
		// changelog/{org}/{repo}/{branch}/ key.
		var (resolvedRepo, resolvedOwner, resolvedBranch) = await ResolveUploadRepoOwnerBranch(repo, owner, branch, resolvedConfig, resolvedDirectory, ctx);

		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new ChangelogUploadService(logFactory, configurationContext);
		var args = new ChangelogUploadArguments
		{
			ArtifactType = parsedArtifactType,
			Target = parsedTarget,
			S3BucketName = s3BucketName,
			Config = resolvedConfig,
			Directory = resolvedDirectory,
			Repo = resolvedRepo,
			Owner = resolvedOwner,
			Branch = resolvedBranch,
			SkipEtagCheck = skipEtagCheck
		};
		serviceInvoker.AddCommand(service, args,
			static async (s, c, state, ct) => await s.Upload(c, state, ct)
		);
		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>Resolves the authoring repo/owner/branch for uploads (CLI flags &gt; <c>bundle.{repo,owner}</c> &gt; git); owner falls back to the <c>owner/</c> prefix of repo (<see cref="ChangelogRepoOwnerResolver"/>) before git, reducing the repo to a single path segment.</summary>
	private async Task<(string? Repo, string? Owner, string? Branch)> ResolveUploadRepoOwnerBranch(string? repoCli, string? ownerCli, string? branchCli, string? configPath, string? uploadDirectory, CancellationToken ctx)
	{
		var bundleConfig = await new ChangelogConfigurationLoader(logFactory, configurationContext, _fileSystem)
			.LoadChangelogConfiguration(collector, configPath, ctx);

		// Anchor the git fallbacks to the upload source (config file or changelog directory), not the
		// process cwd, so an out-of-tree --config/--directory resolves the right origin and branch. Both
		// values are absolute (FileInfo/DirectoryInfo FullName) when present.
		string? anchor = null;
		if (!string.IsNullOrWhiteSpace(configPath))
		{
			var configDir = _fileSystem.Path.GetDirectoryName(configPath);
			if (!string.IsNullOrWhiteSpace(configDir) && _fileSystem.Directory.Exists(configDir))
				anchor = configDir;
		}
		if (anchor is null && !string.IsNullOrWhiteSpace(uploadDirectory) && _fileSystem.Directory.Exists(uploadDirectory))
			anchor = uploadDirectory;
		anchor ??= Directory.GetCurrentDirectory();

		string? gitOwner = null;
		string? gitRepo = null;
		var repoRoot = Paths.FindGitRoot(_fileSystem.DirectoryInfo.New(anchor))?.FullName ?? anchor;
		if (GitRemoteConfigurationReader.TryReadOriginUrl(_fileSystem, repoRoot, out var originUrl))
			_ = GitHubRemoteParser.TryParseGitHubComOwnerRepo(originUrl, out gitOwner, out gitRepo);

		var explicitRepo = !string.IsNullOrWhiteSpace(repoCli) ? repoCli : bundleConfig?.Bundle?.Repo;
		var resolvedRepo = explicitRepo ?? gitRepo;
		var resolvedOwner = ChangelogRepoOwnerResolver.ResolveOwner(ownerCli ?? bundleConfig?.Bundle?.Owner, explicitRepo, gitOwner);

		// The producer branch is the branch being published: --branch, else the current checkout's branch.
		// bundle.branch is intentionally not consulted here — it selects which pool to read when bundling.
		var resolvedBranch = branchCli;
		if (string.IsNullOrWhiteSpace(resolvedBranch))
		{
			var checkout = GitCheckoutInformationFactory.Create(_fileSystem.DirectoryInfo.New(anchor), _fileSystem);
			resolvedBranch = checkout.Branch;
		}

		resolvedRepo = ChangelogRepoOwnerResolver.NormalizeRepo(resolvedRepo);

		return (resolvedRepo, resolvedOwner, resolvedBranch);
	}

	/// <summary>
	/// Normalizes a file path by expanding tilde (~) to the user's home directory
	/// and converting relative paths to absolute paths.
	/// </summary>
	/// <param name="path">The path to normalize</param>
	/// <returns>The normalized absolute path</returns>
	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return path;

		var trimmedPath = path.Trim();

		// Expand tilde to user's home directory
		if (trimmedPath.StartsWith("~/", StringComparison.Ordinal) || trimmedPath.StartsWith("~\\", StringComparison.Ordinal))
		{
			var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			homeDirectory = homeDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			trimmedPath = homeDirectory + Path.DirectorySeparatorChar + trimmedPath[2..];
		}
		else if (trimmedPath == "~")
		{
			trimmedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		}

		// Convert to absolute path (handles relative paths like ./file or ../file)
		return Path.GetFullPath(trimmedPath);
	}

}

