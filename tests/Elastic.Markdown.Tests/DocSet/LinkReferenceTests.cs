// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.DocSet;

public class LinkReferenceTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void Create()
	{
		var reference = LinkReference.Create(Set);

		reference.Should().NotBeNull();
	}
}

public class GitConfigurationTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void Create()
	{
		var git = GitConfiguration.Create(ReadFileSystem);

		git.Should().NotBeNull();
		git!.Branch.Should().NotBeNullOrWhiteSpace();
		// this validates we are not returning the test instance as were doing a real read
		git.Branch.Should().NotContain(git.Ref);
		git.Ref.Should().NotBeNullOrWhiteSpace();
		git.Remote.Should().NotBeNullOrWhiteSpace();
	}
}
