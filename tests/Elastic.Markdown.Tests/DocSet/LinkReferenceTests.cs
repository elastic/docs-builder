// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Discovery;
using Elastic.Markdown.IO.State;
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

public class GitCheckoutInformationTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void Create()
	{
		var git = GitCheckoutInformation.Create(ReadFileSystem);

		git.Should().NotBeNull();
		git!.Branch.Should().NotBeNullOrWhiteSpace();
		// this validates we are not returning the test instance as were doing a real read
		git.Branch.Should().NotContain(git.Ref);
		git.Ref.Should().NotBeNullOrWhiteSpace();
		git.Remote.Should().NotBeNullOrWhiteSpace();
		git.Remote.Should().NotContain("unknown");
		git.RepositoryName.Should().NotContain(".git");
		git.Remote.Should().NotContain(".git");
	}
}
