// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// Unit tests for VersionOrDate parsing and display formatting.
/// </summary>
public class VersionOrDateTests
{
	[Theory]
	[InlineData("9.3.0")]
	[InlineData("1.0.0")]
	[InlineData("10.0.0-beta1")]
	public void Parse_SemverVersions_ReturnsSemVer(string version)
	{
		var result = VersionOrDate.Parse(version);
		result.SemVer.Should().NotBeNull();
		result.Date.Should().BeNull();
		result.Raw.Should().BeNull();
	}

	[Theory]
	[InlineData("2025-08-05")]
	[InlineData("2025-12-31")]
	[InlineData("2024-01-01")]
	public void Parse_FullDates_ReturnsDate(string version)
	{
		var result = VersionOrDate.Parse(version);
		result.SemVer.Should().BeNull();
		result.Date.Should().NotBeNull();
		result.Raw.Should().BeNull();
	}

	[Theory]
	[InlineData("2025-08")]
	[InlineData("2025-12")]
	[InlineData("2024-01")]
	public void Parse_YearMonthDates_ReturnsDate(string version)
	{
		var result = VersionOrDate.Parse(version);
		result.SemVer.Should().BeNull();
		result.Date.Should().NotBeNull();
		result.Raw.Should().BeNull();
	}

	[Theory]
	[InlineData("release-alpha")]
	[InlineData("custom-version")]
	public void Parse_NonStandardVersions_ReturnsRaw(string version)
	{
		var result = VersionOrDate.Parse(version);
		result.SemVer.Should().BeNull();
		result.Date.Should().BeNull();
		result.Raw.Should().Be(version);
	}

	[Theory]
	[InlineData("2025-08", "August 2025")]
	[InlineData("2025-12", "December 2025")]
	[InlineData("2025-01", "January 2025")]
	[InlineData("2024-06", "June 2024")]
	public void FormatDisplayVersion_YearMonth_ReturnsMonthYear(string version, string expected) =>
		VersionOrDate.FormatDisplayVersion(version).Should().Be(expected);

	[Theory]
	[InlineData("2025-08-05", "August 5, 2025")]
	[InlineData("2025-12-31", "December 31, 2025")]
	[InlineData("2025-01-01", "January 1, 2025")]
	public void FormatDisplayVersion_FullDate_ReturnsMonthDayYear(string version, string expected) =>
		VersionOrDate.FormatDisplayVersion(version).Should().Be(expected);

	[Theory]
	[InlineData("9.3.0", "9.3.0")]
	[InlineData("1.0.0", "1.0.0")]
	public void FormatDisplayVersion_Semver_ReturnsUnchanged(string version, string expected) =>
		VersionOrDate.FormatDisplayVersion(version).Should().Be(expected);

	[Theory]
	[InlineData("release-alpha", "release-alpha")]
	[InlineData("custom-version", "custom-version")]
	public void FormatDisplayVersion_RawString_ReturnsUnchanged(string version, string expected) =>
		VersionOrDate.FormatDisplayVersion(version).Should().Be(expected);

	[Fact]
	public void Parse_YearMonthDates_SortChronologically()
	{
		var dec = VersionOrDate.Parse("2025-12");
		var aug = VersionOrDate.Parse("2025-08");
		var jan = VersionOrDate.Parse("2025-01");

		dec.Should().BeGreaterThan(aug);
		aug.Should().BeGreaterThan(jan);
	}

	[Fact]
	public void Parse_YearMonthDates_SortWithFullDates()
	{
		var yearMonth = VersionOrDate.Parse("2025-08");
		var fullDate = VersionOrDate.Parse("2025-08-15");

		// Both are dates, should sort chronologically
		// 2025-08 parses as 2025-08-01, so 2025-08-15 > 2025-08
		fullDate.Should().BeGreaterThan(yearMonth);
	}
}
