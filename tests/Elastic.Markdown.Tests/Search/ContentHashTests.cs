// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search;

namespace Elastic.Markdown.Tests.Search;

public class ContentHashTests
{
	[Fact]
	public void CreateNormalized_SameContentDifferentWhitespace_ProducesSameHash()
	{
		var hash1 = ContentHash.CreateNormalized("hello world");
		var hash2 = ContentHash.CreateNormalized("hello   world");
		var hash3 = ContentHash.CreateNormalized("hello\tworld");
		var hash4 = ContentHash.CreateNormalized("hello\nworld");
		var hash5 = ContentHash.CreateNormalized("  hello  world  ");

		hash1.Should().Be(hash2);
		hash1.Should().Be(hash3);
		hash1.Should().Be(hash4);
		hash1.Should().Be(hash5);
	}

	[Fact]
	public void CreateNormalized_DifferentContent_ProducesDifferentHash()
	{
		var hash1 = ContentHash.CreateNormalized("hello world");
		var hash2 = ContentHash.CreateNormalized("hello earth");

		hash1.Should().NotBe(hash2);
	}

	[Fact]
	public void CreateNormalized_EmptyAndWhitespaceOnly_ProduceSameHash()
	{
		var hash1 = ContentHash.CreateNormalized("");
		var hash2 = ContentHash.CreateNormalized("   ");
		var hash3 = ContentHash.CreateNormalized("\n\t\r");

		hash1.Should().Be(hash2);
		hash1.Should().Be(hash3);
	}

	[Fact]
	public void CreateNormalized_NewlinesAndTabs_NormalizedToSpace()
	{
		var hash1 = ContentHash.CreateNormalized("line one\nline two\nline three");
		var hash2 = ContentHash.CreateNormalized("line one line two line three");

		hash1.Should().Be(hash2);
	}
}
