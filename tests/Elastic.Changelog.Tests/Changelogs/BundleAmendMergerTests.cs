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

	private static BundledEntry CreateFileEntry(string name, string checksum) => new()
	{
		File = new BundledFile
		{
			Name = name,
			Checksum = checksum
		}
	};
}
