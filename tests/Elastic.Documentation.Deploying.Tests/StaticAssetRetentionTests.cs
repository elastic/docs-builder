// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.S3.Model;
using AwesomeAssertions;
using Elastic.Documentation.Deploying.Synchronization;

namespace Elastic.Documentation.Deploying.Tests;

public class StaticAssetRetentionTests
{
	[Test]
	[Arguments("docs/_static/main-BM-isZMZ.js", "docs/_static/main.js")]
	[Arguments("docs/_static/main-BM-isZMZ.js.map", "docs/_static/main.js.map")]
	[Arguments("docs/_static/chunk-ModalSearch-DqleNXOZ.js", "docs/_static/chunk-ModalSearch.js")]
	[Arguments("docs/_static/chunk-emotion-element-f0de968e.browser.esm-xLC9ZohH.js", "docs/_static/chunk-emotion-element-f0de968e.browser.esm.js")]
	[Arguments("docs/_static/chunk-AppliesTo-Cq9yR2xk.css", "docs/_static/chunk-AppliesTo.css")]
	[Arguments("docs/_static/asset-logo-B5UY0lxh.svg", "docs/_static/asset-logo.svg")]
	[Arguments("_static/main-BM-isZMZ.js", "_static/main.js")]
	public void FamilyOf_HashedAsset_StripsTheHash(string key, string family) => StaticAssetRetention.FamilyOf(key).Should().Be(family);

	[Test]
	[Arguments("docs/_static/styles.css")]
	[Arguments("docs/_static/main.js")]
	[Arguments("docs/_static/branding-logo.svg")]
	[Arguments("docs/_static/mermaid.min.js")]
	[Arguments("docs/index.html")]
	[Arguments("docs/images/chunk-shot-DqleNXOZ.png")]
	public void FamilyOf_OtherFile_IsNull(string key) => StaticAssetRetention.FamilyOf(key).Should().BeNull();

	[Test]
	public void Retain_KeepsNewestPerFamily_AndNothingElse()
	{
		var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
		var stale = new List<S3Object>();
		foreach (var i in Enumerable.Range(0, StaticAssetRetention.Keep + 2))
			stale.Add(new S3Object { Key = $"docs/_static/main-hash{i:D4}.js", LastModified = start.AddDays(i) });
		stale.Add(new S3Object { Key = "docs/_static/chunk-ModalSearch-aaaaaaaa.js", LastModified = start });
		stale.Add(new S3Object { Key = "docs/_static/styles.css", LastModified = start });
		stale.Add(new S3Object { Key = "docs/old-page.html", LastModified = start.AddDays(20) });

		var retained = StaticAssetRetention.Retain(stale);

		retained.Should().HaveCount(StaticAssetRetention.Keep + 1);
		retained.Should().NotContain("docs/_static/main-hash0000.js");
		retained.Should().NotContain("docs/_static/main-hash0001.js");
		retained.Should().Contain("docs/_static/main-hash0002.js");
		retained.Should().Contain("docs/_static/main-hash0011.js");
		retained.Should().Contain("docs/_static/chunk-ModalSearch-aaaaaaaa.js");
		retained.Should().NotContain("docs/_static/styles.css");
		retained.Should().NotContain("docs/old-page.html");
	}
}
