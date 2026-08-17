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
using YamlDotNet.Core;

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
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Changelog bundle for `{StartRef}..{EndRef}`")
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
				_ => "missing"
			};
			var entry = row.EntryFileNames.Count > 0 ? string.Join(", ", row.EntryFileNames.Select(f => $"`{f}`")) : "—";
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"| [#{row.Number}]({row.Url}) | {source} | {entry} |");
		}

		if (CommitsWithoutPullRequest.Count > 0)
		{
			_ = sb.AppendLine()
				.AppendLine("Commits without an associated pull request:")
				.AppendLine();
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
public class GitRangeEntryResolver(IGitHubPrService prService, ILogger logger)
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
		Cancel ctx)
	{
		var parsedCandidates = candidates.Select(c => ParseCandidate(c.FileName, c.Content)).ToList();

		var rows = new List<GitRangePrReportRow>();
		var entries = new List<MatchedChangelogFile>();
		var includedFileNames = new HashSet<string>(StringComparer.Ordinal);
		var success = true;

		foreach (var pr in resolution.PullRequests)
		{
			var matches = parsedCandidates.Where(c => MatchesPr(c, pr.Number, options)).ToList();
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
						collector.EmitError(match.FileName,
							$"Changelog entry '{match.FileName}' matches PR #{pr.Number} but could not be parsed: {match.ParseError}");
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
				continue;
			}

			var (row, synthesized, failed) = await SynthesizeFromPrMetadata(collector, pr, config, options, ctx);
			rows.Add(row);
			if (synthesized != null)
				entries.Add(synthesized);
			if (failed)
				success = false;
		}

		var report = new GitRangeBundleReport
		{
			StartRef = options.StartRef,
			EndRef = options.EndRef,
			TotalCommits = resolution.TotalCommits,
			Rows = rows,
			CommitsWithoutPullRequest = resolution.CommitsWithoutPullRequest
		};

		return new GitRangeEntryResolutionResult
		{
			Success = success,
			Entries = entries,
			Report = report
		};
	}

	private sealed record ParsedCandidate(string FileName, IReadOnlyList<int> FileNameNumbers, MatchedChangelogFile? Entry, string? ParseError, IReadOnlyList<string> NormalizedPrs);

	private static ParsedCandidate ParseCandidate(string fileName, string content)
	{
		var numbers = ParseLeadingPrNumbers(fileName);
		try
		{
			var checksum = ChangelogBundlingService.ComputeSha1(content);
			var normalized = ReleaseNotesSerialization.NormalizeYaml(content);
			var dto = ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(normalized);
			var entry = new MatchedChangelogFile
			{
				Data = ReleaseNotesSerialization.ConvertEntry(dto),
				FilePath = fileName,
				FileName = fileName,
				Checksum = checksum
			};
			var prs = dto.Prs ?? (dto.Pr != null ? [dto.Pr] : new List<string>());
			return new ParsedCandidate(fileName, numbers, entry, null, prs);
		}
		catch (YamlException ex)
		{
			return new ParsedCandidate(fileName, numbers, null, ex.Message, []);
		}
	}

	/// <summary>
	/// Parses PR numbers from the leading dash-separated numeric segments of an entry file name,
	/// covering the PR-number naming schemes (<c>123.yaml</c>, <c>123-456.yaml</c>,
	/// <c>123-bug-fix-slug.yaml</c>). File names survive scrubbing, so this match works for
	/// private pools whose <c>prs</c> references were removed from the public copies.
	/// </summary>
	internal static IReadOnlyList<int> ParseLeadingPrNumbers(string fileName)
	{
		var stem = fileName;
		var extensionIndex = stem.LastIndexOf('.');
		if (extensionIndex > 0)
			stem = stem[..extensionIndex];

		var numbers = new List<int>();
		foreach (var segment in stem.Split('-'))
		{
			if (segment.Length > 0 && segment.All(char.IsAsciiDigit) && int.TryParse(segment, out var number))
				numbers.Add(number);
			else
				break;
		}

		return numbers;
	}

	private static bool MatchesPr(ParsedCandidate candidate, int prNumber, GitRangeEntryResolutionOptions options)
	{
		if (candidate.FileNameNumbers.Contains(prNumber))
			return true;

		var expected = $"{options.Owner}/{options.Repo}#{prNumber}".ToLowerInvariant();
		return candidate.NormalizedPrs.Any(pr =>
			ChangelogBundlingService.NormalizePrForComparison(pr, options.Owner, options.Repo) == expected);
	}

	/// <summary>
	/// Synthesizes an in-memory changelog entry from PR metadata — the same extraction path
	/// <c>changelog add</c> uses: release-note text from the PR body as the description, labels
	/// mapped to type/areas/products/feature-id/highlight via the config pivots, and
	/// <c>rules.create</c> label rules deciding inclusion.
	/// </summary>
	private async Task<(GitRangePrReportRow Row, MatchedChangelogFile? Entry, bool Failed)> SynthesizeFromPrMetadata(
		IDiagnosticsCollector collector,
		CommitRangePullRequest pr,
		ChangelogConfiguration? config,
		GitRangeEntryResolutionOptions options,
		Cancel ctx)
	{
		var prInfo = await prService.FetchPrInfoAsync(pr.Url, options.Owner, options.Repo, ctx);
		if (prInfo == null || string.IsNullOrWhiteSpace(prInfo.Title))
		{
			collector.EmitWarning(string.Empty,
				$"No checked-in changelog entry was found for PR {pr.Url} and its metadata could not be fetched from GitHub. " +
				"The bundle will not include an entry for this PR.");
			return (Row(pr, GitRangePrSourceKind.Missing), null, false);
		}

		var labels = prInfo.Labels.ToArray();
		var labelProducts = config?.LabelToProducts != null
			? PrInfoProcessor.MapLabelsToProducts(labels, config.LabelToProducts)
			: [];

		if (config != null)
		{
			var effectiveProducts = labelProducts.Count > 0 ? labelProducts : (options.FallbackProducts ?? []).ToList();
			if (PrInfoProcessor.ShouldSkipPrDueToLabelBlockers(labels, effectiveProducts, config, collector, pr.Url))
				return (Row(pr, GitRangePrSourceKind.Excluded), null, false);
		}

		var title = prInfo.Title;
		if (config?.Extract.StripTitlePrefix == true)
			title = ChangelogTextUtilities.StripSquareBracketPrefix(title);

		var typeString = config?.LabelToType != null
			? PrInfoProcessor.MapLabelsToType(labels, config.LabelToType)
			: null;
		if (typeString == null)
		{
			collector.EmitWarning(pr.Url,
				$"Could not derive a changelog type from the labels of PR #{pr.Number}; defaulting to 'other'. " +
				"Configure pivot.types in changelog.yml to map labels to types.");
		}

		var type = ChangelogEntryTypeExtensions.TryParse(typeString ?? "other", out var parsedType, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? parsedType
			: ChangelogEntryType.Other;

		var products = ResolveProducts(collector, pr, labelProducts, options);
		if (products == null)
			return (Row(pr, GitRangePrSourceKind.Missing), null, true);

		var description = config?.Extract.ReleaseNotes != false
			? ReleaseNotesExtractor.FindReleaseNote(prInfo.Body)
			: null;

		var areas = config?.LabelToAreas != null
			? PrInfoProcessor.MapLabelsToAreas(labels, config.LabelToAreas)
			: [];

		var featureId = config?.LabelToFeatures != null
			? PrInfoProcessor.MapLabelsToFeatureId(labels, config.LabelToFeatures, collector)
			: null;

		var highlight = config?.HighlightLabels is { Count: > 0 } highlightLabels &&
			labels.Any(label => highlightLabels.Contains(label, StringComparer.OrdinalIgnoreCase))
			? true
			: (bool?)null;

		var issues = config?.Extract.Issues != false && prInfo.LinkedIssues.Count > 0
			? prInfo.LinkedIssues.ToList()
			: null;

		var entryData = new ChangelogEntry
		{
			Title = title,
			Type = type,
			Description = description,
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
		logger.LogInformation("Synthesized changelog entry for PR #{Number} from PR metadata ({Kind})", pr.Number, kind);
		return (Row(pr, kind, fileName), entry, false);
	}

	/// <summary>
	/// Products for a synthesized entry: label-derived products win; otherwise the concrete
	/// (non-wildcard) fallback products from the profile/CLI. Returns null after emitting an
	/// error when neither yields a product — a bundle entry without products is invalid.
	/// </summary>
	private static List<ProductReference>? ResolveProducts(
		IDiagnosticsCollector collector,
		CommitRangePullRequest pr,
		IReadOnlyList<ProductArgument> labelProducts,
		GitRangeEntryResolutionOptions options)
	{
		var source = labelProducts.Count > 0
			? labelProducts
			: (options.FallbackProducts ?? []).Where(p => !string.IsNullOrWhiteSpace(p.Product) && p.Product != "*").ToList();

		if (source.Count == 0)
		{
			collector.EmitError(string.Empty,
				$"Cannot determine products for the entry synthesized from PR {pr.Url}: its labels map to no product and no output products are configured. " +
				"Configure pivot.products label mappings or set output_products on the bundle profile.");
			return null;
		}

		return source.Select(p => p.ToProductReference()).ToList();
	}

	private static GitRangePrReportRow Row(CommitRangePullRequest pr, GitRangePrSourceKind source, string? fileName = null) => new()
	{
		Number = pr.Number,
		Url = pr.Url,
		Source = source,
		EntryFileNames = fileName != null ? [fileName] : []
	};
}
