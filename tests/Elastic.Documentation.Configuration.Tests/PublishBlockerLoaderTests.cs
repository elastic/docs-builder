// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.Changelog;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class PublishBlockerLoaderTests
{
	private readonly MockFileSystem _fileSystem = new();

	[Fact]
	public void ReturnsNull_WhenFileDoesNotExist()
	{
		var result = PublishBlockerLoader.Load(_fileSystem, "/nonexistent/changelog.yml");

		result.Should().BeNull();
	}

	[Fact]
	public void ReturnsNull_WhenFileIsEmpty()
	{
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(""));

		var result = PublishBlockerLoader.Load(_fileSystem, "/docs/changelog.yml");

		result.Should().BeNull();
	}

	[Fact]
	public void ReturnsNull_WhenNoBlockSection()
	{
		// language=yaml
		var yaml = """
		           project: test
		           other: value
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = PublishBlockerLoader.Load(_fileSystem, "/docs/changelog.yml");

		result.Should().BeNull();
	}

	[Fact]
	public void ReturnsNull_WhenBlockSectionHasNoPublish()
	{
		// language=yaml
		var yaml = """
		           block:
		             create: some-label
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = PublishBlockerLoader.Load(_fileSystem, "/docs/changelog.yml");

		result.Should().BeNull();
	}

	[Fact]
	public void ParsesTypesOnly()
	{
		// language=yaml
		var yaml = """
		           block:
		             publish:
		               types:
		                 - deprecation
		                 - known-issue
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = PublishBlockerLoader.Load(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(2)
			.And.Contain("deprecation")
			.And.Contain("known-issue");
		result.Areas.Should().BeNull();
		result.HasBlockingRules.Should().BeTrue();
	}

	[Fact]
	public void ParsesAreasOnly()
	{
		// language=yaml
		var yaml = """
		           block:
		             publish:
		               areas:
		                 - Internal
		                 - Experimental
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = PublishBlockerLoader.Load(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Areas.Should().HaveCount(2)
			.And.Contain("Internal")
			.And.Contain("Experimental");
		result.Types.Should().BeNull();
		result.HasBlockingRules.Should().BeTrue();
	}

	[Fact]
	public void ParsesTypesAndAreas()
	{
		// language=yaml
		var yaml = """
		           block:
		             publish:
		               types:
		                 - deprecation
		               areas:
		                 - Internal
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = PublishBlockerLoader.Load(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("deprecation");
		result.Areas.Should().HaveCount(1).And.Contain("Internal");
		result.HasBlockingRules.Should().BeTrue();
	}

	[Fact]
	public void ReturnsNull_WhenPublishHasEmptyTypesAndAreas()
	{
		// language=yaml
		var yaml = """
		           block:
		             publish:
		               types: []
		               areas: []
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = PublishBlockerLoader.Load(_fileSystem, "/docs/changelog.yml");

		result.Should().BeNull();
	}

	[Fact]
	public void IgnoresOtherProperties()
	{
		// language=yaml
		var yaml = """
		           project: test-project
		           pivot:
		             types:
		               feature: labels
		           lifecycles:
		             - ga
		             - beta
		           block:
		             create: some-label
		             publish:
		               types:
		                 - deprecation
		             product:
		               elasticsearch:
		                 create: es-label
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = PublishBlockerLoader.Load(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("deprecation");
		result.Areas.Should().BeNull();
	}

	[Fact]
	public void HandlesUnderscoreNaming()
	{
		// The YAML deserializer uses underscored naming convention
		// language=yaml
		var yaml = """
		           block:
		             publish:
		               types:
		                 - known_issue
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = PublishBlockerLoader.Load(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("known_issue");
	}
}
