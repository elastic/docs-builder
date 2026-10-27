// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// Unit tests for VersionOrDate parsing and display formatting.
/// </summary>
public class VersionOrDateTests
{
	[Test]
	[Arguments("9.3.0")]
	[Arguments("1.0.0")]
	[Arguments("10.0.0-beta1")]
	public void Parse_SemverVersions_ReturnsSemVer(string version)
	{
		var result = VersionOrDate.Parse(version);
		result.SemVer.Should().NotBeNull();
		result.Date.Should().BeNull();
		result.Raw.Should().BeNull();
	}

	[Test]
	[Arguments("2025-08-05")]
	[Arguments("2025-12-31")]
	[Arguments("2024-01-01")]
	public void Parse_FullDates_ReturnsDate(string version)
	{
		var result = VersionOrDate.Parse(version);
		result.SemVer.Should().BeNull();
		result.Date.Should().NotBeNull();
		result.Raw.Should().BeNull();
	}

	[Test]
	[Arguments("2025-08")]
	[Arguments("2025-12")]
	[Arguments("2024-01")]
	public void Parse_YearMonthDates_ReturnsDate(string version)
	{
		var result = VersionOrDate.Parse(version);
		result.SemVer.Should().BeNull();
		result.Date.Should().NotBeNull();
		result.Raw.Should().BeNull();
	}

	[Test]
	[Arguments("release-alpha")]
	[Arguments("custom-version")]
	public void Parse_NonStandardVersions_ReturnsRaw(string version)
	{
		var result = VersionOrDate.Parse(version);
		result.SemVer.Should().BeNull();
		result.Date.Should().BeNull();
		result.Raw.Should().Be(version);
	}

	[Test]
	[Arguments("2025-08", "August 2025")]
	[Arguments("2025-12", "December 2025")]
	[Arguments("2025-01", "January 2025")]
	[Arguments("2024-06", "June 2024")]
	public void FormatDisplayVersion_YearMonth_ReturnsMonthYear(string version, string expected) =>
		VersionOrDate.FormatDisplayVersion(version).Should().Be(expected);

	[Test]
	[Arguments("2025-08-05", "August 5, 2025")]
	[Arguments("2025-12-31", "December 31, 2025")]
	[Arguments("2025-01-01", "January 1, 2025")]
	public void FormatDisplayVersion_FullDate_ReturnsMonthDayYear(string version, string expected) =>
		VersionOrDate.FormatDisplayVersion(version).Should().Be(expected);

	[Test]
	[Arguments("9.3.0", "9.3.0")]
	[Arguments("1.0.0", "1.0.0")]
	public void FormatDisplayVersion_Semver_ReturnsUnchanged(string version, string expected) =>
		VersionOrDate.FormatDisplayVersion(version).Should().Be(expected);

	[Test]
	[Arguments("release-alpha", "release-alpha")]
	[Arguments("custom-version", "custom-version")]
	public void FormatDisplayVersion_RawString_ReturnsUnchanged(string version, string expected) =>
		VersionOrDate.FormatDisplayVersion(version).Should().Be(expected);

	[Test]
	public void Parse_YearMonthDates_SortChronologically()
	{
		var dec = VersionOrDate.Parse("2025-12");
		var aug = VersionOrDate.Parse("2025-08");
		var jan = VersionOrDate.Parse("2025-01");

		dec.Should().BeGreaterThan(aug);
		aug.Should().BeGreaterThan(jan);
	}

	[Test]
	public void Parse_YearMonthDates_SortWithFullDates()
	{
		var yearMonth = VersionOrDate.Parse("2025-08");
		var fullDate = VersionOrDate.Parse("2025-08-15");

		// Both are dates, should sort chronologically
		// 2025-08 parses as 2025-08-01, so 2025-08-15 > 2025-08
		fullDate.Should().BeGreaterThan(yearMonth);
	}
}
