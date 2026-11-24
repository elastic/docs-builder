// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Isolated;

/// <summary>
/// The page of the preview site that lists the pull requests whose labels put their release note in the wrong place, by product,
/// release and problem, with the labels that fix each. A pull request of a private repository is named by repository and number only.
/// </summary>
internal static class UnifiedReleaseNotesDiagnosticsPage
{
	public const string FileName = "diagnostics.md";

	private const int MaxLabelsShown = 12;

	private static readonly (PreviewProblemKind Kind, string Title, string Effect, bool Open)[] Kinds =
	[
		(PreviewProblemKind.MissingProductLabel, "No product label", "The release note shows under the product of its bundle, which may not be its product.", true),
		(PreviewProblemKind.MissingAreaLabel, "No area label", "The release note has no area.", true),
		(PreviewProblemKind.MissingTypeLabel, "No type label", "The release note shows under Other changes.", false)
	];

	public static string Generate(IReadOnlyList<ProductVersions> products, IReadOnlyList<PreviewProblem> problems)
	{
		var names = products.ToDictionary(p => p.Product.Id, p => p.Product.DisplayName, StringComparer.Ordinal);
		var count = problems.Select(p => (p.Owner, p.Repo, p.Number)).Distinct().Count();
		var sb = new StringBuilder();
		_ = sb
			.AppendLine("---")
			.AppendLine("navigation_title: Diagnostics")
			.AppendLine("---")
			.AppendLine()
			.AppendLine("# Release notes diagnostics")
			.AppendLine()
			.AppendLine("This page lists the pull requests whose release notes end up in the wrong place because a label is missing.")
			.AppendLine("A pull request without a product label may show under the wrong product. One without an area label has no area.")
			.AppendLine(
				"One without a type label shows under Other changes. Add the label to the pull request, and the next hourly rebuild of this site picks it up."
			)
			.AppendLine();

		if (count == 0)
		{
			_ = sb.AppendLine("**No pull request lacks a label right now.** There is nothing to fix.");
			return sb.ToString();
		}

		_ = sb
			.AppendLine(CultureInfo.InvariantCulture, $"{Plural(count)} a label. They are listed below, by product and release.")
			.AppendLine()
			.AppendLine(":::{note}")
			.AppendLine("A pull request of a private repository is named by repository and number, without a link or a title.")
			.AppendLine(":::")
			.AppendLine();

		foreach (var product in problems.GroupBy(p => p.Product).OrderBy(g => g.Key, StringComparer.Ordinal))
		{
			var productName = names.GetValueOrDefault(product.Key, product.Key);
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"## {Escape(productName)}").AppendLine();
			foreach (var release in product.GroupBy(p => p.Release).OrderByDescending(g => VersionOrDate.Parse(g.Key)))
				AppendRelease(sb, productName, release.Key, release.ToList());
		}

		return sb.ToString();
	}

	private static void AppendRelease(StringBuilder sb, string productName, string release, List<PreviewProblem> problems)
	{
		_ = sb.AppendLine(
			CultureInfo.InvariantCulture,
			$"### {Escape(productName)} {Escape(UnifiedReleaseNotesDocset.VersionTitle(release))}"
		).AppendLine();
		foreach (var (kind, title, effect, open) in Kinds)
		{
			var ofKind = problems.Where(p => p.Kind == kind).ToList();
			if (ofKind.Count == 0)
				continue;

			_ = sb.AppendLine(CultureInfo.InvariantCulture, $":::{{dropdown}} {title} ({ofKind.Count})");
			if (open)
				_ = sb.AppendLine(":open:");
			_ = sb.AppendLine().AppendLine(effect).AppendLine();
			if (LabelsOf(ofKind) is { Length: > 0 } labels)
				_ = sb.AppendLine(labels).AppendLine();
			foreach (var problem in ofKind)
				_ = sb.AppendLine(Line(problem));
			_ = sb.AppendLine(":::").AppendLine();
		}
	}

	private static string LabelsOf(List<PreviewProblem> problems)
	{
		var labels = (problems.Select(p => p.Labels).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty).Split(
			',',
			StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
		);
		if (labels.Length == 0)
			return string.Empty;

		var shown = string.Join(
			", ",
			labels.Take(MaxLabelsShown).Select(l => $"`{l.Replace("`", string.Empty, StringComparison.Ordinal)}`")
		);
		var more = labels.Length > MaxLabelsShown
			? $", and {labels.Length - MaxLabelsShown} more in the `changelog.yml` of the repository"
			: string.Empty;
		return $"Add one of these labels: {shown}{more}.";
	}

	private static string Line(PreviewProblem problem)
	{
		var name = $"{problem.Owner}/{problem.Repo}#{problem.Number}".TrimStart('/');
		if (problem.Url is null)
			return $"- `{(problem.Owner.Length == 0 ? $"#{problem.Number}" : name)}`";

		return $"- [{Escape(name)}]({problem.Url}) {Escape(problem.Title ?? string.Empty)}".TrimEnd();
	}

	private static string Plural(int count) => count == 1 ? "1 pull request lacks" : $"{count} pull requests lack";

	/// <summary>
	/// Backslash-escapes the characters that change the structure of a line of Markdown, and the colon and at sign that make a URL or an
	/// address a link. A title comes from GitHub, so it is text.
	/// </summary>
	internal static string Escape(string text)
	{
		var sb = new StringBuilder(text.Length);
		foreach (var c in text.ReplaceLineEndings(" "))
		{
			if ("\\`*_{}[]<>()#|!~&:@".Contains(c, StringComparison.Ordinal))
				_ = sb.Append('\\');
			_ = sb.Append(c);
		}

		return sb.ToString();
	}
}
