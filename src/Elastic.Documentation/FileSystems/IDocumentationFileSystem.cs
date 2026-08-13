// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Marker interface for the documentation-set scope: a single checked-out repository
/// anchored to a <c>docset.yml</c>. Only <c>DocumentationFileSystem</c> implements
/// this; declaring it on parameters ensures the compiler rejects an assembler-scope
/// <c>ICheckoutsFileSystem</c> or a changelog-scope <c>IChangelogFileSystem</c>
/// where a docset-anchored read scope is required.
/// </summary>
public interface IDocumentationFileSystem : IFileSystem;
