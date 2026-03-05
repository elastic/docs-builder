// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration;

/// <summary>
/// A parsed cross-link entry from docset.yml, with the target registry for lookup.
/// </summary>
/// <param name="Repository">Repository name (e.g. elasticsearch, docs-eng-team).</param>
/// <param name="Registry">Registry to use for lookup (public S3 or codex environment).</param>
public record CrossLinkEntry(string Repository, DocSetRegistry Registry);
