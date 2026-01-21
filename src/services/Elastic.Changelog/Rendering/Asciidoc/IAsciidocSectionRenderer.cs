// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Interface for asciidoc section renderers that render changelog sections to a StringBuilder
/// </summary>
public interface IAsciidocSectionRenderer
{
	/// <summary>
	/// Renders the section content to the StringBuilder
	/// </summary>
	void Render(StringBuilder sb, List<ChangelogData> entries, ChangelogRenderContext context);
}
