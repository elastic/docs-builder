// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

public class BundleLoaderFromContentTests
{
	private readonly BundleLoader _loader = new(new FileSystem());

	private static (string FileName, string Content) Bundle(string fileName, string content) => (fileName, content);

	[Fact]
	public void LoadBundlesFromContent_InlineEntries_AreLoaded()
	{
		var warnings = new List<string>();
		// language=yaml
		var bundle = Bundle(
			"9.3.0.yaml",
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: elasticsearch
			    owner: elastic
			entries:
			  - file:
			      name: 1.yaml
			      checksum: c0ffee
			    type: enhancement
			    title: Sample enhancement
			"""
		);

		var bundles = _loader.LoadBundlesFromContent([bundle], warnings.Add);

		warnings.Should().BeEmpty();
		bundles.Should().ContainSingle();
		var loaded = bundles[0];
		loaded.Version.Should().Be("9.3.0");
		loaded.Repo.Should().Be("elasticsearch");
		loaded.Entries.Should().ContainSingle();
		loaded.Entries[0].Title.Should().Be("Sample enhancement");
	}

	[Fact]
	public void LoadBundlesFromContent_FileOnlyEntry_IsSkippedWithWarning()
	{
		var warnings = new List<string>();
		// language=yaml
		var bundle = Bundle(
			"9.3.0.yaml",
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - file:
			      name: orphan.yaml
			      checksum: deadbeef
			"""
		);

		var bundles = _loader.LoadBundlesFromContent([bundle], warnings.Add);

		bundles.Should().ContainSingle();
		bundles[0].Entries.Should().BeEmpty();
		var warning = warnings.Should().ContainSingle().Which;
		warning.Should().Contain("no inline content");
		warning.Should().Contain("9.3.0.yaml");
		warning.Should().Contain("orphan.yaml");
	}

	[Fact]
	public void LoadBundlesFromContent_InvalidYaml_IsSkippedWithWarning()
	{
		var warnings = new List<string>();
		var bundle = Bundle("broken.yaml", "products: [unterminated");

		var bundles = _loader.LoadBundlesFromContent([bundle], warnings.Add);

		bundles.Should().BeEmpty();
		warnings.Should().ContainSingle().Which.Should().Contain("broken.yaml");
	}

	[Fact]
	public void LoadBundlesFromContent_AmendFile_IsMergedIntoParent()
	{
		var warnings = new List<string>();
		// language=yaml
		var parent = Bundle(
			"9.3.0.yaml",
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - type: enhancement
			    title: Base entry
			"""
		);
		// language=yaml
		var amend = Bundle(
			"9.3.0.amend-1.yaml",
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - type: bug-fix
			    title: Amended fix
			"""
		);

		var bundles = _loader.LoadBundlesFromContent([parent, amend], warnings.Add);

		bundles.Should().ContainSingle("the amend file merges into its parent");
		bundles[0].Entries.Select(e => e.Title).Should().BeEquivalentTo("Base entry", "Amended fix");
	}

	[Fact]
	public void LoadBundlesFromContent_AmendNotesFile_IsMergedIntoParent()
	{
		var warnings = new List<string>();
		// language=yaml
		var parent = Bundle(
			"cloud-4.2.0.yaml",
			"""
			products:
			  - product: cloud-enterprise
			    target: 4.2.0
			entries:
			  - type: enhancement
			    title: Base entry
			"""
		);
		// language=yaml
		var amendNotes = Bundle(
			"cloud-4.2.0.amend-notes.yaml",
			"""
			products:
			  - product: cloud-enterprise
			    target: 4.2.0
			entries:
			  - type: security
			    title: Late note
			"""
		);

		var bundles = _loader.LoadBundlesFromContent([parent, amendNotes], warnings.Add);

		bundles.Should().ContainSingle("the amend-notes sidecar merges into its parent");
		bundles[0].Version.Should().Be("4.2.0");
		bundles[0].Entries.Select(e => e.Title).Should().Equal("Base entry", "Late note");
		warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadBundlesFromContent_AmendNotesFile_AppliesAfterNumberedAmends()
	{
		var warnings = new List<string>();
		// language=yaml
		var parent = Bundle(
			"9.3.0.yaml",
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - type: feature
			    title: Keep
			    file:
			      name: keep.yaml
			      checksum: aaa
			  - type: bug-fix
			    title: Removed by numbered amend
			    file:
			      name: gone.yaml
			      checksum: bbb
			"""
		);
		// language=yaml
		var numbered = Bundle(
			"9.3.0.amend-1.yaml",
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			exclude-entries:
			  - file:
			      name: gone.yaml
			      checksum: bbb
			entries:
			  - type: enhancement
			    title: Numbered addition
			"""
		);
		// language=yaml
		var amendNotes = Bundle(
			"9.3.0.amend-notes.yaml",
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - type: security
			    title: Notes addition
			"""
		);

		var bundles = _loader.LoadBundlesFromContent([parent, numbered, amendNotes], warnings.Add);

		bundles.Should().ContainSingle();
		bundles[0].Entries.Select(e => e.Title).Should().Equal("Keep", "Numbered addition", "Notes addition");
		warnings.Should().BeEmpty();
	}
}
