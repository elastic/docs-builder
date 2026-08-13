// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation.FileSystems;

/// <summary>
/// Marker interface for the CI runner scope: the working directory root plus one or more
/// runner-provided paths (<c>RUNNER_TEMP</c>, artifact output dirs, metadata paths).
/// Services that operate across the CI workspace without a fixed docset anchor declare this
/// rather than bare <see cref="IFileSystem"/>.
/// </summary>
public interface IRunnerTempFileSystem : IFileSystem;
