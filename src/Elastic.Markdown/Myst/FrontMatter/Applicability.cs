// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Elastic.Markdown.Helpers;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

[YamlSerializable]
public record ApplicabilityOverTime : IReadOnlyCollection<Applicability>
{
	private readonly IReadOnlyCollection<Applicability> _items;
	public ApplicabilityOverTime(Applicability[] items) => _items = items;

	// <lifecycle> [version]
	public static bool TryParse(string? value, out ApplicabilityOverTime? availability)
	{
		availability = null;
		if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "all", StringComparison.InvariantCultureIgnoreCase))
		{
			availability = GenerallyAvailable;
			return true;
		}

		var items = value.Split(',');
		var applications = new List<Applicability>(items.Length);
		foreach (var item in items)
		{
			if (Applicability.TryParse(item.Trim(), out var a))
				applications.Add(a);
		}

		if (applications.Count == 0)
			return false;

		availability = new ApplicabilityOverTime(applications.ToArray());
		return true;
	}

	public virtual bool Equals(ApplicabilityOverTime? other)
	{
		if ((object)this == other)
			return true;

		if ((object?)other is null || EqualityContract != other.EqualityContract)
			return false;

		var comparer = StructuralComparisons.StructuralEqualityComparer;
		return comparer.Equals(_items, other._items);
	}

	public override int GetHashCode()
	{
		var comparer = StructuralComparisons.StructuralEqualityComparer;
		return (EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295)
		       + comparer.GetHashCode(_items);
	}


	public static explicit operator ApplicabilityOverTime(string b)
	{
		var productAvailability = TryParse(b, out var version) ? version  : null;
		return productAvailability ?? throw new ArgumentException($"'{b}' is not a valid applicability string array.");
	}

	public static ApplicabilityOverTime GenerallyAvailable { get; }
		= new([Applicability.GenerallyAvailable]);

	public override string ToString()
	{
		if (this == GenerallyAvailable)
			return "all";
		var sb = new StringBuilder();
		foreach (var item in _items)
			sb.Append(item).Append(", ");
		return sb.ToString();
	}

	public IEnumerator<Applicability> GetEnumerator() => _items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public int Count => _items.Count;
}

[YamlSerializable]
public record Applicability
{
	public ProductLifecycle Lifecycle { get; init; }
	public SemVersion? Version { get; init; }

	public static Applicability GenerallyAvailable { get; } = new()
	{
		Lifecycle = ProductLifecycle.GenerallyAvailable,
		Version = AllVersions.Instance
	};

	public override string ToString()
	{
		if (this == GenerallyAvailable)
			return "all";
		var sb = new StringBuilder();
		var lifecycle = Lifecycle switch
		{
			ProductLifecycle.TechnicalPreview => "preview",
			ProductLifecycle.Beta => "beta",
			ProductLifecycle.Development => "dev",
			ProductLifecycle.Deprecated => "deprecated",
			ProductLifecycle.Coming => "coming",
			ProductLifecycle.Discontinued => "discontinued",
			ProductLifecycle.Unavailable => "unavailable",
			ProductLifecycle.GenerallyAvailable => "ga",
			_ => throw new ArgumentOutOfRangeException()
		};
		sb.Append(lifecycle);
		if (Version is not null && Version != AllVersions.Instance)
			sb.Append(" ").Append(Version);
		return sb.ToString();
	}

	public static explicit operator Applicability(string b)
	{
		var productAvailability = TryParse(b, out var version) ? version : TryParse(b + ".0", out version) ? version : null;
		return productAvailability ?? throw new ArgumentException($"'{b}' is not a valid applicability string.");
	}

	// <lifecycle> [version]
	public static bool TryParse(string? value, [NotNullWhen(true)] out Applicability? availability)
	{
		if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "all", StringComparison.InvariantCultureIgnoreCase))
		{
			availability = GenerallyAvailable;
			return true;
		}

		var tokens = value.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length < 1)
		{
			availability = null;
			return false;
		}
		var lifecycle = tokens[0].ToLowerInvariant() switch
		{
			"preview" => ProductLifecycle.TechnicalPreview,
			"tech-preview" => ProductLifecycle.TechnicalPreview,
			"beta" => ProductLifecycle.Beta,
			"dev" => ProductLifecycle.Development,
			"development" => ProductLifecycle.Development,
			"deprecated" => ProductLifecycle.Deprecated,
			"coming" => ProductLifecycle.Coming,
			"discontinued" => ProductLifecycle.Discontinued,
			"unavailable" => ProductLifecycle.Unavailable,
			"ga" => ProductLifecycle.GenerallyAvailable,
			_ => throw new ArgumentOutOfRangeException(nameof(tokens), tokens, $"Unknown product lifecycle: {tokens[0]}")
		};

		var version = tokens.Length < 2 ? null : tokens[1] switch
		{
			null => AllVersions.Instance,
			"all" => AllVersions.Instance,
			"" => AllVersions.Instance,
			var t => SemVersionConverter.TryParse(t, out var v) ? v : null
		};
		availability = new Applicability { Version = version, Lifecycle = lifecycle };
		return true;
	}
}
