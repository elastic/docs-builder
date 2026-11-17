// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Serialization;
using Nullean.Snapshots;

namespace Elastic.Markdown.Tests.AppliesTo;

internal static class StringBuilderLines
{
	/// <summary>Appends a line of text. The text is built from strings only, so the culture has no say in it.</summary>
	public static StringBuilder Line(this StringBuilder sb, string text) => sb.AppendLine(text);
}

/// <summary>
/// The <see cref="ApplicableTo"/> model on its own: reading and writing its JSON form, equality, ordering and the
/// text forms. Today's results are recorded as they are, odd ones included.
/// </summary>
public class ApplicableToModelSnapshotTests
{
	private static string Compact(ApplicableTo? value) =>
		value is null
			? "null"
			: JsonNode.Parse(JsonSerializer.Serialize(value, SourceGenerationContext.Default.ApplicableTo))!.ToJsonString();

	private static string ReadJson(string json)
	{
		try
		{
			var value = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ApplicableTo);
			return value is null ? "null" : $"{Compact(value)}  =>  {value}";
		}
		catch (Exception e)
		{
			return $"throws {e.GetType().Name}: {e.Message.Split('\n')[0].TrimEnd('\r')}";
		}
	}

	private static string OneLine(string yaml) => yaml.Replace("\n", " / ");

	private static IEnumerable<string> Lines(IEnumerable<string> inputs, Func<string, string> describe) =>
		inputs.Select(input => $"{input}\n    {describe(input)}");

	[Test]
	public void ReadJson_EntryShapes_FromTheFlatWireFormat() =>
		string
			.Join(
				"\n",
				Lines(
					[
						"[]",
						"null",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "ga" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "ga", "version": "9.0+" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "beta", "version": "9.0-9.2" }, { "type": "stack", "sub_type": "stack", "lifecycle": "ga", "version": "9.3+" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "ga", "version": "all" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "ga", "version": "ALL" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "ga", "version": "bogus" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "ga", "version": "=9.1" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "ga", "version": "9.0.4!" }]""",
						"""[{ "type": "stack", "sub_type": "stack" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": null, "version": null }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "PREVIEW" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "tech-preview" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "nonsense" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "experimental" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "deprecated" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "removed" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "unavailable" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "development" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "planned" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "discontinued" }]""",
						"""[{ "type": "stack", "sub_type": "ignored", "lifecycle": "ga" }]""",
						"""[{ "type": "deployment", "sub_type": "self", "lifecycle": "ga", "version": "9.0+" }, { "type": "deployment", "sub_type": "ece", "lifecycle": "beta" }, { "type": "deployment", "sub_type": "eck", "lifecycle": "ga" }, { "type": "deployment", "sub_type": "ess", "lifecycle": "ga" }]""",
						"""[{ "type": "deployment", "sub_type": "ech", "lifecycle": "ga" }]""",
						"""[{ "type": "deployment", "sub_type": "ech", "lifecycle": "ga" }, { "type": "deployment", "sub_type": "ess", "lifecycle": "beta" }]""",
						"""[{ "type": "deployment", "sub_type": "unknown", "lifecycle": "ga" }]""",
						"""[{ "type": "serverless", "sub_type": "elasticsearch", "lifecycle": "ga" }, { "type": "serverless", "sub_type": "observability", "lifecycle": "ga" }, { "type": "serverless", "sub_type": "security", "lifecycle": "beta" }, { "type": "serverless", "sub_type": "vectordb", "lifecycle": "ga" }]""",
						"""[{ "type": "serverless", "sub_type": "vector_db", "lifecycle": "ga" }]""",
						"""[{ "type": "product", "sub_type": "product", "lifecycle": "ga", "version": "9.0+" }]""",
						"""[{ "type": "product", "sub_type": "ecctl", "lifecycle": "ga", "version": "1.0+" }, { "type": "product", "sub_type": "apm-agent-java", "lifecycle": "beta" }]""",
						"""[{ "type": "product", "sub_type": "apm_agent_java", "lifecycle": "ga" }]""",
						"""[{ "type": "product", "sub_type": "no-such-product", "lifecycle": "ga" }]""",
						"""[{ "type": "elsewhere", "sub_type": "stack", "lifecycle": "ga" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "ga", "extra": 1 }]""",
						"""[{ "sub_type": "stack", "lifecycle": "ga" }]""",
						"""[{ "type": "stack", "lifecycle": "ga" }]""",
						"""[{ "type": "", "sub_type": "stack" }]""",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": 1 }]""",
						"""[{ "type": "stack", "sub_type": "stack", "version": 9 }]""",
						"""[{ "type": 1, "sub_type": "stack" }]""",
						"""[1]""",
						"""["stack"]""",
						"""[[]]""",
						"""{ "type": "stack", "sub_type": "stack" }""",
						"""{}""",
						"\"stack\"",
						"1",
						"true",
						"[",
						"",
						"""[{ "type": "stack", "sub_type": "stack", "lifecycle": "ga" },]"""
					],
					ReadJson
				)
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void WriteJson_RepresentativeModels_WriteTheFlatArray() =>
		string
			.Join(
				"\n",
				new (string Name, ApplicableTo Value)[]
				{
					("empty", new ApplicableTo()),
					("All", ApplicableTo.All),
					("Default", ApplicableTo.Default),
					("stack only", new ApplicableTo { Stack = (AppliesCollection)"ga 9.0+" }),
					("stack with two lifecycles", new ApplicableTo { Stack = (AppliesCollection)"ga 9.2+, beta 9.0-9.1" }),
					("deployment partial", new ApplicableTo
					{
						Deployment = new DeploymentApplicability { Ece = (AppliesCollection)"beta 3.0" }
					}),
					("serverless partial", new ApplicableTo
					{
						Serverless = new ServerlessProjectApplicability { Security = (AppliesCollection)"preview" }
					}),
					("product simple", new ApplicableTo { Product = (AppliesCollection)"deprecated 2.0" }),
					("product specific", new ApplicableTo
					{
						ProductApplicability = new ProductApplicability
						{
							EdotJava = (AppliesCollection)"ga 1.0+",
							Curator = (AppliesCollection)"removed"
						}
					}),
					("version null", new ApplicableTo
					{
						Stack = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta }])
					}),
					("empty collection", new ApplicableTo { Stack = new AppliesCollection([]) })
				}.Select(c => $"{c.Name}\n    {Compact(c.Value)}\n    {c.Value}")
			)
			.Should()
			.MatchSnapshot();

	[Test]
	public void ToAppliesTo_WireEntries_MatchTheJsonShape()
	{
		var value = new ApplicableTo
		{
			Stack = (AppliesCollection)"ga 9.2+, beta 9.0-9.1",
			Deployment = new DeploymentApplicability { Self = (AppliesCollection)"ga 9.0+", Ess = (AppliesCollection)"preview" },
			Serverless = ServerlessProjectApplicability.All,
			Product = (AppliesCollection)"unavailable",
			ProductApplicability = new ProductApplicability
			{
				Ecctl = (AppliesCollection)"ga 1.0+",
				ApmAgentGo = (AppliesCollection)"unavailable"
			}
		};

		var entries = value.ToAppliesTo().Select(e => $"{e.Type}/{e.SubType}: {e.Lifecycle} {e.Version ?? "(no version)"}");

		string.Join("\n", entries).Should().MatchSnapshot();
	}

	// each of these is read from YAML, written to JSON, read back and written again
	private static readonly string[] RoundTripYaml =
	[
		"stack: ga",
		"stack: ga 9.0+",
		"stack: ga 9.2+, beta 9.0-9.1",
		"stack: preview 9.0, ga 9.1, deprecated 9.3, removed 9.5",
		"stack: ga =9.0",
		"stack: ga 9.0.4!",
		"stack: ga 9.0!-9.2",
		"stack: all",
		"all",
		"deployment: ga",
		"deployment:\n  ece: ga 3.0+\n  eck: beta\n  ess: preview\n  self: deprecated 9.0",
		"deployment:\n  ech: ga",
		"serverless: ga",
		"serverless:\n  elasticsearch: ga\n  security: beta",
		"product: ga 9.0+",
		"product:\n  ecctl: ga 1.0+\n  edot_java: beta",
		"stack: ga 9.0\nserverless: preview\ndeployment:\n  self: ga",
		"stack: dev",
		"stack: coming 9.9",
		"stack: discontinued",
		"stack: tech-preview 9.1",
		"stack: removed"
	];

	[Test]
	public void RoundTrip_ParsedYaml_ThroughJsonAndBack()
	{
		var sb = new StringBuilder();
		foreach (var yaml in RoundTripYaml)
		{
			var parsed = Parse(yaml);
			var json = Compact(parsed);
			var back = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ApplicableTo);
			_ = sb
				.Append(OneLine(yaml))
				.AppendLine()
				.Append("    json again identical: ")
				.AppendLine(Compact(back) == json ? "yes" : "no")
				.Append("    models equal:         ")
				.AppendLine(Equals(parsed with { Diagnostics = null }, back) ? "yes" : "no")
				.Append("    text before: ")
				.AppendLine(parsed?.ToString())
				.Append("    text after:  ")
				.AppendLine(back?.ToString());
		}

		sb.ToString().Should().MatchSnapshot();
	}

	private static ApplicableTo? Parse(string yaml) =>
		Elastic
			.Markdown
			.Myst
			.YamlSerialization
			.Deserialize<ApplicableTo>(
				yaml,
				TestHelpers.CreateConfigurationContext(new System.IO.Abstractions.TestingHelpers.MockFileSystem()).Catalog.Products
			);

	[Test]
	public void Equality_ParsedAndConstructedModels_AreComparedStructurally()
	{
		var cases = new (string Name, ApplicableTo? Left, ApplicableTo? Right)[]
		{
			("All vs parsed 'all'", ApplicableTo.All, Parse("all")),
			("All vs itself", ApplicableTo.All, ApplicableTo.All),
			("parsed twice", Parse("stack: ga 9.0+"), Parse("stack: ga 9.0+")),
			("ga 9.0 vs ga 9.0+", Parse("stack: ga 9.0"), Parse("stack: ga 9.0+")),
			("ga 9.0 vs ga 9.0.0", Parse("stack: ga 9.0"), Parse("stack: ga 9.0.0")),
			("ga 9.0 vs ga 9.0.1", Parse("stack: ga 9.0"), Parse("stack: ga 9.0.1")),
			("ga vs ga all", Parse("stack: ga"), Parse("stack: ga all")),
			("ga vs empty value", Parse("stack: ga"), Parse("stack:")),
			("list order is normalised", Parse("stack: ga 9.2+, beta 9.0-9.1"), Parse("stack: beta 9.0-9.1, ga 9.2+")),
			("ech vs ess", Parse("deployment:\n  ech: ga"), Parse("deployment:\n  ess: ga")),
			("deployment scalar vs mapping", Parse("deployment: ga"), Parse("deployment:\n  ece: ga\n  eck: ga\n  ess: ga\n  self: ga")),
			("serverless scalar vs mapping", Parse("serverless: ga"), Parse(
				"serverless:\n  elasticsearch: ga\n  observability: ga\n  security: ga\n  vectordb: ga"
			)),
			("diagnostics are part of equality", Parse("stack: ga\nunknown: x"), Parse("stack: ga")),
			("Default vs parsed", ApplicableTo.Default, Parse("stack: ga 9.0\nserverless: ga")),
			("product simple vs product mapping", Parse("product: ga"), Parse("product:\n  ecctl: ga")),
			("empty vs empty", new ApplicableTo(), new ApplicableTo()),
			("null vs empty", null, new ApplicableTo())
		};

		var sb = new StringBuilder();
		foreach (var (name, left, right) in cases)
		{
			var equal = Equals(left, right);
			var hashes = left is not null && right is not null && left.GetHashCode() == right.GetHashCode();
			_ = sb.Line($"{name,-40} equal: {(equal ? "yes" : "no")}, same hash: {(hashes ? "yes" : "no")}");
		}

		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void Ordering_Applicabilities_SortByVersionWithUnversionedLast()
	{
		var items = new[]
		{
			"ga",
			"ga 9.0",
			"ga 9.1",
			"ga 9.0.1",
			"ga 9.0+",
			"ga =9.0",
			"ga 9.0-9.2",
			"ga 9.1-9.3",
			"ga 8.19",
			"ga 10.0",
			"beta 9.0",
			"preview 9.0",
			"deprecated 9.0",
			"removed 9.0",
			"ga all",
			"beta",
			"ga 9.0.4!",
			"ga =9.0.1!",
			"ga 7.17"
		}.Select(s => (Name: s, Value: (Applicability)s)).ToArray();

		var sb = new StringBuilder();
		_ = sb.AppendLine("sorted ascending (stable):");
		foreach (var item in items.OrderBy(i => i.Value).Select(i => i.Name))
			_ = sb.AppendLine("  " + item);
		_ = sb.AppendLine("sorted descending (stable):");
		foreach (var item in items.OrderByDescending(i => i.Value).Select(i => i.Name))
			_ = sb.AppendLine("  " + item);

		_ = sb.AppendLine("pairwise CompareTo sign (row vs column):");
		_ = sb.Append("".PadRight(14));
		foreach (var column in items.Take(12))
			_ = sb.Append(column.Name.PadRight(11));
		_ = sb.AppendLine();
		foreach (var row in items.Take(12))
		{
			_ = sb.Append(row.Name.PadRight(14));
			foreach (var column in items.Take(12))
				_ = sb.Append(
					Math
						.Sign(row.Value.CompareTo(column.Value))
						.ToString("+0;-0;0", System.Globalization.CultureInfo.InvariantCulture)
						.PadRight(11)
				);
			_ = sb.AppendLine();
		}

		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void Operators_Applicabilities_FollowCompareTo()
	{
		var a = (Applicability)"ga 9.0";
		var b = (Applicability)"ga 9.1";
		var unversioned = Applicability.GenerallyAvailable;
		Applicability? none = null;

		string Row(string name, Applicability? l, Applicability? r) =>
			$"{name,-28} <: {(l < r ? "T" : "F")}  <=: {(l <= r ? "T" : "F")}  >: {(l > r ? "T" : "F")}  >=: {(l >= r ? "T" : "F")}";

		string
			.Join(
				"\n",
				Row("9.0 vs 9.1", a, b),
				Row("9.1 vs 9.0", b, a),
				Row("9.0 vs 9.0", a, a),
				Row("9.0 vs unversioned", a, unversioned),
				Row("unversioned vs 9.0", unversioned, a),
				Row("unversioned vs unversioned", unversioned, unversioned),
				Row("null vs 9.0", none, a),
				Row("9.0 vs null", a, none),
				Row("null vs null", none, none)
			)
			.Should()
			.MatchInlineSnapshot(
				"""
			9.0 vs 9.1                   <: T  <=: T  >: F  >=: F
			9.1 vs 9.0                   <: F  <=: F  >: T  >=: T
			9.0 vs 9.0                   <: F  <=: T  >: F  >=: T
			9.0 vs unversioned           <: F  <=: F  >: T  >=: T
			unversioned vs 9.0           <: T  <=: T  >: F  >=: F
			unversioned vs unversioned   <: F  <=: T  >: F  >=: T
			null vs 9.0                  <: T  <=: T  >: F  >=: F
			9.0 vs null                  <: F  <=: F  >: T  >=: T
			null vs null                 <: F  <=: T  >: F  >=: T
			"""
			);
	}

	[Test]
	public void Collections_TryParse_SortsDescendingAndComparesStructurally()
	{
		var sb = new StringBuilder();
		foreach (var input in new[]
		{
			"ga 9.0, ga 9.2, ga 9.1",
			"beta, ga 9.0",
			"ga 9.0, beta",
			"ga all, beta 9.0",
			"ga 9.0-9.2, ga 9.3+",
			"all",
			"ALL",
			"",
			"  ",
			"nonsense",
			"ga 9.0, nonsense",
			"ga 9.0,,beta 9.1"
		})
		{
			var diagnostics = new List<(Severity, string)>();
			var ok = AppliesCollection.TryParse(input, diagnostics, out var collection);
			_ = sb
				.Append($"'{input}'")
				.Line(
					$" -> parsed: {(ok ? "yes" : "no")}, count: {collection?.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-"}, text: {collection}"
				)
				.Line($"    items: {(collection is null ? "-" : string.Join(" | ", collection.Select(c => c.ToString())))}");
			foreach (var (severity, message) in diagnostics)
				_ = sb.Line($"    {severity}: {message}");
		}

		var pairs = new (string Left, string Right)[]
		{
			("ga 9.0, beta 8.0", "beta 8.0, ga 9.0"),
			("ga 9.0", "ga 9.0"),
			("ga 9.0", "ga 9.0+"),
			("ga", "all"),
			("ga", "ga all")
		};
		foreach (var (left, right) in pairs)
		{
			_ = AppliesCollection.TryParse(left, [], out var l);
			_ = AppliesCollection.TryParse(right, [], out var r);
			_ = sb.Line($"'{left}' == '{right}': {Equals(l, r)}, same hash: {l!.GetHashCode() == r!.GetHashCode()}");
		}

		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void Conversions_ExplicitCasts_ThrowOnDiagnosticsOrFailure()
	{
		static string Try(Func<object> convert)
		{
			try
			{
				return convert().ToString() ?? "(null)";
			}
			catch (Exception e)
			{
				return $"throws {e.GetType().Name}: {e.Message.Split('\n')[0].TrimEnd('\r')}";
			}
		}

		var sb = new StringBuilder();
		foreach (var input in new[] { "ga", "ga 9.0", "ga 9.0+", "9.0", "9", "dev", "nonsense", "", "ga 9.0, beta 8.0", "all" })
		{
			_ = sb.Line($"'{input}'");
			_ = sb.Line($"    Applicability:     {Try(() => (Applicability)input)}");
			_ = sb.Line($"    AppliesCollection: {Try(() => (AppliesCollection)input)}");
		}

		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void ProductApplicability_EveryProduct_ToStringJsonNameAndProductId()
	{
		var sb = new StringBuilder();
		foreach (var property in typeof(ProductApplicability).GetProperties())
		{
			var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name;
			var value = new ProductApplicability();
			property.SetValue(value, (AppliesCollection)"ga 1.0+");
			var wrapper = new ApplicableTo { ProductApplicability = value };
			_ = sb.Line(
				$"{name,-20} property: {property.Name,-18} text: {value}  id: {ProductApplicabilityConversion.ProductApplicabilityToProductId(value) ?? "(none)"}"
			).Line($"{"",-20} wire: {Compact(wrapper)}");
		}

		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void ProductApplicability_Combinations_ToStringAndFirstProductWins()
	{
		var cases = new (string Name, ProductApplicability Value)[]
		{
			("empty", new ProductApplicability()),
			("two", new ProductApplicability { Ecctl = (AppliesCollection)"ga 1.0+", Curator = (AppliesCollection)"deprecated" }),
			("three", new ProductApplicability
			{
				Ecctl = (AppliesCollection)"ga",
				ApmAgentJava = (AppliesCollection)"beta 1.0",
				EdotJava = (AppliesCollection)"preview"
			}),
			("apm agent and edot of the same platform", new ProductApplicability
			{
				ApmAgentIos = (AppliesCollection)"ga",
				EdotIos = (AppliesCollection)"ga"
			}),
			("last in declaration order", new ProductApplicability
			{
				EdotCollector = (AppliesCollection)"ga",
				EdotCfGcp = (AppliesCollection)"ga"
			}),
			("multi lifecycle", new ProductApplicability { Ecctl = (AppliesCollection)"ga 2.0+, beta 1.0-1.9" })
		};

		string
			.Join(
				"\n",
				cases.Select(
					c =>
						$"{c.Name}\n    text: {c.Value}\n    id: {ProductApplicabilityConversion.ProductApplicabilityToProductId(c.Value) ?? "(none)"}\n    equal to itself: {c.Value == c.Value with { }}"
				)
			)
			.Should()
			.MatchSnapshot();
	}

	[Test]
	public void Lifecycles_EveryValue_NamesOrderAndWireForms()
	{
		var sb = new StringBuilder();
		foreach (var lifecycle in Enum.GetValues<ProductLifecycle>())
		{
			var applicability = new Applicability { Lifecycle = lifecycle, Version = (Elastic.Documentation.Versions.VersionSpec)"9.1" };
			var wire = new ApplicableTo { Stack = new AppliesCollection([applicability]) }.ToAppliesTo().Single();
			var metadata = ProductLifecycleInfo.GetMetadata(lifecycle);
			_ = sb.Line(
				$"{lifecycle,-20} text: {applicability,-18} short: {applicability.GetLifeCycleName(),-13} display: {metadata.DisplayText,-20} order: {metadata.Order} wire: {wire.Lifecycle}"
			);
		}

		_ = sb.Line(
			$"undefined value: {ProductLifecycleInfo.GetShortName((ProductLifecycle)99)}|{ProductLifecycleInfo.GetOrder((ProductLifecycle)99)}"
		);
		sb.ToString().Should().MatchSnapshot();
	}
}
