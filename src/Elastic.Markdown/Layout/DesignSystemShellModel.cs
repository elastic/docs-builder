// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Layout;

/// <summary>
/// A documentation page inside the design-system shell: sidebar, article, and the right-hand rail. The shell is two partials,
/// start and end, because a layout writes its body in place, so the body has to be rendered between them.
/// </summary>
/// <param name="Layout">The page's layout model.</param>
/// <param name="Hub">Hub pages own the full content column and drop the rail, feedback and pager.</param>
public sealed record DesignSystemShellModel(MarkdownLayoutViewModel Layout, bool Hub);
