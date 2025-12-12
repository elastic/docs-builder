// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	/// Changelog commands. Use 'changelog add' to create a new changelog fragment.
	/// </summary>
	[Command("")]
	public Task<int> Default()
	{
		collector.EmitError(string.Empty, "Please specify a subcommand. Use 'changelog add' to create a new changelog fragment. Run 'changelog add --help' for usage information.");
		return Task.FromResult(1);
	}

	/// <summary>
	/// Add a new changelog fragment from command-line input
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
	/// <param name="output">Optional: Output directory for the changelog fragment. Defaults to current directory</param>
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
	/// Bundle changelog fragments into a single YAML file
	/// </summary>
	/// <param name="directory">Optional: Directory containing changelog YAML files. Defaults to current directory</param>
	/// <param name="output">Optional: Output file path for the bundled changelog. Defaults to 'changelog-bundle.yaml' in the input directory</param>
	/// <param name="all">Include all changelogs in the directory</param>
	/// <param name="inputProducts">Filter by products in format "product target lifecycle, ..." (e.g., "cloud-serverless 2025-12-02, cloud-serverless 2025-12-06")</param>
	/// <param name="outputProducts">Explicitly set the products array in the output file in format "product target lifecycle, ...". Overrides any values from changelogs.</param>
	/// <param name="resolve">Copy the contents of each changelog file into the entries array</param>
	/// <param name="prs">Filter by pull request URLs or numbers (can specify multiple times)</param>
	/// <param name="prsFile">Path to a newline-delimited file containing PR URLs or numbers</param>
	/// <param name="owner">Optional: GitHub repository owner (used when PRs are specified as numbers)</param>
	/// <param name="repo">Optional: GitHub repository name (used when PRs are specified as numbers)</param>
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
		string? prsFile = null,
		string? owner = null,
		string? repo = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new ChangelogService(logFactory, configurationContext, null);

		var input = new ChangelogBundleInput
		{
			Directory = directory ?? Directory.GetCurrentDirectory(),
			Output = output,
			All = all,
			InputProducts = inputProducts,
			OutputProducts = outputProducts,
			Resolve = resolve,
			Prs = prs,
			PrsFile = prsFile,
			Owner = owner,
			Repo = repo
		};

		serviceInvoker.AddCommand(service, input,
			async static (s, collector, state, ctx) => await s.BundleChangelogs(collector, state, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}
}

