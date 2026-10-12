// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Codex;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Publishing;
using Nullean.Entities.CsCheck;
using Nullean.Entities.CsCheck.FileSystem;

namespace Elastic.Documentation.Tests.Domain;

public class NavigationInAssemblyConstraintsTests
{
	/// <summary>The assembly publishes every repository the navigation mounts, because a mount can only be built from one.</summary>
	private static NavigationInAssembly Published(NavigationInAssembly p) =>
		p with
		{
			Assembly = p.Assembly with
			{
				ReferenceRepositories = new Dictionary<string, Repository>(p.Assembly.ReferenceRepositories)
					.Concat(
						SiteNavigationConstraints
							.AllMounts(p.Navigation)
							.Select(m => m.Source.Scheme)
							.Distinct(StringComparer.OrdinalIgnoreCase)
							.Where(scheme => !p.Assembly.AvailableRepositories.Keys.Contains(scheme, StringComparer.OrdinalIgnoreCase))
							.Select(scheme => KeyValuePair.Create(scheme, new Repository { Name = scheme }))
					)
					.ToDictionary()
			}
		};

	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest
			.For(
				NavigationInAssemblyConstraints.All,
				DomainGen.Of<NavigationInAssembly>(
					o => AssemblyConstraintsTests
						.Configure(o)
						.Normalize<SiteNavigationDefinition>(SiteNavigationConstraintsTests.Valid)
						.Normalize<NavigationInAssembly>(Published)
				)
			)
			.CounterExamples(CounterExamples)
			.Axes();

	private static IReadOnlyDictionary<string, Func<NavigationInAssembly, NavigationInAssembly>> CounterExamples =>
		new Dictionary<string, Func<NavigationInAssembly, NavigationInAssembly>>
		{
			// a mount of a repository the assembly does not publish
			["mount-repository-in-assembly"] =
				p =>
					p with
					{
						Navigation = p.Navigation with
						{
							TableOfContents =
							[
								.. p.Navigation.TableOfContents,
								new SiteNavigationEntry(new SiteTableOfContentsRef(
									new Uri("unpublished://reference"),
									"reference/unpublished",
									[]
								))
							]
						}
					}
		};
}

public class DocumentationSetInCodexConstraintsTests
{
	// codex.group refers to a group the codex site defines: the declared reference does it, so the groups are the generated ones
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest
			.For(
				DocumentationSetInCodexConstraints.All,
				DomainGen.Of<DocumentationSetInCodex>(
					o => o.UseFileSystem().Reference<DocumentationSetDefinition, CodexGroupDefinition, string>(
						d => d.CodexGroup!,
						g => g.Id
					)
				)
			)
			.Options(o => o.UseFileSystem())
			.CounterExamples(CounterExamples)
			.Axes();

	private static IReadOnlyDictionary<string, Func<DocumentationSetInCodex, DocumentationSetInCodex>> CounterExamples =>
		new Dictionary<string, Func<DocumentationSetInCodex, DocumentationSetInCodex>>
		{
			["codex-group-known"] = p => p with { Docset = p.Docset with { CodexGroup = "not-a-group-of-the-codex" } }
		};
}
