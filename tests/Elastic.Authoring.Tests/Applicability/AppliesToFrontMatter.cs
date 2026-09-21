// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;

// Alias needed: file namespace contains 'Applicability' as a segment, which shadows the type.
using Applicab = Elastic.Documentation.AppliesTo.Applicability;

namespace Elastic.Authoring.Tests.Applicability.AppliesToFrontMatter;

// Helper: wraps YAML under applies_to: in a document front matter
static file class FrontMatter
{
	public static Scenario WithYaml(string yaml) => Setup.Document($"---\n{yaml}\n---\n# Document\n");
}

public class ApplyDefaultsToAll : AuthoringTest
{
	protected override Scenario Scenario => FrontMatter.WithYaml("""
			applies_to:
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() => await Docs.AppliesTo(null);
}

public class ApplyDefaultToTopLevelArguments : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   deployment:
			   serverless:
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Deployment = DeploymentApplicability.All,
			Serverless = ServerlessProjectApplicability.All,
		});
}

public class ParsesServerlessAsStringToSetAllProjects : AuthoringTest
{
	protected override Scenario Scenario => FrontMatter.WithYaml("""
			applies_to:
			   serverless: ga
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected()
	{
		var expectedAvailability = Applies("ga");
		await Docs.AppliesTo(new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = expectedAvailability,
				Observability = expectedAvailability,
				Security = expectedAvailability,
				VectorDatabase = expectedAvailability,
			},
		});
	}
}

public class ParsesServerlessVectorDatabaseProject : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   serverless:
			      vectordb: ga
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo { Serverless = new ServerlessProjectApplicability { VectorDatabase = Applies("ga") } });
}

public class ParsesTopLevelVectordbAsServerlessProject : AuthoringTest
{
	protected override Scenario Scenario => FrontMatter.WithYaml("""
			applies_to:
			   vectordb: ga
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo { Serverless = new ServerlessProjectApplicability { VectorDatabase = Applies("ga") } });
}

public class ServerlessShorthandWithTopLevelProjectOverride : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   serverless: ga
			   vectordb: preview
			""");

	[Test, DisplayName("overrides only the specified serverless project")]
	public async Task OverridesOnlySpecifiedProject()
	{
		var expectedAvailability = Applies("ga");
		await Docs.AppliesTo(new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = expectedAvailability,
				Observability = expectedAvailability,
				Security = expectedAvailability,
				VectorDatabase = Applies("preview"),
			},
		});
	}
}

// Two separate scenarios exercised in a single test to pin that the overridden scenario
// does not mutate the shared serverless defaults used by the subsequent scenario.
public class EmptyServerlessShorthandWithTopLevelProjectOverride
{
	private static readonly Scenario Overridden = FrontMatter.WithYaml("applies_to:\n   serverless:\n   vectordb: preview\n");

	private static readonly Scenario Subsequent = FrontMatter.WithYaml("applies_to:\n   serverless:\n");

	[Test, DisplayName("does not mutate shared serverless defaults")]
	public async Task DoesNotMutateSharedDefaults()
	{
		var expectedAll = Applies("all");
		await Overridden.AppliesTo(new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = expectedAll,
				Observability = expectedAll,
				Security = expectedAll,
				VectorDatabase = Applies("preview"),
			},
		});
		await Subsequent.AppliesTo(new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = expectedAll,
				Observability = expectedAll,
				Security = expectedAll,
				VectorDatabase = expectedAll,
			},
		});
	}
}

public class ParsesServerlessProjects : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml(
			"""
			applies_to:
			   serverless:
			      security: ga
			      elasticsearch: beta
			      observability: removed
			      vectordb: preview
			"""
		);

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Security = Applies("ga"),
				Elasticsearch = Applies("beta"),
				Observability = Applies("removed"),
				VectorDatabase = Applies("preview"),
			},
		});
}

public class ParsesStack : AuthoringTest
{
	protected override Scenario Scenario => FrontMatter.WithYaml("""
			applies_to:
			   stack: ga 9.1
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() => await Docs.AppliesTo(new ApplicableTo { Stack = Applies("ga 9.1.0") });
}

public class ParsesDeploymentAsStringToSetAllDeploymentTargets : AuthoringTest
{
	protected override Scenario Scenario => FrontMatter.WithYaml("""
			applies_to:
			   deployment: ga
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected()
	{
		var expectedAvailability = Applies("ga");
		await Docs.AppliesTo(new ApplicableTo
		{
			Deployment = new DeploymentApplicability
			{
				Eck = expectedAvailability,
				Ess = expectedAvailability,
				Ece = expectedAvailability,
				Self = expectedAvailability,
			},
		});
	}
}

public class ParsesDeploymentTypesAsIndividualProperties : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml(
			"""
			applies_to:
			   deployment:
			      eck: ga 9.0
			      ess: beta
			      ece: removed 9.2.0
			      self: unavailable 9.3.0
			"""
		);

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Deployment = new DeploymentApplicability
			{
				Eck = Applies("ga 9.0"),
				Ess = Applies("beta"),
				Ece = Applies("removed 9.2.0"),
				Self = Applies("unavailable 9.3.0"),
			},
		});
}

public class ParsesEchAsAliasForEss : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   deployment:
			      ech: ga
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo { Deployment = new DeploymentApplicability { Ess = Applies("ga") } });
}

public class ParsesEchAlongsideOtherDeploymentTypes : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml(
			"""
			applies_to:
			   deployment:
			      ech: beta
			      eck: ga 9.0
			      ece: removed 9.2.0
			      self: unavailable 9.3.0
			"""
		);

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Deployment = new DeploymentApplicability
			{
				Ess = Applies("beta"),
				Eck = Applies("ga 9.0"),
				Ece = Applies("removed 9.2.0"),
				Self = Applies("unavailable 9.3.0"),
			},
		});
}

public class BothEssAndEchDefinedUsesEchValueAndWarns : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml(
			"""
			applies_to:
			   deployment:
			      ess: ga
			      ech: beta
			"""
		);

	[Test, DisplayName("ech value wins")]
	public async Task EchValueWins() =>
		await Docs.AppliesTo(new ApplicableTo { Deployment = new DeploymentApplicability { Ess = Applies("beta") } });

	[Test, DisplayName("emits warning about both being defined")]
	public async Task EmitsWarning() => await Docs.HasWarning("Both 'ess' and 'ech' are defined");
}

public class ParsesEchAtTopLevel : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   ech: preview
			   stack: ga 9.1
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Deployment = new DeploymentApplicability { Ess = Applies("preview") },
			Stack = Applies("ga 9.1.0"),
		});
}

public class ParsesProductComingDeprecated : AuthoringTest
{
	protected override Scenario Scenario => FrontMatter.WithYaml("""
			applies_to:
			   product: coming 9.5
			""");

	[Test, DisplayName("should warn of deprecated lifecycle state")]
	public async Task WarnOfDeprecatedLifecycle() => await Docs.HasHint("The 'coming' lifecycle is deprecated and will be removed");
}

public class ParsesProductPlanned : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   product: planned 9.5
			""");

	[Test, DisplayName("should warn of deprecated lifecycle state")]
	public async Task WarnOfDeprecatedLifecycle() => await Docs.HasHint("The 'planned' lifecycle is deprecated and will be removed");
}

public class ParsesProductRemoved : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   product: removed 9.5
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo { Product = new AppliesCollection([(Applicab)"removed 9.5"]) });
}

public class ParsesProductMultiple : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   product: preview 9.5, removed 9.7
			""");

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() => await Docs.AppliesTo(new ApplicableTo { Product = Applies("removed 9.7, preview 9.5") });
}

public class LenientToDefiningTypesAtTopLevel : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml(
			"""
			applies_to:
			  eck: ga 9.0
			  ess: beta
			  ece: removed 9.2.0
			  self: unavailable 9.3.0
			  security: ga
			  elasticsearch: beta
			  observability: removed
			  vectordb: preview
			  product: preview 9.5, removed 9.7
			  apm_agent_dotnet: ga 9.0
			  ecctl: ga 10.0
			  stack: ga 9.1
			"""
		);

	[Test, DisplayName("apply matches expected")]
	public async Task ApplyMatchesExpected() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Deployment = new DeploymentApplicability
			{
				Eck = Applies("ga 9.0"),
				Ess = Applies("beta"),
				Ece = Applies("removed 9.2.0"),
				Self = Applies("unavailable 9.3.0"),
			},
			Serverless = new ServerlessProjectApplicability
			{
				Security = Applies("ga"),
				Elasticsearch = Applies("beta"),
				Observability = Applies("removed"),
				VectorDatabase = Applies("preview"),
			},
			Stack = Applies("ga 9.1.0"),
			Product = Applies("preview 9.5, removed 9.7"),
			ProductApplicability = new ProductApplicability { ApmAgentDotnet = Applies("ga 9.0"), Ecctl = Applies("ga 10.0"), },
		});
}

public class ParsesEmptyAppliesToAsNull : AuthoringTest
{
	protected override Scenario Scenario => FrontMatter.WithYaml("""
			applies_to:
			""");

	[Test, DisplayName("does not render label")]
	public async Task DoesNotRenderLabel() => await Docs.AppliesTo(null);
}

public class ParsesAppliesToWithMultipleCategoriesInAnyOrder : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml(
			"""
			applies_to:
			   product: ga
			   deployment:
			      eck: ga 9.0
			   serverless:
			      security: ga
			   stack: ga 9.1
			   ecctl: ga 10.0
			   apm_agent_dotnet: ga 9.0
			"""
		);

	[Test, DisplayName("parses all categories regardless of YAML order")]
	public async Task ParsesAllCategoriesRegardlessOfOrder() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Stack = Applies("ga 9.1"),
			Serverless = new ServerlessProjectApplicability { Security = Applies("ga") },
			Deployment = new DeploymentApplicability { Eck = Applies("ga 9.0") },
			ProductApplicability = new ProductApplicability { Ecctl = Applies("ga 10.0"), ApmAgentDotnet = Applies("ga 9.0"), },
			Product = Applies("ga"),
		});
}

public class DeploymentTypesAreRenderedInCorrectOrder : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml(
			"""
			applies_to:
			   deployment:
			      self: ga 9.0
			      ece: ga 9.1
			      ess: ga
			      eck: ga 9.3
			"""
		);

	[Test, DisplayName("deployment types are rendered in ESS ECK ECE Self order")]
	public async Task DeploymentTypesRenderedInOrder() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Deployment = new DeploymentApplicability
			{
				Ess = Applies("ga"),
				Eck = Applies("ga 9.3"),
				Ece = Applies("ga 9.1"),
				Self = Applies("ga 9.0"),
			},
		});
}

public class SortsAppliesToVersionsInDescendingOrder : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   stack: preview 8.18.6, ga 9.2, beta 9.1, preview 9.0.6
			""");

	[Test, DisplayName("versions are sorted highest to lowest")]
	public async Task VersionsSortedHighestToLowest() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Stack = new AppliesCollection([
				(Applicab)"ga 9.2",
				(Applicab)"beta 9.1",
				(Applicab)"preview 9.0.6",
				(Applicab)"preview 8.18.6",
			]),
		});
}

public class SortsGaBeforeAll : AuthoringTest
{
	protected override Scenario Scenario => FrontMatter.WithYaml("""
			applies_to:
			   stack: ga, all
			""");

	[Test, DisplayName("versioned items are sorted first, non-versioned items last")]
	public async Task VersionedItemsSortedFirst() =>
		await Docs.AppliesTo(new ApplicableTo { Stack = new AppliesCollection([(Applicab)"ga", (Applicab)"all",]), });
}

public class ApplicabilityComparisons
{
	[Test, DisplayName("equals")]
	public void ApplicabilityEquals() => ((Applicab)"ga").Should().Be((Applicab)"ga");

	[Test, DisplayName("not equals")]
	public void ApplicabilityNotEquals() => ((Applicab)"ga").Should().NotBe((Applicab)"all");

	[Test, DisplayName("any version beats no version")]
	public void AnyVersionBeatsNoVersion()
	{
		((Applicab)"ga 8.1.0" > (Applicab)"ga").Should().BeTrue();
		((Applicab)"all" < (Applicab)"ga 8.1.0").Should().BeTrue();
	}

	[Test, DisplayName("comparison on version number only")]
	public void ComparisonOnVersionNumberOnly()
	{
		((Applicab)"ga 8.1.0" < (Applicab)"beta 8.2.0").Should().BeTrue();
		((Applicab)"beta 8.1.0-beta" < (Applicab)"beta 8.1.0").Should().BeTrue();
	}
}

public class SortsAppliesToWithMixedVersionedAndNonVersionedItems : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   stack: ga 8.18.6, ga, ga 9.1.2, all, ga 8.19.2
			""");

	[Test, DisplayName("versioned items are sorted first, non-versioned items last")]
	public async Task VersionedItemsSortedFirst() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Stack = new AppliesCollection([
				(Applicab)"ga 9.1.2",
				(Applicab)"ga 8.19.2",
				(Applicab)"ga 8.18.6",
				(Applicab)"ga",
				(Applicab)"all",
			]),
		});
}

public class SortsAppliesToWithPatchVersionsCorrectly : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   stack: ga 9.1, ga 9.1.1, ga 9.0.5
			""");

	[Test, DisplayName("patch versions are sorted correctly")]
	public async Task PatchVersionsSortedCorrectly() =>
		await Docs.AppliesTo(new ApplicableTo
		{
			Stack = new AppliesCollection([(Applicab)"ga 9.1.1", (Applicab)"ga 9.1", (Applicab)"ga 9.0.5",]),
		});
}

public class SortsAppliesToWithMajorVersionsCorrectly : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   stack: ga 3.x, ga 5.x
			""");

	[Test, DisplayName("major versions are sorted correctly")]
	public async Task MajorVersionsSortedCorrectly() =>
		await Docs.AppliesTo(new ApplicableTo { Stack = new AppliesCollection([(Applicab)"ga 5.x", (Applicab)"ga 3.x",]), });
}

public class ServerlessStringWithVersionEmitsError : AuthoringTest
{
	protected override Scenario Scenario => FrontMatter.WithYaml("""
			applies_to:
			   serverless: ga 9.5
			""");

	[Test, DisplayName("emits error for versioned serverless")]
	public async Task EmitsErrorForVersionedServerless() => await Docs.HasError("Can't specify a version for 'serverless'");
}

public class ServerlessProjectWithVersionEmitsError : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   serverless:
			      elasticsearch: ga 9.5
			""");

	[Test, DisplayName("emits error for versioned elasticsearch project")]
	public async Task EmitsError() => await Docs.HasError("Can't specify a version for 'elasticsearch'");
}

public class ServerlessVectorDatabaseWithVersionEmitsError : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   serverless:
			      vectordb: ga 9.5
			""");

	[Test, DisplayName("emits error for versioned vector database project")]
	public async Task EmitsError() => await Docs.HasError("Can't specify a version for 'vectordb'");
}

public class DeploymentEssWithVersionEmitsError : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   deployment:
			      ess: ga 9.5
			""");

	[Test, DisplayName("emits error for versioned ess")]
	public async Task EmitsError() => await Docs.HasError("Can't specify a version for 'ess'");
}

public class DeploymentEchWithVersionEmitsError : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   deployment:
			      ech: preview 9.5
			""");

	[Test, DisplayName("emits error for versioned ech")]
	public async Task EmitsError() => await Docs.HasError("Can't specify a version for 'ech'");
}

public class TopLevelEchWithVersionEmitsError : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   ech: preview 9.5
			   stack: ga 9.1
			""");

	[Test, DisplayName("emits error for versioned top level ech")]
	public async Task EmitsError() => await Docs.HasError("Can't specify a version for 'ech'");
}

public class DeploymentStringShorthandWithVersionEmitsErrorForEss : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatter.WithYaml("""
			applies_to:
			   deployment: ga 9.0.0
			""");

	[Test, DisplayName("emits error because ess does not support versions")]
	public async Task EmitsError() => await Docs.HasError("Can't specify a version for 'ess'");
}
