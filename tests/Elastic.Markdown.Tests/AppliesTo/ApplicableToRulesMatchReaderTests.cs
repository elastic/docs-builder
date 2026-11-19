// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Products;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.Tests.AppliesTo;

/// <summary>The YAML reader reports a few checks while it reads; the model's rules say the same sentences about a block that was built.</summary>
public class ApplicableToRulesMatchReaderTests
{
	private static readonly ProductsConfiguration Products = TestHelpers.CreateConfigurationContext(
		new MockFileSystem()
	).ProductsConfiguration;

	[Test]
	[Arguments("stack: beta =9.0, beta =9.1")]
	[Arguments("stack: ga 9.5-9.1")]
	[Arguments("stack: beta 9.0-9.3, ga 9.2-9.5")]
	[Arguments("deployment:\n  ess: ga 9.0")]
	[Arguments("serverless:\n  security: ga 9.0")]
	public void Read_ProblemsTheRulesCover_AreWordedAsTheRulesWordThem(string yaml)
	{
		var read = YamlSerialization.Deserialize<ApplicableTo>(yaml, Products);

		var reported = read.Diagnostics!.Select(d => d.Item2).ToList();
		var byRules = ApplicableTo.Constraints.Validate(read).Select(v => v.Message).ToList();

		reported.Should().NotBeEmpty();
		byRules.Should().BeEquivalentTo(reported);
	}

	public static IEnumerable<string> ProductKeys() =>
		typeof(ProductApplicability).GetProperties().Where(p => p.PropertyType == typeof(AppliesCollection)).Select(p => p.Name);

	// Every member of ProductApplicability, so a product added to the model but not to the rules cannot go unnoticed.
	[Test]
	[MethodDataSource(nameof(ProductKeys))]
	public void Read_ARepeatedVersionUnderAProductKey_IsReportedByTheRulesAtTheSameSeverityAndWording(string member)
	{
		var jsonName = typeof(ProductApplicability).GetProperty(member)!
			.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), false)
			.Cast<System.Text.Json.Serialization.JsonPropertyNameAttribute>()
			.Single()
			.Name;
		var yamlKey = jsonName.Replace('-', '_');

		var read = YamlSerialization.Deserialize<ApplicableTo>($"product:\n  {yamlKey}: beta =9.0, beta =9.1", Products);

		var reported = read.Diagnostics!.Select(d => (d.Item1, d.Item2)).ToList();
		var byRules = ApplicableTo.Constraints.Validate(read).Select(v => (v.Severity, v.Message)).ToList();

		reported.Should().ContainSingle().Which.Item2.Should().Contain($"Key '{yamlKey}':");
		byRules.Should().BeEquivalentTo(reported);
	}
}
