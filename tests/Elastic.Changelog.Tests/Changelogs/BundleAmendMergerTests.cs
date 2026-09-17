// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleAmendMergerTests
{
	[Fact]
	public void MergeEntries_AppliesExclusionsBeforeAdditionsWithinAmend()
	{
		var parent = new List<BundledEntry> { CreateFileEntry("keep.yaml", "aaa"), CreateFileEntry("remove.yaml", "bbb") };

		var amend = new Bundle { ExcludeEntries = [CreateFileEntry("remove.yaml", "bbb")], Entries = [CreateFileEntry("add.yaml", "ccc")] };

		var merged = BundleAmendMerger.MergeEntries(parent, [amend]);

		merged.Should().HaveCount(2);
		merged.Should().Contain(e => e.File!.Name == "keep.yaml");
		merged.Should().Contain(e => e.File!.Name == "add.yaml");
		merged.Should().NotContain(e => e.File!.Name == "remove.yaml");
	}

	[Fact]
	public void MergeEntries_AppliesAmendsInOrder()
	{
		var parent = new List<BundledEntry> { CreateFileEntry("one.yaml", "1") };

		var amend1 = new Bundle { Entries = [CreateFileEntry("two.yaml", "2")] };
		var amend2 = new Bundle { ExcludeEntries = [CreateFileEntry("one.yaml", "1")] };

		var merged = BundleAmendMerger.MergeEntries(parent, [amend1, amend2]);

		merged.Should().HaveCount(1);
		merged[0].File!.Name.Should().Be("two.yaml");
	}

	[Theory]
	[InlineData("9.3.0.amend-1.yaml", "9.3.0.yaml")]
	[InlineData("repo-9.3.0.amend-12.yml", "repo-9.3.0.yml")]
	[InlineData("cloud-2025-11.AMEND-2.YAML", "cloud-2025-11.YAML")]
	[InlineData("/releases/9.3.0.amend-1.yaml", "/releases/9.3.0.yaml")]
	[InlineData("elasticsearch-9.3.0.amend-notes.yaml", "elasticsearch-9.3.0.yaml")]
	[InlineData("cloud-2025-11.amend-notes.yml", "cloud-2025-11.yml")]
	public void GetParentBundlePath_AmendFile_StripsAmendSuffix(string amendPath, string expectedParent) =>
		BundleAmendMerger.GetParentBundlePath(amendPath).Should().Be(expectedParent);

	[Theory]
	[InlineData("elasticsearch-9.3.0.amend-notes.yaml", true)]
	[InlineData("elasticsearch-9.3.0.amend-1.yaml", false)]
	[InlineData("9.3.0.yaml", false)]
	public void IsNotesAmendFile_DetectsNotesSidecar(string path, bool expected) =>
		BundleAmendMerger.IsNotesAmendFile(path).Should().Be(expected);

	[Fact]
	public void MergeDescription_OmittedAmend_InheritsParent()
	{
		var parent = "Original intro";
		var amend = new Bundle { Entries = [CreateFileEntry("add.yaml", "ccc")] };

		BundleAmendMerger.MergeDescription(parent, [amend]).Should().Be(parent);
	}

	[Fact]
	public void MergeDescription_LastNumberedAmendWins()
	{
		var amend1 = new Bundle { Description = "First intro" };
		var amend2 = new Bundle { Description = "Second intro" };

		BundleAmendMerger.MergeDescription("Original", [amend1, amend2]).Should().Be("Second intro");
	}

	[Fact]
	public void MergeDescription_EmptyString_ClearsParent()
	{
		var amend = new Bundle { Description = "" };

		BundleAmendMerger.MergeDescription("Original intro", [amend]).Should().BeNull();
	}

	private static BundledEntry CreateFileEntry(string name, string checksum) =>
		new() { File = new BundledFile { Name = name, Checksum = checksum } };
}
