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
		var parent = new List<BundledEntry>
		{
			CreateFileEntry("keep.yaml", "aaa"),
			CreateFileEntry("remove.yaml", "bbb")
		};

		var amend = new Bundle
		{
			ExcludeEntries = [CreateFileEntry("remove.yaml", "bbb")],
			Entries = [CreateFileEntry("add.yaml", "ccc")]
		};

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

		var amend1 = new Bundle
		{
			Entries = [CreateFileEntry("two.yaml", "2")]
		};
		var amend2 = new Bundle
		{
			ExcludeEntries = [CreateFileEntry("one.yaml", "1")]
		};

		var merged = BundleAmendMerger.MergeEntries(parent, [amend1, amend2]);

		merged.Should().HaveCount(1);
		merged[0].File!.Name.Should().Be("two.yaml");
	}

	[Theory]
	[InlineData("9.3.0.amend-1.yaml", "9.3.0.yaml")]
	[InlineData("repo-9.3.0.amend-12.yml", "repo-9.3.0.yml")]
	[InlineData("cloud-2025-11.AMEND-2.YAML", "cloud-2025-11.YAML")]
	[InlineData("/releases/9.3.0.amend-1.yaml", "/releases/9.3.0.yaml")]
	public void GetParentBundlePath_AmendFile_StripsAmendSuffix(string amendPath, string expectedParent) =>
		BundleAmendMerger.GetParentBundlePath(amendPath).Should().Be(expectedParent);

	[Theory]
	[InlineData("9.3.0.yaml")]
	[InlineData("9.3.0.amend-.yaml")]
	[InlineData("9.3.0.amend-1.json")]
	public void GetParentBundlePath_NonAmendFile_ReturnsNull(string path) =>
		BundleAmendMerger.GetParentBundlePath(path).Should().BeNull();

	private static BundledEntry CreateFileEntry(string name, string checksum) => new()
	{
		File = new BundledFile
		{
			Name = name,
			Checksum = checksum
		}
	};
}
