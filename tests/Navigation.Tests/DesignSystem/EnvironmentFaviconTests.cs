// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public class EnvironmentFaviconTests
{
	[Test]
	[Arguments(BuildType.Assembler, "prod", "favicon.svg")]
	[Arguments(BuildType.Assembler, "staging", "favicon-staging.svg")]
	[Arguments(BuildType.Assembler, "edge", "favicon-edge.svg")]
	[Arguments(BuildType.Assembler, "demo", "favicon-demo.svg")]
	[Arguments(BuildType.Assembler, "dev", "favicon-dev.svg")]
	[Arguments(BuildType.Assembler, "local", "favicon-dev.svg")]
	[Arguments(BuildType.Assembler, "preview", "favicon-preview.svg")]
	[Arguments(BuildType.Assembler, "air-gapped", "favicon-other.svg")]
	[Arguments(BuildType.Isolated, null, "favicon-isolated.svg")]
	[Arguments(BuildType.Codex, null, "favicon.svg")]
	public void FileName_EnvironmentAndBuildType_PicksTheFavicon(BuildType buildType, string? environment, string expected) =>
		EnvironmentFavicon.FileName(buildType, environment).Should().Be(expected);

	[Test]
	[Arguments(BuildType.Assembler, "prod", "og-default.png")]
	[Arguments(BuildType.Assembler, null, "og-default.png")]
	[Arguments(BuildType.Assembler, "staging", "og-staging.png")]
	[Arguments(BuildType.Assembler, "local", "og-dev.png")]
	[Arguments(BuildType.Assembler, "air-gapped", "og-other.png")]
	[Arguments(BuildType.Codex, null, "og-codex.png")]
	[Arguments(BuildType.Isolated, null, null)]
	public void OgImageFileName_BuildTypeAndEnvironment_PicksTheImage(BuildType buildType, string? environment, string? expected) =>
		EnvironmentFavicon.OgImageFileName(buildType, environment).Should().Be(expected);

	[Test]
	public void FileName_EveryNonProdFavicon_ExistsAsStaticAsset()
	{
		var assets = typeof(EnvironmentFavicon).Assembly.GetManifestResourceNames();
		foreach (var name in new[] { "staging", "edge", "demo", "dev", "preview", "other", "isolated" })
			assets.Should().Contain(n => n.EndsWith($"favicon-{name}.svg"));
		assets.Should().Contain(n => n.EndsWith("_static.favicon.svg"));
		assets.Should().Contain(n => n.EndsWith("apple-touch-icon.png"));
		assets.Should().Contain(n => n.EndsWith("favicon.ico"));
		assets.Should().Contain(n => n.EndsWith("og-default.png"));
		assets.Should().Contain(n => n.EndsWith("og-codex.png"));
		foreach (var name in new[] { "staging", "edge", "demo", "dev", "preview", "other" })
			assets.Should().Contain(n => n.EndsWith($"og-{name}.png"));
	}
}
