// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.StyleGuide;

/// <summary>A page for the style guide's previews: the real layout around a body of placeholder content.</summary>
public sealed record PreviewPageModel(MarkdownLayoutViewModel Layout, string BodyHtml);
