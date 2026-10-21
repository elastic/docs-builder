// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Isolated;
using Nullean.Entities;

namespace Documentation.Builder.Tests;

public class BuildOptionsConstraintsTests
{
	private static readonly IReadOnlySet<Exporter> SomeExporters = new HashSet<Exporter> { Exporter.Html };

	/// <summary>
	/// Law: --metadata-only next to --exporters is reported as a warning, whichever command it is on, and either flag alone is fine.
	/// Consequence if broken: --metadata-only is silently ignored, so a build the user thinks writes only metadata renders every page.
	/// </summary>
	[Test]
	public void Isolated_MetadataOnlyWithExporters_IsAWarning()
	{
		var both = IsolatedBuildOptionsConstraints.All.Validate(new IsolatedBuildOptions
		{
			MetadataOnly = true,
			Exporters = SomeExporters
		});

		both.Should().ContainSingle().Which.Severity.Should().Be(Severity.Warning);
		both.Single().Message.Should().Contain("--metadata-only").And.Contain("--exporters");
		IsolatedBuildOptionsConstraints.All.Validate(new IsolatedBuildOptions { MetadataOnly = true }).Should().BeEmpty();
		IsolatedBuildOptionsConstraints.All.Validate(new IsolatedBuildOptions { Exporters = SomeExporters }).Should().BeEmpty();
		IsolatedBuildOptionsConstraints
			.All
			.Validate(new IsolatedBuildOptions { MetadataOnly = false, Exporters = SomeExporters })
			.Should()
			.BeEmpty();
		IsolatedBuildOptionsConstraints.All.Validate(new IsolatedBuildOptions()).Should().BeEmpty();
	}

	[Test]
	public void Assembler_MetadataOnlyWithExporters_IsAWarning()
	{
		var both = AssemblerBuildOptionsConstraints.All.Validate(new AssemblerBuildOptions
		{
			MetadataOnly = true,
			Exporters = SomeExporters
		});

		both.Should().ContainSingle().Which.Severity.Should().Be(Severity.Warning);
		AssemblerBuildOptionsConstraints.All.Validate(new AssemblerBuildOptions { MetadataOnly = true }).Should().BeEmpty();
		AssemblerBuildOptionsConstraints.All.Validate(new AssemblerBuildOptions { Exporters = SomeExporters }).Should().BeEmpty();
		AssemblerBuildOptionsConstraints.All.Validate(new AssemblerBuildOptions()).Should().BeEmpty();
	}
}
