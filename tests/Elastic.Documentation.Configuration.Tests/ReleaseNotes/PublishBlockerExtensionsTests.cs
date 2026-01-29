// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// Unit tests for PublishBlockerExtensions.ShouldBlock.
/// These tests verify the blocking logic for changelog entries.
/// </summary>
public class PublishBlockerExtensionsTests
{
	[Fact]
	public void ShouldBlock_ReturnsFalse_WhenNoBlockingRules()
	{
		// Arrange
		var blocker = new PublishBlocker();
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature };

		// Act
		var result = blocker.ShouldBlock(entry);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void ShouldBlock_ReturnsTrue_WhenTypeIsBlocked()
	{
		// Arrange
		var blocker = new PublishBlocker { Types = ["regression", "known-issue"] };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Regression };

		// Act
		var result = blocker.ShouldBlock(entry);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_ReturnsFalse_WhenTypeIsNotBlocked()
	{
		// Arrange
		var blocker = new PublishBlocker { Types = ["regression", "known-issue"] };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature };

		// Act
		var result = blocker.ShouldBlock(entry);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void ShouldBlock_ReturnsTrue_WhenAreaIsBlocked()
	{
		// Arrange
		var blocker = new PublishBlocker { Areas = ["Internal", "Experimental"] };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Internal"] };

		// Act
		var result = blocker.ShouldBlock(entry);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_ReturnsFalse_WhenAreaIsNotBlocked()
	{
		// Arrange
		var blocker = new PublishBlocker { Areas = ["Internal", "Experimental"] };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search"] };

		// Act
		var result = blocker.ShouldBlock(entry);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void ShouldBlock_ReturnsTrue_WhenAnyAreaIsBlocked()
	{
		// Arrange
		var blocker = new PublishBlocker { Areas = ["Internal"] };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] };

		// Act
		var result = blocker.ShouldBlock(entry);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_IsCaseInsensitive_ForTypes()
	{
		// Arrange
		var blocker = new PublishBlocker { Types = ["REGRESSION"] };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Regression };

		// Act
		var result = blocker.ShouldBlock(entry);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_IsCaseInsensitive_ForAreas()
	{
		// Arrange
		var blocker = new PublishBlocker { Areas = ["INTERNAL"] };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature, Areas = ["internal"] };

		// Act
		var result = blocker.ShouldBlock(entry);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public void ShouldBlock_ReturnsFalse_WhenEntryHasNoAreas()
	{
		// Arrange
		var blocker = new PublishBlocker { Areas = ["Internal"] };
		var entry = new ChangelogEntry { Title = "Test", Type = ChangelogEntryType.Feature };

		// Act
		var result = blocker.ShouldBlock(entry);

		// Assert
		result.Should().BeFalse();
	}
}
