// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Ensures <c>config/assembler.yml</c> stays valid YAML for <see cref="AssemblyConfiguration"/> so private-repo changelog sanitization can resolve references in CI.
/// </summary>
public class AssemblerConfigurationYamlTests
{
	[Fact]
	public void ConfigAssemblerYml_DeserializesWithNonEmptyReferences()
	{
		var root = Paths.GetSolutionDirectory() ?? throw new InvalidOperationException("Solution directory not found.");
		var path = Path.Combine(root.FullName, "config", "assembler.yml");
		File.Exists(path).Should().BeTrue();

		var yaml = File.ReadAllText(path);
		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		asm.ReferenceRepositories.Should().NotBeEmpty();
	}
}
