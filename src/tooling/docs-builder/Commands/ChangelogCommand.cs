// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using ConsoleAppFramework;
using Documentation.Builder.Arguments;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Elastic.Documentation.Services.Changelog;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed class ChangelogCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>
	/// Changelog commands. Use 'changelog add' to create a new changelog or 'changelog bundle' to create a consolidated list of changelogs.
	/// </summary>
	[Command("")]
	public Task<int> Default()
	{
		collector.EmitError(string.Empty, "Please specify a subcommand. Available subcommands:\n  - 'changelog add': Create a new changelog from command-line input\n  - 'changelog bundle': Create a consolidated list of changelog files\n  - 'changelog render': Render a bundled changelog to markdown files\n\nRun 'changelog add --help', 'changelog bundle --help', or 'changelog render --help' for usage information.");
		return Task.FromResult(1);
	}

	/// <summary>
	/// Add a new changelog from command-line input
	/// </summary>
	/// <param name="title">Optional: A short, user-facing title (max 80 characters). Required if --pr is not specified. If --pr and --title are specified, the latter value is used instead of what exists in the PR.</param>
	/// <param name="type">Optional: Type of change (feature, enhancement, bug-fix, breaking-change, etc.). Required if --pr is not specified. If mappings are configured, type can be derived from the PR.</param>
	/// <param name="products">Required: Products affected in format "product target lifecycle, ..." (e.g., "elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05")</param>
	/// <param name="subtype">Optional: Subtype for breaking changes (api, behavioral, configuration, etc.)</param>
	/// <param name="areas">Optional: Area(s) affected (comma-separated or specify multiple times)</param>
	/// <param name="pr">Optional: Pull request URL or PR number (if --owner and --repo are provided). If specified, --title can be derived from the PR. If mappings are configured, --areas and --type can also be derived from the PR.</param>
	/// <param name="owner">Optional: GitHub repository owner (used when --pr is just a number)</param>
	/// <param name="repo">Optional: GitHub repository name (used when --pr is just a number)</param>
	/// <param name="issues">Optional: Issue URL(s) (comma-separated or specify multiple times)</param>
	/// <param name="description">Optional: Additional information about the change (max 600 characters)</param>
	/// <param name="impact">Optional: How the user's environment is affected</param>
	/// <param name="action">Optional: What users must do to mitigate</param>
	/// <param name="featureId">Optional: Feature flag ID</param>
	/// <param name="highlight">Optional: Include in release highlights</param>
	/// <param name="output">Optional: Output directory for the changelog. Defaults to current directory</param>
	/// <param name="config">Optional: Path to the changelog.yml configuration file. Defaults to 'docs/changelog.yml'</param>
	/// <param name="ctx"></param>
	[Command("add")]
	public async Task<int> Create(
		[ProductInfoParser] List<ProductInfo> products,
		string? title = null,
		string? type = null,
		string? subtype = null,
		string[]? areas = null,
		string? pr = null,
		string? owner = null,
		string? repo = null,
		string[]? issues = null,
		string? description = null,
		string? impact = null,
		string? action = null,
		string? featureId = null,
		bool? highlight = null,
		string? output = null,
		string? config = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		IGitHubPrService githubPrService = new GitHubPrService(logFactory);
		var service = new ChangelogService(logFactory, configurationContext, githubPrService);

		var input = new ChangelogInput
		{
			Title = title,
			Type = type,
			Products = products,
			Subtype = subtype,
			Areas = areas ?? [],
			Pr = pr,
			Owner = owner,
			Repo = repo,
			Issues = issues ?? [],
			Description = description,
			Impact = impact,
			Action = action,
			FeatureId = featureId,
			Highlight = highlight,
			Output = output,
			Config = config
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.CreateChangelog(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Bundle changelog files
	/// </summary>
	/// <param name="directory">Optional: Directory containing changelog YAML files. Defaults to current directory</param>
	/// <param name="output">Optional: Output file path for the bundled changelog. Defaults to 'changelog-bundle.yaml' in the input directory</param>
	/// <param name="all">Include all changelogs in the directory. Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.</param>
	/// <param name="inputProducts">Filter by products in format "product target lifecycle, ..." (e.g., "cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"). When specified, all three parts (product, target, lifecycle) are required but can be wildcards (*). Examples: "elasticsearch * *" matches all elasticsearch changelogs, "cloud-serverless 2025-12-02 *" matches cloud-serverless 2025-12-02 with any lifecycle, "* 9.3.* *" matches any product with target starting with "9.3.", "* * *" matches all changelogs (equivalent to --all). Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.</param>
	/// <param name="outputProducts">Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...". Overrides any values from changelogs.</param>
	/// <param name="resolve">Optional: Copy the contents of each changelog file into the entries array. By default, the bundle contains only the file names and checksums.</param>
	/// <param name="prs">Filter by pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times. Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.</param>
	/// <param name="owner">GitHub repository owner (required only when PRs are specified as numbers)</param>
	/// <param name="repo">GitHub repository name (required only when PRs are specified as numbers)</param>
	/// <param name="ctx"></param>
	[Command("bundle")]
	public async Task<int> Bundle(
		string? directory = null,
		string? output = null,
		bool all = false,
		[ProductInfoParser] List<ProductInfo>? inputProducts = null,
		[ProductInfoParser] List<ProductInfo>? outputProducts = null,
		bool resolve = false,
		string[]? prs = null,
		string? owner = null,
		string? repo = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogService(logFactory, configurationContext, null);

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
		var filterCount = 0;
		if (all)
			filterCount++;
		if (inputProducts != null && inputProducts.Count > 0)
			filterCount++;
		if (allPrs.Count > 0)
			filterCount++;

		if (filterCount == 0)
		{
			collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, or --prs");
			return 1;
		}

		if (filterCount > 1)
		{
			collector.EmitError(string.Empty, "Only one filter option can be specified at a time: --all, --input-products, or --prs");
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
					return 1;
				}

				// When --input-products is used, target and lifecycle are required (but can be "*")
				// If they're null, it means they weren't provided in the input
				if (product.Target == null)
				{
					collector.EmitError(string.Empty, $"--input-products: target is required for product '{product.Product}' (use '*' for wildcard)");
					return 1;
				}

				if (product.Lifecycle == null)
				{
					collector.EmitError(string.Empty, $"--input-products: lifecycle is required for product '{product.Product}' (use '*' for wildcard)");
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

		var input = new ChangelogBundleInput
		{
			Directory = directory ?? Directory.GetCurrentDirectory(),
			Output = output,
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
	/// Render bundled changelog(s) to markdown files
	/// </summary>
	/// <param name="input">Required: Bundle input(s) in format "bundle-file-path, changelog-file-path, repo". Can be specified multiple times. Only bundle-file-path is required.</param>
	/// <param name="output">Optional: Output directory for rendered markdown files. Defaults to current directory</param>
	/// <param name="title">Optional: Title to use for section headers in output markdown files. Defaults to version from first bundle</param>
	/// <param name="subsections">Optional: Group entries by area/component in subsections. For breaking changes with a subtype, groups by subtype instead of area. Defaults to false</param>
	/// <param name="hidePrivateLinks">Optional: Hide private links by commenting them out in the markdown output. Defaults to false</param>
	/// <param name="hideFeatures">Filter by feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs. Can be specified multiple times. Entries with matching feature-id values will be commented out in the markdown output.</param>
	/// <param name="ctx"></param>
	[Command("render")]
	public async Task<int> Render(
		[BundleInputParser] List<BundleInput> input,
		string? output = null,
		string? title = null,
		bool subsections = false,
		bool hidePrivateLinks = false,
		string[]? hideFeatures = null,
		string? config = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogService(logFactory, configurationContext, null);

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

		var renderInput = new ChangelogRenderInput
		{
			Bundles = input ?? [],
			Output = output,
			Title = title,
			Subsections = subsections,
			HidePrivateLinks = hidePrivateLinks,
			HideFeatures = allFeatureIds.Count > 0 ? allFeatureIds.ToArray() : null,
			Config = config
		};

		serviceInvoker.AddCommand(service, renderInput,
			async static (s, collector, state, ctx) => await s.RenderChangelogs(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}
}

