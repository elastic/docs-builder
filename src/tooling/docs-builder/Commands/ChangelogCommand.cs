// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Linq;
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

internal sealed class ChangelogCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	private readonly IFileSystem _fileSystem = new FileSystem();
	/// <summary>
	/// Changelog commands. Use 'changelog add' to create a new changelog or 'changelog bundle' to create a consolidated list of changelogs.
	/// </summary>
	[Command("")]
	public Task<int> Default()
	{
		collector.EmitError(string.Empty, "Please specify a subcommand. Available subcommands:\n  - 'changelog add': Create a new changelog from command-line input\n  - 'changelog bundle': Create a consolidated list of changelog files\n  - 'changelog render': Render a bundled changelog to markdown or asciidoc files\n  - 'changelog gh-release': Create changelogs from a GitHub release\n\nRun 'changelog add --help', 'changelog bundle --help', 'changelog render --help', or 'changelog gh-release --help' for usage information.");
		return Task.FromResult(1);
	}

	/// <summary>
	/// Add a new changelog from command-line input
	/// </summary>
	/// <param name="products">Required: Products affected in format "product target lifecycle, ..." (e.g., "elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05")</param>
	/// <param name="action">Optional: What users must do to mitigate</param>
	/// <param name="areas">Optional: Area(s) affected (comma-separated or specify multiple times)</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="description">Optional: Additional information about the change (max 600 characters)</param>
	/// <param name="extractReleaseNotes">Optional: When used with --prs, extract release notes from PR descriptions. Short release notes (≤120 characters, single line) are used as the title, long release notes (>120 characters or multi-line) are used as the description. Looks for content in formats like "Release Notes: ...", "Release-Notes: ...", "## Release Note", etc.</param>
	/// <param name="featureId">Optional: Feature flag ID</param>
	/// <param name="highlight">Optional: Include in release highlights</param>
	/// <param name="impact">Optional: How the user's environment is affected</param>
	/// <param name="issues">Optional: Issue URL(s) (comma-separated or specify multiple times)</param>
	/// <param name="owner">Optional: GitHub repository owner (used when --prs contains just numbers)</param>
	/// <param name="output">Optional: Output directory for the changelog. Defaults to current directory</param>
	/// <param name="prs">Optional: Pull request URL(s) or PR number(s) (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. Each occurrence can be either comma-separated PRs (e.g., `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (e.g., `--prs /path/to/file.txt`). When specifying PRs directly, provide comma-separated values. When specifying a file path, provide a single value that points to a newline-delimited file. If --owner and --repo are provided, PR numbers can be used instead of URLs. If specified, --title can be derived from the PR. If mappings are configured, --areas and --type can also be derived from the PR. Creates one changelog file per PR.</param>
	/// <param name="repo">Optional: GitHub repository name (used when --prs contains just numbers)</param>
	/// <param name="stripTitlePrefix">Optional: When used with --prs, remove square brackets and text within them from the beginning of PR titles, and also remove a colon if it follows the closing bracket (e.g., "[Inference API] Title" becomes "Title", "[ES|QL]: Title" becomes "Title")</param>
	/// <param name="subtype">Optional: Subtype for breaking changes (api, behavioral, configuration, etc.)</param>
	/// <param name="title">Optional: A short, user-facing title (max 80 characters). Required if --pr is not specified. If --pr and --title are specified, the latter value is used instead of what exists in the PR.</param>
	/// <param name="type">Optional: Type of change (feature, enhancement, bug-fix, breaking-change, etc.). Required if --pr is not specified. If mappings are configured, type can be derived from the PR.</param>
	/// <param name="usePrNumber">Optional: Use the PR number as the filename instead of generating it from a unique ID and title</param>
	/// <param name="ctx"></param>
	[Command("add")]
	public async Task<int> Create(
		[ProductInfoParser] List<ProductInfo> products,
		string? action = null,
		string[]? areas = null,
		string? config = null,
		string? description = null,
		bool extractReleaseNotes = false,
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
				// Check if this is a file path
				if (_fileSystem.File.Exists(trimmedValue))
				{
					// Read all lines from the file (newline-delimited)
					try
					{
						var fileLines = await _fileSystem.File.ReadAllLinesAsync(trimmedValue, ctx);
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
						collector.EmitError(string.Empty, $"Failed to read PRs from file '{trimmedValue}': {ex.Message}", ex);
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

		var input = new ChangelogInput
		{
			Title = title,
			Type = type,
			Products = products,
			Subtype = subtype,
			Areas = areas ?? [],
			Prs = parsedPrs,
			Owner = owner,
			Repo = repo,
			Issues = issues ?? [],
			Description = description,
			Impact = impact,
			Action = action,
			FeatureId = featureId,
			Highlight = highlight,
			Output = output,
			Config = config,
			UsePrNumber = usePrNumber,
			StripTitlePrefix = stripTitlePrefix,
			ExtractReleaseNotes = extractReleaseNotes
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.CreateChangelog(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Bundle changelog files
	/// </summary>
	/// <param name="all">Include all changelogs in the directory. Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.</param>
	/// <param name="directory">Optional: Directory containing changelog YAML files. Defaults to current directory</param>
	/// <param name="inputProducts">Filter by products in format "product target lifecycle, ..." (e.g., "cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"). When specified, all three parts (product, target, lifecycle) are required but can be wildcards (*). Examples: "elasticsearch * *" matches all elasticsearch changelogs, "cloud-serverless 2025-12-02 *" matches cloud-serverless 2025-12-02 with any lifecycle, "* 9.3.* *" matches any product with target starting with "9.3.", "* * *" matches all changelogs (equivalent to --all). Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.</param>
	/// <param name="output">Optional: Output path for the bundled changelog. Can be either (1) a directory path, in which case 'changelog-bundle.yaml' is created in that directory, or (2) a file path ending in .yml or .yaml. Defaults to 'changelog-bundle.yaml' in the input directory</param>
	/// <param name="outputProducts">Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...". Overrides any values from changelogs.</param>
	/// <param name="owner">GitHub repository owner (required only when PRs are specified as numbers)</param>
	/// <param name="prs">Filter by pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.</param>
	/// <param name="repo">GitHub repository name (required only when PRs are specified as numbers)</param>
	/// <param name="resolve">Optional: Copy the contents of each changelog file into the entries array. By default, the bundle contains only the file names and checksums.</param>
	/// <param name="ctx"></param>
	[Command("bundle")]
	public async Task<int> Bundle(
		bool all = false,
		string? directory = null,
		[ProductInfoParser] List<ProductInfo>? inputProducts = null,
		string? output = null,
		[ProductInfoParser] List<ProductInfo>? outputProducts = null,
		string? owner = null,
		string[]? prs = null,
		string? repo = null,
		bool resolve = false,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogBundlingService(logFactory);

		// Process each --prs occurrence: each can be comma-separated PRs or a file path
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

		// Validate filter options - at least one must be specified
		var specifiedFilters = new List<string>();
		if (all)
			specifiedFilters.Add("--all");
		if (inputProducts != null && inputProducts.Count > 0)
			specifiedFilters.Add("--input-products");
		if (allPrs.Count > 0)
			specifiedFilters.Add("--prs");

		if (specifiedFilters.Count == 0)
		{
			collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, or --prs");
			_ = collector.StartAsync(ctx);
			await collector.WaitForDrain();
			await collector.StopAsync(ctx);
			return 1;
		}

		if (specifiedFilters.Count > 1)
		{
			collector.EmitError(string.Empty, $"Multiple filter options cannot be specified together. You specified: {string.Join(", ", specifiedFilters)}. Please use only one filter option: --all, --input-products, or --prs");
			_ = collector.StartAsync(ctx);
			await collector.WaitForDrain();
			await collector.StopAsync(ctx);
			return 1;
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

		var input = new ChangelogBundleInput
		{
			Directory = directory ?? Directory.GetCurrentDirectory(),
			Output = processedOutput,
			All = all,
			InputProducts = inputProducts,
			OutputProducts = outputProducts,
			Resolve = resolve,
			Prs = allPrs.Count > 0 ? allPrs.ToArray() : null,
			Owner = owner,
			Repo = repo
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.BundleChangelogs(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Render bundled changelog(s) to markdown or asciidoc files
	/// </summary>
	/// <param name="input">Required: Bundle input(s) in format "bundle-file-path|changelog-file-path|repo|link-visibility" (use pipe as delimiter). To merge multiple bundles, separate them with commas. Only bundle-file-path is required. link-visibility can be "hide-links" or "keep-links" (default). Paths must be absolute or use environment variables; tilde (~) expansion is not supported.</param>
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

		var renderInput = new ChangelogRenderInput
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

		var input = new GitHubReleaseInput
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
}

