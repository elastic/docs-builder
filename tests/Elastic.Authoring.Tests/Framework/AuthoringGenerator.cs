// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Versions;
using Elastic.Markdown;
using Elastic.Markdown.IO;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// The C# equivalent of <c>Setup.Generator</c> from <c>tests/authoring/Framework/Setup.fs</c>.
/// Builds a <see cref="DocumentationGenerator"/> backed by a <see cref="MockFileSystem"/>,
/// runs <see cref="DocumentationGenerator.GenerateAll"/>, and returns materialized results.
/// </summary>
public static class AuthoringGenerator
{
	public static async Task<GeneratorResults> GenerateAsync(SetupOptions options, IReadOnlyCollection<TestFile> files)
	{
		var fileData = files.ToDictionary(f => $"docs/{f.Path}", f => new MockFileData(f.Contents));

		var mockFileSystem = new MockFileSystem(
			fileData,
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);

		DocSetYamlGenerator.Generate(mockFileSystem, options.DocsetProducts);

		var collector = new TestDiagnosticsCollector();
		var versionsConfig = AuthoringConfiguration.CreateVersionsConfiguration();
		var productsConfig = AuthoringConfiguration.CreateProductsConfiguration(versionsConfig);

		// Bare .git directory anchors FindGitRoot so OutputDirectory = WorkingDirectoryRoot/.artifacts/docs/html.
		// Missing config triggers IsLegacyTestWithoutGitLayout → canned data, which we override
		// with Git = Unavailable because authoring tests exercise rendering, not real git metadata.
		var gitPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".git");
		if (!mockFileSystem.Directory.Exists(gitPath))
			mockFileSystem.Directory.CreateDirectory(gitPath);

		var invocation = mockFileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs"));

		var docFs = DocumentationFileSystem.Resolve(
			invocation,
			new DocumentationScopeOptions { Inner = mockFileSystem, Git = GitCheckoutInformation.Unavailable }
		);

		var configurationFileProvider = new ConfigurationFileProvider(new TestLoggerFactory(), new ConfigurationFileSystem(mockFileSystem));

		var configurationContext = new ConfigurationContext
		{
			VersionsConfiguration = versionsConfig,
			ConfigurationFileProvider = configurationFileProvider,
			Endpoints = new DocumentationEndpoints { Elasticsearch = ElasticsearchEndpoint.Default },
			ProductsConfiguration = productsConfig,
			LegacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] },
			SearchConfiguration = new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
		};

		var context = new BuildContext(collector, docFs, configurationContext)
		{
			UrlPathPrefix = options.UrlPathPrefix,
			CanonicalBaseUrl = new Uri("https://www.elastic.co/")
		};

		var loggerFactory = new TestLoggerFactory();
		var conversionCollector = new TestConversionCollector();
		var crossLinkResolver = new TestCrossLinkResolver(context.Configuration);
		var set = new DocumentationSet(context, loggerFactory, crossLinkResolver);
		var generator = new DocumentationGenerator(set, loggerFactory, conversionCollector: conversionCollector);

		var generatorContext = new GeneratorContext
		{
			Collector = collector,
			ConversionCollector = conversionCollector,
			Set = set,
			Generator = generator,
			ReadFileSystem = docFs,
			WriteFileSystem = mockFileSystem
		};

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
		var ctx = cts.Token;

		_ = collector.StartAsync(ctx);
		await generator.GenerateAll(ctx);
		await collector.StopAsync(ctx);

		// Materialize all results (including MinimalParse) so the lazy F# Seq-per-assertion
		// pattern is gone — each MarkdownResult is computed once.
		var markdownResults = await Task.WhenAll(conversionCollector
			.Results
			.Values
			.Select(async cr =>
			{
				var minimalParse = await cr.File.MinimalParseAsync(path => set.TryFindDocumentByRelativePath(path), ctx);
				return new MarkdownResult
				{
					File = cr.File,
					Document = cr.Document,
					Html = cr.Html,
					MinimalParse = minimalParse,
					Context = generatorContext
				};
			}));

		return new GeneratorResults { Context = generatorContext, MarkdownResults = markdownResults };
	}
}
