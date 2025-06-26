// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AngleSharp.Diffing;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit.Internal;
using Xunit.Sdk;

namespace Elastic.Markdown.Tests;

public static class PrettyHtmlExtensions
{
	public static string PrettyHtml([LanguageInjection("html")] this string html, bool sanitize = true)
	{
		var parser = new HtmlParser();

		var document = parser.ParseDocument(html);
		var element = document.Body;
		if (element is null)
			return string.Empty;

		if (sanitize)
		{
			var links = element.QuerySelectorAll("a");
			links
				.ForEach(l =>
				{
					l.RemoveAttribute("hx-get");
					l.RemoveAttribute("hx-select-oob");
					l.RemoveAttribute("hx-swap");
					l.RemoveAttribute("hx-indicator");
					l.RemoveAttribute("hx-push-url");
					l.RemoveAttribute("preload");
				});
		}

		using var sw = new StringWriter();
		var formatter = new PrettyMarkupFormatter();
		element.Children
			.ForEach(c =>
			{
				c.ToHtml(sw, formatter);
			});
		return sw.ToString().TrimStart('\n');
	}

	public static void ShouldBeHtml(
		[LanguageInjection("html")] this string actual,
		[LanguageInjection("html")] string expected,
		bool sanitize = true
	)
	{
		expected = expected.Trim('\n').PrettyHtml(sanitize);
		actual = actual.Trim('\n').PrettyHtml(sanitize);

		var diff = DiffBuilder
			.Compare(actual)
			.WithTest(expected)
			.Build()
			.ToArray();

		if (diff.Length == 0)
			return;

		throw new XunitException(CreateDiff(actual, expected, sanitize));
	}
	public static void ShouldContainHtml(
		[LanguageInjection("html")] this string actual,
		[LanguageInjection("html")] string expected,
		bool sanitize = true
	)
	{
		expected = expected.Trim('\n').PrettyHtml(sanitize);
		actual = actual.Trim('\n').PrettyHtml(sanitize);

		var actualCompare = actual.Replace("\t", string.Empty);
		var expectedCompare = actual.Replace("\t", string.Empty);

		// we compare over unindented HTML, but if that fails, we rely on the pretty HTML Contain().
		// to throw for improved error messages
		if (!actualCompare.Contains(expectedCompare))
			actual.Should().Contain(expected);
	}

	public static string CreateDiff(this string actual, string expected, bool sanitize = true)
	{
		expected = expected.Trim('\n').PrettyHtml(sanitize);
		actual = actual.Trim('\n').PrettyHtml(sanitize);
		var diffLines = InlineDiffBuilder.Diff(expected, actual).Lines;

		var mutatedCount =
			diffLines
				.Count(l => l.Type switch
				{
					ChangeType.Unchanged => false,
					ChangeType.Deleted => true,
					ChangeType.Inserted => true,
					ChangeType.Imaginary => false,
					ChangeType.Modified => true,
					_ => false
				});
		if (mutatedCount == 0)
			return string.Empty;

		var actualLineLength = actual.Split("\n").Length;
		if (mutatedCount >= actualLineLength)
		{
			return $$"""
			        Mutations {{mutatedCount}} on all {{actualLineLength}} showing
			        EXPECTED:
			        {{expected}}
			        ACTUAL:
			        {{actual}}
			        """;
		}

		using var sw = new StringWriter();
		diffLines
			.ForEach(l =>
			{
				switch (l.Type)
				{
					case ChangeType.Unchanged:
						sw.WriteLine(l.Text);
						break;
					case ChangeType.Deleted:
						sw.WriteLine("- " + l.Text);
						break;
					case ChangeType.Inserted:
						sw.WriteLine("+ " + l.Text);
						break;
					case ChangeType.Imaginary:
						sw.WriteLine("? " + l.Text);
						break;
					case ChangeType.Modified:
						sw.WriteLine("+ " + l.Text);
						break;
				}
			});

		return sw.ToString();
	}
}
