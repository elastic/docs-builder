// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Tests;

public class StringExtensionsTests
{
	[Test]
	[Arguments(null, null)]
	[Arguments("", null)]
	[Arguments("a", "a")]
	[Arguments(" ", " ")]
	public void NullIfEmpty_EmptyOrNull_ReturnsNull(string? value, string? expected) => value.NullIfEmpty().Should().Be(expected);

	[Test]
	public void JoinOrNull_NoValues_ReturnsNull() => Array.Empty<string>().JoinOrNull(' ').Should().BeNull();

	[Test]
	public void JoinOrNull_OnlyEmptyValues_ReturnsNull() => new[] { "" }.JoinOrNull(' ').Should().BeNull();

	[Test]
	public void JoinOrNull_Values_JoinsWithTheSeparator() => new[] { "a", "b" }.JoinOrNull(' ').Should().Be("a b");

	[Test]
	[Arguments("get-actions-connector-oauth-callback-script", "Get Actions Connector Oauth Callback Script")]
	[Arguments("async_search_delete", "Async Search Delete")]
	[Arguments("GET-Script", "Get Script")]
	[Arguments("-", "")]
	public void TitleCaseFromSlug_KebabOrSnakeCase_ReturnsTitleCasedWords(string slug, string expected) =>
		slug.TitleCaseFromSlug().Should().Be(expected);

	[Test]
	[Arguments("fix create visualization", "Fix create visualization")]
	[Arguments("bulk delete observables", "Bulk delete observables")]
	[Arguments("Already capitalized", "Already capitalized")]
	[Arguments("don't return an empty response", "Don't return an empty response")]
	[Arguments("don\u2019t return an empty response", "Don\u2019t return an empty response")]
	[Arguments("kibana.alert.rule is renamed", "kibana.alert.rule is renamed")]
	[Arguments("inputsForVersions is refreshed", "inputsForVersions is refreshed")]
	[Arguments("snake_case_field is renamed", "snake_case_field is renamed")]
	[Arguments("`code` first", "`code` first")]
	[Arguments("elasticsearch projects", "Elasticsearch projects")]
	[Arguments("ml_anomaly", "ml_anomaly")]
	[Arguments("v2 search", "v2 search")]
	[Arguments("search v2", "Search v2")]
	[Arguments("search APIs", "Search APIs")]
	[Arguments("", "")]
	public void CapitalizeFirstLetter_CapitalizesPlainWordsAndKeepsCodeLikeNames(string input, string expected) =>
		input.CapitalizeFirstLetter().Should().Be(expected);

	[Test]
	public void Slugify_NullInput_ReturnsEmptyString() => ((string?)null).Slugify().Should().BeEmpty();

	[Test]
	[Arguments("", "")]
	[Arguments("   ", "")]
	[Arguments("  Hello   World  ", "hello-world")]
	[Arguments("a - b", "a-b")]
	[Arguments("-a-", "-a-")]
	[Arguments("Café Ünï", "cafe-uni")]
	[Arguments("1.2.3 a_b", "1.2.3-a_b")]
	[Arguments("C# (a/b?c)", "c-abc")]
	[Arguments("日本語 text", "-text")]
	public void Slugify_Rules_AreStable(string input, string expected) => input.Slugify().Should().Be(expected);

	/// <summary>
	/// <c>slugify-4.0.1.golden.tsv</c> holds what Slugify.Core 4.0.1 produced for the headings in <c>docs/</c> plus edge cases,
	/// so a slug (and every link to it) stays the same now that the package is gone.
	/// </summary>
	[Test]
	public void Slugify_MatchesSlugifyCore401Output()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "slugify-4.0.1.golden.tsv");
		var lines = File.ReadAllLines(path);
		lines.Should().NotBeEmpty();
		var mismatches = new List<string>();
		foreach (var line in lines)
		{
			var tab = line.LastIndexOf('\t');
			var input = line[..tab].Replace("\\t", "\t").Replace("\\\\", "\\");
			var expected = line[(tab + 1)..];
			if (input.Slugify() != expected)
				mismatches.Add($"[{input}] expected [{expected}] got [{input.Slugify()}]");
		}
		mismatches.Should().BeEmpty();
	}
}
