// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Evaluation;

public class GithubDecisionMetadataTests
{
	[Fact]
	public void SerializationRoundTrip_WithAllFields_PreservesValues()
	{
		var metadata = new GithubDecisionMetadata
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
			CommitOutcome = CommitOutcome.Committed,
			CommittedFile = "changelogs/1234.yaml",
			CreateRules = new CreateRules
			{
				Labels = ["changelog:skip", "no-changelog"],
				Mode = FieldMode.Exclude,
				Match = MatchMode.Any,
				ByProduct = new Dictionary<string, CreateRules>
				{
					["elasticsearch"] = new() { Labels = ["es:skip"], Mode = FieldMode.Exclude, Match = MatchMode.All }
				}
			}
		};

		var json = JsonSerializer.Serialize(metadata, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);
		var deserialized = JsonSerializer.Deserialize(json, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);

		deserialized.Should().NotBeNull();
		deserialized.PrNumber.Should().Be(42);
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
		deserialized.CommitOutcome.Should().Be(CommitOutcome.Committed);
		deserialized.CommittedFile.Should().Be("changelogs/1234.yaml");
		deserialized.CreateRules.Should().NotBeNull();
		deserialized.CreateRules.Labels.Should().BeEquivalentTo(["changelog:skip", "no-changelog"]);
		deserialized.CreateRules.Mode.Should().Be(FieldMode.Exclude);
		deserialized.CreateRules.Match.Should().Be(MatchMode.Any);
		deserialized.CreateRules.ByProduct.Should().ContainKey("elasticsearch");
	}

	[Fact]
	public void SerializationRoundTrip_WithNullOptionalFields_PreservesNulls()
	{
		var metadata = new GithubDecisionMetadata
		{
			PrNumber = 1,
			HeadRef = "main",
			HeadSha = "deadbeef",
			Status = "no-label",
			IsFork = false,
			CanCommit = false,
			MaintainerCanModify = false
		};

		var json = JsonSerializer.Serialize(metadata, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);
		var deserialized = JsonSerializer.Deserialize(json, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);

		deserialized.Should().NotBeNull();
		deserialized.PrNumber.Should().Be(1);
		deserialized.IsFork.Should().BeFalse();
		deserialized.CanCommit.Should().BeFalse();
		deserialized.HeadRepo.Should().BeNull();
		deserialized.LabelTable.Should().BeNull();
		deserialized.ProductLabelTable.Should().BeNull();
		deserialized.SkipLabels.Should().BeNull();
		deserialized.ConfigFile.Should().BeNull();
		deserialized.ChangelogDir.Should().BeNull();
		deserialized.CommitOutcome.Should().BeNull();
		deserialized.CommittedFile.Should().BeNull();
		deserialized.CreateRules.Should().BeNull();
	}

	[Fact]
	public void Serialization_UsesSnakeCasePropertyNames()
	{
		var metadata = new GithubDecisionMetadata
		{
			PrNumber = 99,
			HeadRef = "fix/bug",
			HeadSha = "aabbcc",
			Status = "skipped",
			IsFork = true,
			CanCommit = false,
			MaintainerCanModify = true,
			HeadRepo = "user/repo",
			ChangelogDir = "changelogs",
			CommitOutcome = CommitOutcome.Failed,
			CommittedFile = "changelogs/99.yaml"
		};

		var json = JsonSerializer.Serialize(metadata, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);

		json.Should().Contain("\"pr_number\"");
		json.Should().Contain("\"head_ref\"");
		json.Should().Contain("\"head_sha\"");
		json.Should().Contain("\"is_fork\"");
		json.Should().Contain("\"can_commit\"");
		json.Should().Contain("\"maintainer_can_modify\"");
		json.Should().Contain("\"head_repo\"");
		json.Should().Contain("\"changelog_dir\"");
		json.Should().Contain("\"commit_outcome\"");
		json.Should().Contain("\"committed_file\"");
		json.Should().NotContain("\"PrNumber\"");
		json.Should().NotContain("\"IsFork\"");
		json.Should().NotContain("\"CanCommit\"");
	}

	[Fact]
	public void Serialization_EnumsUseStringValues()
	{
		var metadata = new GithubDecisionMetadata
		{
			PrNumber = 1,
			HeadRef = "main",
			HeadSha = "abc",
			Status = "success",
			IsFork = false,
			CanCommit = true,
			MaintainerCanModify = false,
			CommitOutcome = CommitOutcome.Committed,
			CreateRules = new CreateRules { Labels = ["skip"], Mode = FieldMode.Include, Match = MatchMode.All }
		};

		var json = JsonSerializer.Serialize(metadata, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);

		json.Should().Contain("\"Include\"");
		json.Should().Contain("\"All\"");
		json.Should().Contain("\"Committed\"");
	}

	[Fact]
	public void SerializationRoundTrip_WithEntryFindings_PreservesFindings()
	{
		var metadata = new GithubDecisionMetadata
		{
			PrNumber = 42,
			HeadRef = "feature/test",
			HeadSha = "abc123",
			Status = "entries-invalid",
			IsFork = false,
			CanCommit = true,
			MaintainerCanModify = false,
			Gate = ValidationGate.Entries,
			EntryFindings =
			[
				new EntryFinding { File = "docs/changelog/42.yaml", Severity = "Error", Message = "title is required" },
				new EntryFinding { File = "docs/changelog/43.yaml", Severity = "Warning", Message = "title exceeds 80 characters" }
			]
		};

		var json = JsonSerializer.Serialize(metadata, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);
		var deserialized = JsonSerializer.Deserialize(json, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);

		deserialized.Should().NotBeNull();
		deserialized.Gate.Should().Be(ValidationGate.Entries);
		deserialized.EntryFindings.Should().HaveCount(2);
		deserialized.EntryFindings![0].File.Should().Be("docs/changelog/42.yaml");
		deserialized.EntryFindings[0].Severity.Should().Be("Error");
		deserialized.EntryFindings[0].Message.Should().Be("title is required");
		deserialized.EntryFindings[1].Severity.Should().Be("Warning");
	}

	[Fact]
	public void Deserialization_OldMetadataWithoutEntryFindings_EntryFindingsNull()
	{
		const string legacyJson =
			"""
			{
			  "pr_number": 7,
			  "head_ref": "fix/typo",
			  "head_sha": "cafebabe",
			  "status": "no-label",
			  "is_fork": false,
			  "can_commit": true,
			  "maintainer_can_modify": false
			}
			""";

		var deserialized = JsonSerializer.Deserialize(legacyJson, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);

		deserialized.Should().NotBeNull();
		deserialized.EntryFindings.Should().BeNull();
		deserialized.Gate.Should().BeNull();
	}

	[Fact]
	public void Deserialization_OldMetadataWithoutCommitOutcomeFields_Succeeds()
	{
		// Verify wire-safety: a metadata.json written before the CommitOutcome/CommittedFile
		// fields were added still deserializes cleanly (fields default to null).
		const string legacyJson =
			"""
			{
			  "pr_number": 7,
			  "head_ref": "fix/typo",
			  "head_sha": "cafebabe",
			  "status": "proceed",
			  "is_fork": false,
			  "can_commit": true,
			  "maintainer_can_modify": false
			}
			""";

		var deserialized = JsonSerializer.Deserialize(legacyJson, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);

		deserialized.Should().NotBeNull();
		deserialized.PrNumber.Should().Be(7);
		deserialized.CommitOutcome.Should().BeNull();
		deserialized.CommittedFile.Should().BeNull();
	}
}
