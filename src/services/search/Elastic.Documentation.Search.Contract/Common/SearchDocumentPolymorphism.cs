// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// AOT-safe composition helpers for <see cref="ISearchDocument"/> / <see cref="SearchDocumentBase"/>
/// polymorphism.
/// <para>
/// The contract project ships all known document types (<see cref="SiteDocument"/>,
/// <see cref="LabsDocument"/>, <see cref="GuideDocument"/>, <see cref="WebsiteSearchDocument"/>,
/// <see cref="DocumentationDocument"/>) with <c>[JsonDerivedType]</c> baked onto
/// <see cref="ISearchDocument"/>. <see cref="AddDerivedType{TBase}"/> and <see cref="Compose"/>
/// remain available for consumers that need to register additional, out-of-repo document types
/// at runtime.
/// </para>
/// <para>
/// All APIs are AOT-compatible: they do not use reflection-based type discovery. Every type
/// whose <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo"/> is touched must be
/// registered in one of the source-gen contexts passed to <see cref="Compose"/>.
/// </para>
/// </summary>
public static class SearchDocumentPolymorphism
{
	// Derived types baked into the contract's own source-gen context.
	private static readonly (Type DerivedType, string Discriminator)[] ContractDerivedTypes =
	[
		(typeof(SiteDocument), "site"),
		(typeof(LabsDocument), "labs"),
		(typeof(GuideDocument), "guide"),
		(typeof(WebsiteSearchDocument), "website"),
		(typeof(DocumentationDocument), "docs"),
	];

	/// <summary>The contract's own source-generated type metadata resolver.</summary>
	public static IJsonTypeInfoResolver ContractResolver => SourceGenerationContext.Default;

	/// <summary>
	/// Returns an idempotent modifier that appends
	/// (<paramref name="derivedType"/> → <paramref name="discriminator"/>) to the
	/// polymorphic <c>DerivedTypes</c> of <typeparamref name="TBase"/>.
	/// <para>
	/// AOT-safe provided <paramref name="derivedType"/> is source-generated in the composed
	/// resolver. If <typeparamref name="TBase"/> has no <c>PolymorphismOptions</c> yet (no
	/// <c>[JsonPolymorphic]</c> attribute), a default options object is created with the
	/// contract's standard discriminator property (<c>$type</c>).
	/// </para>
	/// </summary>
	public static Action<JsonTypeInfo> AddDerivedType<TBase>(Type derivedType, string discriminator) =>
		typeInfo =>
		{
			if (typeInfo.Type != typeof(TBase))
				return;

			typeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions
			{
				TypeDiscriminatorPropertyName = "$type",
				IgnoreUnrecognizedTypeDiscriminators = true,
			};

			var poly = typeInfo.PolymorphismOptions;
			// Guard against exact duplicates only; the same type may not share two discriminators in STJ.
			if (poly.DerivedTypes.Any(d => Equals(d.TypeDiscriminator, discriminator) && d.DerivedType == derivedType))
				return;

			poly.DerivedTypes.Add(new JsonDerivedType(derivedType, discriminator));
		};

	/// <summary>
	/// Returns a modifier that configures <see cref="SearchDocumentBase"/> as a concrete
	/// polymorphic root with <see cref="JsonUnknownDerivedTypeHandling.FallBackToBaseType"/>:
	/// a missing or unrecognized <c>$type</c> deserializes to a <see cref="SearchDocumentBase"/>
	/// instance instead of throwing <see cref="NotSupportedException"/>.
	/// <para>
	/// All contract-known derived types (site / labs / guide / website / docs) are registered on
	/// <see cref="SearchDocumentBase"/> so that known discriminators still dispatch to the
	/// correct concrete type. Register additional consumer-specific types with
	/// <see cref="AddDerivedType{TBase}"/> targeting <see cref="SearchDocumentBase"/> before or
	/// after this modifier in the <see cref="Compose"/> call.
	/// </para>
	/// <para>
	/// Note: the fallback only applies when the declared deserialisation type is
	/// <see cref="SearchDocumentBase"/> (constructible). Reading as <see cref="ISearchDocument"/>
	/// (an interface) with a missing/unknown discriminator still throws because the interface
	/// cannot be instantiated.
	/// </para>
	/// </summary>
	public static Action<JsonTypeInfo> WithFallback() =>
		typeInfo =>
		{
			if (typeInfo.Type != typeof(SearchDocumentBase))
				return;

			typeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions
			{
				TypeDiscriminatorPropertyName = "$type",
				IgnoreUnrecognizedTypeDiscriminators = true,
				UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType,
			};

			var poly = typeInfo.PolymorphismOptions;
			poly.IgnoreUnrecognizedTypeDiscriminators = true;
			poly.UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType;

			foreach (var (derived, disc) in ContractDerivedTypes)
			{
				if (poly.DerivedTypes.Any(d => Equals(d.TypeDiscriminator, disc)))
					continue;
				poly.DerivedTypes.Add(new JsonDerivedType(derived, disc));
			}
		};

	/// <summary>
	/// Combines the contract's source-gen resolver with one or more consumer contexts and
	/// applies <paramref name="modifiers"/> in order via
	/// <see cref="JsonTypeInfoResolverChain.WithAddedModifier"/>.
	/// <para>
	/// Typical docs search call site:
	/// <code>
	/// SearchDocumentPolymorphism.Compose(
	///     consumerContexts: [
	///         Elastic.Documentation.Search.SourceGenerationContext.Default,
	///     ],
	///     SearchDocumentPolymorphism.WithFallback()
	/// );
	/// </code>
	/// </para>
	/// </summary>
	public static IJsonTypeInfoResolver Compose(
		IEnumerable<IJsonTypeInfoResolver> consumerContexts,
		params Action<JsonTypeInfo>[] modifiers)
	{
		var resolvers = new[] { ContractResolver }
			.Concat(consumerContexts)
			.ToArray();

		var combined = JsonTypeInfoResolver.Combine(resolvers);

		foreach (var modifier in modifiers)
			combined = combined.WithAddedModifier(modifier);

		return combined;
	}
}
