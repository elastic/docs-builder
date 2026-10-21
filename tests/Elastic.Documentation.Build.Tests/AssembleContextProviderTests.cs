// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;

namespace Elastic.Documentation.Build.Tests;

public class AssembleContextProviderTests
{
	private const string AssemblerYaml =
		"""
		environments:
		  dev:
		    uri: https://dev.elastic.co
		    path_prefix: docs
		    content_source: next
		narrative:
		  checkout_strategy: full
		references: {}
		""";

	private static (AssembleContextProvider Provider, IDiagnosticsCollector Collector, CheckoutsFileSystem Fs) Setup()
	{
		var fileSystem = new FileSystem();
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var provider = new AssembleContextProvider(AssemblyLoader.Deserialize(AssemblerYaml), configurationContext);
		return (provider, new DiagnosticsCollector([]), CheckoutsFileSystem.FromWorkingDirectory(fileSystem));
	}

	[Test]
	public void TwoServicesAskingForTheSameRun_ShareOneContext_AndParseNavigationOnce()
	{
		var (provider, collector, fs) = Setup();

		// two "services" of one run each ask the provider for their context
		var first = provider.Get("dev", collector, fs);
		var second = provider.Get("dev", collector, fs);

		_ = second.Should().BeSameAs(first);
		var navigation = first.Navigation;
		var diagnosticsAfterFirstLoad = collector.Errors + collector.Warnings + collector.Hints;
		_ = second.Navigation.Should().BeSameAs(navigation);
		_ = (collector.Errors + collector.Warnings + collector.Hints).Should().Be(diagnosticsAfterFirstLoad);
	}

	[Test]
	public void AnotherCollectorOrEnvironment_IsAnotherRun()
	{
		var (provider, collector, fs) = Setup();

		var context = provider.Get("dev", collector, fs);

		_ = provider.Get("dev", new DiagnosticsCollector([]), fs).Should().NotBeSameAs(context);
		_ = provider.Get("dev", collector, fs, output: "/elsewhere").Should().NotBeSameAs(context);
	}
}
