// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Changelog.Evaluation;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Evaluation;

public class ChangelogArtifactMetadataTests
{
	[Fact]
	public void SerializationRoundTrip_WithAllFields_PreservesValues()
	{
		var metadata = new ChangelogArtifactMetadata
		{
			PrNumber = 42,
			HeadRef = "feature/test",
			HeadSha = "abc123def456",
			Status = "success",
			IsFork = true,
			CanCommit = true,
			MaintainerCanModify = true,
			HeadRepo = "contributor/repo",
			LabelTable = "| label | type |\n| --- | --- |",
			ProductLabelTable = "| label | product |\n| --- | --- |",
			SkipLabels = "changelog:skip,skip-ci",
			ConfigFile = "changelog.yml",
			ChangelogDir = "changelogs",
			CreateRules = new CreateRules
			{
				Labels = ["changelog:skip", "no-changelog"],
				Mode = FieldMode.Exclude,
				Match = MatchMode.Any,
				ByProduct = new Dictionary<string, CreateRules>
				{
					["elasticsearch"] = new()
					{
						Labels = ["es:skip"],
						Mode = FieldMode.Exclude,
						Match = MatchMode.All
					}
				}
			}
		};

		var json = JsonSerializer.Serialize(metadata, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);
		var deserialized = JsonSerializer.Deserialize(json, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);

		deserialized.Should().NotBeNull();
		deserialized!.PrNumber.Should().Be(42);
		deserialized.HeadRef.Should().Be("feature/test");
		deserialized.HeadSha.Should().Be("abc123def456");
		deserialized.Status.Should().Be("success");
		deserialized.IsFork.Should().BeTrue();
		deserialized.CanCommit.Should().BeTrue();
		deserialized.MaintainerCanModify.Should().BeTrue();
		deserialized.HeadRepo.Should().Be("contributor/repo");
		deserialized.LabelTable.Should().Be("| label | type |\n| --- | --- |");
		deserialized.ProductLabelTable.Should().Be("| label | product |\n| --- | --- |");
		deserialized.SkipLabels.Should().Be("changelog:skip,skip-ci");
		deserialized.ConfigFile.Should().Be("changelog.yml");
		deserialized.ChangelogDir.Should().Be("changelogs");
		deserialized.CreateRules.Should().NotBeNull();
		deserialized.CreateRules!.Labels.Should().BeEquivalentTo(["changelog:skip", "no-changelog"]);
		deserialized.CreateRules.Mode.Should().Be(FieldMode.Exclude);
		deserialized.CreateRules.Match.Should().Be(MatchMode.Any);
		deserialized.CreateRules.ByProduct.Should().ContainKey("elasticsearch");
	}

	[Fact]
	public void SerializationRoundTrip_WithNullOptionalFields_PreservesNulls()
	{
		var metadata = new ChangelogArtifactMetadata
		{
			PrNumber = 1,
			HeadRef = "main",
			HeadSha = "deadbeef",
			Status = "no-label",
			IsFork = false,
			CanCommit = false,
			MaintainerCanModify = false
		};

		var json = JsonSerializer.Serialize(metadata, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);
		var deserialized = JsonSerializer.Deserialize(json, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);

		deserialized.Should().NotBeNull();
		deserialized!.PrNumber.Should().Be(1);
		deserialized.IsFork.Should().BeFalse();
		deserialized.CanCommit.Should().BeFalse();
		deserialized.HeadRepo.Should().BeNull();
		deserialized.LabelTable.Should().BeNull();
		deserialized.ProductLabelTable.Should().BeNull();
		deserialized.SkipLabels.Should().BeNull();
		deserialized.ConfigFile.Should().BeNull();
		deserialized.ChangelogDir.Should().BeNull();
		deserialized.CreateRules.Should().BeNull();
	}

	[Fact]
	public void Serialization_UsesSnakeCasePropertyNames()
	{
		var metadata = new ChangelogArtifactMetadata
		{
			PrNumber = 99,
			HeadRef = "fix/bug",
			HeadSha = "aabbcc",
			Status = "skipped",
			IsFork = true,
			CanCommit = false,
			MaintainerCanModify = true,
			HeadRepo = "user/repo",
			ChangelogDir = "changelogs"
		};

		var json = JsonSerializer.Serialize(metadata, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);

		json.Should().Contain("\"pr_number\"");
		json.Should().Contain("\"head_ref\"");
		json.Should().Contain("\"head_sha\"");
		json.Should().Contain("\"is_fork\"");
		json.Should().Contain("\"can_commit\"");
		json.Should().Contain("\"maintainer_can_modify\"");
		json.Should().Contain("\"head_repo\"");
		json.Should().Contain("\"changelog_dir\"");
		json.Should().NotContain("\"PrNumber\"");
		json.Should().NotContain("\"IsFork\"");
		json.Should().NotContain("\"CanCommit\"");
	}

	[Fact]
	public void Serialization_EnumsUseStringValues()
	{
		var metadata = new ChangelogArtifactMetadata
		{
			PrNumber = 1,
			HeadRef = "main",
			HeadSha = "abc",
			Status = "success",
			IsFork = false,
			CanCommit = true,
			MaintainerCanModify = false,
			CreateRules = new CreateRules
			{
				Labels = ["skip"],
				Mode = FieldMode.Include,
				Match = MatchMode.All
			}
		};

		var json = JsonSerializer.Serialize(metadata, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);

		json.Should().Contain("\"Include\"");
		json.Should().Contain("\"All\"");
	}
}
