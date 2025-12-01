// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core;
using FluentAssertions;

namespace Elastic.Documentation.Api.Infrastructure.Tests;

public class LogSanitizerTests
{
	[Fact]
	public void SanitizeWhenInputIsNullReturnsEmptyString()
	{
		// Act
		var result = LogSanitizer.Sanitize(null);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public void SanitizeWhenInputIsEmptyReturnsEmptyString()
	{
		// Act
		var result = LogSanitizer.Sanitize(string.Empty);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public void SanitizeWhenInputHasNoNewlinesReturnsSameString()
	{
		// Arrange
		const string input = "Hello World";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().BeSameAs(input); // Same reference - no allocation
	}

	[Fact]
	public void SanitizeWhenInputHasCarriageReturnRemovesIt()
	{
		// Arrange
		const string input = "Hello\rWorld";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be("HelloWorld");
	}

	[Fact]
	public void SanitizeWhenInputHasNewlineRemovesIt()
	{
		// Arrange
		const string input = "Hello\nWorld";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be("HelloWorld");
	}

	[Fact]
	public void SanitizeWhenInputHasCrlfRemovesBoth()
	{
		// Arrange
		const string input = "Hello\r\nWorld";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be("HelloWorld");
	}

	[Fact]
	public void SanitizeWhenInputHasMultipleNewlinesRemovesAll()
	{
		// Arrange
		const string input = "Line1\nLine2\r\nLine3\rLine4";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be("Line1Line2Line3Line4");
	}

	[Fact]
	public void SanitizeWhenInputIsOnlyNewlinesReturnsEmptyString()
	{
		// Arrange
		const string input = "\r\n\r\n";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public void SanitizePreservesOtherSpecialCharacters()
	{
		// Arrange
		const string input = "Hello\tWorld! @#$%^&*()";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be(input);
	}

	[Theory]
	[InlineData("fake-log-entry\nINFO: Injected message", "fake-log-entryINFO: Injected message")]
	[InlineData("user-id-123\r\nERROR: Attack!", "user-id-123ERROR: Attack!")]
	public void SanitizePreventsLogForging(string maliciousInput, string expected)
	{
		// Act
		var result = LogSanitizer.Sanitize(maliciousInput);

		// Assert
		result.Should().Be(expected);
		result.Should().NotContain("\n");
		result.Should().NotContain("\r");
	}
}
