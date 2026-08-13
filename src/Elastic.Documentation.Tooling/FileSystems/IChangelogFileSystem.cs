// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Marker interface for the changelog scope: the git root of the target repository.
/// Changelog services declare this rather than bare <see cref="IFileSystem"/> so that the
/// compiler enforces that only a properly-scoped filesystem (one that can reach the
/// changelog YAML files and read <c>.git</c> metadata) is wired in.
/// </summary>
public interface IChangelogFileSystem : IFileSystem;
