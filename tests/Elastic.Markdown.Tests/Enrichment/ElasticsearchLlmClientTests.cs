// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Enrichment;

/// <summary>
/// Tests for the chunking logic in ElasticsearchLlmClient.
/// These test the pure SplitIntoChunks function without needing network mocks.
/// </summary>
public class ChunkingTests
{
	// Use reflection to access the private static method
	private static List<string> SplitIntoChunks(string body)
	{
		var type = typeof(Elastic.Markdown.Exporters.Elasticsearch.Enrichment.ElasticsearchLlmClient);
		var method = type.GetMethod("SplitIntoChunks", BindingFlags.NonPublic | BindingFlags.Static);
		return (List<string>)method!.Invoke(null, [body])!;
	}

	[Fact]
	public void SplitIntoChunks_SmallDocument_ReturnsSingleChunk()
	{
		// Arrange - document smaller than MaxChunkSize (200K)
		var body = string.Join("\n\n", Enumerable.Range(1, 100).Select(i => $"Paragraph {i} content here."));

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(1);
		chunks[0].Should().Contain("Paragraph 1");
		chunks[0].Should().Contain("Paragraph 100");
	}

	[Fact]
	public void SplitIntoChunks_500K_SplitsIntoFourChunks()
	{
		// Arrange - 50 paragraphs × 10K = ~500K chars
		var paragraph = new string('x', 10_000);
		var body = string.Join("\n\n", Enumerable.Range(1, 50).Select(_ => paragraph));

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(4);
	}

	[Fact]
	public void SplitIntoChunks_1M_SplitsIntoSevenChunks()
	{
		// Arrange - 50 paragraphs × 20K = ~1M chars
		// numChunks = ceil(1M / 200K) = 6, targetSize ~166K
		// With 20K paragraphs: 8 fit per chunk, 50/8 = 6.25 → 7 chunks
		var paragraph = new string('y', 20_000);
		var body = string.Join("\n\n", Enumerable.Range(1, 50).Select(_ => paragraph));

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(7);
	}

	[Fact]
	public void SplitIntoChunks_NoParagraphBreaks_ReturnsSingleChunk()
	{
		// Arrange - 300K chars with no paragraph breaks
		var body = new string('z', 300_000);

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(1);
	}

	[Fact]
	public void SplitIntoChunks_PreservesAllContent()
	{
		// Arrange - 30 small paragraphs with unique identifiers
		var paragraphs = Enumerable.Range(1, 30).Select(i => $"Paragraph {i} id=#{i}#").ToList();
		var body = string.Join("\n\n", paragraphs);

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(1);
		foreach (var i in Enumerable.Range(1, 30))
			chunks[0].Should().Contain($"id=#{i}#");
	}

	[Fact]
	public void SplitIntoChunks_FiltersEmptyParagraphs()
	{
		// Arrange - consecutive newlines create empty paragraphs that get filtered
		var body = "First\n\n\n\nSecond\n\n\n\n\n\nThird";

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(1);
		chunks[0].Should().Be("First\n\nSecond\n\nThird");
	}

	[Fact]
	public void SplitIntoChunks_250K_SplitsIntoTwoChunks()
	{
		// Arrange - 50 paragraphs × 5K = ~250K chars
		var paragraph = new string('a', 5_000);
		var body = string.Join("\n\n", Enumerable.Range(1, 50).Select(_ => paragraph));

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(2);
	}

	[Fact]
	public void SplitIntoChunks_FinalFlush_CapturesRemainingContent()
	{
		// Arrange - content that doesn't trigger mid-loop flush
		// but must be captured by final FlushCurrentChunk() call
		var body = "First\n\nSecond\n\nThird";

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert - all content captured in final flush
		chunks.Should().HaveCount(1);
		chunks[0].Should().Contain("First");
		chunks[0].Should().Contain("Second");
		chunks[0].Should().Contain("Third");
	}

	// === Boundary tests around MaxChunkSize (200,000) ===

	[Fact]
	public void SplitIntoChunks_Boundary_Minus100_SingleChunk()
	{
		// Arrange - 199,900 chars (100 below boundary)
		var body = new string('a', 199_900);

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(1);
	}

	[Fact]
	public void SplitIntoChunks_Boundary_Minus1_SingleChunk()
	{
		// Arrange - 199,999 chars (1 below boundary)
		var body = new string('b', 199_999);

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(1);
	}

	[Fact]
	public void SplitIntoChunks_Boundary_Exact_SingleChunk()
	{
		// Arrange - exactly 200,000 chars (at boundary)
		var body = new string('c', 200_000);

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(1);
	}

	[Fact]
	public void SplitIntoChunks_Boundary_Plus1_TwoChunks()
	{
		// Arrange - 200,001 chars (1 above boundary)
		// numChunks = ceil(200001 / 200000) = 2
		// With paragraphs, this splits into 2 chunks
		var paragraph = new string('d', 100_000);
		var body = $"{paragraph}\n\n{paragraph}a"; // 200,003 chars

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(2);
	}

	[Fact]
	public void SplitIntoChunks_Boundary_Plus100_TwoChunks()
	{
		// Arrange - 200,100 chars (100 above boundary)
		var paragraph = new string('e', 100_000);
		var body = $"{paragraph}\n\n{paragraph}{new string('f', 98)}"; // 200,100 chars

		// Act
		var chunks = SplitIntoChunks(body);

		// Assert
		chunks.Should().HaveCount(2);
	}

	// === Content integrity tests ===

	[Fact]
	public void SplitIntoChunks_ContentIntegrity_NothingLost()
	{
		// Arrange - paragraphs with unique markers
		var paragraphs = Enumerable.Range(1, 100).Select(i => $"[P{i}]{new string('x', 5_000)}[/P{i}]").ToList();
		var body = string.Join("\n\n", paragraphs);

		// Act
		var chunks = SplitIntoChunks(body);
		var reassembled = string.Join("\n\n", chunks);

		// Assert - every paragraph marker present
		foreach (var i in Enumerable.Range(1, 100))
		{
			reassembled.Should().Contain($"[P{i}]");
			reassembled.Should().Contain($"[/P{i}]");
		}
	}

	[Fact]
	public void SplitIntoChunks_ContentIntegrity_NoDuplicates()
	{
		// Arrange - paragraphs with unique IDs
		var paragraphs = Enumerable.Range(1, 50).Select(i => $"ID={i:D5}|{new string('y', 8_000)}").ToList();
		var body = string.Join("\n\n", paragraphs);

		// Act
		var chunks = SplitIntoChunks(body);
		var reassembled = string.Join("\n\n", chunks);

		// Assert - each ID appears exactly once
		foreach (var i in Enumerable.Range(1, 50))
		{
			var id = $"ID={i:D5}|";
			var count = CountOccurrences(reassembled, id);
			count.Should().Be(1, $"ID {i} should appear exactly once");
		}
	}

	[Fact]
	public void SplitIntoChunks_ContentIntegrity_PreservesOrder()
	{
		// Arrange - numbered paragraphs
		var paragraphs = Enumerable.Range(1, 60).Select(i => $"SEQ{i:D4}").ToList();
		var body = string.Join("\n\n", paragraphs);

		// Act
		var chunks = SplitIntoChunks(body);
		var reassembled = string.Join("\n\n", chunks);

		// Assert - sequence numbers appear in order
		var lastIndex = -1;
		foreach (var i in Enumerable.Range(1, 60))
		{
			var marker = $"SEQ{i:D4}";
			var index = reassembled.IndexOf(marker, StringComparison.Ordinal);
			index.Should().BeGreaterThan(lastIndex, $"SEQ{i} should come after SEQ{i - 1}");
			lastIndex = index;
		}
	}

	[Fact]
	public void SplitIntoChunks_ContentIntegrity_ExactMatch()
	{
		// Arrange - small enough to fit in one chunk
		var paragraphs = Enumerable.Range(1, 20).Select(i => $"Para {i}").ToList();
		var body = string.Join("\n\n", paragraphs);

		// Act
		var chunks = SplitIntoChunks(body);
		var reassembled = string.Join("\n\n", chunks);

		// Assert - exact match
		reassembled.Should().Be(body);
	}

	private static int CountOccurrences(string text, string pattern)
	{
		var count = 0;
		var index = 0;
		while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
		{
			count++;
			index += pattern.Length;
		}
		return count;
	}
}
