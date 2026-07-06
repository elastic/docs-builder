// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer;

/// <summary>
/// A page section heading. <paramref name="Route"/> adds the operation-page section navigation
/// buttons; <paramref name="ContentTypeBadge"/> adds a content-type badge next to the title.
/// </summary>
public record SectionHeader(string Title, string Anchor, string? Route = null, string? ContentTypeBadge = null);
