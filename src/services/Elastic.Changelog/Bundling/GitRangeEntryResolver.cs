// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;
using SharpYaml;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Options for resolving the changelog entries of a commit-range bundle.
/// </summary>
public record GitRangeEntryResolutionOptions
{
	/// <summary>Authoring GitHub owner (org) the range was resolved against.</summary>
	public required string Owner { get; init; }

	/// <summary>Authoring GitHub repository the range was resolved against.</summary>
	public required string Repo { get; init; }

	/// <summary>Start ref of the range (report provenance).</summary>
	public required string StartRef { get; init; }

	/// <summary>End ref of the range (report provenance).</summary>
	public required string EndRef { get; init; }

	/// <summary>
	/// Products applied to entries synthesized from PR metadata when the PR's labels do not map
	/// to any product (typically the profile's <c>output_products</c>). Wildcards are ignored.
	/// </summary>
	public IReadOnlyList<ProductArgument>? FallbackProducts { get; init; }
}

/// <summary>How a pull request's changelog entry was sourced for a commit-range bundle.</summary>
public enum GitRangePrSourceKind
{
	/// <summary>A checked-in entry existed in the entry pool (CDN or local directory).</summary>
	Pool,

	/// <summary>Synthesized from PR metadata, including release-note text extracted from the PR body.</summary>
	InferredPrBody,

	/// <summary>Synthesized from PR metadata using the PR title only (no release-note text found).</summary>
	InferredTitle,

	/// <summary>Excluded by <c>rules.create</c> label rules.</summary>
	Excluded,

	/// <summary>Merged into a branch other than the requested base branch (for example a feature branch).</summary>
	ExcludedBaseBranch,

	/// <summary>The PR's metadata could not be fetched; no entry could be produced.</summary>
	Missing
}

/// <summary>Per-PR row of the commit-range bundle run report.</summary>
public record GitRangePrReportRow
{
	public required int Number { get; init; }
	public required string Url { get; init; }
	public required GitRangePrSourceKind Source { get; init; }

	/// <summary>Entry file name(s) backing this PR (pool file names or the synthesized name).</summary>
	public IReadOnlyList<string> EntryFileNames { get; init; } = [];
}

/// <summary>
/// Run report for a commit-range bundle: the resolved PR list with per-PR entry source, plus the
/// commits that could not be attributed to a PR. Rendered as Markdown suitable for a release PR body.
/// </summary>
public record GitRangeBundleReport
{
	public required string StartRef { get; init; }
	public required string EndRef { get; init; }
	public required int TotalCommits { get; init; }
	public required IReadOnlyList<GitRangePrReportRow> Rows { get; init; }
	public required IReadOnlyList<string> CommitsWithoutPullRequest { get; init; }

	/// <summary>Renders the report as Markdown, suitable for a release PR body or job summary.</summary>
	public string ToMarkdown()
	{
		var sb = new StringBuilder();
		_ = sb
			.AppendLine(CultureInfo.InvariantCulture, $"### Changelog bundle for `{StartRef}..{EndRef}`")
			.AppendLine()
			.AppendLine(CultureInfo.InvariantCulture, $"{TotalCommits} commit(s), {Rows.Count} pull request(s).")
			.AppendLine()
			.AppendLine("| PR | Source | Entry |")
			.AppendLine("|---|---|---|");

		foreach (var row in Rows)
		{
			var source = row.Source switch
			{
				GitRangePrSourceKind.Pool => "pool",
				GitRangePrSourceKind.InferredPrBody => "inferred (PR body)",
				GitRangePrSourceKind.InferredTitle => "inferred (title)",
				GitRangePrSourceKind.Excluded => "excluded (rules)",
				GitRangePrSourceKind.ExcludedBaseBranch => "excluded (base branch)",
				_ => "missing"
			};
			var entry = row.EntryFileNames.Count > 0 ? string.Join(", ", row.EntryFileNames.Select(f => $"`{f}`")) : "—";
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"| [#{row.Number}]({row.Url}) | {source} | {entry} |");
		}

		if (CommitsWithoutPullRequest.Count > 0)
		{
			_ = sb.AppendLine().AppendLine("Commits without an associated pull request:").AppendLine();
			foreach (var sha in CommitsWithoutPullRequest)
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"- `{sha}`");
		}

		return sb.ToString();
	}
}

/// <summary>Result of resolving commit-range entries.</summary>
public record GitRangeEntryResolutionResult
{
	public required bool Success { get; init; }
	public required IReadOnlyList<MatchedChangelogFile> Entries { get; init; }
	public required GitRangeBundleReport Report { get; init; }
}

/// <summary>
/// Resolves the changelog entries for the pull requests of a commit range, applying the RFC's
/// sourcing precedence per PR: a checked-in entry from the pool (matched by file-name-derived PR
/// numbers — file names survive scrubbing — or by the entry's <c>prs</c> references) wins over an
/// entry synthesized from PR metadata via the same extraction path <c>changelog add</c> uses
/// (release-note text from the PR body, label-mapped type/areas/products); PRs whose metadata
/// cannot be fetched are reported as missing rather than silently dropped.
/// </summary>
public class GitRangeEntryResolver(IPullRequestSummaryProvider summaries, DecisionLog decisions)
{
	/// <summary>
	/// Resolves entries for every PR in <paramref name="resolution"/> against the candidate pool
	/// <paramref name="candidates"/>. Never throws for per-PR failures; the report captures them.
	/// </summary>
	public async Task<GitRangeEntryResolutionResult> ResolveAsync(
		IDiagnosticsCollector collector,
		CommitRangeResolution resolution,
		IReadOnlyList<(string FileName, string Content)> candidates,
		ChangelogConfiguration? config,
		GitRangeEntryResolutionOptions options,
		Cancel ctx
	)
	{
		var parsedCandidates = candidates.Select(c => ParseCandidate(c.FileName, c.Content)).ToList();

		var rows = new List<GitRangePrReportRow>();
		var entries = new List<MatchedChangelogFile>();
		var includedFileNames = new HashSet<string>(StringComparer.Ordinal);
		var success = true;

		// Pull requests without a pool entry need their metadata. It is read up front, in batches, and everything after
		// that runs in range order, so the report and diagnostics stay stable.
		var needSummary = resolution
			.PullRequests
			.Where(pr => !parsedCandidates.Any(c => MatchesPr(c, pr.Number, options.Owner, options.Repo)))
			.ToList();
		var prSummaries = await summaries.GetAsync(collector, options.Owner, options.Repo, needSummary, ctx);
		if (prSummaries is null)
		{
			// The provider has reported why. Listing every pull request as unreadable on top of that would only repeat it.
			return new GitRangeEntryResolutionResult
			{
				Success = false,
				Entries = [],
				Report = new GitRangeBundleReport
				{
					StartRef = options.StartRef,
					EndRef = options.EndRef,
					TotalCommits = resolution.TotalCommits,
					Rows = [],
					CommitsWithoutPullRequest = resolution.CommitsWithoutPullRequest
				}
			};
		}

		foreach (var pr in resolution.PullRequests)
		{
			var matches = parsedCandidates.Where(c => MatchesPr(c, pr.Number, options.Owner, options.Repo)).ToList();
			if (matches.Count > 0)
			{
				var fileNames = new List<string>();
				foreach (var match in matches)
				{
					fileNames.Add(match.FileName);
					if (!includedFileNames.Add(match.FileName))
						continue;
					if (match.Entry == null)
					{
						collector.EmitError(
							match.FileName,
							$"Changelog entry '{match.FileName}' matches PR #{pr.Number} but could not be parsed: {match.ParseError}"
						);
						success = false;
						continue;
					}

					entries.Add(match.Entry);
				}

				rows.Add(new GitRangePrReportRow
				{
					Number = pr.Number,
					Url = pr.Url,
					Source = GitRangePrSourceKind.Pool,
					EntryFileNames = fileNames
				});
				decisions.Record("entries", "pool", $"#{pr.Number}");
				continue;
			}

			var (row, synthesized, failed) = SynthesizeFromPrMetadata(
				collector,
				pr,
				prSummaries.GetValueOrDefault(pr.Number),
				config,
				options
			);
			rows.Add(row);
			if (synthesized != null)
				entries.Add(synthesized);
			if (failed)
				success = false;
		}

		foreach (var excluded in resolution.ExcludedPullRequests)
		{
			rows.Add(new GitRangePrReportRow
			{
				Number = excluded.Number,
				Url = excluded.Url,
				Source = GitRangePrSourceKind.ExcludedBaseBranch
			});
		}

		var report = new GitRangeBundleReport
		{
			StartRef = options.StartRef,
			EndRef = options.EndRef,
			TotalCommits = resolution.TotalCommits,
			Rows = rows,
			CommitsWithoutPullRequest = resolution.CommitsWithoutPullRequest
		};

		return new GitRangeEntryResolutionResult { Success = success, Entries = entries, Report = report };
	}

	internal sealed record ChangelogPoolCandidate(
		string FileName,
		string Content,
		IReadOnlyList<int> FileNameNumbers,
		MatchedChangelogFile? Entry,
		string? ParseError,
		IReadOnlyList<string> Prs
	);

	internal static ChangelogPoolCandidate ParseCandidate(string fileName, string content)
	{
		var numbers = ChangelogPrIdentity.ParseLeadingPrNumbers(fileName);
		try
		{
			var checksum = ChangelogBundlingService.ComputeSha1(content);
			var normalized = ReleaseNotesSerialization.NormalizeYaml(content);
			var dto = SharpYaml.YamlSerializer.Deserialize<ChangelogEntryDto>(
				normalized,
				ReleaseNotesSerialization.GetEntryDeserializerOptions(fileName)
			)
				?? throw new InvalidOperationException("Failed to deserialize changelog entry YAML.");
			var entry = new MatchedChangelogFile
			{
				Data = ReleaseNotesSerialization.ConvertEntry(dto),
				FilePath = fileName,
				FileName = fileName,
				Checksum = checksum
			};
			var prs = dto.Prs ?? (dto.Pr != null ? [dto.Pr] : new List<string>());
			return new ChangelogPoolCandidate(fileName, content, numbers, entry, null, prs);
		}
		catch (YamlException ex)
		{
			return new ChangelogPoolCandidate(fileName, content, numbers, null, ex.Message, []);
		}
	}

	/// <summary>
	/// Whether a pool entry belongs to a PR: by file-name-derived PR numbers (file names survive
	/// scrubbing, so this works for private pools whose <c>prs</c> references were removed from the
	/// public copies) or by the entry's <c>prs</c> references.
	/// </summary>
	internal static bool MatchesPr(ChangelogPoolCandidate candidate, int prNumber, string owner, string repo)
	{
		if (candidate.FileNameNumbers.Contains(prNumber))
			return true;

		var expected = $"{owner}/{repo}#{prNumber}".ToLowerInvariant();
		return candidate.Prs.Any(pr => ChangelogBundlingService.NormalizePrForComparison(pr, owner, repo) == expected);
	}

	/// <summary>
	/// Synthesizes an in-memory changelog entry from PR metadata — the same extraction path
	/// <c>changelog add</c> uses: release-note text from the PR body as the description, its
	/// <c>## Impact</c> and <c>## Action</c> sections as impact and action, labels mapped to
	/// type/areas/products/feature-id/highlight via the config pivots, and <c>rules.create</c>
	/// label rules deciding inclusion.
	/// </summary>
	private (GitRangePrReportRow Row, MatchedChangelogFile? Entry, bool Failed) SynthesizeFromPrMetadata(
		IDiagnosticsCollector collector,
		CommitRangePullRequest pr,
		PullRequestSummary? summary,
		ChangelogConfiguration? config,
		GitRangeEntryResolutionOptions options
	)
	{
		if (summary == null || string.IsNullOrWhiteSpace(summary.Title))
		{
			collector.EmitWarning(
				string.Empty,
				$"No checked-in changelog entry was found for PR {pr.Url} and its metadata could not be fetched from GitHub. " +
					"The bundle will not include an entry for this PR."
			);
			return (Row(pr, GitRangePrSourceKind.Missing), null, false);
		}

		var labels = summary.Labels.ToArray();
		// Labels win, then products.default, then the profile's output products: the order of changelog add.
		var labelProducts = config != null ? ProductResolution.FromLabels(labels, config) : [];
		var resolvedProducts = ResolveProductsFromConfiguration(labelProducts, config, options);

		// rules.create exclusions come first: a pull request that a skip label leaves out needs no product label.
		if (config != null)
		{
			if (PrInfoProcessor.ShouldSkipPrDueToLabelBlockers(labels, resolvedProducts, config, pr.Url, decisions))
				return (Row(pr, GitRangePrSourceKind.Excluded), null, false);

			// Two or more mapped products and no default: the pull request must name its product with a label.
			// pivot.required lists products: the label is required even when products.default is set.
			var productLabelMissing = labelProducts.Count == 0
				&& ProductResolution.RequiresProductLabel(config)
				&& (ProductResolution.RequiresProductLabelExplicitly(config) || ProductResolution.Defaults(config).Count == 0);
			if (productLabelMissing)
			{
				collector.EmitError(pr.Url, ProductResolution.MissingProductLabelMessage($"PR {pr.Url}", config));
				return (Row(pr, GitRangePrSourceKind.Missing), null, true);
			}

			if (AreaResolution.RequiresAreaLabel(config) && AreaResolution.FromLabels(labels, config).Count == 0)
			{
				collector.EmitError(pr.Url, AreaResolution.MissingAreaLabelMessage($"PR {pr.Url}", config));
				return (Row(pr, GitRangePrSourceKind.Missing), null, true);
			}
		}

		var title = summary.Title;
		if (config?.Extract.StripTitlePrefix == true)
			title = ChangelogTextUtilities.StripTitlePrefix(title);

		var typeString = config?.LabelToType != null ? PrInfoProcessor.MapLabelsToType(labels, config.LabelToType) : null;
		if (typeString == null)
		{
			collector.EmitWarning(
				pr.Url,
				$"Could not derive a changelog type from the labels of PR #{pr.Number}; defaulting to 'other'. " +
					"Configure pivot.types in changelog.yml to map labels to types."
			);
		}

		var type = ChangelogEntryTypeExtensions.TryParse(
			typeString ?? "other",
			out var parsedType,
			ignoreCase: true,
			allowMatchingMetadataAttribute: true
		) ? parsedType : ChangelogEntryType.Other;

		var products = ResolveProducts(collector, pr, resolvedProducts);
		if (products == null)
			return (Row(pr, GitRangePrSourceKind.Missing), null, true);

		string? description = null;
		string? impact = null;
		string? action = null;
		if (config?.Extract.ReleaseNotes != false)
		{
			var extraction = summary.ReleaseNote;
			description = extraction.Content;
			ReleaseNoteExtractionDiagnostics.Record(decisions, pr.Number, extraction);
			// The same sections changelog add reads: a breaking change written in the PR keeps its Impact and Action.
			impact = summary.Impact;
			action = summary.Action;
		}

		var areas = config?.LabelToAreas != null ? PrInfoProcessor.MapLabelsToAreas(labels, config.LabelToAreas) : [];

		var featureId = config?.LabelToFeatures != null
			? PrInfoProcessor.MapLabelsToFeatureId(labels, config.LabelToFeatures, collector)
			: null;

		var highlight = config?.HighlightLabels is { Count: > 0 } highlightLabels
			&& labels.Any(label => highlightLabels.Contains(label, StringComparer.OrdinalIgnoreCase)) ? true : (bool?)null;

		var issues = config?.Extract.Issues != false && summary.LinkedIssues.Count > 0 ? summary.LinkedIssues.ToList() : null;

		var entryData = new ChangelogEntry
		{
			Title = title,
			Type = type,
			Description = description,
			Impact = impact,
			Action = action,
			Products = products,
			Areas = areas.Count > 0 ? areas : null,
			FeatureId = featureId,
			Highlight = highlight,
			Prs = [pr.Url],
			Issues = issues
		};

		var yaml = ReleaseNotesSerialization.SerializeEntry(entryData);
		var fileName = $"{pr.Number}.yaml";
		var entry = new MatchedChangelogFile
		{
			Data = entryData,
			FilePath = fileName,
			FileName = fileName,
			Checksum = ChangelogBundlingService.ComputeSha1(yaml)
		};

		var kind = description != null ? GitRangePrSourceKind.InferredPrBody : GitRangePrSourceKind.InferredTitle;
		decisions.Record("entries", "inferred", $"#{pr.Number}", kind == GitRangePrSourceKind.InferredPrBody ? "PR body" : "title");
		return (Row(pr, kind, fileName), entry, false);
	}

	/// <summary>
	/// The products of a pull request from the configuration, in the order <c>changelog add</c> uses: the products its
	/// labels map to and imply (<paramref name="labelProducts"/>), else <c>products.default</c>, else the
	/// concrete (non-wildcard) fallback products from the profile/CLI. Empty when none applies.
	/// </summary>
	private static List<ProductArgument> ResolveProductsFromConfiguration(
		List<ProductArgument> labelProducts,
		ChangelogConfiguration? config,
		GitRangeEntryResolutionOptions options
	)
	{
		if (labelProducts.Count > 0)
			return labelProducts;

		var defaults = config != null ? ProductResolution.Defaults(config) : [];
		if (defaults.Count > 0)
			return defaults;

		return (options.FallbackProducts ?? []).Where(p => !string.IsNullOrWhiteSpace(p.Product) && p.Product != "*").ToList();
	}

	/// <summary>
	/// Products for a synthesized entry: the products resolved from the configuration. Returns null after emitting an
	/// error when none applies — a bundle entry without products is invalid.
	/// </summary>
	private static List<ProductReference>? ResolveProducts(
		IDiagnosticsCollector collector,
		CommitRangePullRequest pr,
		IReadOnlyList<ProductArgument> resolvedProducts
	)
	{
		if (resolvedProducts.Count == 0)
		{
			collector.EmitError(
				string.Empty,
				$"Cannot determine products for the entry synthesized from PR {pr.Url}: its labels map to no product, and neither products.default nor output products are configured. " +
					"Configure pivot.products label mappings, set products.default, or set output_products on the bundle profile."
			);
			return null;
		}

		return resolvedProducts.Select(p => p.ToProductReference()).ToList();
	}

	private static GitRangePrReportRow Row(CommitRangePullRequest pr, GitRangePrSourceKind source, string? fileName = null) =>
		new() { Number = pr.Number, Url = pr.Url, Source = source, EntryFileNames = fileName != null ? [fileName] : [] };
}
