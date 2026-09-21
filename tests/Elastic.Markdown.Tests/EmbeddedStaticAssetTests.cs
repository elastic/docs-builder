// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Site.FileProviders;

namespace Elastic.Markdown.Tests;

public class EmbeddedStaticAssetTests
{
	private static readonly string[] ResourceNames =
		typeof(EmbeddedOrPhysicalFileProvider).Assembly.GetManifestResourceNames();

	[Fact]
	public void EmbedGeneratedAssets_includes_source_maps_unconditionally()
	{
		var mapEmbeds = File.ReadAllLines(SiteCsprojPath())
			.Where(static line => line.Contains("EmbeddedResource", StringComparison.Ordinal)
				&& (line.Contains("*.js.map", StringComparison.Ordinal) || line.Contains("*.css.map", StringComparison.Ordinal)))
			.ToArray();

		mapEmbeds.Should().HaveCount(2);
		mapEmbeds.Should().NotContain(static line => line.Contains("Condition", StringComparison.Ordinal));
	}

	[Fact]
	public void NpmRunBuild_deletes_hashed_source_maps_before_parcel()
	{
		var csproj = File.ReadAllText(SiteCsprojPath());
		csproj.Should().Contain("""Include="_static/*.????????*.js.map" """);
		csproj.Should().Contain("""Include="_static/*.????????*.css.map" """);
		csproj.Should().NotContain("CleanOldHashedAssets");
	}

	[Fact]
	public void Site_assembly_embeds_js_and_css_source_maps()
	{
		ResourceNames.Should().Contain(static n => n.EndsWith(".js.map", StringComparison.Ordinal));
		ResourceNames.Should().Contain(static n => n.EndsWith(".css.map", StringComparison.Ordinal));
	}

	private static string SiteCsprojPath() =>
		Path.Combine(Paths.WorkingDirectoryRoot.FullName, "src", "Elastic.Documentation.Site", "Elastic.Documentation.Site.csproj");
}
