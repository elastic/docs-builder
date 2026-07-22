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
		var bundle = Bundle("9.3.0.yaml", """
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
			""");

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
		var bundle = Bundle("9.3.0.yaml", """
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - file:
			      name: orphan.yaml
			      checksum: deadbeef
			""");

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
		warnings.Should().ContainSingle()
			.Which.Should().Contain("broken.yaml");
	}

	[Fact]
	public void LoadBundlesFromContent_AmendFile_IsMergedIntoParent()
	{
		var warnings = new List<string>();
		// language=yaml
		var parent = Bundle("9.3.0.yaml", """
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - type: enhancement
			    title: Base entry
			""");
		// language=yaml
		var amend = Bundle("9.3.0.amend-1.yaml", """
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - type: bug-fix
			    title: Amended fix
			""");

		var bundles = _loader.LoadBundlesFromContent([parent, amend], warnings.Add);

		bundles.Should().ContainSingle("the amend file merges into its parent");
		bundles[0].Entries.Select(e => e.Title)
			.Should().BeEquivalentTo("Base entry", "Amended fix");
	}
}
