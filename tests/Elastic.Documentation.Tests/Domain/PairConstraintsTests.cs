// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Codex;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Publishing;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class NavigationInAssemblyConstraintsTests
{
	[Test]
	public void Laws() => NavigationInAssemblyConstraints.All.Laws(Valid(), CounterExamples);

	private static SiteNavigationDefinition Navigation(params string[] sources) =>
		new()
		{
			TableOfContents = [.. sources.Select(s => new SiteNavigationEntry(new SiteTableOfContentsRef(new Uri(s), "reference", [])))]
		};

	private static NavigationInAssembly Valid()
	{
		var narrative = new NarrativeRepository();
		var kibana = new Repository { Name = "kibana" };
		return new NavigationInAssembly(
			Navigation("kibana://reference", "docs-content://get-started"),
			new AssemblyConfiguration
			{
				Narrative = narrative,
				ReferenceRepositories = new Dictionary<string, Repository> { ["kibana"] = kibana },
				AvailableRepositories = new Dictionary<string, Repository> { ["kibana"] = kibana, [narrative.Name] = narrative },
				PrivateRepositories = new Dictionary<string, Repository>(),
				AllPrivateRepositoryNames = new HashSet<string>(),
				Environments = new Dictionary<string, PublishEnvironment>(),
				SharedConfigurations = new Dictionary<string, Repository>()
			}
		);
	}

	private static IReadOnlyDictionary<string, Func<NavigationInAssembly, NavigationInAssembly>> CounterExamples =>
		new Dictionary<string, Func<NavigationInAssembly, NavigationInAssembly>>
		{
			["mount-repository-in-assembly"] = p => p with { Navigation = Navigation("kibana://reference", "beats://reference") }
		};
}

public class DocumentationSetInCodexConstraintsTests
{
	[Test]
	public void Laws() => DocumentationSetInCodexConstraints.All.Laws(Valid(), CounterExamples);

	private static DocumentationSetInCodex Valid()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData> { ["/docs/docset.yml"] = new("project: test") });
		return new DocumentationSetInCodex(
			new DocumentationSetDefinition
			{
				SourceFile = fs.FileInfo.New("/docs/docset.yml"),
				SourceDirectory = fs.DirectoryInfo.New("/docs"),
				FileSystem = fs,
				Project = "test",
				CodexGroup = "observability"
			},
			new CodexConfiguration { Groups = [new CodexGroupDefinition { Id = "observability", Name = "Observability" }] }
		);
	}

	private static IReadOnlyDictionary<string, Func<DocumentationSetInCodex, DocumentationSetInCodex>> CounterExamples =>
		new Dictionary<string, Func<DocumentationSetInCodex, DocumentationSetInCodex>>
		{
			["codex-group-known"] = p => p with { Docset = p.Docset with { CodexGroup = "security" } }
		};
}
