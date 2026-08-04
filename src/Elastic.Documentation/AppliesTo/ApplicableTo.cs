// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Versions;

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

[JsonConverter(typeof(ApplicableToJsonConverter))]
public record ApplicableTo
{
	public AppliesCollection? Stack { get; set; }

	public DeploymentApplicability? Deployment { get; set; }

	public ServerlessProjectApplicability? Serverless { get; set; }

	public AppliesCollection? Product { get; set; }

	public ProductApplicability? ProductApplicability { get; set; }

	[JsonIgnore]
	public ApplicabilityDiagnosticsCollection? Diagnostics { get; set; }

	public static ApplicableTo All { get; } = new()
	{
		Stack = AppliesCollection.GenerallyAvailable,
		Serverless = ServerlessProjectApplicability.All,
		Deployment = DeploymentApplicability.All,
		Product = AppliesCollection.GenerallyAvailable
	};

	private static readonly VersionSpec DefaultVersion = VersionSpec.TryParse("9.0", out var v) ? v : AllVersionsSpec.Instance;

	public static ApplicableTo Default { get; } = new()
	{
		Stack = new AppliesCollection([new Applicability { Version = DefaultVersion, Lifecycle = ProductLifecycle.GenerallyAvailable }]),
		Serverless = ServerlessProjectApplicability.All
	};

	/// <summary>
	/// Convert this rich applicability description into the flat wire-format entries indexed in Elasticsearch
	/// under <c>applies_to</c>. Produces the same JSON shape as <c>ApplicableToJsonConverter</c>.
	/// </summary>
	public IReadOnlyCollection<AppliesToEntry> ToAppliesTo()
	{
		var entries = new List<AppliesToEntry>();

		if (Stack is not null)
			AddEntries(entries, "stack", "stack", Stack);

		if (Deployment is not null)
		{
			if (Deployment.Self is not null)
				AddEntries(entries, "deployment", "self", Deployment.Self);
			if (Deployment.Ece is not null)
				AddEntries(entries, "deployment", "ece", Deployment.Ece);
			if (Deployment.Eck is not null)
				AddEntries(entries, "deployment", "eck", Deployment.Eck);
			if (Deployment.Ess is not null)
				AddEntries(entries, "deployment", "ess", Deployment.Ess);
		}

		if (Serverless is not null)
		{
			if (Serverless.Elasticsearch is not null)
				AddEntries(entries, "serverless", "elasticsearch", Serverless.Elasticsearch);
			if (Serverless.Observability is not null)
				AddEntries(entries, "serverless", "observability", Serverless.Observability);
			if (Serverless.Security is not null)
				AddEntries(entries, "serverless", "security", Serverless.Security);
		}

		if (Product is not null)
			AddEntries(entries, "product", "product", Product);

		if (ProductApplicability is not null)
		{
			foreach (var prop in typeof(ProductApplicability).GetProperties())
			{
				var name = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
				if (name is null)
					continue;
				if (prop.GetValue(ProductApplicability) is AppliesCollection coll)
					AddEntries(entries, "product", name, coll);
			}
		}

		return entries;
	}

	private static void AddEntries(List<AppliesToEntry> sink, string type, string subType, AppliesCollection coll)
	{
		foreach (var a in coll)
			sink.Add(new AppliesToEntry
			{
				Type = type,
				SubType = subType,
				Lifecycle = LifecycleName(a.Lifecycle),
				Version = a.Version?.ToString()
			});
	}

	private static string LifecycleName(ProductLifecycle lc) => lc switch
	{
		ProductLifecycle.TechnicalPreview => "preview",
		ProductLifecycle.Experimental => "experimental",
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

public record DeploymentApplicability
{
	public AppliesCollection? Self { get; set; }

	public AppliesCollection? Ece { get; set; }

	public AppliesCollection? Eck { get; set; }

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

public record ServerlessProjectApplicability
{
	public AppliesCollection? Elasticsearch { get; set; }

	public AppliesCollection? Observability { get; set; }

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
