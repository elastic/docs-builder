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

/// <summary>The <c>applies_to</c> block of a page, section or toc entry: which stack, serverless, deployment and product versions a piece of documentation applies to, and in which lifecycle.</summary>
/// <remarks>
/// <para><b>Built by:</b> the <c>ApplicableToYamlConverter</c> in <c>Elastic.Documentation.Configuration</c> (YAML) and <c>ApplicableToJsonConverter</c> (JSON, in the domain), plus the domain helpers <c>ApplicableTo.ForStack</c>, <c>ApplicableTo.Default</c> and <c>AppliesCollection.Of</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer (<c>new ApplicableTo { Stack = ... }</c>), <c>ApplicableTo.ForStack</c>, or <c>DomainGen.Of&lt;ApplicableTo&gt;()</c> (tests), and <c>with</c> for variations.</para>
/// <para><b>Serialised:</b> read by its own <c>ApplicableToJsonConverter</c> inside this assembly, so the internal setters need no <c>[JsonInclude]</c>.</para>
/// </remarks>
[JsonConverter(typeof(ApplicableToJsonConverter))]
public record ApplicableTo
{
	public AppliesCollection? Stack { get; internal init; }

	public DeploymentApplicability? Deployment { get; internal init; }

	public ServerlessProjectApplicability? Serverless { get; internal init; }

	public AppliesCollection? Product { get; internal init; }

	public ProductApplicability? ProductApplicability { get; internal init; }

	[JsonIgnore]
	public ApplicabilityDiagnosticsCollection? Diagnostics { get; internal init; }

	/// <summary>
	/// Keys the block wrote that the reader never maps, in the order written: nested ones as <c>deployment.x</c>, and top-level keys that pass the
	/// known-key check but are not the exact key a reader maps (<c>apm-agent-java</c>). Top-level unknown keys are in <see cref="Diagnostics"/>.
	/// </summary>
	[JsonIgnore]
	public IReadOnlyList<string> UnknownKeys { get; internal init; } = [];

	/// <summary>Keys (nested as <c>deployment.x</c>) whose value was not text, which the reader ignores.</summary>
	[JsonIgnore]
	public IReadOnlyList<string> IgnoredValues { get; internal init; } = [];

	// UnknownKeys and IgnoredValues record how the block was written, not what it means: they take no part in equality.
	public virtual bool Equals(ApplicableTo? other) =>
		other is not null
			&& EqualityComparer<AppliesCollection?>.Default.Equals(Stack, other.Stack)
			&& EqualityComparer<DeploymentApplicability?>.Default.Equals(Deployment, other.Deployment)
			&& EqualityComparer<ServerlessProjectApplicability?>.Default.Equals(Serverless, other.Serverless)
			&& EqualityComparer<AppliesCollection?>.Default.Equals(Product, other.Product)
			&& EqualityComparer<ProductApplicability?>.Default.Equals(ProductApplicability, other.ProductApplicability)
			&& EqualityComparer<ApplicabilityDiagnosticsCollection?>.Default.Equals(Diagnostics, other.Diagnostics);

	public override int GetHashCode() => HashCode.Combine(Stack, Deployment, Serverless, Product, ProductApplicability, Diagnostics);

	public static ApplicableTo All { get; } = new()
	{
		Stack = AppliesCollection.GenerallyAvailable,
		Serverless = ServerlessProjectApplicability.All,
		Deployment = DeploymentApplicability.All,
		Product = AppliesCollection.GenerallyAvailable
	};

	/// <summary>Only the stack applicability: the block <c>stack: &lt;lifecycle&gt; &lt;version&gt;</c> declares.</summary>
	public static ApplicableTo ForStack(AppliesCollection stack) => new() { Stack = stack };

	/// <summary>Only the stack applicability, of one lifecycle (<paramref name="version"/> null means every version).</summary>
	public static ApplicableTo ForStack(ProductLifecycle lifecycle, VersionSpec? version) =>
		ForStack(AppliesCollection.Of([new Applicability { Lifecycle = lifecycle, Version = version }]));

	/// <summary>
	/// The same applicability once its parse diagnostics were reported: a reader collects them where it has no collector, the renderer reports them
	/// and drops them so they are not reported again.
	/// </summary>
	public ApplicableTo WithoutDiagnostics() => this with { Diagnostics = null };

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
			if (Serverless.VectorDatabase is not null)
				AddEntries(entries, "serverless", "vectordb", Serverless.VectorDatabase);
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

/// <summary>The <c>deployment</c> part of an <c>applies_to</c> block: self-managed, ECE, ECK and Elastic Cloud Hosted.</summary>
/// <remarks>
/// <para><b>Built by:</b> the <c>ApplicableToYamlConverter</c> in <c>Elastic.Documentation.Configuration</c> (YAML) and <c>ApplicableToJsonConverter</c> (JSON, in the domain), plus the domain helpers <c>ApplicableTo.ForStack</c>, <c>ApplicableTo.Default</c> and <c>AppliesCollection.Of</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new DeploymentApplicability { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public record DeploymentApplicability
{
	public AppliesCollection? Self { get; internal init; }

	public AppliesCollection? Ece { get; internal init; }

	public AppliesCollection? Eck { get; internal init; }

	public AppliesCollection? Ess { get; internal init; }

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

/// <summary>The <c>serverless</c> part of an <c>applies_to</c> block, one entry per project type.</summary>
/// <remarks>
/// <para><b>Built by:</b> the <c>ApplicableToYamlConverter</c> in <c>Elastic.Documentation.Configuration</c> (YAML) and <c>ApplicableToJsonConverter</c> (JSON, in the domain), plus the domain helpers <c>ApplicableTo.ForStack</c>, <c>ApplicableTo.Default</c> and <c>AppliesCollection.Of</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new ServerlessProjectApplicability { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public record ServerlessProjectApplicability
{
	public AppliesCollection? Elasticsearch { get; internal init; }

	public AppliesCollection? Observability { get; internal init; }

	public AppliesCollection? Security { get; internal init; }

	public AppliesCollection? VectorDatabase { get; internal init; }

	/// <summary>
	/// Returns if all serverless project types share the same applicability.
	/// <c>serverless: ga</c> expands to Elasticsearch, Observability, Security, and Vector Database.
	/// </summary>
	public AppliesCollection? AllProjects =>
		Elasticsearch == Observability && Observability == Security && Security == VectorDatabase ? Elasticsearch : null;

	public static ServerlessProjectApplicability All { get; } = new()
	{
		Elasticsearch = AppliesCollection.GenerallyAvailable,
		Observability = AppliesCollection.GenerallyAvailable,
		Security = AppliesCollection.GenerallyAvailable,
		VectorDatabase = AppliesCollection.GenerallyAvailable
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
			hasContent = true;
		}

		if (VectorDatabase is not null)
		{
			if (hasContent)
				_ = sb.Append(", ");
			_ = sb.Append("vectordb=").Append(VectorDatabase);
		}

		return sb.ToString();
	}
}
