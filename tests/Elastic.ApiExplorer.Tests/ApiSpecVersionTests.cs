// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Tests;

public class ApiSpecVersionTests
{
	[Theory]
	[InlineData("main")]
	[InlineData("0")]
	[InlineData("8")]
	[InlineData("9")]
	[InlineData("10")]
	public void TryParse_CanonicalKeys_RoundTrip(string indexKey)
	{
		ApiSpecVersion.TryParse(indexKey, out var version).Should().BeTrue();
		version.IndexKey.Should().Be(indexKey);
		version.ToString().Should().Be(indexKey);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("master")]
	[InlineData("09")]
	[InlineData("00")]
	[InlineData("9.4")]
	[InlineData("next")]
	[InlineData("+9")]
	[InlineData(" 9")]
	public void TryParse_NonCanonicalKeys_ReturnsFalse(string? indexKey) => ApiSpecVersion.TryParse(indexKey, out _).Should().BeFalse();

	[Fact]
	public void Latest_IsDefaultAndHasMainIndexKey()
	{
		default(ApiSpecVersion).Should().Be(ApiSpecVersion.Latest);
		ApiSpecVersion.Latest.IsLatest.Should().BeTrue();
		ApiSpecVersion.Latest.TryGetMajor(out _).Should().BeFalse();
		ApiSpecVersion.Latest.IndexKey.Should().Be("main");
	}

	[Fact]
	public void Major_RejectsNegative() =>
		FluentActions.Invoking(() => ApiSpecVersion.Major(-1)).Should().Throw<ArgumentOutOfRangeException>();

	[Fact]
	public void CompareTo_LatestSortsAboveMajors()
	{
		var versions = new[] { ApiSpecVersion.Major(9), ApiSpecVersion.Latest, ApiSpecVersion.Major(10), ApiSpecVersion.Major(8) };

		versions.Max().Should().Be(ApiSpecVersion.Latest);
		new[] { ApiSpecVersion.Major(9), ApiSpecVersion.Major(10), ApiSpecVersion.Major(8) }.Max().Should().Be(ApiSpecVersion.Major(10));
	}
}
