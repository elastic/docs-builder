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
	public void SanitizeWhenInputHasNoControlCharsReturnsSameString()
	{
		// Arrange
		const string input = "Hello World! @#$%^&*()";

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
	public void SanitizeWhenInputHasTabRemovesIt()
	{
		// Arrange
		const string input = "Hello\tWorld";

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
	public void SanitizeWhenInputHasMultipleControlCharsRemovesAll()
	{
		// Arrange - includes \n, \r, \t, and escape char \x1B
		const string input = "Line1\nLine2\r\nLine3\tLine4\x1BLine5";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be("Line1Line2Line3Line4Line5");
	}

	[Fact]
	public void SanitizeWhenInputIsOnlyControlCharsReturnsEmptyString()
	{
		// Arrange
		const string input = "\r\n\t\x00\x1F";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public void SanitizePreservesSpaceAndPrintableChars()
	{
		// Arrange - space (0x20) is NOT a control char
		const string input = "Hello World! @#$%^&*() 123";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be(input);
	}

	[Fact]
	public void SanitizeRemovesNullChar()
	{
		// Arrange
		const string input = "Hello\x00World";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be("HelloWorld");
	}

	[Fact]
	public void SanitizeRemovesEscapeSequence()
	{
		// Arrange - \x1B is ESC, commonly used in ANSI escape sequences
		const string input = "Hello\x1B[31mRed\x1B[0mWorld";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be("Hello[31mRed[0mWorld");
	}

	[Theory]
	[InlineData("fake-log-entry\nINFO: Injected message", "fake-log-entryINFO: Injected message")]
	[InlineData("user-id-123\r\nERROR: Attack!", "user-id-123ERROR: Attack!")]
	[InlineData("input\twith\ttabs", "inputwithtabs")]
	[InlineData("escape\x1B[0msequence", "escape[0msequence")]
	public void SanitizePreventsLogForging(string maliciousInput, string expected)
	{
		// Act
		var result = LogSanitizer.Sanitize(maliciousInput);

		// Assert
		result.Should().Be(expected);
	}

	[Fact]
	public void SanitizeRemovesAllAsciiControlChars()
	{
		// Arrange - all ASCII control chars 0x00-0x1F
		var allControlChars = string.Create(32, 0, static (span, _) =>
		{
			for (var i = 0; i < 32; i++)
				span[i] = (char)i;
		});
		var input = $"Before{allControlChars}After";

		// Act
		var result = LogSanitizer.Sanitize(input);

		// Assert
		result.Should().Be("BeforeAfter");
	}
}
