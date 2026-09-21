// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Marker interface for filesystems that include the application data directory
/// (<see cref="Elastic.Documentation.Configuration.Paths.ApplicationData"/>) in their scope.
/// Services that read or write AppData (caches, link indices, config-runtime state) should
/// declare this interface rather than the bare <see cref="IFileSystem"/>.
/// </summary>
public interface IAppDataFileSystem : IFileSystem;
