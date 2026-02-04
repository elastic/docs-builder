// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Codex;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests.Codex;

public class PathValidatorTests
{
	[Theory]
	[InlineData("valid-name")]
	[InlineData("valid_name")]
	[InlineData("valid123")]
	[InlineData("Valid-Name")]
	public void ValidatePathComponent_ValidInput_ReturnsInput(string input)
	{
		var result = PathValidator.ValidatePathComponent(input, "test");
		result.Should().Be(input);
	}

	[Theory]
	[InlineData("../etc/passwd")]
	[InlineData("..")]
	[InlineData("../../test")]
	[InlineData("test/../../../etc")]
	[InlineData("test/..")]
	public void ValidatePathComponent_PathTraversal_ThrowsException(string input)
	{
		var act = () => PathValidator.ValidatePathComponent(input, "test");
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData("/absolute/path")]
	public void ValidatePathComponent_AbsolutePath_ThrowsException(string input)
	{
		var act = () => PathValidator.ValidatePathComponent(input, "test");
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData(null)]
	public void ValidatePathComponent_EmptyOrNull_ThrowsException(string? input)
	{
		var act = () => PathValidator.ValidatePathComponent(input!, "test");
		act.Should().Throw<ArgumentException>()
			.WithMessage("*null or whitespace*");
	}

	[Theory]
	[InlineData("docs")]
	[InlineData("docs/api")]
	[InlineData("path/to/docs")]
	public void ValidateRelativePath_ValidInput_ReturnsInput(string input)
	{
		var result = PathValidator.ValidateRelativePath(input, "test");
		result.Should().Be(input);
	}

	[Theory]
	[InlineData("docs/../etc")]
	[InlineData("../etc/passwd")]
	[InlineData("docs/../../etc")]
	[InlineData("test/..")]
	public void ValidateRelativePath_PathTraversal_ThrowsException(string input)
	{
		var act = () => PathValidator.ValidateRelativePath(input, "test");
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData("docs/.")]
	[InlineData("./docs")]
	[InlineData("docs/./api")]
	public void ValidateRelativePath_CurrentDirectoryReference_ThrowsException(string input)
	{
		var act = () => PathValidator.ValidateRelativePath(input, "test");
		act.Should().Throw<ArgumentException>();
	}
}
