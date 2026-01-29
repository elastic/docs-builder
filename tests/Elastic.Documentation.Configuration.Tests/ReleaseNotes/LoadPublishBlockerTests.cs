// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// Unit tests for ReleaseNotesYamlSerialization.LoadPublishBlocker.
/// These tests verify the publish blocker loading functionality that
/// was consolidated from the old PublishBlockerLoader.
/// </summary>
public class LoadPublishBlockerTests
{
	private readonly MockFileSystem _fileSystem = new();

	[Fact]
	public void LoadPublishBlocker_ReturnsNull_WhenFileDoesNotExist()
	{
		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/nonexistent/changelog.yml");

		result.Should().BeNull();
	}

	[Fact]
	public void LoadPublishBlocker_ReturnsNull_WhenFileIsEmpty()
	{
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(""));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().BeNull();
	}

	[Fact]
	public void LoadPublishBlocker_ReturnsNull_WhenNoBlockSection()
	{
		// language=yaml
		var yaml = """
		           project: test
		           other: value
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().BeNull();
	}

	[Fact]
	public void LoadPublishBlocker_ParsesTypesOnly()
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

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(2)
			.And.Contain("deprecation")
			.And.Contain("known-issue");
		result.Areas.Should().BeNull();
		result.HasBlockingRules.Should().BeTrue();
	}

	[Fact]
	public void LoadPublishBlocker_ParsesAreasOnly()
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

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Areas.Should().HaveCount(2)
			.And.Contain("Internal")
			.And.Contain("Experimental");
		result.Types.Should().BeNull();
		result.HasBlockingRules.Should().BeTrue();
	}

	[Fact]
	public void LoadPublishBlocker_ParsesTypesAndAreas()
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

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("deprecation");
		result.Areas.Should().HaveCount(1).And.Contain("Internal");
		result.HasBlockingRules.Should().BeTrue();
	}

	[Fact]
	public void LoadPublishBlocker_ReturnsNull_WhenPublishHasEmptyTypesAndAreas()
	{
		// language=yaml
		var yaml = """
		           block:
		             publish:
		               types: []
		               areas: []
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().BeNull();
	}

	[Fact]
	public void LoadPublishBlocker_IgnoresOtherProperties()
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

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("deprecation");
		result.Areas.Should().BeNull();
	}
}
