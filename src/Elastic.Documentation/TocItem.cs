// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation;

/// <summary>A heading on a rendered page: its plain-text title, the slug of its anchor id, and its level.</summary>
public sealed record TocItem(string Heading, string Slug, int Level);
