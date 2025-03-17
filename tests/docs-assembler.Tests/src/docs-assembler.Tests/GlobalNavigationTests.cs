// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Documentation.Assembler.Navigation;
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
		var globalNavigation = GlobalNavigation.Deserialize(assembleContext);
		globalNavigation.References.Should().NotBeNull().And.NotBeEmpty();
	}
}
