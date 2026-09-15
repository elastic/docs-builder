// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Read-only contract every indexed search document satisfies. Polymorphic dispatch is opt-in:
/// declare the deserialisation target as <see cref="ISearchDocument"/> and STJ routes by the
/// <c>$type</c> JSON discriminator to one of the registered concrete types. Concrete-type reads
/// (e.g. <see cref="WebsiteSearchDocument"/>, <see cref="DocumentationDocument"/>) stay flat and
/// don't trigger dispatch — useful at the search-service boundary where the index already holds a
/// uniform on-the-wire shape.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(SiteDocument), "site")]
[JsonDerivedType(typeof(LabsDocument), "labs")]
[JsonDerivedType(typeof(GuideDocument), "guide")]
[JsonDerivedType(typeof(WebsiteSearchDocument), "website")]
[JsonDerivedType(typeof(DocumentationDocument), "docs")]
public interface ISearchDocument
{
	string Title { get; }
	string SearchTitle { get; }
	string Type { get; }
	string ContentType { get; }
	string Path { get; }
	string Hash { get; }
}
