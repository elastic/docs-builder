// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Encodings.Web;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Versions;
using Nullean.Snapshots.Json;

namespace Elastic.Markdown.Tests.AppliesTo;

public class ApplicableToJsonConverterSerializationTests
{
	private readonly JsonSerializerOptions _options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

	[Test]
	public void SerializeStackProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo { Stack = AppliesCollection.GenerallyAvailable };

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			"""
		);
	}

	[Test]
	public void SerializeStackWithVersionProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Stack = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"8.0.0" }])
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "beta",
			    "version": "8.0+"
			  }
			]
			"""
		);
	}

	[Test]
	public void SerializeMultipleApplicabilitiesProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Stack = new AppliesCollection([
				new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"8.0.0" },
				new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"7.17.0" }
			])
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "ga",
			    "version": "8.0+"
			  },
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "beta",
			    "version": "7.17+"
			  }
			]
			"""
		);
	}

	[Test]
	public void SerializeDeploymentProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Deployment = new DeploymentApplicability
			{
				Ece = new AppliesCollection([
					new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"3.0.0" }
				]),
				Ess = AppliesCollection.GenerallyAvailable
			}
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "deployment",
			    "sub_type": "ece",
			    "lifecycle": "ga",
			    "version": "3.0+"
			  },
			  {
			    "type": "deployment",
			    "sub_type": "ess",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			"""
		);
	}

	[Test]
	public void SerializeServerlessProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = new AppliesCollection([
					new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"1.0.0" }
				]),
				Security = AppliesCollection.GenerallyAvailable
			}
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "serverless",
			    "sub_type": "elasticsearch",
			    "lifecycle": "beta",
			    "version": "1.0+"
			  },
			  {
			    "type": "serverless",
			    "sub_type": "security",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			"""
		);
	}

	[Test]
	public void Serialize_ServerlessVectorDatabase_ProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability { VectorDatabase = AppliesCollection.GenerallyAvailable }
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "serverless",
			    "sub_type": "vectordb",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			"""
		);
	}

	[Test]
	public void SerializeProductProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Product = new AppliesCollection([
				new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = (VersionSpec)"0.5.0" }
			])
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "product",
			    "sub_type": "product",
			    "lifecycle": "preview",
			    "version": "0.5+"
			  }
			]
			"""
		);
	}

	[Test]
	public void SerializeProductApplicabilityProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability
			{
				Ecctl = new AppliesCollection([
					new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = (VersionSpec)"5.0.0" }
				]),
				ApmAgentDotnet = AppliesCollection.GenerallyAvailable
			}
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "product",
			    "sub_type": "ecctl",
			    "lifecycle": "deprecated",
			    "version": "5.0+"
			  },
			  {
			    "type": "product",
			    "sub_type": "apm-agent-dotnet",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			"""
		);
	}

	[Test]
	public void SerializeAllLifecyclesProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Stack = new AppliesCollection([
				new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = (VersionSpec)"1.0.0" },
				new Applicability { Lifecycle = ProductLifecycle.Experimental, Version = (VersionSpec)"1.0.0" },
				new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"1.0.0" },
				new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"1.0.0" },
				new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = (VersionSpec)"1.0.0" },
				new Applicability { Lifecycle = ProductLifecycle.Removed, Version = (VersionSpec)"1.0.0" }
			])
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "preview",
			    "version": "1.0+"
			  },
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "experimental",
			    "version": "1.0+"
			  },
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "beta",
			    "version": "1.0+"
			  },
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "ga",
			    "version": "1.0+"
			  },
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "deprecated",
			    "version": "1.0+"
			  },
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "removed",
			    "version": "1.0+"
			  }
			]
			"""
		);
	}

	[Test]
	public void SerializeComplexProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Stack = new AppliesCollection([
				new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"8.0.0" }
			]),
			Deployment = new DeploymentApplicability { Ece = AppliesCollection.GenerallyAvailable },
			Product = AppliesCollection.GenerallyAvailable
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "ga",
			    "version": "8.0+"
			  },
			  {
			    "type": "deployment",
			    "sub_type": "ece",
			    "lifecycle": "ga",
			    "version": "all"
			  },
			  {
			    "type": "product",
			    "sub_type": "product",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			"""
		);
	}

	[Test]
	public void SerializeEmptyProducesEmptyArray()
	{
		var applicableTo = new ApplicableTo();

		var json = JsonSerializer.Serialize(applicableTo, _options);

		json.Should().MatchInlineJsonSnapshot("[]");
	}

	[Test]
	public void SerializeValidatesJsonStructure()
	{
		var original = new ApplicableTo
		{
			Stack = AppliesCollection.GenerallyAvailable,
			Deployment = new DeploymentApplicability
			{
				Ece = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"3.0.0" }])
			}
		};

		var json = JsonSerializer.Serialize(original, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			[
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "ga",
			    "version": "all"
			  },
			  {
			    "type": "deployment",
			    "sub_type": "ece",
			    "lifecycle": "beta",
			    "version": "3.0+"
			  }
			]
			"""
		);
	}
}
