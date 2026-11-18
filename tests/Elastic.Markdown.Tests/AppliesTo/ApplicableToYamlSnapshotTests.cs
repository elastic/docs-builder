// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Serialization;
using Elastic.Markdown.Myst;
using Nullean.Snapshots;

namespace Elastic.Markdown.Tests.AppliesTo;

/// <summary>
/// <c>applies_to</c> YAML to <see cref="ApplicableTo"/>, as the <c>ApplicableToYamlConverter</c> reads it today. Every case
/// is a YAML document; its block in the verified text is the JSON the page metadata serializes to, the
/// <see cref="ApplicableTo.ToString"/> text and each diagnostic the converter collects. Odd results are recorded as they are.
/// </summary>
public class ApplicableToYamlSnapshotTests
{
	private static readonly ProductsConfiguration Products = TestHelpers.CreateConfigurationContext(
		new MockFileSystem()
	).ProductsConfiguration;

	internal static string Describe(string yaml)
	{
		var sb = new StringBuilder();
		var lines = yaml.Split('\n');
		_ = sb.Append(lines.Length == 1 ? "yaml: " : "yaml:\n").AppendLine(
			string.Join("\n", lines.Length == 1 ? lines : lines.Select(l => "  | " + l))
		);
		try
		{
			var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(yaml, Products);
			if (applicableTo is null)
				return sb.AppendLine("result: null").ToString();

			var json = JsonNode.Parse(JsonSerializer.Serialize(applicableTo, SourceGenerationContext.Default.ApplicableTo))!.ToJsonString();
			_ = sb.Append("json: ").AppendLine(json);
			_ = sb.Append("text: ").AppendLine(applicableTo.ToString());
			foreach (var (severity, message) in applicableTo.Diagnostics ?? new ApplicabilityDiagnosticsCollection([]))
				_ = sb.Append("  ").Append(SeverityName(severity)).Append(": ").AppendLine(message);
		}
		catch (Exception e)
		{
			_ = sb.Append("throws ").Append(e.GetType().Name).Append(": ").AppendLine(e.Message.Split('\n')[0].TrimEnd('\r'));
		}

		return sb.ToString();
	}

	private static string SeverityName(Severity severity) => severity switch
	{
		Severity.Error => "error",
		Severity.Warning => "warning",
		_ => "hint"
	};

	private static string Table(IEnumerable<string> cases) => string.Join("\n", cases.Select(Describe));

	// the keys the converter maps to ProductApplicability, in the underscore spelling it looks them up by
	private static readonly string[] ProductKeys =
	[
		"ecctl",
		"curator",
		"apm_agent_android",
		"apm_agent_dotnet",
		"apm_agent_go",
		"apm_agent_ios",
		"apm_agent_java",
		"apm_agent_node",
		"apm_agent_php",
		"apm_agent_python",
		"apm_agent_ruby",
		"apm_agent_rum_js",
		"edot_ios",
		"edot_android",
		"edot_collector",
		"edot_dotnet",
		"edot_java",
		"edot_node",
		"edot_browser",
		"edot_react_native",
		"edot_php",
		"edot_python",
		"edot_cf_aws",
		"edot_cf_azure",
		"edot_cf_gcp"
	];

	[Test]
	public void Read_ProductKeysUnderProduct_UseTheUnderscoreSpelling() =>
		Table(ProductKeys.Select(k => $"product:\n  {k}: ga 9.0+")).Should().MatchSnapshot();

	[Test]
	public void Read_ProductKeysAtTheTopLevel_AreReadButFlaggedAsUnknown() =>
		Table(ProductKeys.Select(k => $"{k}: beta")).Should().MatchSnapshot();

	[Test]
	public void Read_ProductKeysWithHyphens_AreNotMatchedToAProduct() =>
		Table(ProductKeys.Select(k => $"product:\n  {k.Replace('_', '-')}: ga")).Should().MatchSnapshot();

	[Test]
	public void Read_ProductKeysWithoutAValue_MeanGenerallyAvailable() =>
		Table(ProductKeys.Select(k => $"product:\n  {k}:")).Should().MatchSnapshot();

	[Test]
	public void Read_Lifecycles_EverySpellingOnStack() =>
		Table(
			new List<string>
			{
				"ga",
				"GA",
				"Ga",
				"preview",
				"Preview",
				"tech-preview",
				"experimental",
				"beta",
				"BETA",
				"deprecated",
				"removed",
				"unavailable",
				"dev",
				"development",
				"coming",
				"planned",
				"discontinued",
				"technical-preview",
				"preview9.0",
				"released",
				"unknown",
				"all",
				"ALL",
				"All",
				"''",
				"' '",
				"~"
			}.Select(l => $"stack: {l}")
		).Should().MatchSnapshot();

	[Test]
	public void Read_LifecyclesWithAVersion_EverySpellingOnStack() =>
		Table(
			new List<string>
			{
				"ga 9.0",
				"preview 9.1",
				"tech-preview 9.2",
				"experimental 9.3",
				"beta 9.4",
				"deprecated 9.5",
				"removed 9.6",
				"unavailable 9.7",
				"dev 9.0",
				"development 9.0",
				"coming 9.9",
				"planned 9.9",
				"discontinued 8.19",
				"ga all",
				"ga ALL",
				"beta all",
				"removed 10.0"
			}.Select(l => $"stack: {l}")
		).Should().MatchSnapshot();

	[Test]
	public void Read_VersionSpecs_AllShapesOnStack() =>
		Table(
			new List<string>
			{
				"ga 9",
				"ga 9.0",
				"ga 9.0+",
				"ga 9.0.0",
				"ga 9.0.1",
				"ga 9.0.1+",
				"ga 9.0.4!",
				"ga 9.0.0!",
				"ga =9.2",
				"ga =9.2.1",
				"ga =9.2.1!",
				"ga 9.0-9.2",
				"ga 9.0.1-9.2.3",
				"ga 9.0-9.2.3!",
				"ga 9.0!-9.2",
				"ga 9.2-9.0",
				"ga 9.0-9.0",
				"ga 8.19",
				"ga 7.17.0",
				"ga 10.0",
				"ga 100.200",
				"ga 0.0",
				"ga x",
				"ga 9.x",
				"ga 9.0.x",
				"ga abc",
				"ga 9..0",
				"ga .9",
				"ga 9.",
				"ga +9.0",
				"ga ==9.0",
				"ga =",
				"ga 9.0++",
				"ga 9.0 9.1",
				"ga 9.0-",
				"ga -9.0",
				"ga v9.0",
				"ga 09.00",
				"ga 9.0-beta",
				"ga 9.0.0-rc1",
				"ga ~9.0",
				"ga >=9.0",
				"ga 9.0,",
				"ga 99999999999.0"
			}.Select(v => $"stack: {v}")
		).Should().MatchSnapshot();

	[Test]
	public void Read_Lists_MultipleLifecyclesInOneValue() =>
		Table([
			"stack: ga 9.2+, beta 9.0-9.1",
			"stack: ga 9.2, preview 9.1, beta 9.0",
			"stack: ga 9.4+, ga 9.2+",
			"stack: ga 9.2+, deprecated 9.4+",
			"stack: preview 9.0, ga 9.1, deprecated 9.3, removed 9.5",
			"stack: ga 9.0+, ga 9.1+, ga 9.3+",
			"stack: ga 9.0, ga 9.1, ga 9.3",
			"stack: ga 9.0, ga 9.1",
			"stack: ga 9.0, ga 10.0",
			"stack: ga 9.0, ga 10.1",
			"stack: ga 9.0.1, ga 9.1.0",
			"stack: ga 9.0.0, ga 9.1.2",
			"stack: ga 9.3, ga 9.0",
			"stack: beta 9.0-9.2, ga 9.1-9.3",
			"stack: beta 9.0-9.2, ga 9.2+",
			"stack: beta 9.0-9.1, ga 9.2+",
			"stack: ga =9.0, ga =9.0",
			"stack: ga =9.0, beta =9.1, ga 9.2+",
			"stack: ga, beta 9.0",
			"stack: ga 9.0, beta",
			"stack: preview, ga",
			"stack: ga 9.0,",
			"stack: ,ga 9.0",
			"stack: ga 9.0,,beta 9.1",
			"stack: ga 9.0 , beta 9.1",
			"stack: ga 9.0, nonsense 9.1",
			"stack: nonsense 9.0, ga 9.1",
			"stack: nonsense, other",
			"stack: ga 9.0, all"
		]).Should().MatchSnapshot();

	[Test]
	public void Read_TopLevelScalars_AndEmptyDocuments() =>
		Table([
			"all",
			"ALL",
			"All",
			"ga",
			"stack",
			"'  '",
			"''",
			"~",
			"null",
			"9.0",
			"true",
			"[]",
			"{}",
			"# only a comment",
			"---",
			"- ",
			"! all"
		]).Should().MatchSnapshot();

	[Test]
	public void Read_Sequences_MergeMappingsAndKeyValueLines() =>
		Table([
			"- stack: ga 9.0\n- serverless: ga",
			"- stack: ga 9.0\n- stack: beta 9.1",
			"- deployment:\n    ece: ga 3.0\n- deployment:\n    eck: beta 3.1",
			"- deployment:\n    ece: ga 3.0\n- deployment:\n    ece: beta 3.1",
			"- 'stack: ga 9.0'\n- 'serverless: ga'",
			"- stack: ga 9.0\n- 'serverless: beta'",
			"- 'stack: ga 9.0'\n- 'stack: beta 9.1'",
			"- not a key value line",
			"- 'stack:'",
			"- 'stack: '",
			"- ':'",
			"- ': ga'",
			"- 'stack ga'",
			"- 'stack: ga 9.0: extra'",
			"- ga",
			"- - stack: ga 9.0",
			"[]",
			"- {}",
			"- ~",
			"- stack: ga\n- ~\n- serverless: ga",
			"- serverless: ga\n- serverless:\n    elasticsearch: beta",
			"- product:\n    ecctl: ga\n- product:\n    curator: beta",
			"- 'deployment: ga'\n- 'deployment: beta'"
		]).Should().MatchSnapshot();

	[Test]
	public void Read_Stack_ValueShapes() =>
		Table([
			"stack:",
			"stack: ~",
			"stack: ''",
			"stack: ga",
			"stack: [ga, beta]",
			"stack:\n  - ga 9.0",
			"stack:\n  ga: 9.0",
			"stack: 9.0",
			"stack: 9",
			"stack: true",
			"stack: 1.5",
			"stack: {}",
			"STACK: ga",
			"Stack: ga",
			"stack : ga",
			"'stack': ga",
			"\"stack\": ga 9.0",
			"stack: ga 9.0\nstack: beta 9.1",
			"stack: ga 9.0 # comment",
			"stack: 'ga 9.0'",
			"stack: \"ga 9.0\"",
			"stack: >-\n  ga 9.0",
			"stack: |\n  ga 9.0"
		]).Should().MatchSnapshot();

	[Test]
	public void Read_Deployment_ScalarAndMappingForms() =>
		Table([
			"deployment:",
			"deployment: ~",
			"deployment: ''",
			"deployment: ga",
			"deployment: all",
			"deployment: beta 9.0",
			"deployment: ga 9.0",
			"deployment: preview 9.1, ga 9.2+",
			"deployment: nonsense",
			"deployment: [ga]",
			"deployment: {}",
			"deployment:\n  self: ga 9.0",
			"deployment:\n  ece: ga 3.0",
			"deployment:\n  eck: beta 3.1",
			"deployment:\n  ess: ga",
			"deployment:\n  ess: ga 9.0",
			"deployment:\n  ech: ga",
			"deployment:\n  ech: beta",
			"deployment:\n  ess: ga\n  ech: beta",
			"deployment:\n  self: ga 9.0\n  ece: ga 3.0\n  eck: ga 3.0\n  ess: ga",
			"deployment:\n  self:\n  ece:\n  eck:\n  ess:",
			"deployment:\n  self: ~",
			"deployment:\n  unknown: ga",
			"deployment:\n  self: ga 9.0\n  unknown: ga",
			"deployment:\n  Self: ga 9.0",
			"deployment:\n  serverless: ga",
			"deployment:\n  - self: ga 9.0",
			"ece: ga 3.0",
			"eck: beta 3.1",
			"ess: ga",
			"ess: ga 9.0",
			"ech: ga",
			"self: ga 9.0",
			"ess: ga\nech: beta",
			"self: ga 9.0\nece: ga 3.0\neck: ga 3.0\ness: ga\nech: ga",
			"ece:\neck:\nself:\ness:"
		]).Should().MatchSnapshot();

	[Test]
	public void Read_Serverless_ScalarAndMappingForms() =>
		Table([
			"serverless:",
			"serverless: ~",
			"serverless: ''",
			"serverless: ga",
			"serverless: all",
			"serverless: beta",
			"serverless: preview",
			"serverless: ga 9.0",
			"serverless: ga, beta",
			"serverless: nonsense",
			"serverless: {}",
			"serverless:\n  elasticsearch: ga",
			"serverless:\n  observability: beta",
			"serverless:\n  security: preview",
			"serverless:\n  vectordb: deprecated",
			"serverless:\n  elasticsearch: ga\n  observability: ga\n  security: ga\n  vectordb: ga",
			"serverless:\n  elasticsearch: ga 9.0",
			"serverless:\n  security:\n  observability:",
			"serverless:\n  vector_db: ga",
			"serverless:\n  vectordatabase: ga",
			"serverless:\n  unknown: ga",
			"serverless:\n  elasticsearch: ga\n  unknown: ga",
			"serverless:\n  - elasticsearch: ga",
			"elasticsearch: ga",
			"observability: beta",
			"security: preview",
			"vectordb: deprecated",
			"elasticsearch: ga\nobservability: ga\nsecurity: ga\nvectordb: ga",
			"serverless: ga\nelasticsearch: beta",
			"serverless: ga\nelasticsearch: beta\nsecurity: removed",
			"serverless:\n  elasticsearch: ga\nobservability: beta",
			"elasticsearch: ga 9.0"
		]).Should().MatchSnapshot();

	[Test]
	public void Read_Product_ScalarAndMappingForms() =>
		Table([
			"product:",
			"product: ~",
			"product: ''",
			"product: ga",
			"product: all",
			"product: beta 9.0",
			"product: ga 9.0+",
			"product: ga 9.2+, beta 9.0-9.1",
			"product: nonsense",
			"product: {}",
			"product:\n  ecctl: ga 1.0",
			"product:\n  ecctl: ga 1.0\n  curator: deprecated",
			"product:\n  ecctl: ga 1.0\n  unknown: ga",
			"product:\n  unknown: ga",
			"product:\n  - ecctl: ga",
			"product: ga\necctl: beta",
			"product:\n  ecctl: ga\necctl: beta",
			"product:\n  ecctl: ga\ncurator: beta"
		]).Should().MatchSnapshot();

	[Test]
	public void Read_UnknownKeys_AreHintsWithSuggestions() =>
		Table([
			"stak: ga",
			"stack: ga\nunknown: ga",
			"unknown:",
			"unknown: ga",
			"deployments: ga",
			"serverles: ga",
			"products: ga",
			"produt: ga",
			"elasticsearc: ga",
			"kibana: ga",
			"apm: ga",
			"apm_agent: ga",
			"apm-agent: ga",
			"stack_: ga",
			"_stack: ga",
			":stack: ga",
			":stack: ga\n:serverless: ga",
			":stack: ga\nstack: beta",
			"stack-trace: ga",
			"vector_db: ga",
			"vectordatabase: ga",
			"ESS: ga",
			"Ech: ga",
			"x: y\nz: w"
		]).Should().MatchSnapshot();

	[Test]
	public void Read_Diagnostics_VersionRules() =>
		Table([
			"stack: ga 9.0, ga 9.1",
			"stack: ga 9.0-9.2, ga 9.1-9.3",
			"stack: ga 9.0+, ga 9.1+",
			"stack: ga 9.0.1+, ga 9.1.1+",
			"stack: ga 9.2-9.0",
			"stack: ga 9.0-9.2, beta 9.1",
			"stack: ga =9.0, beta =9.0",
			"stack: ga 9.0, beta 9.0",
			"stack: ga 9.2+, beta 9.0-9.2",
			"stack: ga 9.2+, beta 9.0-9.3",
			"stack: ga 9.2+, beta 9.3-9.5",
			"stack: ga 9.2+, beta =9.2",
			"stack: ga 9.2+, beta =9.1",
			"deployment: ga 9.0, beta 9.1",
			"deployment:\n  ece: ga 3.0, beta 3.0\n  eck: ga 3.0, ga 3.1",
			"deployment:\n  ess: ga 9.0",
			"deployment:\n  ech: ga 9.0",
			"deployment: ga 9.0",
			"serverless: ga 9.0",
			"serverless:\n  elasticsearch: ga 9.0\n  security: beta 9.1",
			"serverless:\n  elasticsearch: ga all",
			"serverless:\n  elasticsearch: ga, beta",
			"elasticsearch: ga 9.0",
			"ess: ga 9.0",
			"ess: ga 9.0-9.2",
			"ess: ga all",
			"product: ga 9.0, ga 9.1",
			"product:\n  ecctl: ga 1.0, ga 1.1\n  curator: ga 5.0-4.0",
			"product:\n  ecctl: ga 1.0+, beta 0.9+"
		]).Should().MatchSnapshot();

	[Test]
	public void Read_Combined_FullDocuments() =>
		Table([
			"stack: ga 9.0+\ndeployment:\n  self: ga 9.0+\n  ece: ga 3.0+\n  eck: ga 3.0+\n  ess: ga\nserverless:\n  elasticsearch: ga\n  observability: ga\n  security: ga\n  vectordb: ga\nproduct:\n  ecctl: ga 1.0+",
			"stack: ga 9.2+, beta 9.0-9.1\ndeployment:\n  ece: ga 3.2+\nserverless: preview",
			"stack: preview 9.1\nserverless: unavailable\ndeployment: unavailable",
			"stack: removed 9.0\ndeployment:\n  ess: removed\nserverless: removed",
			"stack: deprecated 9.0\nproduct: deprecated 1.0",
			"stack: ga 9.0\nserverless: ga\ndeployment: ga\nproduct: ga",
			"stack: ga\ndeployment:\nserverless:\nproduct:",
			"stack:\ndeployment:\nserverless:",
			"deployment: ga\nserverless:\n  elasticsearch: beta\nstack: preview 9.1",
			"stack: ga 9.0\nunknown: x\nserverless: ga",
			"stack: nonsense\nserverless: ga",
			"stack: ga 9.0\nserverless: nonsense",
			"stack: nonsense\nserverless: nonsense"
		]).Should().MatchSnapshot();
}
