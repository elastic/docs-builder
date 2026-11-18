// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// The typed operations on a <see cref="SiteNavigationDefinition"/> (<c>WithoutRepositories</c>, <c>WithAppended</c>) leave the navigation the build reads
/// from the line-based filter of <c>ConfigurationFileProvider.CreateNavigationFile</c>. The build still uses the file filter; the operations are the
/// target for replacing it, and these tests keep the two equal until then.
/// </summary>
public class SiteNavigationFilterEquivalenceTests
{
	// language=yaml
	private const string LocalDocsBuilderToc =
		"""
		toc:
		  - toc: docs-builder://
		    path_prefix: reference/docs-builder
		    children:
		      - toc: docs-builder://development
		        path_prefix: reference/docs-builder/dev
		        children:
		          - toc: docs-builder://development/link-validation
		            path_prefix: reference/docs-builder/dev/link-val
		""";

	private static string Typed(string yaml, AssemblyConfiguration config)
	{
		var navigation = SiteNavigationLoader
			.Deserialize(yaml)
			.WithoutRepositories(new HashSet<string>(config.PrivateRepositories.Keys))
			.WithAppended(
				config.AvailableRepositories.TryGetValue("docs-builder", out var docsBuilder) &&
					docsBuilder is { Skip: false, Path: not null }
					? SiteNavigationLoader.Deserialize(LocalDocsBuilderToc).TableOfContents
					: []
			);
		return SiteNavDump.ToText(navigation);
	}

	[Test]
	[Arguments("navigation.yml", false)]
	[Arguments("navigation_preview.yml", true)]
	public void WithoutRepositories_BaselineNavigation_EqualsTheFileFilter(string name, bool preview)
	{
		using var provider = GoldenConfig.CreateProvider(skipPrivateRepositories: true);
		if (preview)
			_ = provider.UseNavigationPreview();
		var config = AssemblyLoader.Create(provider);
		var filteredText = provider.CreateNavigationFile(config).OpenText().ReadToEnd();

		Typed(GoldenConfig.BaselineText(name), config).Should().Be(SiteNavDump.ToText(SiteNavigationLoader.Deserialize(filteredText)));
	}

	[Test]
	public void WithoutRepositories_NestedPrivateMountsAndPhantoms_EqualsTheFileFilter()
	{
		// language=yaml
		const string yaml =
			"""
			phantoms:
			  - toc: closed-a://hidden
			  - toc: open://visible
			toc:
			  - toc: docs-content://top
			    path_prefix: top
			    children:
			      - toc: closed-a://parent
			        path_prefix: parent
			        children:
			          - toc: open-child://nested
			            path_prefix: nested
			      - toc: open://after
			        path_prefix: after
			      - toc: closed-b://leaf
			        path_prefix: leaf
			  - section: Reference
			    children:
			      - toc: closed-b://section-child
			        path_prefix: section-child
			        children:
			          - toc: open://promoted
			            path_prefix: promoted
			  - toc: open://last
			    path_prefix: last
			""";
		using var provider = GoldenConfig.CreateProvider(GoldenConfig.Navigation, yaml, skipPrivateRepositories: true);
		var config = AssemblyLoader.Deserialize(
			"references:\n  open:\n  open-child:\n  closed-a:\n    private: true\n  closed-b:\n    private: true\n",
			skipPrivateRepositories: true
		);
		var filteredText = provider.CreateNavigationFile(config).OpenText().ReadToEnd();

		Typed(yaml, config).Should().Be(SiteNavDump.ToText(SiteNavigationLoader.Deserialize(filteredText)));
	}
}
