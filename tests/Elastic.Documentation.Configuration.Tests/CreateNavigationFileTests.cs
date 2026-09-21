// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests;

public class CreateNavigationFileTests
{
	private static ConfigurationFileProvider CreateProvider(MockFileSystem fileSystem) =>
		new(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fileSystem),
			skipPrivateRepositories: true,
			ConfigurationSource.Embedded
		);

	private static AssemblyConfiguration CreateConfig(params string[] privateRepoNames)
	{
		var refsYaml = string.Join("\n", privateRepoNames.Select(name => $"  {name}:\n    private: true"));
		var yaml = $"narrative:\n  repo: git@github.com:elastic/docs-content.git\nreferences:\n{refsYaml}";
		return AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: true);
	}

	[Fact]
	public void ConsecutiveSiblingPrivateEntries_BothRemoved()
	{
		var fileSystem = new MockFileSystem();
		var provider = CreateProvider(fileSystem);

		// language=yaml
		var navYaml =
			"""
		              toc:
		                - toc: docs-content://getting-started
		                  path_prefix: getting-started
		                  children:
		                    - toc: public-repo://section-a
		                      path_prefix: section-a
		                    - toc: private-a://section-b
		                      path_prefix: section-b
		                    - toc: private-b://section-c
		                      path_prefix: section-c
		                    - toc: public-repo-two://section-d
		                      path_prefix: section-d
		              """;
		fileSystem.File.WriteAllText(provider.NavigationFile.FullName, navYaml);

		var config = CreateConfig("private-a", "private-b");
		var result = provider.CreateNavigationFile(config);
		var output = fileSystem.File.ReadAllText(result.FullName);

		output.Should().NotContain("private-a://");
		output.Should().NotContain("private-b://");
		output.Should().Contain("public-repo://section-a");
		output.Should().Contain("public-repo-two://section-d");
	}

	[Fact]
	public void DeeplyNestedConsecutivePrivateEntries_BothRemoved()
	{
		var fileSystem = new MockFileSystem();
		var provider = CreateProvider(fileSystem);

		// language=yaml
		var navYaml =
			"""
		              toc:
		                - toc: docs-content://top
		                  path_prefix: top
		                  children:
		                    - toc: docs-content://mid
		                      path_prefix: mid
		                      children:
		                        - toc: public-repo://leaf-a
		                          path_prefix: leaf-a
		                        - toc: private-a://leaf-b
		                          path_prefix: leaf-b
		                        - toc: private-b://leaf-c
		                          path_prefix: leaf-c
		                        - toc: public-repo-two://leaf-d
		                          path_prefix: leaf-d
		              """;
		fileSystem.File.WriteAllText(provider.NavigationFile.FullName, navYaml);

		var config = CreateConfig("private-a", "private-b");
		var result = provider.CreateNavigationFile(config);
		var output = fileSystem.File.ReadAllText(result.FullName);

		output.Should().NotContain("private-a://");
		output.Should().NotContain("private-b://");
		output.Should().Contain("public-repo://leaf-a");
		output.Should().Contain("public-repo-two://leaf-d");
	}

	[Fact]
	public void PrivateRepoWithChildren_EntryRemovedChildrenReindented()
	{
		var fileSystem = new MockFileSystem();
		var provider = CreateProvider(fileSystem);

		// language=yaml
		var navYaml =
			"""
		              toc:
		                - toc: docs-content://top
		                  path_prefix: top
		                  children:
		                    - toc: private-a://parent
		                      path_prefix: parent
		                      children:
		                        - toc: public-child://nested
		                          path_prefix: nested
		                    - toc: public-repo://after
		                      path_prefix: after
		              """;
		fileSystem.File.WriteAllText(provider.NavigationFile.FullName, navYaml);

		var config = CreateConfig("private-a");
		var result = provider.CreateNavigationFile(config);
		var output = fileSystem.File.ReadAllText(result.FullName);

		output.Should().NotContain("private-a://");
		output.Should().Contain("public-child://nested");
		output.Should().Contain("public-repo://after");
	}

	[Fact]
	public void PublicEntriesBetweenPrivateEntries_Preserved()
	{
		var fileSystem = new MockFileSystem();
		var provider = CreateProvider(fileSystem);

		// language=yaml
		var navYaml =
			"""
		              toc:
		                - toc: docs-content://top
		                  path_prefix: top
		                  children:
		                    - toc: private-a://first
		                      path_prefix: first
		                    - toc: public-repo://middle
		                      path_prefix: middle
		                    - toc: private-b://last
		                      path_prefix: last
		              """;
		fileSystem.File.WriteAllText(provider.NavigationFile.FullName, navYaml);

		var config = CreateConfig("private-a", "private-b");
		var result = provider.CreateNavigationFile(config);
		var output = fileSystem.File.ReadAllText(result.FullName);

		output.Should().NotContain("private-a://");
		output.Should().NotContain("private-b://");
		output.Should().Contain("public-repo://middle");
	}

	[Fact]
	public void IslandProperty_OnPublicEntry_SurvivesPrivateRepoStripping()
	{
		var fileSystem = new MockFileSystem();
		var provider = CreateProvider(fileSystem);

		// language=yaml
		var navYaml =
			"""
		              toc:
		                - toc: public-repo://reference
		                  path_prefix: reference
		                  island: true
		                  children:
		                    - toc: private-a://section
		                      path_prefix: reference/private
		                    - toc: public-child://nested
		                      path_prefix: reference/nested
		              """;
		fileSystem.File.WriteAllText(provider.NavigationFile.FullName, navYaml);

		var config = CreateConfig("private-a");
		var result = provider.CreateNavigationFile(config);
		var output = fileSystem.File.ReadAllText(result.FullName);

		// The public entry and its island: true property are preserved
		output.Should().Contain("public-repo://reference");
		output.Should().Contain("island: true", "island: true on a public entry must survive private-repo stripping");
		output.Should().Contain("public-child://nested");
		// The private entry is removed
		output.Should().NotContain("private-a://");
	}

	[Fact]
	public void IslandProperty_OnPrivateEntry_IsRemovedWithEntry()
	{
		var fileSystem = new MockFileSystem();
		var provider = CreateProvider(fileSystem);

		// language=yaml
		var navYaml =
			"""
		              toc:
		                - toc: public-repo://reference
		                  path_prefix: reference
		                - toc: private-a://section
		                  path_prefix: section
		                  island: true
		                - toc: public-after://other
		                  path_prefix: other
		              """;
		fileSystem.File.WriteAllText(provider.NavigationFile.FullName, navYaml);

		var config = CreateConfig("private-a");
		var result = provider.CreateNavigationFile(config);
		var output = fileSystem.File.ReadAllText(result.FullName);

		// The private entry and its island: true property are removed
		output.Should().NotContain("private-a://");
		output.Should().NotContain("island: true", "island: true on a private entry is removed with the entry");
		// Public entries are preserved
		output.Should().Contain("public-repo://reference");
		output.Should().Contain("public-after://other");
	}

	[Fact]
	public void NoPrivateRepositories_ReturnsOriginalFile()
	{
		var fileSystem = new MockFileSystem();
		var provider = CreateProvider(fileSystem);

		var originalContent = fileSystem.File.ReadAllText(provider.NavigationFile.FullName);

		// Empty config with no private repos
		var yaml = "narrative:\n  repo: git@github.com:elastic/docs-content.git\nreferences:\n  public-repo:\n    private: false";
		var config = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: true);

		var result = provider.CreateNavigationFile(config);

		result.FullName.Should().Be(provider.NavigationFile.FullName);
		var output = fileSystem.File.ReadAllText(result.FullName);
		output.Should().Be(originalContent);
	}
}
