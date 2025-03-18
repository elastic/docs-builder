// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Documentation.Assembler.Building;
using Documentation.Assembler.Configuration;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Markdown.Diagnostics;
using FluentAssertions;

namespace Documentation.Assembler.Tests;

public class GlobalNavigationTests
{

	[Fact]
	public async Task ParsesGlobalNavigation()
	{
		await using var collector = new DiagnosticsCollector([]);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		var assembleContext = new AssembleContext(collector, new FileSystem(), new FileSystem(), null, null);
		var globalNavigation = GlobalNavigationFile.Deserialize(assembleContext);
		globalNavigation.TableOfContents.Should().NotBeNull().And.NotBeEmpty();
	}

	[Fact]
	public async Task UriResolving()
	{
		await using var collector = new DiagnosticsCollector([]);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		var fs = new FileSystem();
		var assembleContext = new AssembleContext(collector, fs, fs, null, null);
		var globalNavigationFile = GlobalNavigationFile.Deserialize(assembleContext);
		globalNavigationFile.TableOfContents.Should().NotBeNull().And.NotBeEmpty();
		var globalNavigation = new GlobalNavigation(assembleContext, globalNavigationFile, [
			new Checkout
			{
				Repository = new Repository { Name = "docs-content", Origin = "elastic/docs-content"},
				HeadReference = Guid.NewGuid().ToString(),
				Directory = fs.DirectoryInfo.New(fs.Path.Combine(".artifacts", "checkouts", "docs-content"))
			},
			new Checkout
			{
				Repository = new Repository { Name = "curator", Origin = "elastic/curator"},
				HeadReference = Guid.NewGuid().ToString(),
				Directory = fs.DirectoryInfo.New(fs.Path.Combine(".artifacts", "checkouts", "curator"))
			},
			new Checkout
			{
				Repository = new Repository { Name = "elasticsearch-net", Origin = "elastic/elasticsearch-net"},
				HeadReference = Guid.NewGuid().ToString(),
				Directory = fs.DirectoryInfo.New(fs.Path.Combine(".artifacts", "checkouts", "elasticsearch-net"))
			}
		]);

		var env = assembleContext.Configuration.Environments["production"];
		var uriResolver = new PublishEnvironmentUriResolver(globalNavigation, env);

		// docs-content://reference/apm/something.md - url hasn't changed
		var resolvedURi = uriResolver.Resolve(new Uri("docs-content://reference/apm/something.md"), "/reference/apm/something");
		resolvedURi.Should().Be("https://www.elastic.co/docs/reference/apm/something");


		resolvedURi = uriResolver.Resolve(new Uri("curator://reference/a/file.md"), "/reference/a/file");
		resolvedURi.Should().Be("https://www.elastic.co/docs/reference/elasticsearch/curator/a/file");

		//path_prefix: reference/elasticsearch/clients/net
		resolvedURi = uriResolver.Resolve(new Uri("elasticsearch-net://reference/b/file.md"), "/reference/b/file");
		resolvedURi.Should().Be("https://www.elastic.co/docs/reference/elasticsearch/clients/net/b/file");
	}
}
