// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Versions;

namespace Elastic.Changelog.Migration;

/// <summary>A single release parsed from a published release-notes Markdown page, mapped to the existing bundle shape.</summary>
public sealed record MigratedRelease
{
	public required string Version { get; init; }
	public required Bundle Bundle { get; init; }
}

/// <summary>
/// TEMPORARY (elastic/docs-eng-team#736): parses a hand-authored release-notes Markdown page —
/// <c>## {version}</c> sections with typed <c>### {section}</c> subsections and bullet entries —
/// into the existing <see cref="Bundle"/> shape with inline entries. No new schema is introduced.
/// Delete together with the migrate-from-web command once the rollout (elastic/docs-eng-team#683) completes.
/// </summary>
public static partial class ReleaseNotesPageParser
{
	[GeneratedRegex(@"^##\s+(?<version>\S+)(?:\s+\[[^\]]*\])?\s*$")]
	private static partial Regex VersionHeadingRegex();

	[GeneratedRegex(@"^###\s+(?<title>.+?)(?:\s*\[[^\]]*\])?\s*$")]
	private static partial Regex SubsectionHeadingRegex();

	[GeneratedRegex(@"^\*\*Release date:?\*\*:?\s*(?<date>.+?)\s*$")]
	private static partial Regex ReleaseDateRegex();

	[GeneratedRegex(@"\[#?\d+\]\((?<url>https://github\.com/[^\s)]+/pull/\d+)\)")]
	private static partial Regex PrLinkRegex();

	[GeneratedRegex(@"(?<=^|[\s(])#(?<number>\d+)\b")]
	private static partial Regex BarePrRefRegex();

	/// <summary>
	/// Parses <paramref name="markdown"/> into one <see cref="MigratedRelease"/> per <c>## {version}</c>
	/// section. Content that cannot be mapped to typed entries is preserved verbatim in the bundle
	/// description so no published content is dropped; anything ambiguous emits a warning on
	/// <paramref name="collector"/> so the operator can review it before uploading.
	/// </summary>
	public static IReadOnlyList<MigratedRelease> Parse(
		IDiagnosticsCollector collector,
		string markdown,
		string sourceId,
		MigrateFromWebScope scope)
	{
		var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
		var releases = new List<MigratedRelease>();
		SectionBuilder? section = null;

		var index = SkipFrontmatter(lines);
		for (; index < lines.Length; index++)
		{
			var line = lines[index];

			// MyST comment lines never contribute content (used for authoring templates like "% ## version.next").
			if (line.TrimStart().StartsWith('%'))
				continue;

			var versionMatch = VersionHeadingRegex().Match(line);
			if (versionMatch.Success)
			{
				AddCompleted(releases, section);
				section = StartSection(collector, versionMatch.Groups["version"].Value, sourceId, scope);
				continue;
			}

			// Content before the first "## {version}" heading is the page intro, not release content.
			section?.ConsumeLine(collector, line);
		}

		AddCompleted(releases, section);
		return releases;
	}

	private static int SkipFrontmatter(string[] lines)
	{
		if (lines.Length == 0 || lines[0].TrimEnd() != "---")
			return 0;

		for (var i = 1; i < lines.Length; i++)
		{
			if (lines[i].TrimEnd() == "---")
				return i + 1;
		}

		return 0;
	}

	private static SectionBuilder? StartSection(IDiagnosticsCollector collector, string version, string sourceId, MigrateFromWebScope scope)
	{
		// A heading token that is neither a version nor a date (e.g. a prose heading) is not a release
		// section; skip it entirely rather than fabricating a bundle for it.
		if (VersionOrDate.Parse(version).Raw is not null)
		{
			collector.EmitWarning(sourceId, $"Skipping section '## {version}': heading is not a recognizable version or date.");
			return null;
		}

		return new SectionBuilder(version, sourceId, scope);
	}

	private static void AddCompleted(List<MigratedRelease> releases, SectionBuilder? section)
	{
		if (section?.Build() is { } release)
			releases.Add(release);
	}

	/// <summary>Accumulates one <c>## {version}</c> section's date, description, and typed entries.</summary>
	private sealed class SectionBuilder(string version, string sourceId, MigrateFromWebScope scope)
	{
		private readonly StringBuilder _description = new();
		private readonly List<BundledEntry> _entries = [];
		private DateOnly? _releaseDate;
		private ChangelogEntryType? _entryType;
		private bool _collectingEntries;
		private int _lastEntryIndex = -1;

		public void ConsumeLine(IDiagnosticsCollector collector, string line)
		{
			var subsectionMatch = SubsectionHeadingRegex().Match(line);
			if (subsectionMatch.Success)
			{
				ConsumeSubsectionHeading(collector, line, subsectionMatch.Groups["title"].Value);
				return;
			}

			var dateMatch = ReleaseDateRegex().Match(line);
			if (dateMatch.Success && TryParseReleaseDate(dateMatch.Groups["date"].Value, out var date))
			{
				_releaseDate = date;
				return;
			}

			if (dateMatch.Success)
				collector.EmitWarning(sourceId, $"Could not parse release date '{dateMatch.Groups["date"].Value}' for {version}; keeping the line as description text.");

			ConsumeContentLine(line);
		}

		private void ConsumeSubsectionHeading(IDiagnosticsCollector collector, string line, string title)
		{
			var type = ResolveSectionType(title);
			if (type is null)
			{
				// Unrecognized subsections flow into the description verbatim (heading included) so the
				// published content is preserved even when it cannot be mapped to typed entries.
				collector.EmitWarning(sourceId, $"Unrecognized subsection '### {title.Trim()}' under {version}; preserving it in the bundle description.");
				_entryType = null;
				_collectingEntries = false;
				AppendDescriptionLine(line);
				return;
			}

			_entryType = type;
			_collectingEntries = true;
		}

		private void ConsumeContentLine(string line)
		{
			var isBlank = string.IsNullOrWhiteSpace(line);
			var trimmed = line.TrimStart();
			var isBullet = trimmed.StartsWith("* ", StringComparison.Ordinal) || trimmed.StartsWith("- ", StringComparison.Ordinal);
			var isIndented = !isBlank && line.Length > 0 && (line[0] == ' ' || line[0] == '\t');

			if (_collectingEntries)
			{
				if (isBlank)
					return;

				if (isBullet && !isIndented)
				{
					_entries.Add(ParseEntry(trimmed[2..], _entryType!.Value, scope));
					_lastEntryIndex = _entries.Count - 1;
					return;
				}

				// A wrapped bullet continues the previous entry.
				if (isIndented && _lastEntryIndex >= 0)
				{
					var entry = _entries[_lastEntryIndex];
					_entries[_lastEntryIndex] = MergeContinuation(entry, trimmed, scope);
					return;
				}

				// First plain paragraph after the entry list ends entry collection: trailing prose
				// (and any lists inside it) belongs to the description, not to the entries.
				_collectingEntries = false;
				_lastEntryIndex = -1;
			}

			AppendDescriptionLine(line);
		}

		private void AppendDescriptionLine(string line)
		{
			if (_description.Length == 0 && string.IsNullOrWhiteSpace(line))
				return;
			_ = _description.Append(line.TrimEnd()).Append('\n');
		}

		public MigratedRelease Build()
		{
			var description = _description.ToString().Trim();
			return new MigratedRelease
			{
				Version = version,
				Bundle = new Bundle
				{
					Products =
					[
						new BundledProduct
						{
							ProductId = scope.ProductId,
							Target = version,
							Lifecycle = Lifecycle.Ga,
							Repo = scope.Repo,
							Owner = scope.Owner
						}
					],
					Description = description.Length > 0 ? description : null,
					ReleaseDate = _releaseDate,
					Entries = _entries
				}
			};
		}
	}

	private static ChangelogEntryType? ResolveSectionType(string heading) =>
		heading.Trim().ToLowerInvariant() switch
		{
			"features and enhancements" or "features" or "enhancements" => ChangelogEntryType.Enhancement,
			"fixes" or "bug fixes" => ChangelogEntryType.BugFix,
			"breaking changes" => ChangelogEntryType.BreakingChange,
			"deprecations" => ChangelogEntryType.Deprecation,
			"known issues" => ChangelogEntryType.KnownIssue,
			"security" or "security updates" => ChangelogEntryType.Security,
			_ => null
		};

	private static bool TryParseReleaseDate(string text, out DateOnly date)
	{
		string[] formats = ["MMMM d, yyyy", "yyyy-MM-dd"];
		return DateOnly.TryParseExact(text.Trim().TrimEnd('.'), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
	}

	private static BundledEntry ParseEntry(string text, ChangelogEntryType type, MigrateFromWebScope scope)
	{
		var (title, prs) = ExtractPrReferences(text, scope);
		return new BundledEntry
		{
			Type = type,
			Title = title,
			Products = [new ProductReference { ProductId = scope.ProductId }],
			Prs = prs.Count > 0 ? prs : null
		};
	}

	private static BundledEntry MergeContinuation(BundledEntry entry, string continuation, MigrateFromWebScope scope)
	{
		var (title, prs) = ExtractPrReferences($"{entry.Title} {continuation}", scope);
		var mergedPrs = (entry.Prs ?? []).Concat(prs).Distinct(StringComparer.Ordinal).ToList();
		return entry with { Title = title, Prs = mergedPrs.Count > 0 ? mergedPrs : null };
	}

	/// <summary>
	/// Extracts PR references from bullet text — Markdown links like <c>[#899](…/pull/899)</c> or
	/// <c>[835](…/pull/835)</c>, and bare <c>#958</c> refs resolved against the scope's repository —
	/// returning the cleaned-up title and the collected PR URLs.
	/// </summary>
	private static (string Title, List<string> Prs) ExtractPrReferences(string text, MigrateFromWebScope scope)
	{
		var prs = new List<string>();

		var title = PrLinkRegex().Replace(text, m =>
		{
			prs.Add(m.Groups["url"].Value);
			return string.Empty;
		});

		title = BarePrRefRegex().Replace(title, m =>
		{
			prs.Add($"https://github.com/{scope.Owner}/{scope.Repo}/pull/{m.Groups["number"].Value}");
			return string.Empty;
		});

		return (NormalizeTitle(title), prs);
	}

	private static string NormalizeTitle(string title)
	{
		// Removing PR tokens can leave empty parentheses and dangling separators behind.
		title = title.Replace("()", string.Empty, StringComparison.Ordinal);
		var collapsed = string.Join(' ', title.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
		return collapsed.TrimEnd(' ', '-', '–', '—', ':', ',', ';');
	}
}
