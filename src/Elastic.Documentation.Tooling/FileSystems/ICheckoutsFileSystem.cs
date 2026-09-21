// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Marker interface for the checkout-tree scope: a working-directory root paired with the
/// application data directory. The checkouts filesystem is the read/write aggregate used by
/// assembler and codex commands that operate across many repository clones.
/// Extends <see cref="IAppDataFileSystem"/> because the checkouts scope always includes AppData.
/// </summary>
public interface ICheckoutsFileSystem : IAppDataFileSystem;
