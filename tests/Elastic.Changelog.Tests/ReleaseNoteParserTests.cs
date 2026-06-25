// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.GitHub;
using Xunit;

namespace Elastic.Changelog.Tests;

public class ReleaseNoteParserTests
{
	// docs-builder's own release-drafter output: level-2 emoji headers and "-" bullets
	// (change-template: "- $TITLE by @$AUTHOR in #$NUMBER").
	[Fact]
	public void Parse_DocsBuilderReleaseDrafterFormat_ExtractsPrsAndType()
	{
		const string body =
			"""
			## 🐛 Bug Fixes

			- fix(frontend): switch select-dom to non-throwing optional variants by @reakaleek in #3532
			- fix: move website search input container outside htmx swap region by @reakaleek in #3524
			- fix(stepper): skip headings inside steps when calculating next step heading level by @theletterf in #3525

			**Full Changelog**: https://github.com/elastic/docs-builder/compare/1.18.0...1.18.1
			""";

		var result = ReleaseNoteParser.Parse(body);

		result.Format.Should().Be(ReleaseNoteFormat.ReleaseDrafter);
		result.PrReferences.Select(p => p.PrNumber).Should().Equal(3532, 3524, 3525);
		result.PrReferences.Should().OnlyContain(p => p.InferredType == "bug-fix");
		result.FullChangelogUrl.Should().Be("https://github.com/elastic/docs-builder/compare/1.18.0...1.18.1");
	}

	[Fact]
	public void Parse_DocsBuilderReleaseDrafterFormat_InfersTypePerSection()
	{
		const string body =
			"""
			## ✨ Features

			- Add a shiny thing by @alice in #1

			## 🐛 Bug Fixes

			- Fix a broken thing by @bob in #2
			""";

		var result = ReleaseNoteParser.Parse(body);

		result.Format.Should().Be(ReleaseNoteFormat.ReleaseDrafter);
		result.PrReferences.Should().HaveCount(2);
		result.PrReferences[0].Should().Match<ExtractedPrReference>(p => p.PrNumber == 1 && p.InferredType == "feature");
		result.PrReferences[1].Should().Match<ExtractedPrReference>(p => p.PrNumber == 2 && p.InferredType == "bug-fix");
	}

	// Regression: the previous, stricter shape (### headers, "*" bullets) still parses.
	[Fact]
	public void Parse_LegacyReleaseDrafterFormat_StillWorks()
	{
		const string body =
			"""
			### ✨ Features

			* Add a shiny thing by @alice in #10

			### 🐛 Bug Fixes

			* Fix a broken thing by @bob in #11
			""";

		var result = ReleaseNoteParser.Parse(body);

		result.Format.Should().Be(ReleaseNoteFormat.ReleaseDrafter);
		result.PrReferences.Select(p => p.PrNumber).Should().Equal(10, 11);
		result.PrReferences[0].InferredType.Should().Be("feature");
		result.PrReferences[1].InferredType.Should().Be("bug-fix");
	}

	[Fact]
	public void Parse_GitHubDefaultFormat_AcceptsBothBullets()
	{
		const string body =
			"""
			## What's Changed

			* Asterisk change by @alice in https://github.com/elastic/docs-builder/pull/5
			- Hyphen change by @bob in #6

			**Full Changelog**: https://github.com/elastic/docs-builder/compare/1.0.0...1.1.0
			""";

		var result = ReleaseNoteParser.Parse(body);

		result.Format.Should().Be(ReleaseNoteFormat.GitHubDefault);
		result.PrReferences.Select(p => p.PrNumber).Should().Equal(5, 6);
		result.PrReferences.Should().OnlyContain(p => p.InferredType == null);
	}

	[Theory]
	[InlineData("## 🐛 Bug Fixes")]
	[InlineData("### 🐛 Bug Fixes")]
	public void DetectFormat_EmojiHeadersAtAnyLevel_IsReleaseDrafter(string header)
	{
		ReleaseNoteParser.DetectFormat($"{header}\n\n- Fix it by @alice in #1")
			.Should().Be(ReleaseNoteFormat.ReleaseDrafter);
	}
}
