// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using FluentAssertions;

namespace Elastic.Markdown.Tests.AppliesTo;

public class ApplicableToJsonConverterRoundTripTests
{
	private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

	[Fact]
	public void RoundTripStackSimple()
	{
		var original = new ApplicableTo
		{
			Stack = AppliesCollection.GenerallyAvailable
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Stack.Should().NotBeNull();
		deserialized.Stack.Should().BeEquivalentTo(original.Stack);
	}

	[Fact]
	public void RoundTripStackWithVersion()
	{
		var original = new ApplicableTo
		{
			Stack = new AppliesCollection(
			[
				new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"8.0.0" },
				new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"7.17.0" }
			])
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Stack.Should().NotBeNull();
		deserialized.Stack.Should().BeEquivalentTo(original.Stack);
	}

	[Fact]
	public void RoundTripDeploymentAllProperties()
	{
		var original = new ApplicableTo
		{
			Deployment = new DeploymentApplicability
			{
				Self = AppliesCollection.GenerallyAvailable,
				Ece = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"3.0.0" }]),
				Eck = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"2.0.0" }]),
				Ess = AppliesCollection.GenerallyAvailable
			}
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Deployment.Should().NotBeNull();
		deserialized.Deployment!.Self.Should().BeEquivalentTo(original.Deployment!.Self);
		deserialized.Deployment.Ece.Should().BeEquivalentTo(original.Deployment.Ece);
		deserialized.Deployment.Eck.Should().BeEquivalentTo(original.Deployment.Eck);
		deserialized.Deployment.Ess.Should().BeEquivalentTo(original.Deployment.Ess);
	}

	[Fact]
	public void RoundTripServerlessAllProperties()
	{
		var original = new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = AppliesCollection.GenerallyAvailable,
				Observability = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = AllVersionsSpec.Instance }]),
				Security = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"1.0.0" }])
			}
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Serverless.Should().NotBeNull();
		deserialized.Serverless!.Elasticsearch.Should().BeEquivalentTo(original.Serverless!.Elasticsearch);
		deserialized.Serverless.Observability.Should().BeEquivalentTo(original.Serverless.Observability);
		deserialized.Serverless.Security.Should().BeEquivalentTo(original.Serverless.Security);
	}

	[Fact]
	public void RoundTripProductSimple()
	{
		var original = new ApplicableTo
		{
			Product = AppliesCollection.GenerallyAvailable
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Product.Should().NotBeNull();
		deserialized.Product.Should().BeEquivalentTo(original.Product);
	}

	[Fact]
	public void RoundTripProductApplicabilitySingleProduct()
	{
		var original = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability
			{
				Ecctl = AppliesCollection.GenerallyAvailable
			}
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.ProductApplicability.Should().NotBeNull();
		deserialized.ProductApplicability!.Ecctl.Should().BeEquivalentTo(original.ProductApplicability!.Ecctl);
	}

	[Fact]
	public void RoundTripProductApplicabilityMultipleProducts()
	{
		var original = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability
			{
				Ecctl = AppliesCollection.GenerallyAvailable,
				Curator = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = (VersionSpec)"5.0.0" }]),
				ApmAgentDotnet = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"1.2.0" }]),
				EdotDotnet = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"0.9.0" }])
			}
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.ProductApplicability.Should().NotBeNull();
		deserialized.ProductApplicability!.Ecctl.Should().BeEquivalentTo(original.ProductApplicability!.Ecctl);
		deserialized.ProductApplicability.Curator.Should().BeEquivalentTo(original.ProductApplicability.Curator);
		deserialized.ProductApplicability.ApmAgentDotnet.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentDotnet);
		deserialized.ProductApplicability.EdotDotnet.Should().BeEquivalentTo(original.ProductApplicability.EdotDotnet);
	}

	[Fact]
	public void RoundTripAllProductApplicabilityProperties()
	{
		var original = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability
			{
				Ecctl = AppliesCollection.GenerallyAvailable,
				Curator = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = (VersionSpec)"5.0.0" }]),
				ApmAgentAndroid = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"1.0.0" }]),
				ApmAgentDotnet = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"1.2.0" }]),
				ApmAgentGo = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"2.0.0" }]),
				ApmAgentIos = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = (VersionSpec)"0.5.0" }]),
				ApmAgentJava = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"1.30.0" }]),
				ApmAgentNode = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"3.0.0" }]),
				ApmAgentPhp = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"1.8.0" }]),
				ApmAgentPython = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"6.0.0" }]),
				ApmAgentRuby = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"4.0.0" }]),
				ApmAgentRumJs = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"5.0.0" }]),
				EdotIos = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"0.9.0" }]),
				EdotAndroid = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"0.8.0" }]),
				EdotDotnet = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"0.9.0" }]),
				EdotJava = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"0.7.0" }]),
				EdotNode = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"0.6.0" }]),
				EdotPhp = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"0.5.0" }]),
				EdotPython = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"0.4.0" }]),
				EdotCfAws = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = (VersionSpec)"0.3.0" }]),
				EdotCfAzure = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = (VersionSpec)"0.2.0" }]),
				EdotCollector = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"1.0.0" }])
			}
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.ProductApplicability.Should().NotBeNull();
		deserialized.ProductApplicability!.Ecctl.Should().BeEquivalentTo(original.ProductApplicability!.Ecctl);
		deserialized.ProductApplicability.Curator.Should().BeEquivalentTo(original.ProductApplicability.Curator);
		deserialized.ProductApplicability.ApmAgentAndroid.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentAndroid);
		deserialized.ProductApplicability.ApmAgentDotnet.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentDotnet);
		deserialized.ProductApplicability.ApmAgentGo.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentGo);
		deserialized.ProductApplicability.ApmAgentIos.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentIos);
		deserialized.ProductApplicability.ApmAgentJava.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentJava);
		deserialized.ProductApplicability.ApmAgentNode.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentNode);
		deserialized.ProductApplicability.ApmAgentPhp.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentPhp);
		deserialized.ProductApplicability.ApmAgentPython.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentPython);
		deserialized.ProductApplicability.ApmAgentRuby.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentRuby);
		deserialized.ProductApplicability.ApmAgentRumJs.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentRumJs);
		deserialized.ProductApplicability.EdotIos.Should().BeEquivalentTo(original.ProductApplicability.EdotIos);
		deserialized.ProductApplicability.EdotAndroid.Should().BeEquivalentTo(original.ProductApplicability.EdotAndroid);
		deserialized.ProductApplicability.EdotDotnet.Should().BeEquivalentTo(original.ProductApplicability.EdotDotnet);
		deserialized.ProductApplicability.EdotJava.Should().BeEquivalentTo(original.ProductApplicability.EdotJava);
		deserialized.ProductApplicability.EdotNode.Should().BeEquivalentTo(original.ProductApplicability.EdotNode);
		deserialized.ProductApplicability.EdotPhp.Should().BeEquivalentTo(original.ProductApplicability.EdotPhp);
		deserialized.ProductApplicability.EdotPython.Should().BeEquivalentTo(original.ProductApplicability.EdotPython);
		deserialized.ProductApplicability.EdotCfAws.Should().BeEquivalentTo(original.ProductApplicability.EdotCfAws);
		deserialized.ProductApplicability.EdotCfAzure.Should().BeEquivalentTo(original.ProductApplicability.EdotCfAzure);
		deserialized.ProductApplicability.EdotCollector.Should().BeEquivalentTo(original.ProductApplicability.EdotCollector);
	}

	[Fact]
	public void RoundTripComplexAllFieldsPopulated()
	{
		var original = new ApplicableTo
		{
			Stack = new AppliesCollection(
			[
				new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"8.0.0" },
				new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"7.17.0" }
			]),
			Deployment = new DeploymentApplicability
			{
				Self = AppliesCollection.GenerallyAvailable,
				Ece = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"3.0.0" }]),
				Eck = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"2.0.0" }]),
				Ess = AppliesCollection.GenerallyAvailable
			},
			Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = AppliesCollection.GenerallyAvailable,
				Observability = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = AllVersionsSpec.Instance }]),
				Security = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"1.0.0" }])
			},
			Product = AppliesCollection.GenerallyAvailable,
			ProductApplicability = new ProductApplicability
			{
				Ecctl = AppliesCollection.GenerallyAvailable,
				ApmAgentDotnet = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"1.2.0" }])
			}
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Stack.Should().BeEquivalentTo(original.Stack);
		deserialized.Deployment.Should().NotBeNull();
		deserialized.Deployment!.Self.Should().BeEquivalentTo(original.Deployment!.Self);
		deserialized.Deployment.Ece.Should().BeEquivalentTo(original.Deployment.Ece);
		deserialized.Deployment.Eck.Should().BeEquivalentTo(original.Deployment.Eck);
		deserialized.Deployment.Ess.Should().BeEquivalentTo(original.Deployment.Ess);
		deserialized.Serverless.Should().NotBeNull();
		deserialized.Serverless!.Elasticsearch.Should().BeEquivalentTo(original.Serverless!.Elasticsearch);
		deserialized.Serverless.Observability.Should().BeEquivalentTo(original.Serverless.Observability);
		deserialized.Serverless.Security.Should().BeEquivalentTo(original.Serverless.Security);
		deserialized.Product.Should().BeEquivalentTo(original.Product);
		deserialized.ProductApplicability.Should().NotBeNull();
		deserialized.ProductApplicability!.Ecctl.Should().BeEquivalentTo(original.ProductApplicability!.Ecctl);
		deserialized.ProductApplicability.ApmAgentDotnet.Should().BeEquivalentTo(original.ProductApplicability.ApmAgentDotnet);
	}

	[Fact]
	public void EchSubType_DeserializesAsEss()
	{
		var json = """
			[
				{ "type": "deployment", "sub_type": "ech", "lifecycle": "ga", "version": "9.0.0" }
			]
			""";

		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Deployment.Should().NotBeNull();
		deserialized.Deployment!.Ess.Should().NotBeNull();
		deserialized.Deployment.Ess!.First().Lifecycle.Should().Be(ProductLifecycle.GenerallyAvailable);
		deserialized.Deployment.Ess!.First().Version.Should().NotBeNull();
	}

	[Fact]
	public void EchSubType_SerializesBackAsEss()
	{
		var json = """
			[
				{ "type": "deployment", "sub_type": "ech", "lifecycle": "ga", "version": "9.0.0" }
			]
			""";

		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);
		var reserialized = JsonSerializer.Serialize(deserialized, _options);

		reserialized.Should().Contain("\"sub_type\": \"ess\"");
		reserialized.Should().NotContain("\"sub_type\": \"ech\"");
	}

	[Fact]
	public void RoundTripAllLifecycles()
	{
		var lifecycles = Enum.GetValues<ProductLifecycle>();
		var applicabilities = lifecycles.Select(lc =>
			new Applicability { Lifecycle = lc, Version = (VersionSpec)"1.0.0" }
		).ToArray();

		var original = new ApplicableTo
		{
			Stack = new AppliesCollection(applicabilities)
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Stack.Should().NotBeNull();
		deserialized.Stack.Should().BeEquivalentTo(original.Stack);
	}

	[Fact]
	public void RoundTripMultipleApplicabilitiesInCollection()
	{
		var original = new ApplicableTo
		{
			Stack = new AppliesCollection(
			[
				new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"8.0.0" },
				new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"7.17.0" },
				new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = (VersionSpec)"7.16.0" },
				new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = (VersionSpec)"6.0.0" }
			])
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Stack.Should().NotBeNull();
		deserialized.Stack.Should().HaveCount(4);
		deserialized.Stack.Should().BeEquivalentTo(original.Stack);
	}

	[Fact]
	public void RoundTripEmptyApplicableTo()
	{
		var original = new ApplicableTo();

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Stack.Should().BeNull();
		deserialized.Deployment.Should().BeNull();
		deserialized.Serverless.Should().BeNull();
		deserialized.Product.Should().BeNull();
		deserialized.ProductApplicability.Should().BeNull();
	}

	[Fact]
	public void RoundTripNullReturnsNull()
	{
		ApplicableTo? original = null;

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().BeNull();
	}

	[Fact]
	public void RoundTripAllVersionsSerializesAsSemanticVersion()
	{
		var original = new ApplicableTo
		{
			Stack = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = AllVersionsSpec.Instance }])
		};

		var json = JsonSerializer.Serialize(original, _options);
		json.Should().Contain("\"version\": \"all\"");

		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);
		deserialized.Should().NotBeNull();
		deserialized!.Stack.Should().NotBeNull();
		deserialized.Stack!.First().Version.Should().Be(AllVersionsSpec.Instance);
	}

	[Fact]
	public void RoundTripProductAndProductApplicabilityBothPresent()
	{
		var original = new ApplicableTo
		{
			Product = AppliesCollection.GenerallyAvailable,
			ProductApplicability = new ProductApplicability
			{
				Ecctl = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"1.0.0" }])
			}
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ApplicableTo>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Product.Should().BeEquivalentTo(original.Product);
		deserialized.ProductApplicability.Should().NotBeNull();
		deserialized.ProductApplicability!.Ecctl.Should().BeEquivalentTo(original.ProductApplicability!.Ecctl);
	}
}
