// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text.RegularExpressions;
using Elastic.Documentation.Svg;
using Elastic.Markdown.Diagnostics;
using Markdig.Parsers;

namespace Elastic.Markdown.Myst.Roles.Icons;

[DebuggerDisplay("{GetType().Name} Line: {Line}, Role: {Role}, Content: {Content}")]
public class IconsRole : RoleLeaf
{
	public IconsRole(string role, string content, InlineProcessor processor) : base(role, content)
	{
		if (EuiSvgIcons.TryGetIcon(content, out var svg))
		{
			Svg = svg;
			Name = content;
		}
		else
			processor.EmitError(this, Role.Length + content.Length, $"Unknown icon: {content}");
	}

	public string? Name { get; }
	public string? Svg { get; }
}

public partial class IconParser : RoleParser<IconsRole>
{
	[GeneratedRegex(@"\{icon\}`([^`]+)`", RegexOptions.Compiled)]
	public static partial Regex IconRegex();

	protected override IconsRole CreateRole(string role, string content, InlineProcessor parserContext) =>
		new(role, content, parserContext);

	protected override bool Matches(ReadOnlySpan<char> role) => role is "{icon}";
}
