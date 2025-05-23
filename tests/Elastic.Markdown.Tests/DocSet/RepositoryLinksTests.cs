// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Links;
using Elastic.Markdown.IO;
using FluentAssertions;

namespace Elastic.Markdown.Tests.DocSet;

public class RepositoryLinksTests : NavigationTestsBase
{
	public RepositoryLinksTests(ITestOutputHelper output) : base(output) => Reference = Set.CreateLinkReference();

	private RepositoryLinks Reference { get; }

	[Fact]
	public void ShouldNotBeNull() =>
		Reference.Should().NotBeNull();

	[Fact]
	public void EmitsLinks() =>
		Reference.Links.Should().NotBeNullOrEmpty();

	[Fact]
	public void ShouldNotIncludeSnippets() =>
		Reference.Links.Should().NotContain(l => l.Key.Contains("_snippets/"));

}

public class GitCheckoutInformationTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void Create()
	{
		var root = ReadFileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		var git = GitCheckoutInformation.Create(root, ReadFileSystem, LoggerFactory.CreateLogger(nameof(GitCheckoutInformation)));

		git.Should().NotBeNull();
		git.Branch.Should().NotBeNullOrWhiteSpace();
		// this validates we are not returning the test instance as were doing a real read
		git.Branch.Should().NotContain(git.Ref);
		git.Ref.Should().NotBeNullOrWhiteSpace();
		git.Remote.Should().NotBeNullOrWhiteSpace();
		git.Remote.Should().NotContain("unknown");
		git.RepositoryName.Should().NotContain(".git");
		git.Remote.Should().NotContain(".git");
	}
}

public class LinkReferenceSerializationTests
{
	[Fact]
	public void SerializesCurrent()
	{
		var linkReference = new RepositoryLinks
		{
			Origin = new GitCheckoutInformation
			{
				Branch = "branch",
				Remote = "remote",
				Ref = "ref"
			},
			UrlPathPrefix = "",
			Links = [],
			CrossLinks = [],
		};
		var json = RepositoryLinks.Serialize(linkReference);
		// language=json
		json.Should().Be(
			"""
			{
			  "origin": {
			    "branch": "branch",
			    "remote": "remote",
			    "ref": "ref",
			    "name": "unavailable"
			  },
			  "url_path_prefix": "",
			  "links": {},
			  "cross_links": [],
			  "redirects": null
			}
			""");
	}

	[Fact]
	public void Deserializes()
	{
		// language=json
		var json =
			"""
			{
			  "origin": {
			    "branch": "branch",
			    "remote": "remote",
			    "ref": "ref",
			    "name": "unavailable"
			  },
			  "url_path_prefix": "",
			  "links": {},
			  "cross_links": [],
			  "redirects": null
			}
			""";
		var linkReference = RepositoryLinks.Deserialize(json);
		linkReference.Origin.Ref.Should().Be("ref");
	}

}
