// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Builder.Arguments;
using Elastic.Changelog;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.Creation;
using Elastic.Changelog.Evaluation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.GithubRelease;
using Elastic.Changelog.Rendering;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed partial class ChangelogCommand(
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
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogCommand>();
	/// <summary>
	/// Changelog commands. Use 'changelog add' to create a new changelog or 'changelog bundle' to create a consolidated list of changelogs.
	/// </summary>
	[Command("")]
	public Task<int> Default()
	{
		collector.EmitError(string.Empty, "Please specify a subcommand. Available subcommands:\n  - 'changelog add': Create a new changelog from command-line input\n  - 'changelog bundle': Create a consolidated list of changelog files\n  - 'changelog init': Initialize changelog configuration and folder structure\n  - 'changelog render': Render a bundled changelog to markdown or asciidoc files\n  - 'changelog upload': Upload changelog or bundle artifacts to S3 or Elasticsearch\n  - 'changelog gh-release': Create changelogs from a GitHub release\n  - 'changelog evaluate-pr': (CI) Evaluate a PR for changelog generation eligibility\n\nRun 'changelog <subcommand> --help' for usage information.");
		return Task.FromResult(1);
	}

	/// <summary>
	/// Initialize changelog configuration and folder structure. Creates changelog.yml from the example template in the docs folder (discovered via docset.yml when present, or at {path}/docs which is created if needed), and creates changelog and releases subdirectories if they do not exist.
	/// When changelog.yml already exists and --changelog-dir or --bundles-dir is specified, updates the bundle.directory and/or bundle.output_directory fields accordingly.
	/// </summary>
	/// <param name="path">Optional: Repository root path. Defaults to the output of pwd (current directory). Docs folder is {path}/docs, created if it does not exist.</param>
	/// <param name="changelogDir">Optional: Path to changelog directory. Defaults to {docsFolder}/changelog.</param>
	/// <param name="bundlesDir">Optional: Path to bundles output directory. Defaults to {docsFolder}/releases.</param>
	[Command("init")]
	public Task<int> Init(
		string? path = null,
		string? changelogDir = null,
		string? bundlesDir = null
	)
	{
		var rootPath = NormalizePath(path ?? ".");
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
		var changelogPath = NormalizePath(changelogDir ?? "changelog");
		var bundlesPath = NormalizePath(bundlesDir ?? "releases");

		var useNonDefaultChangelogDir = changelogDir != null;
		var useNonDefaultBundlesDir = bundlesDir != null;
		var repoRoot = Paths.FindGitRoot(docsFolder)?.FullName ?? docsFolder.FullName;

		// Create changelog.yml from example if it does not exist
		if (!_fileSystem.File.Exists(configPath))
		{
			byte[]? templateBytes = null;
			using (var stream = typeof(ChangelogCommand).Assembly.GetManifestResourceStream("Documentation.Builder.changelog.example.yml"))
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

				_fileSystem.File.WriteAllText(configPath, content);
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

	/// <summary>
	/// Add a new changelog from command-line input
	/// </summary>
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
	/// <param name="issues">Optional: Issue URL(s) or number(s) (comma-separated), or a path to a newline-delimited file containing issue URLs or numbers. Can be specified multiple times. Each occurrence can be either comma-separated issues (e.g., `--issues "https://github.com/owner/repo/issues/123,456"`) or a file path (e.g., `--issues /path/to/file.txt`). If --owner and --repo are provided, issue numbers can be used instead of URLs. If specified, --title can be derived from the issue. Creates one changelog file per issue.</param>
	/// <param name="owner">Optional: GitHub repository owner (used when --prs or --issues contains just numbers, or when using --release-version). Falls back to bundle.owner in changelog.yml when not specified. If that value is also absent, "elastic" is used.</param>
	/// <param name="output">Optional: Output directory for the changelog. Falls back to bundle.directory in changelog.yml when not specified. Defaults to current directory.</param>
	/// <param name="prs">Optional: Pull request URL(s) or PR number(s) (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. Each occurrence can be either comma-separated PRs (e.g., `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (e.g., `--prs /path/to/file.txt`). When specifying PRs directly, provide comma-separated values. When specifying a file path, provide a single value that points to a newline-delimited file. If --owner and --repo are provided, PR numbers can be used instead of URLs. If specified, --title can be derived from the PR. If mappings are configured, --areas and --type can also be derived from the PR. Creates one changelog file per PR.</param>
	/// <param name="repo">Optional: GitHub repository name (used when --prs or --issues contains just numbers, or when using --release-version). Falls back to bundle.repo in changelog.yml when not specified.</param>
	/// <param name="stripTitlePrefix">Optional: When used with --prs, remove square brackets and text within them from the beginning of PR titles, and also remove a colon if it follows the closing bracket (e.g., "[Inference API] Title" becomes "Title", "[ES|QL]: Title" becomes "Title", "[Discover][ESQL] Title" becomes "Title")</param>
	/// <param name="subtype">Optional: Subtype for breaking changes (api, behavioral, configuration, etc.)</param>
	/// <param name="title">Optional: A short, user-facing title (max 80 characters). Required if neither --prs nor --issues is specified. If --prs and --title are specified, the latter value is used instead of what exists in the PR.</param>
	/// <param name="type">Optional: Type of change (feature, enhancement, bug-fix, breaking-change, etc.). Required if neither --prs nor --issues is specified. If mappings are configured, type can be derived from the PR or issue.</param>
	/// <param name="concise">Optional: Omit schema reference comments from generated YAML files. Useful in CI to produce compact output.</param>
	/// <param name="usePrNumber">Optional: Use PR numbers for filenames instead of timestamp-slug. With both --prs (which creates one changelog per specified PR) and --issues (which creates one changelog per specified issue), each changelog filename will be derived from its PR numbers. Requires --prs or --issues. Mutually exclusive with --use-issue-number.</param>
	/// <param name="useIssueNumber">Optional: Use issue numbers for filenames instead of timestamp-slug. With both --prs (which creates one changelog per specified PR) and --issues (which creates one changelog per specified issue), each changelog filename will be derived from its issues. Requires --prs or --issues. Mutually exclusive with --use-pr-number.</param>
	/// <param name="releaseVersion">Optional: GitHub release tag to fetch PRs from (e.g., "v9.2.0" or "latest"). When specified, creates one changelog per PR in the release notes. Requires --repo (or bundle.repo in changelog.yml). Mutually exclusive with --prs and --issues. Does not create a bundle; use 'changelog gh-release' for that.</param>
	/// <param name="ctx"></param>
	[Command("add")]
	public async Task<int> Create(
		[ProductInfoParser] List<ProductArgument>? products = null,
		string? action = null,
		string[]? areas = null,
		bool concise = false,
		string? config = null,
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
		string? releaseVersion = null,
		string? repo = null,
		bool stripTitlePrefix = false,
		string? subtype = null,
		string? title = null,
		string? type = null,
		bool usePrNumber = false,
		bool useIssueNumber = false,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		// Mutual exclusivity: --release-version cannot be combined with --prs or --issues
		if (releaseVersion != null)
		{
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
		// This applies to --prs, --issues, and --release-version alike.
		var bundleConfig = await new ChangelogConfigurationLoader(logFactory, configurationContext, _fileSystem)
			.LoadChangelogConfiguration(collector, config, ctx);
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
				Config = config,
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

		// Parse PRs: handle both comma-separated values and file paths
		string[]? parsedPrs = null;
		if (prs is { Length: > 0 })
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
		var resolvedProducts = products ?? [];

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
			collector.EmitError(string.Empty, "--use-pr-number requires --prs or --issues to be specified.");
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
			Config = config,
			UsePrNumber = usePrNumber,
			UseIssueNumber = useIssueNumber,
			StripTitlePrefix = stripTitlePrefixResolved,
			ExtractReleaseNotes = extractReleaseNotes,
			ExtractIssues = extractIssues,
			Concise = concise
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.CreateChangelog(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Bundle changelog files. Can use either profile-based bundling (for example, "bundle elasticsearch-release 9.2.0") or command-line options (for example, "bundle --all") Only one command-line filter option can be specified: `--all`, `--input-products`, `--prs`, `--issues`, `--release-version`, or `--report`.
	/// </summary>
	/// <param name="profile">Optional: Profile name from bundle.profiles in config (for example, "elasticsearch-release"). When specified, the second argument is the version or promotion report URL.</param>
	/// <param name="profileArg">Optional: Version number or promotion report URL/path when using a profile (for example, "9.2.0" or "https://buildkite.../promotion-report.html")</param>
	/// <param name="profileReport">Optional: Promotion report or URL list file when also providing a version. When provided, the second argument must be a version string and this is the PR/issue filter source (for example, "bundle serverless-release 2026-02 ./report.html").</param>
	/// <param name="all">Include all changelogs in the directory.</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="directory">Optional: Directory containing changelog YAML files. Uses config bundle.directory or defaults to current directory</param>
	/// <param name="hideFeatures">Optional: Filter by feature IDs (comma-separated) or a path to a newline-delimited file containing feature IDs. Can be specified multiple times. Entries with matching feature-id values will be commented out when the bundle is rendered (by CLI render or {changelog} directive).</param>
	/// <param name="inputProducts">Filter by products in format "product target lifecycle, ..." (for example, "cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"). When specified, all three parts (product, target, lifecycle) are required but can be wildcards (*). Examples: "elasticsearch * *" matches all elasticsearch changelogs, "cloud-serverless 2025-12-02 *" matches cloud-serverless 2025-12-02 with any lifecycle, "* 9.3.* *" matches any product with target starting with "9.3.", "* * *" matches all changelogs (equivalent to --all).</param>
	/// <param name="issues">Filter by issue URLs (comma-separated), or a path to a newline-delimited file containing fully-qualified GitHub issue URLs. Can be specified multiple times.</param>
	/// <param name="output">Optional: Output path for the bundled changelog. Can be either (1) a directory path, in which case 'changelog-bundle.yaml' is created in that directory, or (2) a file path ending in .yml or .yaml. Uses config bundle.output_directory or defaults to 'changelog-bundle.yaml' in the input directory</param>
	/// <param name="outputProducts">Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...". Overrides any values from changelogs.</param>
	/// <param name="owner">GitHub repository owner, which is used when PRs or issues are specified as numbers or when using --release-version. Falls back to bundle.owner in changelog.yml when not specified. If that value is also absent, "elastic" is used.</param>
	/// <param name="prs">Filter by pull request URLs (comma-separated), or a path to a newline-delimited file containing fully-qualified GitHub PR URLs. Can be specified multiple times.</param>
	/// <param name="repo">GitHub repository name, which is used when PRs or issues are specified as numbers or when using --release-version. Falls back to bundle.repo in changelog.yml when not specified. If that value is also absent, the product ID is used.</param>
	/// <param name="report">A URL or file path to a promotion report. Extracts PR URLs and uses them as the filter.</param>
	/// <param name="releaseVersion">GitHub release tag to use as a filter source (for example, "v9.2.0" or "latest"). When specified, fetches the release, parses PR references from the release notes, and uses those PRs as the filter — equivalent to passing the PR list via --prs. When --output-products is not specified, it is inferred from the release tag and repository name.</param>
	/// <param name="resolve">Optional: Copy the contents of each changelog file into the entries array. Uses config bundle.resolve or defaults to false.</param>
	/// <param name="noResolve">Optional: Explicitly turn off resolve (overrides config).</param>
	/// <param name="ctx"></param>
	[Command("bundle")]
	public async Task<int> Bundle(
		[Argument] string? profile = null,
		[Argument] string? profileArg = null,
		[Argument] string? profileReport = null,
		bool all = false,
		string? config = null,
		string? directory = null,
		string[]? hideFeatures = null,
		[ProductInfoParser] List<ProductArgument>? inputProducts = null,
		string? output = null,
		[ProductInfoParser] List<ProductArgument>? outputProducts = null,
		string[]? issues = null,
		string? owner = null,
		string[]? prs = null,
		string? releaseVersion = null,
		string? repo = null,
		string? report = null,
		bool? resolve = null,
		bool noResolve = false,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogBundlingService(logFactory, configurationContext);

		var isProfileMode = !string.IsNullOrWhiteSpace(profile);

		// --release-version mode: resolve the release into a PR list and proceed as if --prs was specified
		if (releaseVersion != null)
		{
			if (all || (inputProducts is { Count: > 0 }) || (prs is { Length: > 0 }) || (issues is { Length: > 0 }))
			{
				collector.EmitError(string.Empty,
					"--release-version is mutually exclusive with --all, --input-products, --prs, and --issues.");
				return 1;
			}

			// Precedence: --repo CLI > bundle.repo config; --owner CLI > bundle.owner config > "elastic"
			var bundleConfig = await new ChangelogConfigurationLoader(logFactory, configurationContext, _fileSystem)
				.LoadChangelogConfiguration(collector, config, ctx);
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

		var allPrs = ExpandCommaSeparated(prs);
		var allIssues = ExpandCommaSeparated(issues);

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
			if (!string.IsNullOrWhiteSpace(report))
				forbidden.Add("--report");
			if (!string.IsNullOrWhiteSpace(output))
				forbidden.Add("--output");
			if (!string.IsNullOrWhiteSpace(repo))
				forbidden.Add("--repo");
			if (!string.IsNullOrWhiteSpace(owner))
				forbidden.Add("--owner");
			if (resolve.HasValue)
				forbidden.Add("--resolve");
			if (noResolve)
				forbidden.Add("--no-resolve");
			if (hideFeatures is { Length: > 0 })
				forbidden.Add("--hide-features");
			if (!string.IsNullOrWhiteSpace(config))
				forbidden.Add("--config");
			if (!string.IsNullOrWhiteSpace(directory))
				forbidden.Add("--directory");

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
			if (!string.IsNullOrWhiteSpace(report))
				specifiedFilters.Add("--report");

			if (specifiedFilters.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, --prs, --issues, --report, or use a profile (e.g., 'bundle elasticsearch-release 9.2.0')");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			if (specifiedFilters.Count > 1)
			{
				collector.EmitError(string.Empty, $"Multiple filter options cannot be specified together. You specified: {string.Join(", ", specifiedFilters)}. Please use only one filter option: --all, --input-products, --prs, --issues, or --report");
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

		// Determine resolve: CLI --no-resolve and --resolve override config. null = use config default.
		var shouldResolve = noResolve ? false : resolve;

		var allFeatureIdsForBundle = ExpandCommaSeparated(hideFeatures);

		var input = new BundleChangelogsArguments
		{
			Directory = directory,
			Output = processedOutput,
			All = all,
			InputProducts = inputProducts,
			OutputProducts = outputProducts,
			Resolve = shouldResolve,
			Prs = allPrs.Count > 0 ? allPrs.ToArray() : null,
			Issues = allIssues.Count > 0 ? allIssues.ToArray() : null,
			Owner = owner,
			Repo = repo,
			Profile = profile,
			ProfileArgument = profileArg,
			ProfileReport = isProfileMode ? profileReport : null,
			Report = !isProfileMode ? report : null,
			Config = config,
			HideFeatures = allFeatureIdsForBundle.Count > 0 ? allFeatureIdsForBundle.ToArray() : null
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.BundleChangelogs(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Remove changelog files. Can use either profile-based removal (e.g., "remove elasticsearch-release 9.2.0") or raw flags (e.g., "remove --all").
	/// When a file is referenced by an unresolved bundle, the command blocks by default to prevent breaking
	/// the {changelog} directive. Use --force to override.
	/// </summary>
	/// <param name="profile">Optional: Profile name from bundle.profiles in config (for example, "elasticsearch-release"). When specified, the second argument is the version or promotion report URL.</param>
	/// <param name="profileArg">Optional: Version number or promotion report URL/path when using a profile (for example, "9.2.0" or "https://buildkite.../promotion-report.html")</param>
	/// <param name="profileReport">Optional: Promotion report or URL list file when also providing a version. When provided, the second argument must be a version string and this is the PR/issue filter source.</param>
	/// <param name="all">Remove all changelogs in the directory. Exactly one filter option must be specified: --all, --products, --prs, --issues, or --report.</param>
	/// <param name="bundlesDir">Optional: Override the directory that is scanned for bundles during the dependency check. Auto-discovered from config or fallback paths when not specified.</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="directory">Optional: Directory containing changelog YAML files. Uses config bundle.directory or defaults to current directory</param>
	/// <param name="dryRun">Print the files that would be removed without deleting them. Valid in both profile and raw mode.</param>
	/// <param name="force">Proceed with removal even when files are referenced by unresolved bundles. Emits warnings instead of errors for each dependency. Valid in both profile and raw mode.</param>
	/// <param name="issues">Filter by issue URLs (comma-separated) or a path to a newline-delimited file containing fully-qualified GitHub issue URLs. Can be specified multiple times.</param>
	/// <param name="owner">Optional: GitHub repository owner, which is used when PRs or issues are specified as numbers or when using --release-version. Falls back to bundle.owner in changelog.yml when not specified. If that value is also absent, "elastic" is used.</param>
	/// <param name="products">Filter by products in format "product target lifecycle, ..." (for example, "elasticsearch 9.3.0 ga"). All three parts are required but can be wildcards (*).</param>
	/// <param name="prs">Filter by pull request URLs (comma-separated) or a path to a newline-delimited file containing fully-qualified GitHub PR URLs. Can be specified multiple times.</param>
	/// <param name="releaseVersion">GitHub release tag to use as a filter source (for example, "v9.2.0" or "latest"). Fetches the release, parses PR references from the release notes, and removes changelogs whose PR URLs match — equivalent to passing the PR list using --prs.</param>
	/// <param name="repo">GitHub repository name, which is used when PRs or issues are specified as numbers or when --release-version is used. Falls back to bundle.repo in changelog.yml when not specified. If that value is also absent, the product ID is used.</param>
	/// <param name="report">Optional (option-based mode only): URL or file path to a promotion report. Extracts PR URLs and uses them as the filter. Mutually exclusive with --all, --products, --prs, and --issues.</param>
	/// <param name="ctx"></param>
	[Command("remove")]
	public async Task<int> Remove(
		[Argument] string? profile = null,
		[Argument] string? profileArg = null,
		[Argument] string? profileReport = null,
		bool all = false,
		string? bundlesDir = null,
		string? config = null,
		string? directory = null,
		bool dryRun = false,
		bool force = false,
		string[]? issues = null,
		string? owner = null,
		[ProductInfoParser] List<ProductArgument>? products = null,
		string[]? prs = null,
		string? releaseVersion = null,
		string? repo = null,
		string? report = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogRemoveService(logFactory, configurationContext);

		var isProfileMode = !string.IsNullOrWhiteSpace(profile);

		// --release-version mode: resolve the release into a PR list and proceed as if --prs was specified
		if (releaseVersion != null)
		{
			if (all || (products is { Count: > 0 }) || (prs is { Length: > 0 }) || (issues is { Length: > 0 }))
			{
				collector.EmitError(string.Empty,
					"--release-version is mutually exclusive with --all, --products, --prs, and --issues.");
				return 1;
			}

			// Precedence: --repo CLI > bundle.repo config; --owner CLI > bundle.owner config > "elastic"
			var bundleConfig = await new ChangelogConfigurationLoader(logFactory, configurationContext, _fileSystem)
				.LoadChangelogConfiguration(collector, config, ctx);
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
		var resolvedDirectory = isProfileMode || string.IsNullOrWhiteSpace(directory)
			? null
			: NormalizePath(directory);

		var input = new ChangelogRemoveArguments
		{
			Directory = resolvedDirectory,
			All = all,
			Products = products,
			Prs = allPrs.Count > 0 ? allPrs.ToArray() : null,
			Issues = allIssues.Count > 0 ? allIssues.ToArray() : null,
			Owner = owner,
			Repo = repo,
			DryRun = dryRun,
			BundlesDir = string.IsNullOrWhiteSpace(bundlesDir) ? null : NormalizePath(bundlesDir),
			Force = force,
			Config = string.IsNullOrWhiteSpace(config) ? null : NormalizePath(config),
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

	/// <summary>
	/// Render bundled changelog(s) to markdown or asciidoc files
	/// </summary>
	/// <param name="input">Required: Bundle input(s) in format "bundle-file-path|changelog-file-path|repo|link-visibility" (use pipe as delimiter). To merge multiple bundles, separate them with commas. Only bundle-file-path is required. link-visibility can be "hide-links" or "keep-links" (default). Use "hide-links" for private repositories; when set, all PR and issue links for each affected entry are hidden (entries may have multiple links via the prs and issues arrays). Paths support tilde (~) expansion and relative paths.</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="fileType">Optional: Output file type. Valid values: "markdown" or "asciidoc". Defaults to "markdown"</param>
	/// <param name="hideFeatures">Filter by feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs. Can be specified multiple times. Entries with matching feature-id values will be commented out in the output.</param>
	/// <param name="output">Optional: Output directory for rendered files. Defaults to current directory</param>
	/// <param name="subsections">Optional: Group entries by area/component in subsections. For breaking changes with a subtype, groups by subtype instead of area. Defaults to false</param>
	/// <param name="title">Optional: Title to use for section headers in output files. Defaults to version from first bundle</param>
	/// <param name="ctx"></param>
	[Command("render")]
	public async Task<int> Render(
		string[]? input = null,
		string? config = null,
		string? fileType = "markdown",
		string[]? hideFeatures = null,
		string? output = null,
		bool subsections = false,
		string? title = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogRenderingService(logFactory, configurationContext);

		var allFeatureIds = ExpandCommaSeparated(hideFeatures);

		ChangelogFileType? ft = fileType?.ToLowerInvariant() switch
		{
			"markdown" => ChangelogFileType.Markdown,
			"asciidoc" => ChangelogFileType.Asciidoc,
			_ => null
		};
		if (ft is null)
		{
			collector.EmitError(string.Empty, $"Invalid file-type '{fileType}'. Valid values are 'markdown' or 'asciidoc'.");
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
			HideFeatures = allFeatureIds.Count > 0 ? allFeatureIds.ToArray() : null,
			FileType = ft.Value,
			Config = config
		};

		serviceInvoker.AddCommand(service, renderInput,
			async static (s, collector, state, ctx) => await s.RenderChangelogs(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Create changelogs from a GitHub release
	/// </summary>
	/// <param name="repo">Required: GitHub repository in owner/repo format (e.g., "elastic/elasticsearch" or just "elasticsearch" which defaults to elastic/elasticsearch)</param>
	/// <param name="version">Optional: Version tag to fetch (e.g., "v9.0.0", "9.0.0"). Defaults to "latest"</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="output">Optional: Output directory for changelog files. Falls back to bundle.directory in changelog.yml when not specified. Defaults to './changelogs'</param>
	/// <param name="stripTitlePrefix">Optional: Remove square brackets and text within them from the beginning of PR titles (e.g., "[Inference API] Title" becomes "Title")</param>
	/// <param name="warnOnTypeMismatch">Optional: Warn when the type inferred from release notes section headers doesn't match the type derived from PR labels. Defaults to true</param>
	/// <param name="ctx"></param>
	[Command("gh-release")]
	public async Task<int> GitHubRelease(
		[Argument] string repo,
		[Argument] string version = "latest",
		string? config = null,
		string? output = null,
		bool stripTitlePrefix = false,
		bool warnOnTypeMismatch = true,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		// --output CLI > bundle.directory config > ./changelogs (service default)
		var bundleConfig = await new ChangelogConfigurationLoader(logFactory, configurationContext, _fileSystem)
			.LoadChangelogConfiguration(collector, config, ctx);
		var resolvedOutput = !string.IsNullOrWhiteSpace(output) ? output : bundleConfig?.Bundle?.Directory;

		IGitHubReleaseService releaseService = new GitHubReleaseService(logFactory);
		IGitHubPrService prService = new GitHubPrService(logFactory);
		var service = new GitHubReleaseChangelogService(logFactory, configurationContext, releaseService, prService);

		// Resolve stripTitlePrefix: CLI flag true → explicit true; otherwise null (use config default)
		var stripTitlePrefixResolved = stripTitlePrefix ? true : (bool?)null;

		var input = new CreateChangelogsFromReleaseArguments
		{
			Repository = repo,
			Version = version,
			Config = config,
			Output = resolvedOutput,
			StripTitlePrefix = stripTitlePrefixResolved,
			WarnOnTypeMismatch = warnOnTypeMismatch
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.CreateChangelogsFromRelease(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Amend a bundle with additional changelog entries, creating an immutable .amend-N.yaml file.
	/// </summary>
	/// <param name="bundlePath">Required: Path to the original bundle file to amend</param>
	/// <param name="add">Required: Path(s) to changelog YAML file(s) to add as comma-separated values (e.g., --add "file1.yaml,file2.yaml"). Supports tilde (~) expansion and relative paths.</param>
	/// <param name="resolve">Optional: Copy the contents of each changelog file into the entries array. When not specified, inferred from the original bundle.</param>
	/// <param name="noResolve">Optional: Explicitly turn off resolve (overrides inference from original bundle).</param>
	/// <param name="ctx"></param>
	[Command("bundle-amend")]
	public async Task<int> BundleAmend(
		[Argument] string bundlePath,
		string[]? add = null,
		bool? resolve = null,
		bool noResolve = false,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogBundleAmendService(logFactory, configurationContext: configurationContext);

		if (add == null || add.Length == 0)
		{
			collector.EmitError(string.Empty, "At least one file must be specified with --add");
			_ = collector.StartAsync(ctx);
			await collector.WaitForDrain();
			await collector.StopAsync(ctx);
			return 1;
		}

		// Normalize the bundle path
		var normalizedBundlePath = NormalizePath(bundlePath);

		var normalizedAddFiles = ExpandCommaSeparated(add)
			.Select(NormalizePath)
			.ToList();

		// Determine resolve: CLI --no-resolve takes precedence, then CLI --resolve, then infer from bundle
		var shouldResolve = noResolve ? false : resolve;

		var input = new AmendBundleArguments
		{
			BundlePath = normalizedBundlePath,
			AddFiles = normalizedAddFiles.ToArray(),
			Resolve = shouldResolve
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.AmendBundle(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// (CI) Evaluate a PR for changelog generation eligibility. Performs pre-flight checks (body-only edit, bot loop, manual edit), loads config, checks label rules, resolves title/type, and sets GitHub Actions outputs.
	/// </summary>
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
	[Command("evaluate-pr")]
	public async Task<int> EvaluatePr(
		string config,
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
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		IGitHubPrService prService = new GitHubPrService(logFactory);
		var service = new ChangelogPrEvaluationService(logFactory, configurationContext, prService, githubActionsService);

		var prBody = environmentVariables.GetEnvironmentVariable("PR_BODY");

		var args = new EvaluatePrArguments
		{
			Config = config,
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

	/// <summary>
	/// Expands a CLI array parameter where each element may be comma-separated into a flat list of values.
	/// Filters out blank entries.
	/// </summary>
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

	/// <summary>
	/// Returns a path suitable for changelog.yml config (relative to repo when possible, forward slashes).
	/// Quotes the value if it contains YAML-special characters.
	/// </summary>
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

	/// <summary>
	/// Upload changelog or bundle artifacts to S3 or Elasticsearch.
	/// Uses content-hash–based incremental upload: only files whose content has changed are transferred.
	/// </summary>
	/// <param name="artifactType">Artifact type to upload: 'changelog' (individual entries) or 'bundle' (consolidated bundles).</param>
	/// <param name="target">Upload destination: 's3' or 'elasticsearch'.</param>
	/// <param name="s3BucketName">S3 bucket name (required when target is 's3').</param>
	/// <param name="config">Path to changelog.yml configuration file. Defaults to docs/changelog.yml.</param>
	/// <param name="directory">Override changelog directory instead of reading it from config.</param>
	[Command("upload")]
	public async Task<int> Upload(
		string artifactType,
		string target,
		string s3BucketName = "",
		string? config = null,
		string? directory = null,
		Cancel ctx = default
	)
	{
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

		var resolvedDirectory = directory != null ? NormalizePath(directory) : null;
		var resolvedConfig = config != null ? NormalizePath(config) : null;

		await using var serviceInvoker = new ServiceInvoker(collector);
		var service = new ChangelogUploadService(logFactory, configurationContext);
		var args = new ChangelogUploadArguments
		{
			ArtifactType = parsedArtifactType,
			Target = parsedTarget,
			S3BucketName = s3BucketName,
			Config = resolvedConfig,
			Directory = resolvedDirectory
		};
		serviceInvoker.AddCommand(service, args,
			static async (s, c, state, ct) => await s.Upload(c, state, ct)
		);
		return await serviceInvoker.InvokeAsync(ctx);
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

