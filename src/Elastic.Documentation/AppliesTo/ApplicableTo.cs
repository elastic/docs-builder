// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Text;
using System.Text.Json.Serialization;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.AppliesTo;

/// Use to collect diagnostics during YAML parsing where we do not have access to the current diagnostics collector
public class ApplicabilityDiagnosticsCollection : IEquatable<ApplicabilityDiagnosticsCollection>, IReadOnlyCollection<(Severity, string)>
{
	private readonly List<(Severity, string)> _list = [];

	public ApplicabilityDiagnosticsCollection(IEnumerable<(Severity, string)> warnings) => _list.AddRange(warnings);

	public bool Equals(ApplicabilityDiagnosticsCollection? other) => other != null && _list.SequenceEqual(other._list);

	public IEnumerator<(Severity, string)> GetEnumerator() => _list.GetEnumerator();

	public override bool Equals(object? obj) => Equals(obj as ApplicabilityDiagnosticsCollection);

	public override int GetHashCode() => _list.GetHashCode();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public int Count => _list.Count;
}

public interface IApplicableToElement
{
	ApplicableTo? AppliesTo { get; }
}

[YamlSerializable]
[JsonConverter(typeof(ApplicableToJsonConverter))]
public record ApplicableTo
{
	[YamlMember(Alias = "stack")]
	public AppliesCollection? Stack { get; set; }

	[YamlMember(Alias = "deployment")]
	public DeploymentApplicability? Deployment { get; set; }

	[YamlMember(Alias = "serverless")]
	public ServerlessProjectApplicability? Serverless { get; set; }

	[YamlMember(Alias = "product")]
	public AppliesCollection? Product { get; set; }

	public ProductApplicability? ProductApplicability { get; set; }

	[JsonIgnore]
	[YamlIgnore]
	public ApplicabilityDiagnosticsCollection? Diagnostics { get; set; }

	public static ApplicableTo All { get; } = new()
	{
		Stack = AppliesCollection.GenerallyAvailable,
		Serverless = ServerlessProjectApplicability.All,
		Deployment = DeploymentApplicability.All,
		Product = AppliesCollection.GenerallyAvailable
	};

	public static ApplicableTo Default { get; } = new()
	{
		Stack = new AppliesCollection([new Applicability { Version = new SemVersion(9, 0, 0), Lifecycle = ProductLifecycle.GenerallyAvailable }]),
		Serverless = ServerlessProjectApplicability.All
	};

	/// <inheritdoc />
	public override string ToString()
	{
		var sb = new StringBuilder();
		var hasContent = false;

		if (Stack is not null)
		{
			_ = sb.Append("stack: ").Append(Stack);
			hasContent = true;
		}

		if (Deployment is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("deployment: ").Append(Deployment);
			hasContent = true;
		}

		if (Serverless is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("serverless: ").Append(Serverless);
			hasContent = true;
		}

		if (Product is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("product: ").Append(Product);
			hasContent = true;
		}

		if (ProductApplicability is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("products: ").Append(ProductApplicability);
		}

		return sb.ToString();
	}
}

[YamlSerializable]
public record DeploymentApplicability
{
	[YamlMember(Alias = "self")]
	public AppliesCollection? Self { get; set; }

	[YamlMember(Alias = "ece")]
	public AppliesCollection? Ece { get; set; }

	[YamlMember(Alias = "eck")]
	public AppliesCollection? Eck { get; set; }

	[YamlMember(Alias = "ess")]
	public AppliesCollection? Ess { get; set; }

	public static DeploymentApplicability All { get; } = new()
	{
		Ece = AppliesCollection.GenerallyAvailable,
		Eck = AppliesCollection.GenerallyAvailable,
		Ess = AppliesCollection.GenerallyAvailable,
		Self = AppliesCollection.GenerallyAvailable
	};

	/// <inheritdoc />
	public override string ToString()
	{
		var sb = new StringBuilder();
		var hasContent = false;

		if (Self is not null)
		{
			_ = sb.Append("self=").Append(Self);
			hasContent = true;
		}

		if (Ece is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("ece=").Append(Ece);
			hasContent = true;
		}

		if (Eck is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("eck=").Append(Eck);
			hasContent = true;
		}

		if (Ess is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("ess=").Append(Ess);
		}

		return sb.ToString();
	}
}

[YamlSerializable]
public record ServerlessProjectApplicability
{
	[YamlMember(Alias = "elasticsearch")]
	public AppliesCollection? Elasticsearch { get; set; }

	[YamlMember(Alias = "observability")]
	public AppliesCollection? Observability { get; set; }

	[YamlMember(Alias = "security")]
	public AppliesCollection? Security { get; set; }

	/// <summary>
	/// Returns if all projects share the same applicability
	/// </summary>
	public AppliesCollection? AllProjects =>
		Elasticsearch == Observability && Observability == Security
			? Elasticsearch
			: null;

	public static ServerlessProjectApplicability All { get; } = new()
	{
		Elasticsearch = AppliesCollection.GenerallyAvailable,
		Observability = AppliesCollection.GenerallyAvailable,
		Security = AppliesCollection.GenerallyAvailable
	};

	/// <inheritdoc />
	public override string ToString()
	{
		var sb = new StringBuilder();
		var hasContent = false;

		if (Elasticsearch is not null)
		{
			_ = sb.Append("elasticsearch=").Append(Elasticsearch);
			hasContent = true;
		}

		if (Observability is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("observability=").Append(Observability);
			hasContent = true;
		}

		if (Security is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("security=").Append(Security);
		}

		return sb.ToString();
	}
}
