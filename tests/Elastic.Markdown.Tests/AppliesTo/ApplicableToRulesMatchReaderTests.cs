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
}
