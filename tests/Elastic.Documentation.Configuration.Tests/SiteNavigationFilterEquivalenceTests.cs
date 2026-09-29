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
/// SiteNavigationLoader.ForAssembly must produce exactly the navigation that ConfigurationFileProvider.CreateNavigationFile's
/// line-based rewrite of navigation.yml parses to, so consumers can move from the text rewrite to the typed operation.
/// </summary>
public class SiteNavigationFilterEquivalenceTests
{
	[Test]
	public void ForAssembly_BaselineNavigation_EqualsTheTextRewrite()
	{
		var provider = BaselineProvider(skipPrivateRepositories: true);
		var assembly = AssemblyLoader.Create(provider);
		var original = SiteNavigationLoader.Deserialize(
			provider.NavigationFile.FileSystem.File.ReadAllText(provider.NavigationFile.FullName)
		);

		var rewritten = provider.CreateNavigationFile(assembly);
		var fromText = SiteNavigationLoader.Deserialize(rewritten.FileSystem.File.ReadAllText(rewritten.FullName));

		assembly.PrivateRepositories.Should().NotBeEmpty("the baseline must exercise the filter");
		SiteNavigationLoader.ForAssembly(original, assembly, skipPrivateRepositories: true).Should().Be(fromText);
	}

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
	public void ForAssembly_NestedPrivateMounts_EqualsTheTextRewrite()
	{
		var fileSystem = new MockFileSystem();
		var provider = new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fileSystem),
			skipPrivateRepositories: true,
			ConfigurationSource.Embedded
		);
		fileSystem.File.WriteAllText(provider.NavigationFile.FullName, NestedWithPrivateParents);
		var assembly = AssemblyLoader.Deserialize(
			"narrative:\n  repo: git@github.com:elastic/docs-content.git\nreferences:\n  private-a:\n    private: true\n  private-b:\n    private: true",
			skipPrivateRepositories: true
		);

		var rewritten = provider.CreateNavigationFile(assembly);
		var fromText = SiteNavigationLoader.Deserialize(fileSystem.File.ReadAllText(rewritten.FullName));

		SiteNavigationLoader
			.ForAssembly(SiteNavigationLoader.Deserialize(NestedWithPrivateParents), assembly, skipPrivateRepositories: true)
			.Should()
			.Be(fromText);
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
