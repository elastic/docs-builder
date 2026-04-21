// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class NaturalStringComparerTests
{
	private static readonly NaturalStringComparer Comparer = NaturalStringComparer.Instance;

	[Fact]
	public void PureAlphabeticalStringsCompareNormally()
	{
		Comparer.Compare("abc", "def").Should().BeNegative();
		Comparer.Compare("def", "abc").Should().BePositive();

		var same = "abc";
		Comparer.Compare(same, new string(same)).Should().Be(0);
	}

	[Fact]
	public void SingleDigitVersionsSort()
	{
		Comparer.Compare("v1", "v2").Should().BeNegative();
		Comparer.Compare("v2", "v1").Should().BePositive();

		var same = "v1";
		Comparer.Compare(same, new string(same)).Should().Be(0);
	}

	[Fact]
	public void MultiDigitNumbersSortNumerically()
	{
		Comparer.Compare("v2", "v10").Should().BeNegative();
		Comparer.Compare("v10", "v2").Should().BePositive();
	}

	[Fact]
	public void VersionNumbersWithUnderscores()
	{
		var files = new[] { "3_10_0.md", "3_2_0.md", "3_1_0.md", "3_0_0.md" };
		var sorted = files.OrderBy(f => f, Comparer).ToList();

		sorted.Should().BeEquivalentTo(
			["3_0_0.md", "3_1_0.md", "3_2_0.md", "3_10_0.md"],
			options => options.WithStrictOrdering()
		);
	}

	[Fact]
	public void VersionNumbersWithDots()
	{
		var files = new[] { "3.10.0.md", "3.2.0.md", "3.1.0.md", "3.0.0.md" };
		var sorted = files.OrderBy(f => f, Comparer).ToList();

		sorted.Should().BeEquivalentTo(
			["3.0.0.md", "3.1.0.md", "3.2.0.md", "3.10.0.md"],
			options => options.WithStrictOrdering()
		);
	}

	[Fact]
	public void NullsAreHandled()
	{
		string? nullA = null;
		string? nullB = null;
		Comparer.Compare(nullA, nullB).Should().Be(0);
		Comparer.Compare(nullA, "a").Should().BeNegative();
		Comparer.Compare("a", nullB).Should().BePositive();
	}

	[Fact]
	public void SameValueReturnsZero()
	{
		var a = "test";
		var b = new string(a);
		Comparer.Compare(a, b).Should().Be(0);
	}

	[Fact]
	public void MixedPrefixesWithNumbers()
	{
		var files = new[] { "file2.md", "file10.md", "file1.md" };
		var sorted = files.OrderBy(f => f, Comparer).ToList();

		sorted.Should().BeEquivalentTo(
			["file1.md", "file2.md", "file10.md"],
			options => options.WithStrictOrdering()
		);
	}

	[Fact]
	public void DifferentLengthStrings()
	{
		Comparer.Compare("v1", "v1a").Should().BeNegative();
		Comparer.Compare("v1a", "v1").Should().BePositive();
	}
}
