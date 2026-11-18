// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public static class SiteNavigationTestFixture
{
	public static MockFileSystem CreateMultiRepositoryFileSystem()
	{
		var fileSystem = new MockFileSystem();

		// Repository 1: serverless-observability
		SetupServerlessObservabilityRepository(fileSystem);

		// Repository 2: serverless-search
		SetupServerlessSearchRepository(fileSystem);

		// Repository 3: serverless-security
		SetupServerlessSecurityRepository(fileSystem);

		// Repository 4: platform
		SetupPlatformRepository(fileSystem);

		// Repository 5: elasticsearch-reference
		SetupElasticsearchReferenceRepository(fileSystem);

		return fileSystem;
	}

	private static void SetupServerlessObservabilityRepository(MockFileSystem fileSystem)
	{
		var baseDir = "/checkouts/current/observability";
		fileSystem.AddDirectory(baseDir);
		fileSystem.AddDirectory($"{baseDir}/.git");

		// Add docset.yml
		// language=yaml
		var docsetYaml =
			"""
		                 project: serverless-observability
		                 toc:
		                   - file: index.md
		                   - folder: getting-started
		                     children:
		                       - file: quick-start.md
		                       - file: installation.md
		                   - folder: monitoring
		                     children:
		                       - file: index.md
		                       - file: logs.md
		                       - file: metrics.md
		                       - file: traces.md
		                 """;
		fileSystem.AddFile($"{baseDir}/docs/docset.yml", new MockFileData(docsetYaml));

		// Add markdown files
		fileSystem.AddFile($"{baseDir}/docs/index.md", new MockFileData("# Serverless Observability"));
		fileSystem.AddFile($"{baseDir}/docs/getting-started/quick-start.md", new MockFileData("# Quick Start"));
		fileSystem.AddFile($"{baseDir}/docs/getting-started/installation.md", new MockFileData("# Installation"));
		fileSystem.AddFile($"{baseDir}/docs/monitoring/index.md", new MockFileData("# Monitoring"));
		fileSystem.AddFile($"{baseDir}/docs/monitoring/logs.md", new MockFileData("# Logs"));
		fileSystem.AddFile($"{baseDir}/docs/monitoring/metrics.md", new MockFileData("# Metrics"));
		fileSystem.AddFile($"{baseDir}/docs/monitoring/traces.md", new MockFileData("# Traces"));
	}

	private static void SetupServerlessSearchRepository(MockFileSystem fileSystem)
	{
		var baseDir = "/checkouts/current/serverless-search";
		fileSystem.AddDirectory(baseDir);
		fileSystem.AddDirectory($"{baseDir}/.git");

		// Add docset.yml
		// language=yaml
		var docsetYaml =
			"""
		                 project: serverless-search
		                 toc:
		                   - file: index.md
		                   - folder: indexing
		                     children:
		                       - file: index.md
		                       - file: documents.md
		                       - file: bulk-api.md
		                   - folder: searching
		                     children:
		                       - file: index.md
		                       - file: query-dsl.md
		                 """;
		fileSystem.AddFile($"{baseDir}/docs/docset.yml", new MockFileData(docsetYaml));

		// Add markdown files
		fileSystem.AddFile($"{baseDir}/docs/index.md", new MockFileData("# Serverless Search"));
		fileSystem.AddFile($"{baseDir}/docs/indexing/index.md", new MockFileData("# Indexing"));
		fileSystem.AddFile($"{baseDir}/docs/indexing/documents.md", new MockFileData("# Documents"));
		fileSystem.AddFile($"{baseDir}/docs/indexing/bulk-api.md", new MockFileData("# Bulk API"));
		fileSystem.AddFile($"{baseDir}/docs/searching/index.md", new MockFileData("# Searching"));
		fileSystem.AddFile($"{baseDir}/docs/searching/query-dsl.md", new MockFileData("# Query DSL"));
	}

	private static void SetupServerlessSecurityRepository(MockFileSystem fileSystem)
	{
		var baseDir = "/checkouts/current/serverless-security";
		fileSystem.AddDirectory(baseDir);
		fileSystem.AddDirectory($"{baseDir}/.git");

		// Add docset.yml with underscore prefix
		// language=yaml
		var docsetYaml =
			"""
		                 project: serverless-security
		                 toc:
		                   - file: index.md
		                   - folder: authentication
		                     children:
		                       - file: index.md
		                       - file: api-keys.md
		                       - file: oauth.md
		                   - folder: authorization
		                     children:
		                       - file: index.md
		                       - file: rbac.md
		                 """;
		fileSystem.AddFile($"{baseDir}/docs/_docset.yml", new MockFileData(docsetYaml));

		// Add markdown files
		fileSystem.AddFile($"{baseDir}/docs/index.md", new MockFileData("# Serverless Security"));
		fileSystem.AddFile($"{baseDir}/docs/authentication/index.md", new MockFileData("# Authentication"));
		fileSystem.AddFile($"{baseDir}/docs/authentication/api-keys.md", new MockFileData("# API Keys"));
		fileSystem.AddFile($"{baseDir}/docs/authentication/oauth.md", new MockFileData("# OAuth"));
		fileSystem.AddFile($"{baseDir}/docs/authorization/index.md", new MockFileData("# Authorization"));
		fileSystem.AddFile($"{baseDir}/docs/authorization/rbac.md", new MockFileData("# RBAC"));
	}

	private static void SetupPlatformRepository(MockFileSystem fileSystem)
	{
		var baseDir = "/checkouts/current/platform";
		fileSystem.AddDirectory(baseDir);
		fileSystem.AddDirectory($"{baseDir}/.git");

		// Add docset.yml
		// language=yaml
		var docsetYaml =
			"""
		                 project: platform
		                 toc:
		                   - file: index.md
		                   - toc: deployment-guide
		                   - toc: cloud-guide
		                 """;
		fileSystem.AddFile($"{baseDir}/docs/docset.yml", new MockFileData(docsetYaml));
		fileSystem.AddFile($"{baseDir}/docs/index.md", new MockFileData("# Platform"));

		// Deployment guide sub-TOC
		var deploymentBaseDir = $"{baseDir}/docs/deployment-guide";
		fileSystem.AddDirectory(deploymentBaseDir);
		// language=yaml
		var deploymentTocYaml =
			"""
		                        toc:
		                          - file: index.md
		                          - folder: self-managed
		                            children:
		                              - file: installation.md
		                              - file: configuration.md
		                        """;
		fileSystem.AddFile($"{deploymentBaseDir}/toc.yml", new MockFileData(deploymentTocYaml));
		fileSystem.AddFile($"{deploymentBaseDir}/index.md", new MockFileData("# Deployment Guide"));
		fileSystem.AddFile($"{deploymentBaseDir}/self-managed/installation.md", new MockFileData("# Installation"));
		fileSystem.AddFile($"{deploymentBaseDir}/self-managed/configuration.md", new MockFileData("# Configuration"));

		// Cloud guide sub-TOC
		var cloudBaseDir = $"{baseDir}/docs/cloud-guide";
		fileSystem.AddDirectory(cloudBaseDir);
		// language=yaml
		var cloudTocYaml =
			"""
		                   toc:
		                     - file: index.md
		                     - folder: aws
		                       children:
		                         - file: setup.md
		                     - folder: azure
		                       children:
		                         - file: setup.md
		                   """;
		fileSystem.AddFile($"{cloudBaseDir}/toc.yml", new MockFileData(cloudTocYaml));
		fileSystem.AddFile($"{cloudBaseDir}/index.md", new MockFileData("# Cloud Guide"));
		fileSystem.AddFile($"{cloudBaseDir}/aws/setup.md", new MockFileData("# AWS Setup"));
		fileSystem.AddFile($"{cloudBaseDir}/azure/setup.md", new MockFileData("# Azure Setup"));
	}

	private static void SetupElasticsearchReferenceRepository(MockFileSystem fileSystem)
	{
		var baseDir = "/checkouts/current/elasticsearch-reference";
		fileSystem.AddDirectory(baseDir);
		fileSystem.AddDirectory($"{baseDir}/.git");

		// Add docset.yml
		// language=yaml
		var docsetYaml =
			"""
		                 project: elasticsearch-reference
		                 toc:
		                   - file: index.md
		                   - folder: rest-apis
		                     children:
		                       - file: index.md
		                       - file: document-apis.md
		                       - file: search-apis.md
		                   - folder: query-dsl
		                     children:
		                       - file: index.md
		                       - file: term-queries.md
		                       - file: full-text-queries.md
		                 """;
		fileSystem.AddFile($"{baseDir}/docs/docset.yml", new MockFileData(docsetYaml));

		// Add markdown files
		fileSystem.AddFile($"{baseDir}/docs/index.md", new MockFileData("# Elasticsearch Reference"));
		fileSystem.AddFile($"{baseDir}/docs/rest-apis/index.md", new MockFileData("# REST APIs"));
		fileSystem.AddFile($"{baseDir}/docs/rest-apis/document-apis.md", new MockFileData("# Document APIs"));
		fileSystem.AddFile($"{baseDir}/docs/rest-apis/search-apis.md", new MockFileData("# Search APIs"));
		fileSystem.AddFile($"{baseDir}/docs/query-dsl/index.md", new MockFileData("# Query DSL"));
		fileSystem.AddFile($"{baseDir}/docs/query-dsl/term-queries.md", new MockFileData("# Term Queries"));
		fileSystem.AddFile($"{baseDir}/docs/query-dsl/full-text-queries.md", new MockFileData("# Full Text Queries"));
	}

	/// <summary>
	/// Repositories that exercise mounts <see cref="CreateMultiRepositoryFileSystem"/> does not: a narrative
	/// repository with nested tocs, <c>apm-server</c> and <c>apm-agents-java</c> whose prefixes start the same way, hidden pages,
	/// <c>kibana</c>, <c>elasticsearch</c> with release notes, and <c>unlisted</c>, which no navigation file mounts.
	/// </summary>
	public static MockFileSystem CreateExtendedRepositoryFileSystem()
	{
		var fileSystem = new MockFileSystem();

		AddRepository(
			fileSystem,
			NarrativeRepository.RepositoryName,
			// language=yaml
			"""
			project: docs-content
			toc:
			  - file: index.md
			  - file: about.md
			  - hidden: legal.md
			  - toc: get-started
			  - toc: reference
			  - toc: release-notes
			""",
			"index.md",
			"about.md",
			"legal.md"
		);
		AddToc(
			fileSystem,
			NarrativeRepository.RepositoryName,
			"get-started",
			// language=yaml
			"""
			toc:
			  - file: index.md
			  - file: tutorial.md
			""",
			"index.md",
			"tutorial.md"
		);
		AddToc(
			fileSystem,
			NarrativeRepository.RepositoryName,
			"reference",
			// language=yaml
			"""
			toc:
			  - file: index.md
			  - toc: apm
			  - toc: apm-agents
			  - folder: glossary
			    children:
			      - file: index.md
			      - hidden: draft.md
			      - file: terms.md
			""",
			"index.md",
			"glossary/index.md",
			"glossary/draft.md",
			"glossary/terms.md"
		);
		AddToc(fileSystem, NarrativeRepository.RepositoryName, "reference/apm", TocWith("overview.md"), "index.md", "overview.md");
		AddToc(fileSystem, NarrativeRepository.RepositoryName, "reference/apm-agents", TocWith("overview.md"), "index.md", "overview.md");
		AddToc(
			fileSystem,
			NarrativeRepository.RepositoryName,
			"release-notes",
			// language=yaml
			"""
			toc:
			  - file: index.md
			  - toc: elasticsearch-clients
			""",
			"index.md"
		);
		AddToc(
			fileSystem,
			NarrativeRepository.RepositoryName,
			"release-notes/elasticsearch-clients",
			TocWith("overview.md"),
			"index.md",
			"overview.md"
		);

		AddRepository(
			fileSystem,
			"apm-server",
			// language=yaml
			"""
			project: apm-server
			toc:
			  - file: index.md
			  - hidden: internal.md
			  - folder: setup
			    children:
			      - file: index.md
			      - hidden: secret.md
			      - file: install.md
			""",
			"index.md",
			"internal.md",
			"setup/index.md",
			"setup/secret.md",
			"setup/install.md"
		);
		AddRepository(fileSystem, "apm-agents-java", DocsetWith("apm-agents-java", "setup.md"), "index.md", "setup.md");
		AddRepository(fileSystem, "kibana", DocsetWith("kibana", "dashboards.md"), "index.md", "dashboards.md");
		AddRepository(
			fileSystem,
			"elasticsearch",
			// language=yaml
			"""
			project: elasticsearch
			toc:
			  - file: index.md
			  - toc: reference
			  - toc: release-notes
			""",
			"index.md"
		);
		AddToc(fileSystem, "elasticsearch", "reference", TocWith("rest-apis.md"), "index.md", "rest-apis.md");
		AddToc(fileSystem, "elasticsearch", "release-notes", TocWith("changelog.md"), "index.md", "changelog.md");
		AddRepository(fileSystem, "unlisted", DocsetWith("unlisted", "page.md"), "index.md", "page.md");
		return fileSystem;
	}

	private static string TocWith(string page) => $"toc:\n  - file: index.md\n  - file: {page}\n";

	private static string DocsetWith(string project, string page) => $"project: {project}\ntoc:\n  - file: index.md\n  - file: {page}\n";

	/// <summary>Adds <paramref name="repository"/> as a checkout with a docset and a page, titled <c>repository: path</c>, for each of <paramref name="pages"/>.</summary>
	public static void AddRepository(MockFileSystem fileSystem, string repository, string docsetYaml, params string[] pages)
	{
		var baseDir = $"/checkouts/current/{repository}";
		fileSystem.AddDirectory($"{baseDir}/.git");
		fileSystem.AddFile($"{baseDir}/docs/docset.yml", new MockFileData(docsetYaml));
		foreach (var page in pages)
			fileSystem.AddFile($"{baseDir}/docs/{page}", new MockFileData($"# {repository}: {page.Replace(".md", string.Empty)}"));
	}

	/// <summary>Adds a nested <c>toc.yml</c> below the docs folder of <paramref name="repository"/> and the pages it lists, relative to that folder.</summary>
	public static void AddToc(MockFileSystem fileSystem, string repository, string tocPath, string tocYaml, params string[] pages)
	{
		var dir = $"/checkouts/current/{repository}/docs/{tocPath}";
		fileSystem.AddFile($"{dir}/toc.yml", new MockFileData(tocYaml));
		foreach (var page in pages)
			fileSystem.AddFile($"{dir}/{page}", new MockFileData($"# {repository}: {tocPath}/{page.Replace(".md", string.Empty)}"));
	}

	public static TestDocumentationSetContext CreateAssemblerContext(
		MockFileSystem fileSystem,
		string repositoryPath,
		TestDiagnosticsCollector? collector = null
	)
	{
		var context = CreateContext(fileSystem, repositoryPath, collector);
		context.BuildType = BuildType.Assembler;
		return context;
	}

	public static TestDocumentationSetContext CreateContext(
		MockFileSystem fileSystem,
		string repositoryPath,
		TestDiagnosticsCollector? collector = null
	)
	{
		var sourceDir = fileSystem.DirectoryInfo.New($"{repositoryPath}/docs");
		var outputDir = fileSystem.DirectoryInfo.New("/output");

		// Try to find docset.yml or _docset.yml
		var configPath = fileSystem.File.Exists($"{sourceDir.FullName}/docset.yml")
			? fileSystem.FileInfo.New($"{sourceDir.FullName}/docset.yml")
			: fileSystem.FileInfo.New($"{sourceDir.FullName}/_docset.yml");

		// Extract repository name from path (e.g., "/checkouts/current/platform" -> "platform")
		var repositoryName = fileSystem.Path.GetFileName(repositoryPath);

		return new TestDocumentationSetContext(fileSystem, sourceDir, outputDir, configPath, repositoryName, collector);
	}
}
