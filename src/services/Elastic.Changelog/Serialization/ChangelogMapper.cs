// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Riok.Mapperly.Abstractions;

namespace Elastic.Changelog.Serialization;

/// <summary>
/// Source-generated mapper for converting between YAML DTOs and domain types.
/// </summary>
[Mapper(ThrowOnPropertyMappingNullMismatch = false)]
internal static partial class ChangelogMapper
{
	// YAML DTO to Domain mappings
	[MapProperty(nameof(ChangelogEntryYaml.Type), nameof(ChangelogEntry.Type), Use = nameof(ParseEntryType))]
	[MapProperty(nameof(ChangelogEntryYaml.Subtype), nameof(ChangelogEntry.Subtype), Use = nameof(ParseEntrySubtype))]
	public static partial ChangelogEntry ToEntry(ChangelogEntryYaml yaml);

	[MapProperty(nameof(ProductInfoYaml.Product), nameof(ProductReference.ProductId), Use = nameof(NullToEmpty))]
	[MapProperty(nameof(ProductInfoYaml.Lifecycle), nameof(ProductReference.Lifecycle), Use = nameof(ParseLifecycle))]
	public static partial ProductReference ToProductReference(ProductInfoYaml yaml);

	// ProductArgument to domain type mappings
	[MapProperty(nameof(ProductArgument.Product), nameof(ProductReference.ProductId), Use = nameof(NullToEmpty))]
	[MapProperty(nameof(ProductArgument.Lifecycle), nameof(ProductReference.Lifecycle), Use = nameof(ParseLifecycle))]
	public static partial ProductReference ToProductReference(ProductArgument arg);

	[MapProperty(nameof(ProductArgument.Product), nameof(BundledProduct.ProductId), Use = nameof(NullToEmpty))]
	[MapProperty(nameof(ProductArgument.Lifecycle), nameof(BundledProduct.Lifecycle), Use = nameof(ParseLifecycle))]
	public static partial BundledProduct ToBundledProduct(ProductArgument arg);

	// BundledEntry to ChangelogEntry mapping
	[MapProperty(nameof(BundledEntry.Type), nameof(ChangelogEntry.Type), Use = nameof(BundledEntryTypeToEntryType))]
	[MapperIgnoreSource(nameof(BundledEntry.File))]
	public static partial ChangelogEntry ToEntry(BundledEntry entry);

	public static partial Bundle ToBundle(BundleYaml yaml);

	[MapProperty(nameof(BundledProductYaml.Product), nameof(BundledProduct.ProductId), Use = nameof(NullToEmpty))]
	[MapProperty(nameof(BundledProductYaml.Lifecycle), nameof(BundledProduct.Lifecycle), Use = nameof(ParseLifecycle))]
	public static partial BundledProduct ToBundledProduct(BundledProductYaml yaml);

	[MapProperty(nameof(BundledEntryYaml.Type), nameof(BundledEntry.Type), Use = nameof(ParseEntryTypeNullable))]
	[MapProperty(nameof(BundledEntryYaml.Subtype), nameof(BundledEntry.Subtype), Use = nameof(ParseEntrySubtype))]
	public static partial BundledEntry ToBundledEntry(BundledEntryYaml yaml);

	public static partial BundledFile ToBundledFile(BundledFileYaml yaml);

	/// <summary>
	/// Converts a ChangelogEntry to a BundledEntry for embedding in bundles.
	/// File property is not mapped; set it separately using a 'with' expression.
	/// </summary>
	[MapProperty(nameof(ChangelogEntry.Type), nameof(BundledEntry.Type), Use = nameof(EntryTypeToNullable))]
	[MapperIgnoreTarget(nameof(BundledEntry.File))]
	public static partial BundledEntry ToBundledEntry(ChangelogEntry entry);

	[MapProperty(nameof(ChangelogEntry.Type), nameof(ChangelogEntryYaml.Type), Use = nameof(EntryTypeToString))]
	[MapProperty(nameof(ChangelogEntry.Subtype), nameof(ChangelogEntryYaml.Subtype), Use = nameof(EntrySubtypeToString))]
	public static partial ChangelogEntryYaml ToYaml(ChangelogEntry entry);

	[MapProperty(nameof(ProductReference.ProductId), nameof(ProductInfoYaml.Product))]
	[MapProperty(nameof(ProductReference.Lifecycle), nameof(ProductInfoYaml.Lifecycle), Use = nameof(LifecycleToString))]
	public static partial ProductInfoYaml ToYaml(ProductReference product);

	public static partial BundleYaml ToYaml(Bundle bundle);

	[MapProperty(nameof(BundledProduct.ProductId), nameof(BundledProductYaml.Product))]
	[MapProperty(nameof(BundledProduct.Lifecycle), nameof(BundledProductYaml.Lifecycle), Use = nameof(LifecycleToString))]
	public static partial BundledProductYaml ToYaml(BundledProduct product);

	[MapProperty(nameof(BundledEntry.Type), nameof(BundledEntryYaml.Type), Use = nameof(EntryTypeNullableToString))]
	[MapProperty(nameof(BundledEntry.Subtype), nameof(BundledEntryYaml.Subtype), Use = nameof(EntrySubtypeToString))]
	public static partial BundledEntryYaml ToYaml(BundledEntry entry);

	public static partial BundledFileYaml ToYaml(BundledFile file);

	/// <summary>Converts nullable string to non-nullable, using empty string for null.</summary>
	private static string NullToEmpty(string? value) => value ?? "";

	private static ChangelogEntryType ParseEntryType(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return ChangelogEntryType.Invalid;

		return ChangelogEntryTypeExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: ChangelogEntryType.Invalid;
	}

	private static ChangelogEntryType? ParseEntryTypeNullable(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		return ChangelogEntryTypeExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: null;
	}

	private static ChangelogEntrySubtype? ParseEntrySubtype(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		return ChangelogEntrySubtypeExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: null;
	}

	private static Lifecycle? ParseLifecycle(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		return LifecycleExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: null;
	}

	private static string? EntryTypeToString(ChangelogEntryType value) =>
		value != ChangelogEntryType.Invalid ? value.ToStringFast(true) : null;

	private static ChangelogEntryType? EntryTypeToNullable(ChangelogEntryType value) =>
		value != ChangelogEntryType.Invalid ? value : null;

	private static string? EntryTypeNullableToString(ChangelogEntryType? value) =>
		value?.ToStringFast(true);

	private static string? EntrySubtypeToString(ChangelogEntrySubtype? value) =>
		value?.ToStringFast(true);

	private static string? LifecycleToString(Lifecycle? value) =>
		value?.ToStringFast(true);

	private static ChangelogEntryType BundledEntryTypeToEntryType(ChangelogEntryType? value) =>
		value ?? ChangelogEntryType.Invalid;
}
