// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Codex;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests.Codex;

public class CodexDocumentationSetReferenceSecurityTests
{
	[Fact]
	public void Name_PathTraversal_ThrowsException()
	{
		var docSet = new CodexDocumentationSetReference { Branch = "main" };

		var act = () => docSet.Name = "../etc";

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void RepoName_PathTraversal_ThrowsException()
	{
		var docSet = new CodexDocumentationSetReference { Name = "test", Branch = "main" };

		var act = () => docSet.RepoName = "../../sensitive";

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Path_PathTraversal_ThrowsException()
	{
		var docSet = new CodexDocumentationSetReference { Name = "test", Branch = "main" };

		var act = () => docSet.Path = "../../../etc/passwd";

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Category_PathTraversal_ThrowsException()
	{
		var docSet = new CodexDocumentationSetReference { Name = "test", Branch = "main" };

		var act = () => docSet.Category = "../bad";

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void ValidValues_ShouldSucceed()
	{
		var docSet = new CodexDocumentationSetReference
		{
			Name = "valid-repo",
			Branch = "main",
			RepoName = "valid_name",
			Path = "docs/api",
			Category = "observability"
		};

		docSet.Name.Should().Be("valid-repo");
		docSet.ResolvedRepoName.Should().Be("valid_name");
		docSet.Path.Should().Be("docs/api");
		docSet.Category.Should().Be("observability");
	}
}
