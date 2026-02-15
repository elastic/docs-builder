// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// Unit tests for ReleaseNotesSerialization.LoadPublishBlocker.
/// These tests verify the publish blocker loading functionality using the new 'rules:' format.
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
	public void LoadPublishBlocker_ReturnsNull_WhenNoRulesSection()
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
	public void LoadPublishBlocker_ParsesExcludeTypesOnly()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               exclude_types:
		                 - deprecation
		                 - known-issue
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(2)
			.And.Contain("deprecation")
			.And.Contain("known-issue");
		result.TypesMode.Should().Be(FieldMode.Exclude);
		result.Areas.Should().BeNull();
		result.HasBlockingRules.Should().BeTrue();
	}

	[Fact]
	public void LoadPublishBlocker_ParsesExcludeAreasOnly()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               exclude_areas:
		                 - Internal
		                 - Experimental
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Areas.Should().HaveCount(2)
			.And.Contain("Internal")
			.And.Contain("Experimental");
		result.AreasMode.Should().Be(FieldMode.Exclude);
		result.Types.Should().BeNull();
		result.HasBlockingRules.Should().BeTrue();
	}

	[Fact]
	public void LoadPublishBlocker_ParsesExcludeTypesAndAreas()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               exclude_types:
		                 - deprecation
		               exclude_areas:
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
	public void LoadPublishBlocker_ReturnsNull_WhenPublishHasEmptyLists()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               exclude_types: []
		               exclude_areas: []
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
		           rules:
		             create:
		               exclude: "some-label"
		             publish:
		               exclude_types:
		                 - deprecation
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("deprecation");
		result.Areas.Should().BeNull();
	}

	[Fact]
	public void LoadPublishBlocker_LoadsProductSpecificBlocker_WhenProductIdSpecified()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               exclude_types:
		                 - regression
		               products:
		                 kibana:
		                   exclude_types:
		                     - docs
		                   exclude_areas:
		                     - "Elastic Security solution"
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml", "kibana");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("docs");
		result.Areas.Should().HaveCount(1).And.Contain("Elastic Security solution");
	}

	[Fact]
	public void LoadPublishBlocker_FallsBackToGlobal_WhenProductNotFound()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               exclude_types:
		                 - regression
		               products:
		                 kibana:
		                   exclude_types:
		                     - docs
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml", "elasticsearch");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("regression");
	}

	[Fact]
	public void LoadPublishBlocker_FallsBackToGlobal_WhenProductIdNotSpecified()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               exclude_types:
		                 - regression
		               products:
		                 kibana:
		                   exclude_types:
		                     - docs
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("regression");
	}

	[Fact]
	public void LoadPublishBlocker_ProductIdIsCaseInsensitive()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               products:
		                 kibana:
		                   exclude_types:
		                     - docs
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml", "KIBANA");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(1).And.Contain("docs");
	}

	[Fact]
	public void LoadPublishBlocker_ProductOnly_NoGlobalFallback()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               products:
		                 kibana:
		                   exclude_types:
		                     - docs
		                   exclude_areas:
		                     - "Elastic Observability solution"
		                     - "Elastic Security solution"
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		// With product specified, should get product-specific blocker
		var resultWithProduct = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml", "kibana");
		resultWithProduct.Should().NotBeNull();
		resultWithProduct!.Types.Should().Contain("docs");
		resultWithProduct.Areas.Should().Contain("Elastic Observability solution");

		// Without product, should get null (no global blocker)
		var resultWithoutProduct = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");
		resultWithoutProduct.Should().BeNull();
	}

	[Fact]
	public void LoadPublishBlocker_ParsesIncludeAreas()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               include_areas:
		                 - Search
		                 - Monitoring
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Areas.Should().HaveCount(2).And.Contain("Search").And.Contain("Monitoring");
		result.AreasMode.Should().Be(FieldMode.Include);
	}

	[Fact]
	public void LoadPublishBlocker_ParsesIncludeTypes()
	{
		// language=yaml
		var yaml = """
		           rules:
		             publish:
		               include_types:
		                 - feature
		                 - bug-fix
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.Types.Should().HaveCount(2).And.Contain("feature").And.Contain("bug-fix");
		result.TypesMode.Should().Be(FieldMode.Include);
	}

	[Fact]
	public void LoadPublishBlocker_ParsesMatchAreas()
	{
		// language=yaml
		var yaml = """
		           rules:
		             match: all
		             publish:
		               exclude_areas:
		                 - Internal
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.MatchAreas.Should().Be(MatchMode.All);
	}

	[Fact]
	public void LoadPublishBlocker_MatchAreasInheritsFromGlobal()
	{
		// language=yaml
		var yaml = """
		           rules:
		             match: all
		             publish:
		               exclude_areas:
		                 - Internal
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.MatchAreas.Should().Be(MatchMode.All);
	}

	[Fact]
	public void LoadPublishBlocker_MatchAreasOverridesGlobal()
	{
		// language=yaml
		var yaml = """
		           rules:
		             match: all
		             publish:
		               match_areas: any
		               exclude_areas:
		                 - Internal
		           """;
		_fileSystem.AddFile("/docs/changelog.yml", new MockFileData(yaml));

		var result = ReleaseNotesSerialization.LoadPublishBlocker(_fileSystem, "/docs/changelog.yml");

		result.Should().NotBeNull();
		result!.MatchAreas.Should().Be(MatchMode.Any);
	}
}
