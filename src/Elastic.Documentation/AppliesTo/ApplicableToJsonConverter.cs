// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.AppliesTo;

/// <summary>
/// JSON converter for ApplicableTo that serializes to a flat array of objects with:
/// - type: stack, deployment, serverless, or product
/// - sub-type: the property name (e.g., "self", "ece", "elasticsearch", "ecctl")
/// - lifecycle: the lifecycle value (if applicable)
/// - version: the version value (if applicable)
/// </summary>
public class ApplicableToJsonConverter : JsonConverter<ApplicableTo>
{
	public override ApplicableTo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType != JsonTokenType.StartArray)
			throw new JsonException("Expected array");

		var result = new ApplicableTo();
		var deploymentProps = new Dictionary<string, List<Applicability>>();
		var serverlessProps = new Dictionary<string, List<Applicability>>();
		var productProps = new Dictionary<string, List<Applicability>>();
		var stackItems = new List<Applicability>();
		var productItems = new List<Applicability>();

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndArray)
				break;

			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException("Expected object");

			string? type = null;
			string? subType = null;
			var lifecycle = ProductLifecycle.GenerallyAvailable;
			VersionSpec? version = null;

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
					break;

				if (reader.TokenType != JsonTokenType.PropertyName)
					throw new JsonException("Expected property name");

				var propertyName = reader.GetString();
				_ = reader.Read();

				switch (propertyName)
				{
					case "type":
						type = reader.GetString();
						break;
					case "sub_type":
						subType = reader.GetString();
						break;
					case "lifecycle":
						var lifecycleStr = reader.GetString();
						if (lifecycleStr != null)
							lifecycle = ParseLifecycle(lifecycleStr);
						break;
					case "version":
						var versionStr = reader.GetString();
						if (versionStr != null)
						{
							// Handle "all" explicitly for AllVersionsSpec
							if (string.Equals(versionStr.Trim(), "all", StringComparison.OrdinalIgnoreCase))
								version = AllVersionsSpec.Instance;
							else if (VersionSpec.TryParse(versionStr, out var v))
								version = v;
						}
						break;
				}
			}

			if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(subType))
				throw new JsonException("Missing type or sub-type");

			var applicability = new Applicability { Lifecycle = lifecycle, Version = version };

			switch (type)
			{
				case "stack":
					stackItems.Add(applicability);
					break;
				case "deployment":
					if (!deploymentProps.ContainsKey(subType))
						deploymentProps[subType] = [];
					deploymentProps[subType].Add(applicability);
					break;
				case "serverless":
					if (!serverlessProps.ContainsKey(subType))
						serverlessProps[subType] = [];
					serverlessProps[subType].Add(applicability);
					break;
				case "product" when subType == "product":
					productItems.Add(applicability);
					break;
				case "product":
					if (!productProps.ContainsKey(subType))
						productProps[subType] = [];
					productProps[subType].Add(applicability);
					break;
			}
		}

		// Create Stack collection
		if (stackItems.Count > 0)
			result.Stack = new AppliesCollection(stackItems.ToArray());

		// Create Product collection
		if (productItems.Count > 0)
			result.Product = new AppliesCollection(productItems.ToArray());

		// Reconstruct DeploymentApplicability
		if (deploymentProps.Count > 0)
		{
			result.Deployment = new DeploymentApplicability
			{
				Self = deploymentProps.TryGetValue("self", out var self) ? new AppliesCollection(self.ToArray()) : null,
				Ece = deploymentProps.TryGetValue("ece", out var ece) ? new AppliesCollection(ece.ToArray()) : null,
				Eck = deploymentProps.TryGetValue("eck", out var eck) ? new AppliesCollection(eck.ToArray()) : null,
				Ess = deploymentProps.TryGetValue("ech", out var ess) || deploymentProps.TryGetValue("ess", out ess) ? new AppliesCollection(ess.ToArray()) : null
			};
		}

		// Reconstruct ServerlessProjectApplicability
		if (serverlessProps.Count > 0)
		{
			result.Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = serverlessProps.TryGetValue("elasticsearch", out var es) ? new AppliesCollection(es.ToArray()) : null,
				Observability = serverlessProps.TryGetValue("observability", out var obs) ? new AppliesCollection(obs.ToArray()) : null,
				Security = serverlessProps.TryGetValue("security", out var sec) ? new AppliesCollection(sec.ToArray()) : null
			};
		}

		// Reconstruct ProductApplicability
		if (productProps.Count > 0)
		{
			var productApplicability = new ProductApplicability();
			var productType = typeof(ProductApplicability);

			foreach (var (key, items) in productProps)
			{
				// Find the property by YamlMember alias
				var property = productType.GetProperties()
					.FirstOrDefault(p => p.GetCustomAttribute<YamlMemberAttribute>()?.Alias == key);

				property?.SetValue(productApplicability, new AppliesCollection(items.ToArray()));
			}

			result.ProductApplicability = productApplicability;
		}

		return result;
	}

	public override void Write(Utf8JsonWriter writer, ApplicableTo value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();

		// Stack
		if (value.Stack != null)
			WriteApplicabilityEntries(writer, "stack", "stack", value.Stack);

		// Deployment
		if (value.Deployment != null)
		{
			if (value.Deployment.Self != null)
				WriteApplicabilityEntries(writer, "deployment", "self", value.Deployment.Self);
			if (value.Deployment.Ece != null)
				WriteApplicabilityEntries(writer, "deployment", "ece", value.Deployment.Ece);
			if (value.Deployment.Eck != null)
				WriteApplicabilityEntries(writer, "deployment", "eck", value.Deployment.Eck);
			if (value.Deployment.Ess != null)
				WriteApplicabilityEntries(writer, "deployment", "ess", value.Deployment.Ess);
		}

		// Serverless
		if (value.Serverless != null)
		{
			if (value.Serverless.Elasticsearch != null)
				WriteApplicabilityEntries(writer, "serverless", "elasticsearch", value.Serverless.Elasticsearch);
			if (value.Serverless.Observability != null)
				WriteApplicabilityEntries(writer, "serverless", "observability", value.Serverless.Observability);
			if (value.Serverless.Security != null)
				WriteApplicabilityEntries(writer, "serverless", "security", value.Serverless.Security);
		}

		// Product (simple)
		if (value.Product != null)
			WriteApplicabilityEntries(writer, "product", "product", value.Product);

		// ProductApplicability (specific products)
		if (value.ProductApplicability != null)
		{
			var productType = typeof(ProductApplicability);
			foreach (var property in productType.GetProperties())
			{
				var yamlAlias = property.GetCustomAttribute<YamlMemberAttribute>()?.Alias;
				if (yamlAlias != null)
				{
					if (property.GetValue(value.ProductApplicability) is AppliesCollection propertyValue)
						WriteApplicabilityEntries(writer, "product", yamlAlias, propertyValue);
				}
			}
		}

		writer.WriteEndArray();
	}

	private static ProductLifecycle ParseLifecycle(string lifecycleStr) => lifecycleStr.ToLowerInvariant() switch
	{
		"preview" => ProductLifecycle.TechnicalPreview,
		"beta" => ProductLifecycle.Beta,
		"ga" => ProductLifecycle.GenerallyAvailable,
		"deprecated" => ProductLifecycle.Deprecated,
		"removed" => ProductLifecycle.Removed,
		"unavailable" => ProductLifecycle.Unavailable,
		"development" => ProductLifecycle.Development,
		"planned" => ProductLifecycle.Planned,
		"discontinued" => ProductLifecycle.Discontinued,
		_ => ProductLifecycle.GenerallyAvailable
	};

	private static void WriteApplicabilityEntries(Utf8JsonWriter writer, string type, string subType, AppliesCollection collection)
	{
		foreach (var applicability in collection)
		{
			writer.WriteStartObject();
			writer.WriteString("type", type);
			writer.WriteString("sub_type", subType);

			// Write lifecycle
			var lifecycleName = applicability.Lifecycle switch
			{
				ProductLifecycle.TechnicalPreview => "preview",
				ProductLifecycle.Beta => "beta",
				ProductLifecycle.GenerallyAvailable => "ga",
				ProductLifecycle.Deprecated => "deprecated",
				ProductLifecycle.Removed => "removed",
				ProductLifecycle.Unavailable => "unavailable",
				ProductLifecycle.Development => "development",
				ProductLifecycle.Planned => "planned",
				ProductLifecycle.Discontinued => "discontinued",
				_ => "ga"
			};
			writer.WriteString("lifecycle", lifecycleName);

			// Write the version
			if (applicability.Version is not null)
				writer.WriteString("version", applicability.Version.ToString());

			writer.WriteEndObject();
		}
	}
}
