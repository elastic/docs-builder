// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// The version tree of one API a reader is in, e.g. key <c>elasticsearch</c> and tree <c>8</c> on
/// <c>/docs/api/doc/elasticsearch/v8</c>. Matches <see cref="ApiOperationMetadata.Key"/> and <see cref="ApiTreeEntry.Tree"/>.
/// </summary>
public record ApiTreeScope(string Key, string Tree);
