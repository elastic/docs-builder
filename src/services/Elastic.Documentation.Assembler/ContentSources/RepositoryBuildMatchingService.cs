// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Actions.Core.Services;
using Actions.Core.Summaries;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Services;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.ContentSources;

public class RepositoryBuildMatchingService(
	ILoggerFactory logFactory,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	CheckoutsFileSystem fileSystem
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<RepositoryBuildMatchingService>();

	private async Task<LinkRegistry> GetRegistryWithRetry(Aws3LinkIndexReader provider, Cancel ctx)
	{
		const int maxAttempts = 3;
		for (var attempt = 1; attempt <= maxAttempts; attempt++)
		{
			try
			{
				return await provider.GetRegistry(ctx);
			}
			catch (Exception ex) when (attempt < maxAttempts)
			{
				var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
				_logger.LogWarning("S3 link registry fetch failed (attempt {Attempt}/{Max}), retrying in {Delay}s: {Message}",
					attempt, maxAttempts, delay.TotalSeconds, ex.Message);
				await Task.Delay(delay, ctx);
			}
		}
		return await provider.GetRegistry(ctx);
	}

	//TODO return contentsourcematch
	/// <summary>
	/// Validates whether the <paramref name="branchOrTag"/> on <paramref name="repository"/> should be build and therefor published.
	/// <para>Will also qualify the branch as being current or next or whether we should build this speculatively</para>
	/// <para>e.g., if a new minor branch gets created, we want to build it even if it's not configured in assembler.yml yet</para>
	/// </summary>
	public async Task<bool> ShouldBuild(IDiagnosticsCollector collector, string? repository, string? branchOrTag, Cancel ctx)
	{
		var repo = repository ?? githubActionsService.GetInput("repository");
		var refName = branchOrTag ?? githubActionsService.GetInput("ref_name");
		_logger.LogInformation(" Validating '{Repository}' '{BranchOrTag}' ", repo, refName);

		if (string.IsNullOrEmpty(repo))
			throw new ArgumentNullException(nameof(repository));
		if (string.IsNullOrEmpty(refName))
			throw new ArgumentNullException(nameof(branchOrTag));

		// the link registry uses short repository names (e.g. "kibana"), not full names (e.g. "elastic/kibana")
		var repoTokens = repo.Split('/');
		var repositoryName = repoTokens.Last();

		// environment does not matter to check the configuration, defaulting to dev
		var linkIndexProvider = Aws3LinkIndexReader.CreateAnonymous();
		var linkRegistry = await GetRegistryWithRetry(linkIndexProvider, ctx);
		var alreadyPublishing = linkRegistry.Repositories.ContainsKey(repositoryName);
		_logger.LogInformation("'{Repository}' (registry key: '{RepositoryName}') publishing to link registry: {PublishState} ", repo, repositoryName, alreadyPublishing);
		var assembleContext = new AssembleContext(configuration, configurationContext, "dev", collector, fileSystem);
		var product = assembleContext.ProductsConfiguration.GetProductByRepositoryName(repo);
		var matches = assembleContext.Configuration.Match(logFactory, repo, refName, product, alreadyPublishing);
		var facts = CreateBuildMatchFacts(new BuildMatchInput
		{
			Configuration = assembleContext.Configuration,
			Repository = repo,
			BranchOrTag = refName,
			RegistryKey = repositoryName,
			Product = product,
			AlreadyPublishing = alreadyPublishing,
			Matches = matches
		});
		LogMatchFacts(facts);
		await WriteGitHubStepSummary(facts);

		if (!facts.ShouldBuild)
		{
			_logger.LogInformation("'{Repository}' '{BranchOrTag}' combination not found in configuration.", repo, refName);
			await githubActionsService.SetOutputAsync("content-source-match", "false");
			await githubActionsService.SetOutputAsync("content-source-next", "false");
			await githubActionsService.SetOutputAsync("content-source-edge", "false");
			await githubActionsService.SetOutputAsync("content-source-current", "false");
			await githubActionsService.SetOutputAsync("content-source-speculative", "false");
			return false;
		}

		if (matches.Current is { } current)
			_logger.LogInformation("'{Repository}' '{BranchOrTag}' is configured as '{Matches}' content-source", repo, refName, current.ToStringFast(true));
		if (matches.Next is { } next)
			_logger.LogInformation("'{Repository}' '{BranchOrTag}' is configured as '{Matches}' content-source", repo, refName, next.ToStringFast(true));
		if (matches.Edge is { } edge)
			_logger.LogInformation("'{Repository}' '{BranchOrTag}' is configured as '{Matches}' content-source", repo, refName, edge.ToStringFast(true));

		await githubActionsService.SetOutputAsync("content-source-match", "true");
		await githubActionsService.SetOutputAsync("content-source-next", matches.Next is not null ? "true" : "false");
		await githubActionsService.SetOutputAsync("content-source-current", matches.Current is not null ? "true" : "false");
		await githubActionsService.SetOutputAsync("content-source-edge", matches.Edge is not null ? "true" : "false");
		await githubActionsService.SetOutputAsync("content-source-speculative", matches.Speculative ? "true" : "false");
		return true;
	}

	internal static BuildMatchFacts CreateBuildMatchFacts(BuildMatchInput input)
	{
		_ = input.Configuration.AvailableRepositories.TryGetValue(input.RegistryKey, out var configuredRepository);
		var matchedContentSources = MatchedContentSources(input.Matches);
		var reason = DetermineReason(input, configuredRepository, matchedContentSources);
		return new BuildMatchFacts
		{
			Repository = input.Repository,
			BranchOrTag = input.BranchOrTag,
			RegistryKey = input.RegistryKey,
			PublishingToLinkRegistry = input.AlreadyPublishing,
			ShouldBuild = input.Matches is not { Current: null, Next: null, Edge: null, Speculative: false },
			Reason = reason,
			MatchedContentSources = FormatList(matchedContentSources),
			Speculative = input.Matches.Speculative,
			RepositoryConfigured = configuredRepository is not null,
			ConfiguredCurrent = configuredRepository?.GetBranch(ContentSource.Current),
			ConfiguredNext = configuredRepository?.GetBranch(ContentSource.Next),
			ConfiguredEdge = configuredRepository?.GetBranch(ContentSource.Edge),
			Product = input.Product?.Id,
			ProductCurrent = input.Product?.VersioningSystem?.Current.ToString()
		};
	}

	private void LogMatchFacts(BuildMatchFacts facts) => _logger.LogInformation(
			"Content-source match summary for '{Repository}' '{BranchOrTag}': should_build={ShouldBuild}; reason={Reason}; matched_sources={MatchedSources}; speculative={Speculative}; registry_key={RegistryKey}; publishing_to_link_registry={Publishing}; repository_configured={RepositoryConfigured}; configured_current={Current}; configured_next={Next}; configured_edge={Edge}; product={Product}; product_current={ProductCurrent}",
			facts.Repository,
			facts.BranchOrTag,
			facts.ShouldBuild,
			facts.Reason,
			facts.MatchedContentSources,
			facts.Speculative,
			facts.RegistryKey,
			facts.PublishingToLinkRegistry,
			facts.RepositoryConfigured,
			FormatOptional(facts.ConfiguredCurrent),
			FormatOptional(facts.ConfiguredNext),
			FormatOptional(facts.ConfiguredEdge),
			FormatOptional(facts.Product),
			FormatOptional(facts.ProductCurrent)
		);

	private async Task WriteGitHubStepSummary(BuildMatchFacts facts)
	{
		if (!Summary.IsAvailable)
			return;

		_ = githubActionsService.Summary
			.AddMarkdownHeading("Docs Builder content-source match", 3)
			.AddRawMarkdown(facts.ToMarkdown(), addNewLine: true);
		_ = await githubActionsService.Summary.WriteAsync();
	}

	private static IReadOnlyList<string> MatchedContentSources(AssemblyConfiguration.ContentSourceMatch matches)
	{
		var matchedContentSources = new List<string>();
		if (matches.Current is not null)
			matchedContentSources.Add(ContentSource.Current.ToStringFast(true));
		if (matches.Next is not null)
			matchedContentSources.Add(ContentSource.Next.ToStringFast(true));
		if (matches.Edge is not null)
			matchedContentSources.Add(ContentSource.Edge.ToStringFast(true));
		return matchedContentSources;
	}

	private static string DetermineReason(
		BuildMatchInput input,
		Repository? configuredRepository,
		IReadOnlyList<string> matchedContentSources)
	{
		var tokens = input.Repository.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var owner = tokens.FirstOrDefault();

		if (matchedContentSources.Count > 0)
			return $"ref matches configured content-source(s): {FormatList(matchedContentSources)}";

		if (tokens.Length < 2 || owner != "elastic")
			return $"repository is not a valid elastic repository (owner: {FormatOptional(owner)})";

		if (configuredRepository is null)
		{
			return input.Matches.Speculative
				? "repository is not in assembler.yml, but ref is main, master, or a version branch"
				: "repository is not in assembler.yml and ref is not main, master, or a version branch";
		}

		return input.Matches.Speculative
			? DetermineSpeculativeReason(input, configuredRepository)
			: DetermineSkipReason(input, configuredRepository);
	}

	private static string DetermineSpeculativeReason(BuildMatchInput input, Repository configuredRepository)
	{
		if (input.BranchOrTag is "main" or "master")
			return "ref is main or master and did not match configured content-source refs, so it builds speculatively";

		if (TryParseVersionBranch(input.BranchOrTag, out var version))
		{
			var current = configuredRepository.GetBranch(ContentSource.Current);
			if (SemVersion.TryParse(current + ".0", out var currentVersion))
			{
				var previousCurrentVersion = PreviousMinor(currentVersion);
				if (version >= currentVersion)
					return $"version branch '{input.BranchOrTag}' is greater than or equal to configured current '{currentVersion}'";
				if (version == previousCurrentVersion)
					return $"version branch '{input.BranchOrTag}' is the previous minor of configured current '{currentVersion}'";
			}
			else if (input.Product?.VersioningSystem is { } versioningSystem && !input.AlreadyPublishing)
			{
				var productVersion = versioningSystem.Current;
				var anchoredProductVersion = AnchorToMinor(productVersion);
				if (version > anchoredProductVersion)
					return $"repository is not in the link registry yet and version branch '{input.BranchOrTag}' is greater than product current '{productVersion}'";
			}
		}

		return "speculative content-source rule matched";
	}

	private static string DetermineSkipReason(BuildMatchInput input, Repository configuredRepository)
	{
		if (TryParseVersionBranch(input.BranchOrTag, out var version))
		{
			var current = configuredRepository.GetBranch(ContentSource.Current);
			if (SemVersion.TryParse(current + ".0", out var currentVersion))
			{
				var previousCurrentVersion = PreviousMinor(currentVersion);
				return $"version branch '{input.BranchOrTag}' is older than configured current '{currentVersion}' and is not previous minor '{previousCurrentVersion}'";
			}

			if (input.Product?.VersioningSystem is not { } versioningSystem)
				return $"configured current is not versioned and no product versioning system is available for '{input.RegistryKey}'";

			if (input.AlreadyPublishing)
				return "configured current is not versioned and repository already publishes to the link registry, so version branches are not built speculatively";

			var productVersion = versioningSystem.Current;
			var anchoredProductVersion = AnchorToMinor(productVersion);
			if (version <= anchoredProductVersion)
				return $"version branch '{input.BranchOrTag}' is not greater than product current '{productVersion}'";
		}

		var currentRef = configuredRepository.GetBranch(ContentSource.Current);
		var nextRef = configuredRepository.GetBranch(ContentSource.Next);
		var edgeRef = configuredRepository.GetBranch(ContentSource.Edge);
		return $"ref does not match configured content-source refs (current: {currentRef}, next: {nextRef}, edge: {edgeRef})";
	}

	private static bool TryParseVersionBranch(string branchOrTag, [NotNullWhen(true)] out SemVersion? version)
	{
		version = null;
		return ContentSourceRegex.MatchVersionBranch().IsMatch(branchOrTag) && SemVersion.TryParse(branchOrTag + ".0", out version);
	}

	private static SemVersion PreviousMinor(SemVersion currentVersion) =>
		new(currentVersion.Major, Math.Max(currentVersion.Minor - 1, 0), 0);

	private static SemVersion AnchorToMinor(SemVersion productVersion) =>
		new(productVersion.Major, productVersion.Minor, 0);

	private static string FormatList(IReadOnlyList<string> values) =>
		values.Count == 0 ? "<none>" : string.Join(", ", values);

	private static string FormatOptional(string? value) =>
		string.IsNullOrWhiteSpace(value) ? "<none>" : value;

	internal sealed record BuildMatchInput
	{
		public required AssemblyConfiguration Configuration { get; init; }
		public required string Repository { get; init; }
		public required string BranchOrTag { get; init; }
		public required string RegistryKey { get; init; }
		public required Product? Product { get; init; }
		public required bool AlreadyPublishing { get; init; }
		public required AssemblyConfiguration.ContentSourceMatch Matches { get; init; }
	}

	internal sealed record BuildMatchFacts
	{
		public required string Repository { get; init; }
		public required string BranchOrTag { get; init; }
		public required string RegistryKey { get; init; }
		public required bool PublishingToLinkRegistry { get; init; }
		public required bool ShouldBuild { get; init; }
		public required string Reason { get; init; }
		public required string MatchedContentSources { get; init; }
		public required bool Speculative { get; init; }
		public required bool RepositoryConfigured { get; init; }
		public required string? ConfiguredCurrent { get; init; }
		public required string? ConfiguredNext { get; init; }
		public required string? ConfiguredEdge { get; init; }
		public required string? Product { get; init; }
		public required string? ProductCurrent { get; init; }

		public string ToMarkdown()
		{
			var sb = new StringBuilder();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Decision:** {(ShouldBuild ? "build" : "skip")}");
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Reason:** {Reason}");
			_ = sb.AppendLine();
			_ = sb.AppendLine("| Fact | Value |");
			_ = sb.AppendLine("|---|---|");
			AppendRow(sb, "repository", Repository);
			AppendRow(sb, "ref", BranchOrTag);
			AppendRow(sb, "registry key", RegistryKey);
			AppendRow(sb, "publishing to link registry", PublishingToLinkRegistry.ToString());
			AppendRow(sb, "repository configured", RepositoryConfigured.ToString());
			AppendRow(sb, "configured current", ConfiguredCurrent);
			AppendRow(sb, "configured next", ConfiguredNext);
			AppendRow(sb, "configured edge", ConfiguredEdge);
			AppendRow(sb, "matched content-sources", MatchedContentSources);
			AppendRow(sb, "speculative", Speculative.ToString());
			AppendRow(sb, "product", Product);
			AppendRow(sb, "product current", ProductCurrent);
			return sb.ToString();
		}

		private static void AppendRow(StringBuilder sb, string fact, string? value) =>
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"| {EscapeMarkdownTableValue(fact)} | {EscapeMarkdownTableValue(FormatOptional(value))} |");

		private static string EscapeMarkdownTableValue(string value) =>
			value.Replace("|", "\\|", StringComparison.Ordinal);
	}
}
