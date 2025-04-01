// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Documentation.Assembler.Configuration;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using FluentAssertions;

namespace Documentation.Assembler.Tests;

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
		Context = new AssembleContext("dev", Collector, FileSystem, FileSystem, CheckoutDirectory.FullName, null);
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
		config.NamedGitReferences.Should().NotBeEmpty()
			.And.ContainKey("stack");

		config.NamedGitReferences["stack"].Should().NotBeNullOrEmpty();

		var agent = config.ReferenceRepositories["elasticsearch"];
		agent.GitReferenceCurrent.Should().NotBeNullOrEmpty()
			.And.Be(config.NamedGitReferences["stack"]);

		// test defaults
		var apmServer = config.ReferenceRepositories["apm-server"];
		apmServer.GitReferenceNext.Should().NotBeNullOrEmpty()
			.And.Be("main");
		apmServer.GitReferenceCurrent.Should().NotBeNullOrEmpty()
			.And.Be("main");

	}
}
