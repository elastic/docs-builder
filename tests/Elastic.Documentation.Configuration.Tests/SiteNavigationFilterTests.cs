// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Publishing;
using Microsoft.Extensions.Logging.Abstractions;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Private repositories are filtered out of the site navigation at the domain level: their mounts and phantoms are
/// dropped and their children promoted. The expectations below are what the former line-based rewrite of
/// navigation.yml produced, which the typed filter was proven equal to before the rewrite was removed.
/// </summary>
public class SiteNavigationFilterTests
{
	// language=yaml
	private const string NestedWithPrivateParents =
		"""
		phantoms:
		  - toc: private-a://hidden
		  - toc: public-repo://visible
		toc:
		  - toc: docs-content://top
		    path_prefix: top
		    children:
		      - toc: private-a://parent
		        path_prefix: parent
		        children:
		          - toc: public-child://nested
		            path_prefix: nested
		            children:
		              - toc: private-b://inside-promoted
		                path_prefix: inside-promoted
		      - toc: public-repo://after
		        path_prefix: after
		      - toc: private-b://leaf
		        path_prefix: leaf
		  - section: Reference
		    children:
		      - toc: private-b://section-child
		        path_prefix: section-child
		        children:
		          - toc: public-repo://promoted-into-section
		            path_prefix: promoted-into-section
		""";

	[Test]
	public void ForAssembly_NestedPrivateMounts_PromotesChildrenAndDropsPrivatePhantoms()
	{
		var assembly = AssemblyLoader.Deserialize(
			"narrative:\n  repo: git@github.com:elastic/docs-content.git\nreferences:\n  private-a:\n    private: true\n  private-b:\n    private: true",
			skipPrivateRepositories: true
		);

		var filtered = SiteNavigationLoader.ForAssembly(
			SiteNavigationLoader.Deserialize(NestedWithPrivateParents),
			assembly,
			skipPrivateRepositories: true
		);

		SiteNavigationText
			.Describe(filtered)
			.ReplaceLineEndings()
			.Should()
			.Be(
				"""
			phantom: public-repo://visible
			toc: docs-content://top path_prefix: top
			  toc: public-child://nested path_prefix: nested
			    toc: private-b://inside-promoted path_prefix: inside-promoted
			  toc: public-repo://after path_prefix: after
			section: Reference
			  toc: public-repo://promoted-into-section path_prefix: promoted-into-section

			""".ReplaceLineEndings()
			);
	}

	[Test]
	public void ForAssembly_PrivateRepositoriesKept_ReturnsTheNavigationUnchanged()
	{
		var assembly = AssemblyLoader.Deserialize("references:\n  private-a:\n    private: true", skipPrivateRepositories: false);
		var navigation = SiteNavigationLoader.Deserialize(NestedWithPrivateParents);

		SiteNavigationLoader.ForAssembly(navigation, assembly, skipPrivateRepositories: false).Should().BeSameAs(navigation);
	}

	[Test]
	public void Load_DuplicatePathPrefixes_ResultCarriesTheViolationsAndNothingElseChecksThem()
	{
		var assembly = AssemblyLoader.Deserialize("references:\n  private-a:\n    private: true", skipPrivateRepositories: false);
		var file = new MockFileSystem().FileInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "navigation.yml"));
		const string yaml = "toc:\n  - toc: docs-content://a\n    path_prefix: same\n  - toc: other://b\n    path_prefix: same\n";

		var result = SiteNavigationLoader.Load(yaml, file, assembly, skipPrivateRepositories: false);

		result.HasErrors.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Diagnostics.Should().Contain(d => d.RuleId == "path-prefixes-unique" && d.File == file.FullName);
	}

	/// <summary>
	/// A private mount at the top level promotes its children to the top level. The text rewrite cannot express this: it
	/// re-indents promoted lines by four columns less, which for a top-level entry yields invalid YAML.
	/// </summary>
	[Test]
	public void WithoutRepositories_TopLevelPrivateMount_PromotesItsChildrenToTheTopLevel()
	{
		var navigation = SiteNavigationLoader.Deserialize(
			"""
			toc:
			  - toc: private-a://top-level
			    path_prefix: top-level
			    children:
			      - toc: public-repo://promoted
			        path_prefix: promoted
			  - toc: public-repo://after
			    path_prefix: after
			"""
		);

		var filtered = navigation.WithoutRepositories(new HashSet<string> { "private-a" });

		filtered.TableOfContents.OfCase<SiteTableOfContentsRef>().Select(t => t.PathPrefix).Should().Equal("promoted", "after");
	}
}
