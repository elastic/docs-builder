// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Text.Json.Nodes;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging.Abstractions;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// Golden snapshots for the sampled public <c>changelog.yml</c> files in <c>Baseline/changelog/</c> and the shipped
/// template, loaded against the baseline catalog. <c>ChangelogConfiguration</c> moves into the domain largely as-is,
/// so the value is projected by reflection over public properties: a renamed property shows up as a snapshot diff.
/// </summary>
public class ChangelogConfigurationGoldenTests
{
	public static IEnumerable<string> ChangelogFiles() =>
		Directory.EnumerateFiles(BaselineFile("changelog"), "*.yml").Select(Path.GetFileName).Order(StringComparer.Ordinal)!;

	[Test]
	[MethodDataSource(nameof(ChangelogFiles))]
	public async Task ChangelogConfiguration_Baseline_MatchesGolden(string file) =>
		await AssertChangelog(
			$"changelog-{Path.GetFileNameWithoutExtension(file).Replace(".changelog", "")}",
			BaselineFile("changelog", file)
		);

	[Test]
	public async Task ChangelogConfiguration_Template_MatchesGolden() =>
		await AssertChangelog("changelog-example-template", BaselineFile("config", "changelog.example.yml"));

	internal static async Task<ChangelogConfiguration?> LoadChangelog(string sourceFile)
	{
		var provider = BaselineProvider();
		var catalog = CatalogLoader.Load(provider).OrThrow();
		var context = new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = ElasticsearchEndpoint.Default },
			ConfigurationFileProvider = provider,
			Catalog = new GlobalCatalog(catalog.VersioningSystems, catalog.Products, LegacyUrlMappingCatalog.None),
			SearchTuning = SearchTuning.None,
		};
		const string configPath = "/repo/docs/changelog.yml";
		var fs = new MockFileSystem(
			new Dictionary<string, MockFileData> { [configPath] = new(await File.ReadAllTextAsync(sourceFile)) },
			"/repo"
		);
		var loader = new ChangelogConfigurationLoader(NullLoggerFactory.Instance, context, fs);
		return await loader.LoadChangelogConfiguration(new CapturingDiagnosticsCollector(), configPath, CancellationToken.None);
	}

	private static async Task AssertChangelog(string name, string sourceFile)
	{
		var provider = BaselineProvider();
		var catalog = CatalogLoader.Load(provider).OrThrow();
		var versions = catalog.VersioningSystems;
		var products = catalog.Products;
		var context = new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = ElasticsearchEndpoint.Default },
			ConfigurationFileProvider = provider,
			Catalog = new GlobalCatalog(versions, products, LegacyUrlMappingCatalog.None),
			SearchTuning = SearchTuning.None,
		};

		const string configPath = "/repo/docs/changelog.yml";
		var fs = new MockFileSystem(
			new Dictionary<string, MockFileData> { [configPath] = new(await File.ReadAllTextAsync(sourceFile)) },
			"/repo"
		);
		var loader = new ChangelogConfigurationLoader(NullLoggerFactory.Instance, context, fs);
		var collector = new CapturingDiagnosticsCollector();

		JsonNode snapshot;
		try
		{
			var configuration = await loader.LoadChangelogConfiguration(
				collector,
				configPath,
				TestContext.Current!.Execution.CancellationToken
			);
			snapshot = Obj(("value", configuration is null ? null : Reflect(configuration, 0)));
		}
		catch (Exception e)
		{
			snapshot = Obj(("failure", Failure(e)));
		}
		snapshot["diagnostics"] = DiagnosticList(collector.Diagnostics);
		await AssertMatches(name, snapshot);
	}

	private static readonly Type KeyValuePairDefinition = typeof(KeyValuePair<object, object>).GetGenericTypeDefinition();

	private static bool IsSetInterface(Type i) =>
		i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ISet<>) || i.GetGenericTypeDefinition() == typeof(IReadOnlySet<>));

	/// <summary>Projects an object graph by its public instance properties, sorted by name; maps are sorted by key.</summary>
	private static JsonNode? Reflect(object? value, int depth)
	{
		if (depth > 12)
			return Str("<max-depth>");
		switch (value)
		{
			case null:
				return null;
			case string s:
				return Str(s);
			case bool b:
				return Bool(b);
			case Enum e:
				return Str(e.ToString());
			case int or long or short or byte or double or float or decimal:
				return JsonValue.Create(Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture));
			case DateOnly or DateTime or DateTimeOffset or TimeSpan or Uri or Version:
				return Str(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
		}

		var type = value.GetType();
		if (value is IEnumerable enumerable)
		{
			var items = enumerable.Cast<object?>().ToList();
			var pairType = type
				.GetInterfaces()
				.Append(type)
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				.Select(i => i.GetGenericArguments()[0])
				.FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == KeyValuePairDefinition);
			if (pairType is not null)
			{
				var keyProperty = pairType.GetProperty("Key")!;
				var valueProperty = pairType.GetProperty("Value")!;
				var map = new JsonObject();
				foreach (var item in items.OrderBy(i => keyProperty.GetValue(i)?.ToString(), StringComparer.Ordinal))
					map[keyProperty.GetValue(item)!.ToString()!] = Reflect(valueProperty.GetValue(item), depth + 1);
				return map;
			}
			// Sets carry no order; sort them by their projected text so snapshots are stable.
			var projected = items.Select(i => Reflect(i, depth + 1)).ToList();
			var isSet = type.GetInterfaces().Any(IsSetInterface);
			if (isSet)
				projected = [.. projected.OrderBy(n => n?.ToJsonString(), StringComparer.Ordinal)];
			var array = new JsonArray();
			foreach (var node in projected)
				array.Add(node);
			return array;
		}

		var obj = new JsonObject { ["$type"] = type.Name };
		foreach (var property in type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.GetIndexParameters().Length == 0 && p.CanRead)
			.OrderBy(p => p.Name, StringComparer.Ordinal))
		{
			try
			{
				obj[property.Name] = Reflect(property.GetValue(value), depth + 1);
			}
			catch (TargetInvocationException e)
			{
				obj[property.Name] = Str($"<throws {e.InnerException?.GetType().Name}>");
			}
		}
		return obj;
	}
}
