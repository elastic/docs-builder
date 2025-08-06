// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Documentation.Assembler.Tests;

public class PublicOnlyAssemblerConfigurationTests
{
	private DiagnosticsCollector Collector { get; }
	private AssembleContext Context { get; }
	private FileSystem FileSystem { get; }
	private IDirectoryInfo CheckoutDirectory { get; set; }
	public PublicOnlyAssemblerConfigurationTests()
	{
		FileSystem = new FileSystem();
		CheckoutDirectory = FileSystem.DirectoryInfo.New(
			FileSystem.Path.Combine(Paths.GetSolutionDirectory()!.FullName, ".artifacts", "checkouts")
		);
		Collector = new DiagnosticsCollector([]);
		var configurationFileProvider = new ConfigurationFileProvider(FileSystem, skipPrivateRepositories: true);
		var configurationContext = TestHelpers.CreateConfigurationContext(FileSystem, configurationFileProvider: configurationFileProvider);
		var config = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		Context = new AssembleContext(config, configurationContext, "dev", Collector, FileSystem, FileSystem, CheckoutDirectory.FullName, null);
	}

	[Fact]
	public void ReadsPrivateRepositories()
	{
		var config = Context.Configuration;
		config.ReferenceRepositories.Should().NotBeEmpty().And.NotContainKey("cloud");
		config.PrivateRepositories.Should().NotBeEmpty().And.ContainKey("cloud");
		var cloud = config.PrivateRepositories["cloud"];
		cloud.Should().NotBeNull();
		cloud.GitReferenceCurrent.Should().NotBeNullOrEmpty()
			.And.Be("master");
	}

}

public class AssemblerConfigurationTests
{
	private DiagnosticsCollector Collector { get; }
	private AssembleContext Context { get; }
	private FileSystem FileSystem { get; }
	private IDirectoryInfo CheckoutDirectory { get; set; }
	public AssemblerConfigurationTests()
	{
		FileSystem = new FileSystem();
		CheckoutDirectory = FileSystem.DirectoryInfo.New(
			FileSystem.Path.Combine(Paths.GetSolutionDirectory()!.FullName, ".artifacts", "checkouts")
		);
		Collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(FileSystem);
		var config = AssemblyConfiguration.Create(configurationContext.ConfigurationFileProvider);
		Context = new AssembleContext(config, configurationContext, "dev", Collector, FileSystem, FileSystem, CheckoutDirectory.FullName, null);
	}

	[Fact]
	public void ReadsConfigurationFiles()
	{
		Context.ConfigurationFileProvider.VersionFile.Name.Should().Be("versions.yml");
		Context.ConfigurationFileProvider.NavigationFile.Name.Should().Be("navigation.yml");
		Context.ConfigurationFileProvider.AssemblerFile.Name.Should().Be("assembler.yml");
		Context.ConfigurationFileProvider.LegacyUrlMappingsFile.Name.Should().Be("legacy-url-mappings.yml");
	}

	[Fact]
	public void ReadsContentSource()
	{
		var environments = Context.Configuration.Environments;
		environments.Should().NotBeEmpty()
			.And.ContainKey("prod");

		var prod = environments["prod"];
		prod.ContentSource.Should().Be(ContentSource.Current);

		var staging = environments["staging"];
		staging.ContentSource.Should().Be(ContentSource.Next);
	}

	[Fact]
	public void ReadsVersions()
	{
		var config = Context.Configuration;
		config.SharedConfigurations.Should().NotBeEmpty()
			.And.ContainKey("stack");

		config.SharedConfigurations["stack"].GitReferenceEdge.Should().NotBeNullOrEmpty();

		//var agent = config.ReferenceRepositories["elasticsearch"];
		//agent.GitReferenceCurrent.Should().NotBeNullOrEmpty()
		//	.And.Be(config.NamedGitReferences["stack"]);

		// test defaults
		var apmServer = config.ReferenceRepositories["apm-server"];
		apmServer.GitReferenceNext.Should().NotBeNullOrEmpty()
			.And.Be("main");
		apmServer.GitReferenceCurrent.Should().NotBeNullOrEmpty()
			.And.Be("main");
		apmServer.GitReferenceEdge.Should().NotBeNullOrEmpty()
			.And.Be("main");

		var beats = config.ReferenceRepositories["beats"];
		beats.GitReferenceCurrent.Should().NotBeNullOrEmpty()
			.And.NotBe("main");

		var cloud = config.ReferenceRepositories["cloud"];
		cloud.GitReferenceCurrent.Should().NotBeNullOrEmpty()
			.And.Be("master");
	}
}
