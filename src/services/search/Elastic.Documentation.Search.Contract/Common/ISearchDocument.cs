// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Read-only contract every indexed search document satisfies. Polymorphic dispatch is opt-in:
/// declare the deserialisation target as <see cref="ISearchDocument"/> and STJ routes by the
/// <c>$type</c> JSON discriminator to one of the registered concrete types. Concrete-type reads
/// (e.g. <see cref="WebsiteSearchDocument"/>, <c>DocumentationDocument</c>) stay flat and
/// don't trigger dispatch — useful at the search-service boundary where the index already holds a
/// uniform on-the-wire shape.
/// <para>
/// Additional derived types (e.g. <c>Elastic.Documentation</c>'s <c>DocumentationDocument</c>) are registered
/// at runtime via <see cref="SearchDocumentPolymorphism.AddDerivedType{TBase}"/>.
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(SiteDocument), "site")]
[JsonDerivedType(typeof(LabsDocument), "labs")]
[JsonDerivedType(typeof(GuideDocument), "guide")]
[JsonDerivedType(typeof(WebsiteSearchDocument), "website")]
public interface ISearchDocument
{
	string Title { get; }
	string SearchTitle { get; }
	string Type { get; }
	string ContentType { get; }
	string Url { get; }
	string Hash { get; }
}
