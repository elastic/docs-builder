// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleAppFramework;
using Documentation.Builder.Arguments;
using Elastic.Changelog;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.GithubRelease;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed partial class ChangelogCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	[GeneratedRegex(@"^( *directory:\s*).+$", RegexOptions.Multiline)]
	private static partial Regex BundleDirectoryRegex();

	[GeneratedRegex(@"^( *output_directory:\s*).+$", RegexOptions.Multiline)]
	private static partial Regex BundleOutputDirectoryRegex();

	private readonly IFileSystem _fileSystem = new FileSystem();
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogCommand>();
	/// <summary>
	/// Changelog commands. Use 'changelog add' to create a new changelog or 'changelog bundle' to create a consolidated list of changelogs.
	/// </summary>
	[Command("")]
	public Task<int> Default()
	{
		collector.EmitError(string.Empty, "Please specify a subcommand. Available subcommands:\n  - 'changelog add': Create a new changelog from command-line input\n  - 'changelog bundle': Create a consolidated list of changelog files\n  - 'changelog init': Initialize changelog configuration and folder structure\n  - 'changelog render': Render a bundled changelog to markdown or asciidoc files\n  - 'changelog gh-release': Create changelogs from a GitHub release\n\nRun 'changelog add --help', 'changelog bundle --help', 'changelog init --help', 'changelog render --help', or 'changelog gh-release --help' for usage information.");
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
			docsFolder = foundDocsFolder!;
		}
		else
		{
			var docsFolderPath = Path.Combine(rootPath, "docs");
			if (!_fileSystem.Directory.Exists(docsFolderPath))
			{
				_logger.LogInformation("Creating docs folder at {DocsFolderPath}", docsFolderPath);
				_ = _fileSystem.Directory.CreateDirectory(docsFolderPath);
			}

			docsFolder = _fileSystem.DirectoryInfo.New(docsFolderPath);
		}

		var configPath = _fileSystem.Path.Combine(docsFolder.FullName, "changelog.yml");
		var changelogPath = NormalizePath(changelogDir ?? "changelog");
		var bundlesPath = NormalizePath(bundlesDir ?? "releases");

		var useNonDefaultChangelogDir = changelogDir != null;
		var useNonDefaultBundlesDir = bundlesDir != null;
		var repoRoot = Paths.DetermineSourceDirectoryRoot(docsFolder)?.FullName ?? docsFolder.FullName;

		// Create changelog.yml from example if it does not exist
		if (!_fileSystem.File.Exists(configPath))
		{
			byte[]? templateBytes = null;
			using (var stream = typeof(ChangelogCommand).Assembly.GetManifestResourceStream("Documentation.Builder.changelog.example.yml"))
			{
				if (stream == null)
				{
					// Fallback: try config relative to current directory (for development)
					var localConfigDir = _fileSystem.Path.Combine(Directory.GetCurrentDirectory(), "config");
					var localConfigPath = _fileSystem.Path.Combine(localConfigDir, "changelog.example.yml");
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
	/// <param name="noExtractReleaseNotes">Optional: Turn off extraction of release notes from PR descriptions. By default, release notes are extracted when using --prs. Short release notes (≤120 characters, single line) are used as the title, long release notes (>120 characters or multi-line) are used as the description.</param>
	/// <param name="noExtractIssues">Optional: Turn off extraction of linked references. When using --prs: turns off extraction of linked issues from the PR body (e.g., "Fixes #123"). When using --issues: turns off extraction of linked PRs from the issue body (e.g., "Fixed by #123"). By default, linked references are extracted in both cases.</param>
	/// <param name="featureId">Optional: Feature flag ID</param>
	/// <param name="highlight">Optional: Include in release highlights</param>
	/// <param name="impact">Optional: How the user's environment is affected</param>
	/// <param name="issues">Optional: Issue URL(s) or number(s) (comma-separated), or a path to a newline-delimited file containing issue URLs or numbers. Can be specified multiple times. Each occurrence can be either comma-separated issues (e.g., `--issues "https://github.com/owner/repo/issues/123,456"`) or a file path (e.g., `--issues /path/to/file.txt`). If --owner and --repo are provided, issue numbers can be used instead of URLs. If specified, --title can be derived from the issue. Creates one changelog file per issue.</param>
	/// <param name="owner">Optional: GitHub repository owner (used when --prs or --issues contains just numbers)</param>
	/// <param name="output">Optional: Output directory for the changelog. Defaults to current directory</param>
	/// <param name="prs">Optional: Pull request URL(s) or PR number(s) (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. Each occurrence can be either comma-separated PRs (e.g., `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (e.g., `--prs /path/to/file.txt`). When specifying PRs directly, provide comma-separated values. When specifying a file path, provide a single value that points to a newline-delimited file. If --owner and --repo are provided, PR numbers can be used instead of URLs. If specified, --title can be derived from the PR. If mappings are configured, --areas and --type can also be derived from the PR. Creates one changelog file per PR.</param>
	/// <param name="repo">Optional: GitHub repository name (used when --prs or --issues contains just numbers)</param>
	/// <param name="stripTitlePrefix">Optional: When used with --prs, remove square brackets and text within them from the beginning of PR titles, and also remove a colon if it follows the closing bracket (e.g., "[Inference API] Title" becomes "Title", "[ES|QL]: Title" becomes "Title", "[Discover][ESQL] Title" becomes "Title")</param>
	/// <param name="subtype">Optional: Subtype for breaking changes (api, behavioral, configuration, etc.)</param>
	/// <param name="title">Optional: A short, user-facing title (max 80 characters). Required if neither --prs nor --issues is specified. If --prs and --title are specified, the latter value is used instead of what exists in the PR.</param>
	/// <param name="type">Optional: Type of change (feature, enhancement, bug-fix, breaking-change, etc.). Required if neither --prs nor --issues is specified. If mappings are configured, type can be derived from the PR or issue.</param>
	/// <param name="usePrNumber">Optional: Use the PR number(s) as the filename. With multiple PRs, uses hyphen-separated list (e.g., 137431-137432.yaml). Requires --prs. Mutually exclusive with --use-issue-number.</param>
	/// <param name="useIssueNumber">Optional: Use the issue number(s) as the filename. Requires --issues. When both --issues and --prs are specified, uses issue number if this flag is set. Mutually exclusive with --use-pr-number.</param>
	/// <param name="ctx"></param>
	[Command("add")]
	public async Task<int> Create(
		[ProductInfoParser] List<ProductArgument>? products = null,
		string? action = null,
		string[]? areas = null,
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

		IGitHubPrService githubPrService = new GitHubPrService(logFactory);
		var service = new ChangelogCreationService(logFactory, configurationContext, githubPrService);

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

		var shouldExtractReleaseNotes = !noExtractReleaseNotes;
		var shouldExtractIssues = !noExtractIssues;

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
			return 1;
		}

		if (usePrNumber && (parsedPrs == null || parsedPrs.Length == 0))
		{
			collector.EmitError(string.Empty, "--use-pr-number requires --prs to be specified.");
			return 1;
		}

		if (useIssueNumber && (parsedIssues == null || parsedIssues.Length == 0))
		{
			collector.EmitError(string.Empty, "--use-issue-number requires --issues to be specified.");
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
			Owner = owner,
			Repo = repo,
			Issues = parsedIssues ?? [],
			Description = description,
			Impact = impact,
			Action = action,
			FeatureId = featureId,
			Highlight = highlight,
			Output = output,
			Config = config,
			UsePrNumber = usePrNumber,
			UseIssueNumber = useIssueNumber,
			StripTitlePrefix = stripTitlePrefix,
			ExtractReleaseNotes = shouldExtractReleaseNotes,
			ExtractIssues = shouldExtractIssues
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.CreateChangelog(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Bundle changelog files. Can use either profile-based bundling (e.g., "bundle elasticsearch-release 9.2.0") or raw flags (e.g., "bundle --all").
	/// </summary>
	/// <param name="profile">Optional: Profile name from bundle.profiles in config (e.g., "elasticsearch-release"). When specified, the second argument is the version or promotion report URL.</param>
	/// <param name="profileArg">Optional: Version number or promotion report URL/path when using a profile (e.g., "9.2.0" or "https://buildkite.../promotion-report.html")</param>
	/// <param name="all">Include all changelogs in the directory. Only one filter option can be specified: `--all`, `--input-products`, `--prs`, or `--issues`.</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="directory">Optional: Directory containing changelog YAML files. Uses config bundle.directory or defaults to current directory</param>
	/// <param name="hideFeatures">Filter by feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs. Can be specified multiple times. Entries with matching feature-id values will be commented out when the bundle is rendered (by CLI render or {changelog} directive).</param>
	/// <param name="inputProducts">Filter by products in format "product target lifecycle, ..." (e.g., "cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"). When specified, all three parts (product, target, lifecycle) are required but can be wildcards (*). Examples: "elasticsearch * *" matches all elasticsearch changelogs, "cloud-serverless 2025-12-02 *" matches cloud-serverless 2025-12-02 with any lifecycle, "* 9.3.* *" matches any product with target starting with "9.3.", "* * *" matches all changelogs (equivalent to --all). Only one filter option can be specified: `--all`, `--input-products`, `--prs`, or `--issues`.</param>
	/// <param name="issues">Filter by issue URLs or numbers (comma-separated), or a path to a newline-delimited file containing issue URLs or numbers. Can be specified multiple times. Each occurrence can be either comma-separated issues (e.g., `--issues "https://github.com/owner/repo/issues/123,456"`) or a file path (e.g., `--issues /path/to/file.txt`). When specifying issues directly, provide comma-separated values. When specifying a file path, provide a single value that points to a newline-delimited file. Only one filter option can be specified: `--all`, `--input-products`, `--prs`, or `--issues`.</param>
	/// <param name="output">Optional: Output path for the bundled changelog. Can be either (1) a directory path, in which case 'changelog-bundle.yaml' is created in that directory, or (2) a file path ending in .yml or .yaml. Uses config bundle.output_directory or defaults to 'changelog-bundle.yaml' in the input directory</param>
	/// <param name="outputProducts">Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...". Overrides any values from changelogs.</param>
	/// <param name="owner">GitHub repository owner (required when PRs or issues are specified as numbers)</param>
	/// <param name="prs">Filter by pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. Only one filter option can be specified: `--all`, `--input-products`, `--prs`, or `--issues`.</param>
	/// <param name="repo">GitHub repository name. Used for PR or issue filtering when PRs or issues are specified as numbers, and also sets the repo field in the bundle output for generating correct PR/issue links. If not specified, the product ID is used as the repo name in links.</param>
	/// <param name="resolve">Optional: Copy the contents of each changelog file into the entries array. Uses config bundle.resolve or defaults to false.</param>
	/// <param name="noResolve">Optional: Explicitly turn off resolve (overrides config).</param>
	/// <param name="ctx"></param>
	[Command("bundle")]
	public async Task<int> Bundle(
		[Argument] string? profile = null,
		[Argument] string? profileArg = null,
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
		string? repo = null,
		bool? resolve = null,
		bool noResolve = false,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogBundlingService(logFactory, configurationContext);

		var isProfileMode = !string.IsNullOrWhiteSpace(profile);

		// Process each --prs occurrence
		var allPrs = new List<string>();
		if (prs is { Length: > 0 })
		{
			foreach (var prsValue in prs.Where(p => !string.IsNullOrWhiteSpace(p)))
			{
				// Check if it contains commas - if so, split and add each as a PR
				if (prsValue.Contains(','))
				{
					var commaSeparatedPrs = prsValue
						.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
						.Where(p => !string.IsNullOrWhiteSpace(p));
					allPrs.AddRange(commaSeparatedPrs);
				}
				else
				{
					// Single value - pass as-is (will be handled by service layer as file path or PR)
					allPrs.Add(prsValue);
				}
			}
		}

		// Process each --issues occurrence
		var allIssues = new List<string>();
		if (issues is { Length: > 0 })
		{
			foreach (var issuesValue in issues.Where(p => !string.IsNullOrWhiteSpace(p)))
			{
				if (issuesValue.Contains(','))
				{
					var commaSeparated = issuesValue
						.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
						.Where(p => !string.IsNullOrWhiteSpace(p));
					allIssues.AddRange(commaSeparated);
				}
				else
				{
					allIssues.Add(issuesValue);
				}
			}
		}

		// In raw mode (no profile), validate filter options
		if (!isProfileMode)
		{
			var specifiedFilters = new List<string>();
			if (all)
				specifiedFilters.Add("--all");
			if (inputProducts != null && inputProducts.Count > 0)
				specifiedFilters.Add("--input-products");
			if (allPrs.Count > 0)
				specifiedFilters.Add("--prs");
			if (allIssues.Count > 0)
				specifiedFilters.Add("--issues");

			if (specifiedFilters.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, --prs, --issues, or use a profile (e.g., 'bundle elasticsearch-release 9.2.0')");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}

			if (specifiedFilters.Count > 1)
			{
				collector.EmitError(string.Empty, $"Multiple filter options cannot be specified together. You specified: {string.Join(", ", specifiedFilters)}. Please use only one filter option: --all, --input-products, --prs, or --issues");
				_ = collector.StartAsync(ctx);
				await collector.WaitForDrain();
				await collector.StopAsync(ctx);
				return 1;
			}
		}
		else
		{
			if (all || (inputProducts != null && inputProducts.Count > 0) || allPrs.Count > 0 || allIssues.Count > 0)
			{
				collector.EmitError(string.Empty, "When using a profile, do not specify --all, --input-products, --prs, or --issues. The profile configuration determines the filter.");
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
				processedOutput = Path.Combine(output, "changelog-bundle.yaml");
			}
		}

		// Determine resolve: CLI --no-resolve takes precedence, then CLI --resolve, then config default
		var shouldResolve = noResolve ? false : resolve;

		// Process each --hide-features occurrence: each can be comma-separated feature IDs or a file path
		var allFeatureIdsForBundle = new List<string>();
		if (hideFeatures is { Length: > 0 })
		{
			foreach (var hideFeaturesValue in hideFeatures.Where(v => !string.IsNullOrWhiteSpace(v)))
			{
				// Check if it contains commas - if so, split and add each as a feature ID
				if (hideFeaturesValue.Contains(','))
				{
					var commaSeparatedFeatureIds = hideFeaturesValue
						.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
						.Where(f => !string.IsNullOrWhiteSpace(f));
					allFeatureIdsForBundle.AddRange(commaSeparatedFeatureIds);
				}
				else
				{
					// Single value - pass as-is (will be handled by service layer as file path or feature ID)
					allFeatureIdsForBundle.Add(hideFeaturesValue);
				}
			}
		}

		var input = new BundleChangelogsArguments
		{
			Directory = directory ?? Directory.GetCurrentDirectory(),
			Output = processedOutput,
			All = all,
			InputProducts = inputProducts,
			OutputProducts = outputProducts,
			Resolve = shouldResolve ?? false,
			Prs = allPrs.Count > 0 ? allPrs.ToArray() : null,
			Issues = allIssues.Count > 0 ? allIssues.ToArray() : null,
			Owner = owner,
			Repo = repo,
			Profile = profile,
			ProfileArgument = profileArg,
			Config = config,
			HideFeatures = allFeatureIdsForBundle.Count > 0 ? allFeatureIdsForBundle.ToArray() : null
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.BundleChangelogs(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Render bundled changelog(s) to markdown or asciidoc files
	/// </summary>
	/// <param name="input">Required: Bundle input(s) in format "bundle-file-path|changelog-file-path|repo|link-visibility" (use pipe as delimiter). To merge multiple bundles, separate them with commas. Only bundle-file-path is required. link-visibility can be "hide-links" or "keep-links" (default). Paths support tilde (~) expansion and relative paths.</param>
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

		// Process each --hide-features occurrence: each can be comma-separated feature IDs or a file path
		var allFeatureIds = new List<string>();
		if (hideFeatures is { Length: > 0 })
		{
			foreach (var hideFeaturesValue in hideFeatures.Where(v => !string.IsNullOrWhiteSpace(v)))
			{
				// Check if it contains commas - if so, split and add each as a feature ID
				if (hideFeaturesValue.Contains(','))
				{
					var commaSeparatedFeatureIds = hideFeaturesValue
						.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
						.Where(f => !string.IsNullOrWhiteSpace(f));
					allFeatureIds.AddRange(commaSeparatedFeatureIds);
				}
				else
				{
					// Single value - pass as-is (will be handled by service layer as file path or feature ID)
					allFeatureIds.Add(hideFeaturesValue);
				}
			}
		}

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
	/// <param name="output">Optional: Output directory for changelog files. Defaults to './changelogs'</param>
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

		IGitHubReleaseService releaseService = new GitHubReleaseService(logFactory);
		IGitHubPrService prService = new GitHubPrService(logFactory);
		var service = new GitHubReleaseChangelogService(logFactory, configurationContext, releaseService, prService);

		var input = new CreateChangelogsFromReleaseArguments
		{
			Repository = repo,
			Version = version,
			Config = config,
			Output = output,
			StripTitlePrefix = stripTitlePrefix,
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

		var service = new ChangelogBundleAmendService(logFactory);

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

		// Process and normalize all add file paths (handles comma-separated values)
		var normalizedAddFiles = new List<string>();
		foreach (var addValue in add.Where(a => !string.IsNullOrWhiteSpace(a)))
		{
			// Check if it contains commas - if so, split and normalize each path
			if (addValue.Contains(','))
			{
				var commaSeparatedPaths = addValue
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Where(p => !string.IsNullOrWhiteSpace(p))
					.Select(NormalizePath);
				normalizedAddFiles.AddRange(commaSeparatedPaths);
			}
			else
			{
				// Single path - normalize it
				normalizedAddFiles.Add(NormalizePath(addValue));
			}
		}

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

	[GeneratedRegex(@"^( *directory:\s*).+$", RegexOptions.Multiline)]
	private static partial Regex MyRegex();
}

