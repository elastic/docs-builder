// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.AppliesTo;

[YamlSerializable]
public record AppliesCollection : IReadOnlyCollection<Applicability>
{
	private readonly Applicability[] _items;
	public AppliesCollection(Applicability[] items) => _items = items;

	// <lifecycle> [version]
	public static bool TryParse(string? value, IList<(Severity, string)> diagnostics, out AppliesCollection? availability)
	{
		availability = null;
		if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "all", StringComparison.OrdinalIgnoreCase))
		{
			availability = GenerallyAvailable;
			return true;
		}

		var items = value.Split(',');
		var applications = new List<Applicability>(items.Length);
		foreach (var item in items)
		{
			if (Applicability.TryParse(item.Trim(), diagnostics, out var a))
				applications.Add(a);
		}

		if (applications.Count == 0)
			return false;

		// Sort by version in descending order (the highest version first)
		// Items without versions (AllVersions.Instance) are sorted last
		var sortedApplications = applications.OrderDescending().ToArray();
		availability = new AppliesCollection(sortedApplications);
		return true;
	}

	public virtual bool Equals(AppliesCollection? other)
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
		return
			(EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295)
			+ comparer.GetHashCode(_items);
	}


	public static explicit operator AppliesCollection(string b)
	{
		var diagnostics = new List<(Severity, string)>();
		var productAvailability = TryParse(b, diagnostics, out var version) ? version : null;
		if (diagnostics.Count > 0)
			throw new ArgumentException("Explicit conversion from string to AppliesCollection failed." + string.Join(Environment.NewLine, diagnostics));
		return productAvailability ?? throw new ArgumentException($"'{b}' is not a valid applicability string array.");
	}

	public static AppliesCollection GenerallyAvailable { get; }
		= new([Applicability.GenerallyAvailable]);

	public override string ToString()
	{
		if (this == GenerallyAvailable)
			return "all";
		var sb = new StringBuilder();
		foreach (var item in _items)
			_ = sb.Append(item).Append(", ");
		return sb.ToString();
	}

	public IEnumerator<Applicability> GetEnumerator() => ((IEnumerable<Applicability>)_items).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public int Count => _items.Length;
}

[YamlSerializable]
public record Applicability : IComparable<Applicability>, IComparable
{
	public ProductLifecycle Lifecycle { get; init; }
	public SemVersion? Version { get; init; }

	public static Applicability GenerallyAvailable { get; } = new()
	{
		Lifecycle = ProductLifecycle.GenerallyAvailable,
		Version = AllVersions.Instance
	};


	public string GetLifeCycleName() =>
		Lifecycle switch
		{
			ProductLifecycle.TechnicalPreview => "Preview",
			ProductLifecycle.Beta => "Beta",
			ProductLifecycle.Development => "Development",
			ProductLifecycle.Deprecated => "Deprecated",
			ProductLifecycle.Planned => "Planned",
			ProductLifecycle.Discontinued => "Discontinued",
			ProductLifecycle.Unavailable => "Unavailable",
			ProductLifecycle.GenerallyAvailable => "GA",
			ProductLifecycle.Removed => "Removed",
			_ => throw new ArgumentOutOfRangeException(nameof(Lifecycle), Lifecycle, null)
		};


	/// <inheritdoc />
	public int CompareTo(Applicability? other)
	{
		var xIsNonVersioned = Version is null || ReferenceEquals(Version, AllVersions.Instance);
		var yIsNonVersioned = other?.Version is null || ReferenceEquals(other.Version, AllVersions.Instance);

		if (xIsNonVersioned && yIsNonVersioned)
			return 0;
		if (xIsNonVersioned)
			return -1; // Non-versioned items sort last
		if (yIsNonVersioned)
			return 1;  // Non-versioned items sort last

		return Version!.CompareTo(other!.Version);
	}

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
			ProductLifecycle.Planned => "planned",
			ProductLifecycle.Discontinued => "discontinued",
			ProductLifecycle.Unavailable => "unavailable",
			ProductLifecycle.GenerallyAvailable => "ga",
			ProductLifecycle.Removed => "removed",
			_ => throw new ArgumentOutOfRangeException()
		};
		_ = sb.Append(lifecycle);
		if (Version is not null && Version != AllVersions.Instance)
			_ = sb.Append(' ').Append(Version);
		return sb.ToString();
	}

	/// <inheritdoc />
	public int CompareTo(object? obj) => CompareTo(obj as Applicability);

	public static explicit operator Applicability(string b)
	{
		var diagnostics = new List<(Severity, string)>();
		var productAvailability = TryParse(b, diagnostics, out var version) ? version : TryParse(b + ".0", diagnostics, out version) ? version : null;
		if (diagnostics.Count > 0)
			throw new ArgumentException("Explicit conversion from string to AppliesCollection failed." + string.Join(Environment.NewLine, diagnostics));
		return productAvailability ?? throw new ArgumentException($"'{b}' is not a valid applicability string.");
	}

	public static bool TryParse(string? value, IList<(Severity, string)> diagnostics, [NotNullWhen(true)] out Applicability? availability)
	{
		if (string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "all", StringComparison.OrdinalIgnoreCase))
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

		var lookup = tokens[0].ToLowerInvariant();
		var lifecycle = lookup switch
		{
			"preview" => ProductLifecycle.TechnicalPreview,
			"tech-preview" => ProductLifecycle.TechnicalPreview,
			"beta" => ProductLifecycle.Beta,
			"ga" => ProductLifecycle.GenerallyAvailable,
			"deprecated" => ProductLifecycle.Deprecated,
			"removed" => ProductLifecycle.Removed,

			// OBSOLETE should be removed once docs are cleaned up
			"unavailable" => ProductLifecycle.Unavailable,
			"dev" => ProductLifecycle.Development,
			"development" => ProductLifecycle.Development,
			"coming" => ProductLifecycle.Planned,
			"planned" => ProductLifecycle.Planned,
			"discontinued" => ProductLifecycle.Discontinued,
			_ => throw new Exception($"Unknown product lifecycle: {tokens[0]}")
		};
		var deprecatedLifecycles = new[]
		{
			ProductLifecycle.Development,
			ProductLifecycle.Planned,
			ProductLifecycle.Discontinued
		};

		// TODO emit as error when all docs have been updated
		if (deprecatedLifecycles.Contains(lifecycle))
			diagnostics.Add((Severity.Hint, $"The '{lookup}' lifecycle is deprecated and will be removed in a future release."));

		var version = tokens.Length < 2
			? null
			: tokens[1] switch
			{
				null => AllVersions.Instance,
				"all" => AllVersions.Instance,
				"" => AllVersions.Instance,
				var t => SemVersionConverter.TryParse(t, out var v) ? v : null
			};
		availability = new Applicability { Version = version, Lifecycle = lifecycle };
		return true;
	}

	public static bool operator <(Applicability? left, Applicability? right) => left is null ? right is not null : left.CompareTo(right) < 0;

	public static bool operator <=(Applicability? left, Applicability? right) => left is null || left.CompareTo(right) <= 0;

	public static bool operator >(Applicability? left, Applicability? right) => left is not null && left.CompareTo(right) > 0;

	public static bool operator >=(Applicability? left, Applicability? right) => left is null ? right is null : left.CompareTo(right) >= 0;
}

