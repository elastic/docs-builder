// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Isolated;
using Nullean.Snapshots;
using AssemblerParser = Elastic.Documentation.Assembler.Building.ExporterParser;
using IsolatedParser = Elastic.Documentation.Isolated.ExporterParser;

namespace Elastic.Documentation.Build.Tests.Snapshots;

/// <summary>
/// What the two build option records accept: the <c>--exporters</c> parsers (the assembler and the isolated build each have
/// their own), the data annotations on the isolated options, and the assume-build rule of the assembler build.
/// </summary>
public class BuildOptionsSnapshotTests
{
	private static readonly string[] ExporterInputs =
	[
		"html",
		"HTML",
		"llm",
		"llmtext",
		"es",
		"elasticsearch",
		"config",
		"configuration",
		"links",
		"linkmetadata",
		"state",
		"documentationstate",
		"redirect",
		"redirects",
		"okf",
		"pagefind",
		"none",
		"default",
		"metadata",
		"html,llm",
		"html, llm ,es",
		"html,html",
		"html,,llm",
		",",
		"",
		"   ",
		"default,es",
		"metadata,pagefind",
		"none,html",
		"nope",
		"html,nope",
		"Html;llm",
		"all",
		"sitemap"
	];

	private static string TryParse(Func<string, (bool Ok, IReadOnlySet<Exporter> Set)> parse, string input)
	{
		try
		{
			var (ok, set) = parse(input);
			return $"{ok}: [{string.Join(", ", set.Select(e => e.ToString()).Order(StringComparer.Ordinal))}]";
		}
		catch (Exception e)
		{
			return $"throws {e.GetType().Name}: {e.Message}";
		}
	}

	private static string ParserTable(Func<string, (bool Ok, IReadOnlySet<Exporter> Set)> parse)
	{
		var sb = new StringBuilder();
		foreach (var input in ExporterInputs)
			_ = sb.Append($"'{input}' => {TryParse(parse, input)}\n");
		return sb.ToString();
	}

	[Test]
	public void AssemblerExporterParser_Table() =>
		ParserTable(s =>
		{
			var ok = new AssemblerParser().TryParse(s, out var set);
			return (ok, set);
		}).Should().MatchSnapshot();

	[Test]
	public void IsolatedExporterParser_Table() =>
		ParserTable(s =>
		{
			var ok = new IsolatedParser().TryParse(s, out var set);
			return (ok, set);
		}).Should().MatchSnapshot();

	[Test]
	public void ExportOptionPresets_AreListedPerPreset()
	{
		string Names(IEnumerable<Exporter> e) => string.Join(", ", e.Select(x => x.ToString()).Order(StringComparer.Ordinal));

		$"Default: {Names(ExportOptions.Default)}\nMetadataOnly: {Names(ExportOptions.MetadataOnly)}\nValidation: {Names(ExportOptions.Validation)}".Should().MatchInlineSnapshot(
			"""
			Default: Configuration, DocumentationState, Html, LLMText, LinkMetadata, Pagefind, Redirects
			MetadataOnly: Configuration, DocumentationState, LinkMetadata, Redirects
			Validation: Html, Pagefind
			"""
		);
	}

	private static string Validate(IsolatedBuildOptions options)
	{
		var results = new List<ValidationResult>();
		var valid = Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
		return $"valid={valid} [{string.Join("; ", results.Select(r => $"{string.Join(",", r.MemberNames)}: {r.ErrorMessage}"))}]";
	}

	[Test]
	public void IsolatedBuildOptions_DataAnnotationsTable()
	{
		var rows = new (string Name, IsolatedBuildOptions Options)[]
		{
			("defaults", new IsolatedBuildOptions()),
			("https canonical url", new IsolatedBuildOptions { CanonicalBaseUrl = new Uri("https://docs.example.com") }),
			("http canonical url", new IsolatedBuildOptions { CanonicalBaseUrl = new Uri("http://docs.example.com/path/") }),
			("ftp canonical url", new IsolatedBuildOptions { CanonicalBaseUrl = new Uri("ftp://docs.example.com") }),
			("file canonical url", new IsolatedBuildOptions { CanonicalBaseUrl = new Uri("file:///docs") }),
			("mailto canonical url", new IsolatedBuildOptions { CanonicalBaseUrl = new Uri("mailto:someone@example.com") }),
			("relative canonical url", new IsolatedBuildOptions { CanonicalBaseUrl = new Uri("/docs", UriKind.Relative) }),
			("everything set", new IsolatedBuildOptions
			{
				Path = new DirectoryInfo("/does/not/exist"),
				Output = new DirectoryInfo("/out"),
				PathPrefix = "/docs",
				Force = true,
				Strict = true,
				AllowIndexing = false,
				MetadataOnly = true,
				CanonicalBaseUrl = new Uri("https://docs.example.com"),
				SkipApi = true,
				SkipCrossLinks = true
			})
		};
		string.Join('\n', rows.Select(r => $"{r.Name}: {Validate(r.Options)}")).Should().MatchSnapshot();
	}

	[Test]
	public void OptionRecords_DefaultsAreAllUnset()
	{
		var isolated = new IsolatedBuildOptions();
		var assembler = new AssemblerBuildOptions();
		$"isolated: Path={isolated.Path?.ToString() ?? "null"} Output={isolated.Output?.ToString() ?? "null"} PathPrefix={isolated.PathPrefix ?? "null"} Force={isolated.Force?.ToString() ?? "null"} Strict={isolated.Strict?.ToString() ?? "null"} AllowIndexing={isolated.AllowIndexing?.ToString() ?? "null"} MetadataOnly={isolated.MetadataOnly?.ToString() ?? "null"} Exporters={isolated.Exporters?.ToString() ?? "null"} CanonicalBaseUrl={isolated.CanonicalBaseUrl?.ToString() ?? "null"} SkipApi={isolated.SkipApi} SkipCrossLinks={isolated.SkipCrossLinks}\nassembler: Strict={assembler.Strict?.ToString() ?? "null"} Environment={assembler.Environment ?? "null"} MetadataOnly={assembler.MetadataOnly?.ToString() ?? "null"} ShowHints={assembler.ShowHints?.ToString() ?? "null"} Exporters={assembler.Exporters?.ToString() ?? "null"} AssumeBuild={assembler.AssumeBuild?.ToString() ?? "null"}".Should().MatchInlineSnapshot(
			"""
			isolated: Path=null Output=null PathPrefix=null Force=null Strict=null AllowIndexing=null MetadataOnly=null Exporters=null CanonicalBaseUrl=null SkipApi=False SkipCrossLinks=False
			assembler: Strict=null Environment=null MetadataOnly=null ShowHints=null Exporters=null AssumeBuild=null
			"""
		);
	}

	[Test]
	public async Task AssemblerBuild_AssumeBuildOnCi_TruthTable()
	{
		var sb = new StringBuilder();
		foreach (var ci in new[] { true, false })
		{
			foreach (bool? assume in new bool?[] { null, true, false })
			{
				var fs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
				var configContext = TestHelpers.CreateConfigurationContext(fs);
				var assemblyConfig = AssemblyConfiguration.Create(configContext.ConfigurationFileProvider);
				var collector = new DiagnosticsCollector([]);
				var env = ci ? MockEnvironmentVariables.CreateCI() : MockEnvironmentVariables.CreateLocal();
				var service = new AssemblerBuildService(new TestLoggerFactory(), assemblyConfig, configContext, new NullCoreService(), env);
				string outcome;
				try
				{
					var result = await service.BuildAll(
						collector,
						new AssemblerBuildOptions { AssumeBuild = assume },
						CheckoutsFileSystem.FromWorkingDirectory(fs),
						TestContext.Current!.Execution.CancellationToken
					);
					outcome = $"returns {result}";
				}
				catch (InvalidOperationException e)
				{
					outcome = $"throws InvalidOperationException: {e.Message}";
				}
				catch (Exception e)
				{
					outcome = $"proceeds past the assume-build check and then throws {e.GetType().Name}: {e.Message.Split('\n')[0]}";
				}
				_ = sb.Append($"ci={ci} assume-build={assume?.ToString() ?? "unset"} => {outcome}\n");
			}
		}
		sb.ToString().Should().MatchSnapshot();
	}
}
